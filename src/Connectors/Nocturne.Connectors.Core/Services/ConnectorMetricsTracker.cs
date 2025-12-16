using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Nocturne.Connectors.Core.Interfaces;

namespace Nocturne.Connectors.Core.Services
{
    /// <summary>
    /// Thread-safe implementation of IConnectorMetricsTracker
    /// </summary>
    public class ConnectorMetricsTracker : IConnectorMetricsTracker
    {
        private long _totalEntries;
        private long _lastEntryTicks = 0; // 0 indicates null/not set
        private long _lastSyncTicks = 0; // 0 indicates null/not set

        // Use a lightweight sliding window mechanism for 24h count
        // We'll store buckets of counts per hour to avoid storing individual timestamps for everything
        // 24 buckets representing 24 hours
        private readonly ConcurrentDictionary<long, int> _hourlyBuckets = new();

        // Store recent entry timestamps (limited to 50 most recent)
        private readonly ConcurrentQueue<DateTime> _recentTimestamps = new();
        private const int MaxRecentTimestamps = 50;

        public long TotalEntries => _totalEntries;

        public DateTime? LastEntryTime
        {
            get
            {
                var ticks = Interlocked.Read(ref _lastEntryTicks);
                return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
            }
        }

        public int EntriesLast24Hours
        {
            get
            {
                CleanOldBuckets();
                return _hourlyBuckets.Values.Sum();
            }
        }

        public DateTime? LastSyncTime
        {
            get
            {
                var ticks = Interlocked.Read(ref _lastSyncTicks);
                return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
            }
        }

        public void TrackEntries(int count, DateTime? latestTimestamp = null)
        {
            if (count <= 0) return;

            Interlocked.Add(ref _totalEntries, count);

            var timestampToUse = latestTimestamp?.ToUniversalTime() ?? DateTime.UtcNow;

            UpdateLastEntryTime(timestampToUse);

            // Add to recent timestamps queue
            _recentTimestamps.Enqueue(timestampToUse);

            // Trim queue if it exceeds max size
            while (_recentTimestamps.Count > MaxRecentTimestamps)
            {
                _recentTimestamps.TryDequeue(out _);
            }

            // Add to the bucket for the current hour
            var currentHourKey = GetHourKey(DateTime.UtcNow);
            _hourlyBuckets.AddOrUpdate(currentHourKey, count, (k, v) => v + count);

            // Cleanup occasionally (could be done more smartly, but this is simple)
            CleanOldBuckets();
        }

        public DateTime[] GetRecentEntryTimestamps(int count)
        {
            return _recentTimestamps
                .OrderByDescending(t => t)
                .Take(count)
                .ToArray();
        }

        public void TrackSync()
        {
            var now = DateTime.UtcNow;
            Interlocked.Exchange(ref _lastSyncTicks, now.Ticks);
        }

        private void UpdateLastEntryTime(DateTime timestamp)
        {
            var newTicks = timestamp.ToUniversalTime().Ticks;
            var currentTicks = Interlocked.Read(ref _lastEntryTicks);

            while (newTicks > currentTicks)
            {
                var original = Interlocked.CompareExchange(ref _lastEntryTicks, newTicks, currentTicks);
                if (original == currentTicks)
                {
                    break;
                }
                currentTicks = original;
            }
        }

        public void Reset()
        {
            _totalEntries = 0;
            Interlocked.Exchange(ref _lastEntryTicks, 0);
            Interlocked.Exchange(ref _lastSyncTicks, 0);
            _hourlyBuckets.Clear();
            _recentTimestamps.Clear();
        }

        private void CleanOldBuckets()
        {
            var cutoffKey = GetHourKey(DateTime.UtcNow.AddHours(-24));

            // Remove keys older than cutoff
            // Note: This iterate-and-remove is fine for ConcurrentDictionary
            foreach (var key in _hourlyBuckets.Keys)
            {
                if (key < cutoffKey)
                {
                    _hourlyBuckets.TryRemove(key, out _);
                }
            }
        }

        private long GetHourKey(DateTime timestamp)
        {
            // Simple integer key: YYYYMMDDHH
            // Or simpler: Total hours since epoch
            return (long)(timestamp - DateTime.UnixEpoch).TotalHours;
        }
    }
}
