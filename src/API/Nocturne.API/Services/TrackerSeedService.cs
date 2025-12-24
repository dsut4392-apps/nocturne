using Nocturne.Core.Models;
using Nocturne.Infrastructure.Data.Entities;
using Nocturne.Infrastructure.Data.Repositories;

namespace Nocturne.API.Services;

/// <summary>
/// Service to seed default tracker definitions for new users
/// </summary>
public interface ITrackerSeedService
{
    /// <summary>
    /// Create default tracker definitions for a new user
    /// </summary>
    Task SeedDefaultDefinitionsAsync(string userId, CancellationToken cancellationToken = default);
}

public class TrackerSeedService : ITrackerSeedService
{
    private readonly TrackerRepository _repository;
    private readonly ILogger<TrackerSeedService> _logger;

    public TrackerSeedService(TrackerRepository repository, ILogger<TrackerSeedService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task SeedDefaultDefinitionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Check if user already has definitions
        var existing = await _repository.GetDefinitionsForUserAsync(userId, cancellationToken);
        if (existing.Any())
        {
            _logger.LogDebug("User {UserId} already has tracker definitions, skipping seed", userId);
            return;
        }

        _logger.LogInformation("Seeding default tracker definitions for user {UserId}", userId);

        var defaults = GetDefaultDefinitions(userId);
        foreach (var definition in defaults)
        {
            await _repository.CreateDefinitionAsync(definition, cancellationToken);
        }

        _logger.LogInformation("Seeded {Count} default tracker definitions for user {UserId}", defaults.Length, userId);
    }

