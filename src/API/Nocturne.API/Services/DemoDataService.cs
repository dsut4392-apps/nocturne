using Nocturne.Core.Contracts;
using Nocturne.Core.Models;

namespace Nocturne.API.Services;

public class DemoModeConfiguration
{
    public bool Enabled { get; set; } = false;
    public bool FilterNonDemoData { get; set; } = true; // When true, only show demo data when demo mode is enabled
    public int IntervalMinutes { get; set; } = 5;
    public int InitialGlucose { get; set; } = 120;
    public int WalkVariance { get; set; } = 10;
    public int MinGlucose { get; set; } = 70;
    public int MaxGlucose { get; set; } = 250;
    public string Device { get; set; } = "demo-cgm";
    public int HistoryMonths { get; set; } = 3;
    public double BasalRate { get; set; } = 1.0;
    public double CarbRatio { get; set; } = 10.0;
    public double CorrectionFactor { get; set; } = 50.0;
}

public interface IDemoDataService
{
    Task<Entry> GenerateEntryAsync(CancellationToken cancellationToken = default);
    bool IsEnabled { get; }
    bool ShouldFilterNonDemoData { get; }
    DemoModeConfiguration GetConfiguration();
    Task<DemoDataGenerationResult> PrepopulateHistoricalDataAsync(
        CancellationToken cancellationToken = default
    );
    Task<DemoDataCleanupResult> CleanupDemoDataAsync(CancellationToken cancellationToken = default);
    (List<Entry> Entries, List<Treatment> Treatments) GenerateHistoricalData();
}

public class DemoDataGenerationResult
{
    public int EntriesGenerated { get; set; }
    public int TreatmentsGenerated { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan Duration { get; set; }
}

public class DemoDataCleanupResult
{
    public long EntriesDeleted { get; set; }
    public long TreatmentsDeleted { get; set; }
}

public class DemoDataService : IDemoDataService
{
    private readonly ILogger<DemoDataService> _logger;
    private readonly DemoModeConfiguration _config;
    private readonly Random _random = new();
    private double _currentGlucose;
    private readonly object _lock = new();

    // Scenarios for realistic glucose patterns
    private enum DayScenario
    {
        Normal,
        HighDay,
        LowDay,
        Exercise,
        SickDay,
        StressDay,
        PoorSleep,
    }

    public DemoDataService(IConfiguration configuration, ILogger<DemoDataService> logger)
    {
        _logger = logger;
        _config =
            configuration.GetSection("DemoMode").Get<DemoModeConfiguration>()
            ?? new DemoModeConfiguration();
        _currentGlucose = _config.InitialGlucose;
    }

    public bool IsEnabled => _config.Enabled;

    public bool ShouldFilterNonDemoData => IsEnabled && _config.FilterNonDemoData;

    public DemoModeConfiguration GetConfiguration() => _config;

    public Task<Entry> GenerateEntryAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            throw new InvalidOperationException("Demo mode is not enabled");
        }

