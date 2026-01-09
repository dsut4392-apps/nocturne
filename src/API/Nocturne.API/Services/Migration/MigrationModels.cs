namespace Nocturne.API.Services.Migration;

/// <summary>
/// Enumerates the modes for data migration
/// </summary>
public enum MigrationMode
{
    /// <summary>
    /// Migrate data via Nightscout REST API
    /// </summary>
    Api,

    /// <summary>
    /// Migrate data directly from MongoDB
    /// </summary>
    MongoDb
}

/// <summary>
/// Current state of a migration job
/// </summary>
public enum MigrationJobState
{
    Pending,
    Validating,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Request to start a new migration job
/// </summary>
public record StartMigrationRequest
{
    /// <summary>
    /// Migration mode (API or MongoDB)
    /// </summary>
    public MigrationMode Mode { get; init; }

    /// <summary>
    /// Nightscout URL (for API mode)
    /// </summary>
    public string? NightscoutUrl { get; init; }

    /// <summary>
    /// Nightscout API secret (for API mode)
    /// </summary>
    public string? NightscoutApiSecret { get; init; }

    /// <summary>
    /// MongoDB connection string (for MongoDB mode)
    /// </summary>
    public string? MongoConnectionString { get; init; }

    /// <summary>
    /// MongoDB database name (for MongoDB mode)
    /// </summary>
    public string? MongoDatabaseName { get; init; }

    /// <summary>
    /// Collections to migrate. Empty means all.
    /// </summary>
    public List<string> Collections { get; init; } = [];

    /// <summary>
    /// Start date for migration (optional)
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// End date for migration (optional)
    /// </summary>
    public DateTime? EndDate { get; init; }
}

/// <summary>
/// Information about a migration job
/// </summary>
public record MigrationJobInfo
{
    public required Guid Id { get; init; }
    public required MigrationMode Mode { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? SourceDescription { get; init; }
}

/// <summary>
/// Status of a migration job including progress
/// </summary>
public record MigrationJobStatus
{
    public required Guid JobId { get; init; }
    public required MigrationJobState State { get; init; }
    public required double ProgressPercentage { get; init; }
    public string? CurrentOperation { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }
    public Dictionary<string, CollectionProgress> CollectionProgress { get; init; } = [];
}

/// <summary>
/// Progress for a specific collection
/// </summary>
public record CollectionProgress
{
    public required string CollectionName { get; init; }
    public long TotalDocuments { get; init; }
    public long DocumentsMigrated { get; init; }
    public long DocumentsFailed { get; init; }
    public bool IsComplete { get; init; }
}

/// <summary>
/// Request to test a Nightscout connection
/// </summary>
public record TestMigrationConnectionRequest
{
    public MigrationMode Mode { get; init; }
    public string? NightscoutUrl { get; init; }
    public string? NightscoutApiSecret { get; init; }
    public string? MongoConnectionString { get; init; }
    public string? MongoDatabaseName { get; init; }
}

/// <summary>
/// Result of testing a migration connection
/// </summary>
public record TestMigrationConnectionResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SiteName { get; init; }
    public string? Version { get; init; }
    public long? EntryCount { get; init; }
    public long? TreatmentCount { get; init; }
    public List<string> AvailableCollections { get; init; } = [];
}