    private static TrackerDefinitionEntity[] GetDefaultDefinitions(string userId) =>
    [
        // CGM Sensors
        new TrackerDefinitionEntity
        {
            UserId = userId,
            Name = "Dexcom G7 Sensor",
            Description = "10-day CGM sensor",
            Category = TrackerCategory.Consumable,
            Icon = "activity",
            TriggerEventTypes = "[\"Sensor Start\",\"Sensor Change\"]",
            LifespanHours = 240, // 10 days
            NotificationThresholds =
            [
                new() { Urgency = NotificationUrgency.Info, Hours = 192, DisplayOrder = 1 },    // Day 8
                new() { Urgency = NotificationUrgency.Warn, Hours = 216, DisplayOrder = 2 },    // Day 9
                new() { Urgency = NotificationUrgency.Hazard, Hours = 234, DisplayOrder = 3 },  // Day 9.75
                new() { Urgency = NotificationUrgency.Urgent, Hours = 240, DisplayOrder = 4 },  // Day 10
            ],
            IsFavorite = true,
        },
        new TrackerDefinitionEntity
        {
            UserId = userId,
            Name = "Dexcom G6 Sensor",
            Description = "10-day CGM sensor",
            Category = TrackerCategory.Consumable,
            Icon = "activity",
            TriggerEventTypes = "[\"Sensor Start\",\"Sensor Change\"]",
            LifespanHours = 240,
            NotificationThresholds =
            [
                new() { Urgency = NotificationUrgency.Info, Hours = 192, DisplayOrder = 1 },
                new() { Urgency = NotificationUrgency.Warn, Hours = 216, DisplayOrder = 2 },
                new() { Urgency = NotificationUrgency.Hazard, Hours = 234, DisplayOrder = 3 },
                new() { Urgency = NotificationUrgency.Urgent, Hours = 240, DisplayOrder = 4 },
            ],
            IsFavorite = false,
        },
        new TrackerDefinitionEntity
        {
            UserId = userId,
            Name = "Libre 3 Sensor",
            Description = "14-day CGM sensor",
            Category = TrackerCategory.Consumable,
            Icon = "activity",
            TriggerEventTypes = "[\"Sensor Start\",\"Sensor Change\"]",
            LifespanHours = 336, // 14 days
            NotificationThresholds =
            [
                new() { Urgency = NotificationUrgency.Info, Hours = 288, DisplayOrder = 1 },    // Day 12
                new() { Urgency = NotificationUrgency.Warn, Hours = 312, DisplayOrder = 2 },    // Day 13
                new() { Urgency = NotificationUrgency.Hazard, Hours = 330, DisplayOrder = 3 },  // Day 13.75
                new() { Urgency = NotificationUrgency.Urgent, Hours = 336, DisplayOrder = 4 },  // Day 14
            ],
            IsFavorite = false,
        },

        // Pump Consumables
        new TrackerDefinitionEntity
        {
            UserId = userId,
            Name = "Omnipod 5",
            Description = "3-day insulin pod",
            Category = TrackerCategory.Consumable,
            Icon = "syringe",
            TriggerEventTypes = "[\"Site Change\",\"Pump Resume\"]",
            LifespanHours = 80, // Pod expires at 80h
            NotificationThresholds =
            [
                new() { Urgency = NotificationUrgency.Info, Hours = 60, DisplayOrder = 1 },    // 2.5 days
                new() { Urgency = NotificationUrgency.Warn, Hours = 72, DisplayOrder = 2 },    // 3 days
                new() { Urgency = NotificationUrgency.Hazard, Hours = 76, DisplayOrder = 3 },  // 3.17 days
                new() { Urgency = NotificationUrgency.Urgent, Hours = 79, DisplayOrder = 4 },  // Just before expiry
            ],
            IsFavorite = true,
        },
        new TrackerDefinitionEntity
        {
            UserId = userId,
            Name = "Tandem Infusion Set",
            Description = "3-day infusion set",
            Category = TrackerCategory.Consumable,
            Icon = "syringe",
            TriggerEventTypes = "[\"Site Change\",\"Cannula Change\"]",
            LifespanHours = 72,
            NotificationThresholds =
            [
                new() { Urgency = NotificationUrgency.Info, Hours = 48, DisplayOrder = 1 },
                new() { Urgency = NotificationUrgency.Warn, Hours = 66, DisplayOrder = 2 },
                new() { Urgency = NotificationUrgency.Hazard, Hours = 70, DisplayOrder = 3 },
                new() { Urgency = NotificationUrgency.Urgent, Hours = 72, DisplayOrder = 4 },
            ],
            IsFavorite = false,
        },
        new TrackerDefinitionEntity
        {
            UserId = userId,
            Name = "Insulin Reservoir",
            Description = "3-day reservoir",
            Category = TrackerCategory.Reservoir,
            Icon = "beaker",
            TriggerEventTypes = "[\"Insulin Change\",\"Reservoir Change\"]",
            LifespanHours = 72,
            NotificationThresholds =
            [
                new() { Urgency = NotificationUrgency.Info, Hours = 48, DisplayOrder = 1 },
                new() { Urgency = NotificationUrgency.Warn, Hours = 66, DisplayOrder = 2 },
                new() { Urgency = NotificationUrgency.Hazard, Hours = 70, DisplayOrder = 3 },
                new() { Urgency = NotificationUrgency.Urgent, Hours = 72, DisplayOrder = 4 },
            ],
            IsFavorite = false,
        },

        // Appointments
        new TrackerDefinitionEntity
        {
            UserId = userId,
            Name = "Endocrinologist Visit",
            Description = "Quarterly endo appointment",
            Category = TrackerCategory.Appointment,
            Icon = "calendar",
            TriggerEventTypes = "[]",
            LifespanHours = 2160, // 90 days
            NotificationThresholds =
            [
                new() { Urgency = NotificationUrgency.Info, Hours = 2088, DisplayOrder = 1 },  // 87 days (1 week notice)
                new() { Urgency = NotificationUrgency.Warn, Hours = 2136, DisplayOrder = 2 },  // 89 days (1 day notice)
            ],
            IsFavorite = false,
        },

        // Reminders
        new TrackerDefinitionEntity
        {
            UserId = userId,
            Name = "A1C Lab Work",
            Description = "Quarterly A1C test",
            Category = TrackerCategory.Reminder,
            Icon = "clock",
            TriggerEventTypes = "[]",
            LifespanHours = 2160, // 90 days
            NotificationThresholds =
            [
                new() { Urgency = NotificationUrgency.Info, Hours = 2088, DisplayOrder = 1 },
                new() { Urgency = NotificationUrgency.Warn, Hours = 2136, DisplayOrder = 2 },
            ],
            IsFavorite = false,
        },
    ];
}
