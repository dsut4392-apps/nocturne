using Microsoft.AspNetCore.Mvc;
using Nocturne.API.Services;
using Nocturne.Core.Contracts;
using Nocturne.Core.Models;

namespace Nocturne.API.Controllers.V4;

/// <summary>
/// Controller for providing chart data with server-side calculations
/// Provides pre-computed IOB, COB, and basal time series for dashboard charts
/// </summary>
[ApiController]
[Route("api/v4/[controller]")]
[Produces("application/json")]
[Tags("V4 Chart Data")]
public class ChartDataController : ControllerBase
{
    private readonly IIobService _iobService;
    private readonly ICobService _cobService;
    private readonly ITreatmentService _treatmentService;
    private readonly IDeviceStatusService _deviceStatusService;
    private readonly IProfileService _profileService;
    private readonly ILogger<ChartDataController> _logger;

    public ChartDataController(
        IIobService iobService,
        ICobService cobService,
        ITreatmentService treatmentService,
        IDeviceStatusService deviceStatusService,
        IProfileService profileService,
        ILogger<ChartDataController> logger
    )
    {
        _iobService = iobService;
        _cobService = cobService;
        _treatmentService = treatmentService;
        _deviceStatusService = deviceStatusService;
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard chart data with pre-calculated IOB, COB, and basal time series
    /// </summary>
    /// <param name="startTime">Start time in Unix milliseconds</param>
    /// <param name="endTime">End time in Unix milliseconds</param>
    /// <param name="intervalMinutes">Interval for IOB/COB calculations (default: 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chart data with all calculated series</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardChartData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardChartData>> GetDashboardChartData(
        [FromQuery] long startTime,
        [FromQuery] long endTime,
        [FromQuery] int intervalMinutes = 5,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (endTime <= startTime)
            {
                return BadRequest(new { error = "endTime must be greater than startTime" });
            }

            if (intervalMinutes < 1 || intervalMinutes > 60)
            {
                return BadRequest(new { error = "intervalMinutes must be between 1 and 60" });
            }

            // Fetch treatments for the period (with buffer for IOB/COB calculation)
            var bufferMs = 8 * 60 * 60 * 1000; // 8 hours buffer for IOB
            var treatments = await _treatmentService.GetTreatmentsAsync(
                count: 5000,
                skip: 0,
                cancellationToken: cancellationToken
            );

            // Filter treatments to relevant time range (with buffer)
            var relevantTreatments = treatments?
                .Where(t => t.Mills >= (startTime - bufferMs) && t.Mills <= endTime)
                .ToList() ?? new List<Treatment>();

            // Fetch device status for the period
            var deviceStatus = await _deviceStatusService.GetDeviceStatusAsync(
                count: 1000,
                skip: 0,
                cancellationToken: cancellationToken
            );

            var deviceStatusList = deviceStatus?.ToList() ?? new List<DeviceStatus>();

            // Get default basal rate from profile
            var defaultBasalRate = _profileService.HasData()
                ? _profileService.GetBasalRate(endTime, null)
                : 1.0;

            // Calculate IOB and COB series at each interval
            var iobSeries = new List<TimeSeriesPoint>();
            var cobSeries = new List<TimeSeriesPoint>();
            var intervalMs = intervalMinutes * 60 * 1000;

            double maxIob = 0;
            double maxCob = 0;

            for (long t = startTime; t <= endTime; t += intervalMs)
            {
                // Calculate IOB at this time
                var iobResult = _iobService.FromTreatments(
                    relevantTreatments,
                    _profileService.HasData() ? _profileService : null,
                    t,
                    null
                );

                var iob = iobResult.Iob;
                iobSeries.Add(new TimeSeriesPoint { Timestamp = t, Value = iob });
                if (iob > maxIob) maxIob = iob;

                // Calculate COB at this time
                var cobResult = _cobService.CobTotal(
                    relevantTreatments,
                    deviceStatusList,
                    _profileService.HasData() ? _profileService : null,
                    t,
                    null
                );

                var cob = cobResult.Cob;
                cobSeries.Add(new TimeSeriesPoint { Timestamp = t, Value = cob });
                if (cob > maxCob) maxCob = cob;
            }

            // Build basal series from temp basal treatments
            var basalSeries = BuildBasalSeries(
                relevantTreatments,
                startTime,
                endTime,
                defaultBasalRate
            );

            var maxBasalRate = Math.Max(
                defaultBasalRate * 2.5,
                basalSeries.Any() ? basalSeries.Max(b => b.Rate) : defaultBasalRate
            );

            return Ok(new DashboardChartData
            {
                IobSeries = iobSeries,
                CobSeries = cobSeries,
                BasalSeries = basalSeries,
                DefaultBasalRate = defaultBasalRate,
                MaxBasalRate = maxBasalRate,
                MaxIob = Math.Max(3, maxIob),
                MaxCob = Math.Max(30, maxCob),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dashboard chart data");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Build basal series from temp basal treatments
    /// </summary>
    private List<BasalPoint> BuildBasalSeries(
        List<Treatment> treatments,
        long startTime,
        long endTime,
        double defaultBasalRate
    )
    {
        var series = new List<BasalPoint>();

        // Start with default rate
        series.Add(new BasalPoint
        {
            Timestamp = startTime,
            Rate = defaultBasalRate,
            IsTemp = false
        });

        // Get temp basal treatments sorted by time
        var tempBasals = treatments
            .Where(t =>
                t.EventType == "Temp Basal" &&
                (t.Rate != null || t.Absolute != null) &&
                t.Mills >= startTime &&
                t.Mills <= endTime
            )
            .OrderBy(t => t.Mills)
            .ToList();

        foreach (var t in tempBasals)
        {
            var treatmentStart = t.Mills;
            var duration = (long)((t.Duration ?? 30) * 60 * 1000); // Duration in ms
            var treatmentEnd = treatmentStart + duration;
            var rate = t.Rate ?? t.Absolute ?? defaultBasalRate;

            // Add point just before temp basal starts (at default rate)
            series.Add(new BasalPoint
            {
                Timestamp = treatmentStart - 1,
                Rate = defaultBasalRate,
                IsTemp = false
            });

            // Add temp basal start
            series.Add(new BasalPoint
            {
                Timestamp = treatmentStart,
                Rate = rate,
                IsTemp = true
            });

            // Add temp basal end
            if (treatmentEnd <= endTime)
            {
                series.Add(new BasalPoint
                {
                    Timestamp = treatmentEnd,
                    Rate = rate,
                    IsTemp = true
                });

                // Return to default rate
                series.Add(new BasalPoint
                {
                    Timestamp = treatmentEnd + 1,
                    Rate = defaultBasalRate,
                    IsTemp = false
                });
            }
        }

        // End with default rate
        series.Add(new BasalPoint
        {
            Timestamp = endTime,
            Rate = defaultBasalRate,
            IsTemp = false
        });

        // Sort and deduplicate
        return series
            .OrderBy(p => p.Timestamp)
            .ToList();
    }
}

/// <summary>
/// Dashboard chart data response with all calculated series
/// </summary>
public class DashboardChartData
{
    /// <summary>
    /// IOB (Insulin on Board) time series
    /// </summary>
    public List<TimeSeriesPoint> IobSeries { get; set; } = new();

    /// <summary>
    /// COB (Carbs on Board) time series
    /// </summary>
    public List<TimeSeriesPoint> CobSeries { get; set; } = new();

    /// <summary>
    /// Basal rate time series with temp basal indicators
    /// </summary>
    public List<BasalPoint> BasalSeries { get; set; } = new();

    /// <summary>
    /// Default basal rate from profile (U/hr)
    /// </summary>
    public double DefaultBasalRate { get; set; }

    /// <summary>
    /// Maximum basal rate in the series (for Y-axis scaling)
    /// </summary>
    public double MaxBasalRate { get; set; }

    /// <summary>
    /// Maximum IOB in the series (for Y-axis scaling)
    /// </summary>
    public double MaxIob { get; set; }

    /// <summary>
    /// Maximum COB in the series (for Y-axis scaling)
    /// </summary>
    public double MaxCob { get; set; }
}

/// <summary>
/// Time series data point with timestamp and value
/// </summary>
public class TimeSeriesPoint
{
    /// <summary>
    /// Timestamp in Unix milliseconds
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Value at this timestamp
    /// </summary>
    public double Value { get; set; }
}

/// <summary>
/// Basal rate data point
/// </summary>
public class BasalPoint
{
    /// <summary>
    /// Timestamp in Unix milliseconds
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Basal rate in U/hr
    /// </summary>
    public double Rate { get; set; }

    /// <summary>
    /// Whether this is a temporary basal rate
    /// </summary>
    public bool IsTemp { get; set; }
}
