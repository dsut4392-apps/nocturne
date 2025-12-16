using System;

namespace Nocturne.Connectors.Core.Interfaces
{
    /// <summary>
    /// tracks metrics for a connector service to be exposed via health checks
    /// </summary>
    public interface IConnectorMetricsTracker
    {
        /// <summary>
        /// Total number of entries processed since the service started
        /// </summary>
        long TotalEntries { get; }

        /// <summary>
        /// Timestamp of the last processed entry (UTC)
        /// </summary>
        DateTime? LastEntryTime { get; }

        /// <summary>
        /// Number of entries processed in the last 24 hours
        /// </summary>
        int EntriesLast24Hours { get; }

        /// <summary>
        /// Timestamp of the last sync operation (UTC)
        /// </summary>
        DateTime? LastSyncTime { get; }

        /// <summary>
        /// Records newly processed entries
        /// </summary>
        /// <param name="count">Number of entries processed</param>
        /// <param name="latestTimestamp">Timestamp of the latest entry if available</param>
        void TrackEntries(int count, DateTime? latestTimestamp = null);

        /// <summary>
        /// Gets the most recent entry timestamps for health reporting
        /// </summary>
        /// <param name="count">Number of recent timestamps to retrieve</param>
        /// <returns>Array of recent entry timestamps in descending order (newest first)</returns>
        DateTime[] GetRecentEntryTimestamps(int count);

        /// <summary>
        /// Records a sync operation
        /// </summary>
        void TrackSync();

        /// <summary>
        /// Resets all metrics
        /// </summary>
        void Reset();
    }
}
