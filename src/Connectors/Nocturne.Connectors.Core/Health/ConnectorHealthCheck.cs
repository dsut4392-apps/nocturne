using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nocturne.Connectors.Core.Interfaces;

namespace Nocturne.Connectors.Core.Health
{
    public class ConnectorHealthCheck : IHealthCheck
    {
        private readonly IConnectorMetricsTracker _metricsTracker;
        private readonly string _connectorSource;

        public ConnectorHealthCheck(IConnectorMetricsTracker metricsTracker, string connectorSource)
        {
            _metricsTracker = metricsTracker ?? throw new ArgumentNullException(nameof(metricsTracker));
            _connectorSource = connectorSource ?? "unknown";
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>
            {
                { "connector_source", _connectorSource },
                { "total_entries", _metricsTracker.TotalEntries },
                { "entries_24h", _metricsTracker.EntriesLast24Hours }
            };

            if (_metricsTracker.LastEntryTime.HasValue)
            {
                data.Add("last_entry_time", _metricsTracker.LastEntryTime.Value.ToString("O"));

                // Calculate time since last entry for cleaner consumption if needed
                var timeSince = DateTime.UtcNow - _metricsTracker.LastEntryTime.Value;
                data.Add("seconds_since_last_entry", (long)timeSince.TotalSeconds);
            }
            else
            {
                data.Add("last_entry_time", "null");
            }

            // You might want to degrade health if no data received for a long time,
            // but for now we just report Healthy with data.
            // If LastEntryTime is very old (e.g. > 1 hour), we could return Degraded?
            // For now, let's keep it simple: Healthy, just reporting metrics.

            return Task.FromResult(HealthCheckResult.Healthy("Connector is running", data));
        }
    }
}
