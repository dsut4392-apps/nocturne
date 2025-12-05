namespace Nocturne.Core.Models.Services;

/// <summary>
/// Result of a manual synchronization operation
/// </summary>
public class ManualSyncResult
{
    /// <summary>
    /// Whether the overall sync operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Total number of connectors that were synced
    /// </summary>
    public int TotalConnectors { get; set; }

    /// <summary>
    /// Number of connectors that completed successfully
    /// </summary>
    public int SuccessfulConnectors { get; set; }

    /// <summary>
    /// Number of connectors that failed
    /// </summary>
    public int FailedConnectors { get; set; }

    /// <summary>
    /// Detailed results for each connector
    /// </summary>
    public List<ConnectorSyncResult> ConnectorResults { get; set; } = new();

    /// <summary>
    /// Start time of the sync operation
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// End time of the sync operation
    /// </summary>
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Duration of the sync operation
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Any error messages from the overall operation
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of syncing a single connector
/// </summary>
public class ConnectorSyncResult
{
    /// <summary>
    /// Name of the connector
    /// </summary>
    public string ConnectorName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this connector synced successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the sync failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration of the sync for this connector
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Number of records synced (if available)
    /// </summary>
    public int? RecordsSynced { get; set; }
}