        lock (_lock)
        {
            // Generate glucose change using drunken walk algorithm
            var change = GenerateRandomWalk();
            _currentGlucose = Math.Max(
                _config.MinGlucose,
                Math.Min(_config.MaxGlucose, _currentGlucose + change)
            );

            var now = DateTime.UtcNow;
            var mills = new DateTimeOffset(now).ToUnixTimeMilliseconds();
            var direction = CalculateDirection(change);

            var entry = new Entry
            {
                Type = "sgv",
                Device = _config.Device,
                Mills = mills,
                Date = now,
                DateString = now.ToString("o"),
                Mgdl = Math.Round(_currentGlucose, 0),
                Sgv = Math.Round(_currentGlucose, 0),
                Direction = direction.ToString(),
                Delta = Math.Round(change, 1),
                IsDemo = true,
                Filtered = Math.Round(_currentGlucose + (_random.NextDouble() - 0.5) * 2, 0),
                Unfiltered = Math.Round(_currentGlucose + (_random.NextDouble() - 0.5) * 5, 0),
                Rssi = _random.Next(0, 101),
                Noise = _random.Next(0, 5),
                CreatedAt = now.ToString("o"),
                ModifiedAt = now,
            };

            _logger.LogDebug(
                "Generated demo entry: SGV={Sgv}, Direction={Direction}, Change={Change}",
                entry.Sgv,
                entry.Direction,
                change
            );

            return Task.FromResult(entry);
        }
    }

    public async Task<DemoDataGenerationResult> PrepopulateHistoricalDataAsync(
        CancellationToken cancellationToken = default
    )
    {
        var startTime = DateTime.UtcNow;
        var result = new DemoDataGenerationResult();

        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddMonths(-_config.HistoryMonths);

        result.StartDate = startDate;
        result.EndDate = endDate;

        var entries = new List<Entry>();
        var treatments = new List<Treatment>();

        _logger.LogInformation(
            "Starting demo data prepopulation from {StartDate} to {EndDate}",
            startDate,
            endDate
        );

        // Generate data day by day
        var currentDay = startDate.Date;
        while (currentDay <= endDate.Date)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dayScenario = SelectDayScenario(currentDay);
            var (dayEntries, dayTreatments) = GenerateDayData(currentDay, dayScenario);

            entries.AddRange(dayEntries);
            treatments.AddRange(dayTreatments);

            currentDay = currentDay.AddDays(1);
        }

        result.EntriesGenerated = entries.Count;
        result.TreatmentsGenerated = treatments.Count;
        result.Duration = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            "Generated {Entries} entries and {Treatments} treatments in {Duration}",
            result.EntriesGenerated,
            result.TreatmentsGenerated,
            result.Duration
        );

        return await Task.FromResult(result);
    }

    public (List<Entry> Entries, List<Treatment> Treatments) GenerateHistoricalData()
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddMonths(-_config.HistoryMonths);

        var entries = new List<Entry>();
        var treatments = new List<Treatment>();

        // Generate data day by day
        var currentDay = startDate.Date;
        while (currentDay <= endDate.Date)
        {
            var dayScenario = SelectDayScenario(currentDay);
            var (dayEntries, dayTreatments) = GenerateDayData(currentDay, dayScenario);

            entries.AddRange(dayEntries);
            treatments.AddRange(dayTreatments);

            currentDay = currentDay.AddDays(1);
        }

        return (entries, treatments);
    }

    public async Task<DemoDataCleanupResult> CleanupDemoDataAsync(
        CancellationToken cancellationToken = default
    )
    {
        // This method prepares the cleanup result - actual deletion happens in the background service
        // since we don't have direct repository access here
        return await Task.FromResult(
            new DemoDataCleanupResult { EntriesDeleted = 0, TreatmentsDeleted = 0 }
        );
    }

    private DayScenario SelectDayScenario(DateTime date)
    {
        // Weight the scenarios for realistic distribution
        // ~60% normal days, 10% each for problematic days
        var roll = _random.Next(100);

        // Add some weekly patterns - weekends more likely to have unusual days
        var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

        if (isWeekend)
        {
            return roll switch
            {
                < 40 => DayScenario.Normal,
                < 55 => DayScenario.HighDay, // More likely on weekends (eating out, sleeping in)
                < 65 => DayScenario.Exercise,
                < 75 => DayScenario.PoorSleep,
                < 85 => DayScenario.LowDay,
                < 95 => DayScenario.StressDay,
                _ => DayScenario.SickDay,
            };
        }

        return roll switch
        {
            < 60 => DayScenario.Normal,
            < 70 => DayScenario.HighDay,
            < 78 => DayScenario.LowDay,
            < 86 => DayScenario.Exercise,
            < 92 => DayScenario.StressDay,
            < 97 => DayScenario.PoorSleep,
            _ => DayScenario.SickDay,
        };
    }

    private (List<Entry> Entries, List<Treatment> Treatments) GenerateDayData(
        DateTime date,
        DayScenario scenario
    )
    {
        var entries = new List<Entry>();
        var treatments = new List<Treatment>();

        // Get scenario-specific parameters
        var scenarioParams = GetScenarioParameters(scenario);

        // Start with fasting glucose based on scenario
        var glucose = scenarioParams.FastingGlucose + (_random.NextDouble() - 0.5) * 20;

        // Generate 5-minute interval entries (288 per day)
        var currentTime = date;
        var endTime = date.AddDays(1);

        // Pre-plan meals and activities for the day
        var mealPlan = GenerateMealPlan(date, scenario);
        var basalAdjustments = GenerateBasalAdjustments(date, scenario);

        // Track meal effects
        var mealEffects =
            new List<(DateTime Time, double CarbsRemaining, double InsulinRemaining)>();

        while (currentTime < endTime)
        {
            // Check for meals at this time
            var meal = mealPlan.FirstOrDefault(m =>
                Math.Abs((m.Time - currentTime).TotalMinutes) < 2.5
            );
            if (meal.Carbs > 0)
            {
                // Generate meal bolus treatment
                var bolus = CalculateMealBolus(meal.Carbs, glucose, scenarioParams);
                treatments.Add(CreateMealTreatment(currentTime, meal.Carbs, bolus, meal.FoodType));

                // Add meal effect for glucose simulation
                mealEffects.Add((currentTime, meal.Carbs, bolus));
            }

            // Check for basal adjustments
            var basalAdj = basalAdjustments.FirstOrDefault(b =>
                Math.Abs((b.Time - currentTime).TotalMinutes) < 2.5
            );
            if (basalAdj.Rate > 0 || basalAdj.Duration > 0)
            {
                treatments.Add(
                    CreateTempBasalTreatment(currentTime, basalAdj.Rate, basalAdj.Duration)
                );
            }

            // Apply meal and insulin effects
            glucose = SimulateGlucose(glucose, currentTime, mealEffects, scenarioParams, scenario);

            // Clamp glucose to realistic range
            glucose = Math.Max(40, Math.Min(400, glucose));

            // Handle low glucose - generate correction treatment
            if (glucose < 70 && _random.NextDouble() < 0.7)
            {
                var correctionCarbs = _random.Next(10, 20);
                treatments.Add(CreateCarbCorrectionTreatment(currentTime, correctionCarbs));
                // Boost glucose from carb correction
                glucose += correctionCarbs * 3; // ~3 mg/dL per gram of fast carbs
            }

            // Handle high glucose - generate correction bolus
            if (glucose > 200 && _random.NextDouble() < 0.5)
            {
                var correctionBolus = Math.Round(
                    (glucose - 120) / scenarioParams.CorrectionFactor,
                    1
                );
                if (correctionBolus >= 0.5)
                {
                    treatments.Add(CreateCorrectionBolusTreatment(currentTime, correctionBolus));
                    mealEffects.Add((currentTime, 0, correctionBolus));
                }
            }

            // Create entry
            var previousGlucose = entries.Count > 0 ? entries[^1].Sgv ?? entries[^1].Mgdl : glucose;
            var delta = glucose - previousGlucose;
            entries.Add(CreateEntry(currentTime, glucose, delta));

            currentTime = currentTime.AddMinutes(5);

            // Clean up old meal effects
            mealEffects.RemoveAll(m => (currentTime - m.Time).TotalHours > 6);
        }

        // Generate scheduled basal entries for the day
        treatments.AddRange(GenerateScheduledBasal(date, scenarioParams));

        return (entries, treatments);
    }

    private ScenarioParameters GetScenarioParameters(DayScenario scenario)
    {
        return scenario switch
        {
            DayScenario.Normal => new ScenarioParameters
            {
                FastingGlucose = 100 + _random.Next(-15, 20),
                CarbRatio = _config.CarbRatio,
                CorrectionFactor = _config.CorrectionFactor,
                BasalMultiplier = 1.0,
                InsulinSensitivityMultiplier = 1.0,
                DawnPhenomenonStrength = 0.3,
            },
            DayScenario.HighDay => new ScenarioParameters
            {
                FastingGlucose = 140 + _random.Next(0, 40),
                CarbRatio = _config.CarbRatio * 0.8, // Need more insulin
                CorrectionFactor = _config.CorrectionFactor * 0.8,
                BasalMultiplier = 1.2,
                InsulinSensitivityMultiplier = 0.7,
                DawnPhenomenonStrength = 0.5,
            },
            DayScenario.LowDay => new ScenarioParameters
            {
                FastingGlucose = 80 + _random.Next(-10, 15),
                CarbRatio = _config.CarbRatio * 1.3, // Need less insulin
                CorrectionFactor = _config.CorrectionFactor * 1.3,
                BasalMultiplier = 0.7,
                InsulinSensitivityMultiplier = 1.4,
                DawnPhenomenonStrength = 0.1,
            },
            DayScenario.Exercise => new ScenarioParameters
            {
                FastingGlucose = 95 + _random.Next(-10, 15),
                CarbRatio = _config.CarbRatio * 1.2,
                CorrectionFactor = _config.CorrectionFactor * 1.3,
                BasalMultiplier = 0.6,
                InsulinSensitivityMultiplier = 1.5,
                DawnPhenomenonStrength = 0.2,
                HasExercise = true,
            },
            DayScenario.SickDay => new ScenarioParameters
            {
                FastingGlucose = 160 + _random.Next(0, 60),
                CarbRatio = _config.CarbRatio * 0.6,
                CorrectionFactor = _config.CorrectionFactor * 0.6,
                BasalMultiplier = 1.5,
                InsulinSensitivityMultiplier = 0.5,
                DawnPhenomenonStrength = 0.6,
            },
            DayScenario.StressDay => new ScenarioParameters
            {
                FastingGlucose = 120 + _random.Next(0, 30),
                CarbRatio = _config.CarbRatio * 0.85,
                CorrectionFactor = _config.CorrectionFactor * 0.85,
                BasalMultiplier = 1.15,
                InsulinSensitivityMultiplier = 0.8,
                DawnPhenomenonStrength = 0.4,
            },
            DayScenario.PoorSleep => new ScenarioParameters
            {
                FastingGlucose = 130 + _random.Next(-10, 30),
                CarbRatio = _config.CarbRatio * 0.9,
                CorrectionFactor = _config.CorrectionFactor * 0.9,
                BasalMultiplier = 1.1,
                InsulinSensitivityMultiplier = 0.85,
                DawnPhenomenonStrength = 0.5,
            },
            _ => new ScenarioParameters
            {
                FastingGlucose = 100,
                CarbRatio = _config.CarbRatio,
                CorrectionFactor = _config.CorrectionFactor,
                BasalMultiplier = 1.0,
                InsulinSensitivityMultiplier = 1.0,
                DawnPhenomenonStrength = 0.3,
            },
        };
    }

    private List<(DateTime Time, double Carbs, string FoodType)> GenerateMealPlan(
        DateTime date,
        DayScenario scenario
    )
    {
        var meals = new List<(DateTime Time, double Carbs, string FoodType)>();

        // Breakfast (6-9 AM)
        var breakfastHour = 6 + _random.Next(0, 4);
        var breakfastMinute = _random.Next(0, 12) * 5;
        var breakfastCarbs =
            scenario == DayScenario.LowDay ? _random.Next(20, 40)
            : scenario == DayScenario.HighDay ? _random.Next(50, 80)
            : _random.Next(30, 60);
        meals.Add(
            (date.AddHours(breakfastHour).AddMinutes(breakfastMinute), breakfastCarbs, "Breakfast")
        );

        // Lunch (11 AM - 1 PM)
        var lunchHour = 11 + _random.Next(0, 3);
        var lunchMinute = _random.Next(0, 12) * 5;
        var lunchCarbs =
            scenario == DayScenario.LowDay ? _random.Next(30, 50)
            : scenario == DayScenario.HighDay ? _random.Next(60, 100)
            : _random.Next(40, 70);
        meals.Add((date.AddHours(lunchHour).AddMinutes(lunchMinute), lunchCarbs, "Lunch"));

        // Dinner (5-8 PM)
        var dinnerHour = 17 + _random.Next(0, 4);
        var dinnerMinute = _random.Next(0, 12) * 5;
        var dinnerCarbs =
            scenario == DayScenario.LowDay ? _random.Next(35, 55)
            : scenario == DayScenario.HighDay ? _random.Next(70, 120)
            : _random.Next(50, 90);
        meals.Add((date.AddHours(dinnerHour).AddMinutes(dinnerMinute), dinnerCarbs, "Dinner"));

        // Snacks (random, 50% chance for each)
        if (_random.NextDouble() < 0.5)
        {
            var snack1Hour = 10 + _random.NextDouble(); // Mid-morning
            meals.Add((date.AddHours(snack1Hour), _random.Next(10, 25), "Snack"));
        }

        if (_random.NextDouble() < 0.5)
        {
            var snack2Hour = 15 + _random.NextDouble(); // Afternoon
            meals.Add((date.AddHours(snack2Hour), _random.Next(10, 25), "Snack"));
        }

        if (_random.NextDouble() < 0.3)
        {
            var snack3Hour = 21 + _random.NextDouble(); // Evening snack
            meals.Add((date.AddHours(snack3Hour), _random.Next(10, 20), "Snack"));
        }

        return meals;
    }

    private List<(DateTime Time, double Rate, int Duration)> GenerateBasalAdjustments(
        DateTime date,
        DayScenario scenario
    )
    {
        var adjustments = new List<(DateTime Time, double Rate, int Duration)>();

        if (scenario == DayScenario.Exercise)
        {
            // Reduce basal before/during exercise
            var exerciseHour = _random.Next(16, 20); // Afternoon/evening exercise
            adjustments.Add((date.AddHours(exerciseHour - 1), _config.BasalRate * 0.5, 120)); // -50% for 2 hours
        }

        if (scenario == DayScenario.LowDay && _random.NextDouble() < 0.5)
        {
            // Reduce basal when running low
            var lowHour = _random.Next(10, 16);
            adjustments.Add((date.AddHours(lowHour), _config.BasalRate * 0.6, 60));
        }

        if (scenario == DayScenario.HighDay && _random.NextDouble() < 0.5)
        {
            // Increase basal when running high
            var highHour = _random.Next(10, 18);
            adjustments.Add((date.AddHours(highHour), _config.BasalRate * 1.3, 120));
        }

        return adjustments;
    }

    private List<Treatment> GenerateScheduledBasal(DateTime date, ScenarioParameters @params)
    {
        var basalTreatments = new List<Treatment>();

        // Generate hourly basal segments with circadian rhythm
        for (var hour = 0; hour < 24; hour++)
        {
            var baseRate = _config.BasalRate * @params.BasalMultiplier;

            // Apply circadian rhythm - higher in early morning (dawn phenomenon)
            var circadianMultiplier = hour switch
            {
                >= 3 and < 8 => 1.0
                    + (@params.DawnPhenomenonStrength * (1 - Math.Abs(hour - 5.5) / 2.5)),
                >= 12 and < 14 => 1.1, // Slight increase around lunch
                >= 22 or < 3 => 0.9, // Slightly lower at night
                _ => 1.0,
            };

            var rate = Math.Round(baseRate * circadianMultiplier, 2);
            var time = date.AddHours(hour);
            var mills = new DateTimeOffset(time).ToUnixTimeMilliseconds();

            basalTreatments.Add(
                new Treatment
                {
                    EventType = "Temp Basal",
                    Rate = rate,
                    Duration = 60,
                    Mills = mills,
                    Created_at = time.ToString("o"),
                    EnteredBy = "demo-pump",
                    IsDemo = true,
                }
            );
        }

        return basalTreatments;
    }

    private double SimulateGlucose(
        double currentGlucose,
        DateTime time,
        List<(DateTime Time, double CarbsRemaining, double InsulinRemaining)> mealEffects,
        ScenarioParameters @params,
        DayScenario scenario
    )
    {
        var glucose = currentGlucose;

        // Base random walk with scenario-adjusted variance
        var baseVariance = scenario switch
        {
            DayScenario.SickDay => 15,
            DayScenario.HighDay => 12,
            DayScenario.LowDay => 8,
            DayScenario.StressDay => 10,
            _ => 8,
        };

        glucose += GenerateRandomWalk(baseVariance);

        // Apply dawn phenomenon (3-8 AM)
        var hour = time.Hour + time.Minute / 60.0;
        if (hour >= 3 && hour < 8)
        {
            var dawnEffect =
                @params.DawnPhenomenonStrength * 3 * Math.Sin((hour - 3) * Math.PI / 5);
            glucose += dawnEffect;
        }

        // Apply meal effects (carb absorption)
        foreach (var effect in mealEffects.ToList())
        {
            var minutesSinceMeal = (time - effect.Time).TotalMinutes;
            if (minutesSinceMeal > 0 && minutesSinceMeal < 240) // 4-hour absorption window
            {
                // Simple carb absorption curve (peaks around 45-60 minutes)
                var carbAbsorptionRate =
                    Math.Exp(-Math.Pow(minutesSinceMeal - 50, 2) / 2000) * 0.15;
                glucose += effect.CarbsRemaining * carbAbsorptionRate;

                // Insulin effect (peaks around 60-90 minutes, lasts longer)
                if (effect.InsulinRemaining > 0 && minutesSinceMeal > 15)
                {
                    var insulinEffectRate =
                        Math.Exp(-Math.Pow(minutesSinceMeal - 75, 2) / 3000) * 0.2;
                    glucose -=
                        effect.InsulinRemaining
                        * @params.InsulinSensitivityMultiplier
                        * insulinEffectRate
                        * @params.CorrectionFactor
                        / 10;
                }
            }
        }

        // Exercise effect (if applicable)
        if (@params.HasExercise)
        {
            if (hour >= 16 && hour < 19) // During exercise window
            {
                glucose -= 2; // Glucose drops during exercise
            }
            else if (hour >= 19 && hour < 24) // Post-exercise insulin sensitivity
            {
                // Increased random variability post-exercise
                glucose += (_random.NextDouble() - 0.6) * 5;
            }
        }

        return glucose;
    }

    private double CalculateMealBolus(
        double carbs,
        double currentGlucose,
        ScenarioParameters @params
    )
    {
        var carbBolus = carbs / @params.CarbRatio;

        // Correction if running high
        var correctionBolus = 0.0;
        if (currentGlucose > 120)
        {
            correctionBolus = (currentGlucose - 120) / @params.CorrectionFactor;
        }

        // Add some natural variation in dosing (people aren't perfect)
        var variation = 1.0 + (_random.NextDouble() - 0.5) * 0.2; // ±10% variation

        return Math.Round((carbBolus + correctionBolus) * variation, 1);
    }

    private Entry CreateEntry(DateTime time, double glucose, double? delta)
    {
        var mills = new DateTimeOffset(time).ToUnixTimeMilliseconds();
        var direction = CalculateDirection(delta ?? 0);

        return new Entry
        {
            Type = "sgv",
            Device = _config.Device,
            Mills = mills,
            Date = time,
            DateString = time.ToString("o"),
            Mgdl = Math.Round(glucose, 0),
            Sgv = Math.Round(glucose, 0),
            Direction = direction.ToString(),
            Delta = delta.HasValue ? Math.Round(delta.Value, 1) : null,
            IsDemo = true,
            Filtered = Math.Round(glucose + (_random.NextDouble() - 0.5) * 2, 0),
            Unfiltered = Math.Round(glucose + (_random.NextDouble() - 0.5) * 5, 0),
            Rssi = _random.Next(0, 101),
            Noise = _random.Next(0, 3),
            CreatedAt = time.ToString("o"),
            ModifiedAt = time,
        };
    }

    private Treatment CreateMealTreatment(
        DateTime time,
        double carbs,
        double insulin,
        string foodType
    )
    {
        var mills = new DateTimeOffset(time).ToUnixTimeMilliseconds();

        return new Treatment
        {
            EventType = "Meal Bolus",
            Carbs = carbs,
            Insulin = insulin,
            FoodType = foodType,
            Mills = mills,
            Created_at = time.ToString("o"),
            EnteredBy = "demo-user",
            IsDemo = true,
        };
    }

    private Treatment CreateCorrectionBolusTreatment(DateTime time, double insulin)
    {
        var mills = new DateTimeOffset(time).ToUnixTimeMilliseconds();

        return new Treatment
        {
            EventType = "Correction Bolus",
            Insulin = insulin,
            Mills = mills,
            Created_at = time.ToString("o"),
            EnteredBy = "demo-user",
            IsDemo = true,
        };
    }

    private Treatment CreateCarbCorrectionTreatment(DateTime time, double carbs)
    {
        var mills = new DateTimeOffset(time).ToUnixTimeMilliseconds();

        return new Treatment
        {
            EventType = "Carb Correction",
            Carbs = carbs,
            Mills = mills,
            Created_at = time.ToString("o"),
            EnteredBy = "demo-user",
            Notes = "Low treatment",
            IsDemo = true,
        };
    }

    private Treatment CreateTempBasalTreatment(DateTime time, double rate, int duration)
    {
        var mills = new DateTimeOffset(time).ToUnixTimeMilliseconds();

        return new Treatment
        {
            EventType = "Temp Basal",
            Rate = rate,
            Duration = duration,
            Mills = mills,
            Created_at = time.ToString("o"),
            EnteredBy = "demo-pump",
            IsDemo = true,
        };
    }

    private double GenerateRandomWalk(double variance = 0)
    {
        var v = variance > 0 ? variance : _config.WalkVariance;
        // Box-Muller transform for normal distribution
        var u1 = _random.NextDouble();
        var u2 = _random.NextDouble();
        var z0 = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);

        // Scale by variance
        return z0 * v;
    }

    private Direction CalculateDirection(double change)
    {
        // Convert glucose change to direction enum
        return change switch
        {
            > 10 => Direction.DoubleUp,
            > 5 => Direction.SingleUp,
            > 2 => Direction.FortyFiveUp,
            > -2 => Direction.Flat,
            > -5 => Direction.FortyFiveDown,
            > -10 => Direction.SingleDown,
            _ => Direction.DoubleDown,
        };
    }

    private class ScenarioParameters
    {
        public double FastingGlucose { get; set; }
        public double CarbRatio { get; set; }
        public double CorrectionFactor { get; set; }
        public double BasalMultiplier { get; set; }
        public double InsulinSensitivityMultiplier { get; set; }
        public double DawnPhenomenonStrength { get; set; }
        public bool HasExercise { get; set; }
    }
}
