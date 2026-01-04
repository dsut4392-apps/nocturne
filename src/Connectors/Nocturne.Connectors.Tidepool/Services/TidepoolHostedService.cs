using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nocturne.Connectors.Core.Services;
using Nocturne.Connectors.Configurations;

namespace Nocturne.Connectors.Tidepool.Services;

/// <summary>
/// Background service that periodically syncs data from Tidepool with resilient polling.
/// Features adaptive polling (fast reconnection) and automatic backfill on recovery.
/// </summary>
public class TidepoolHostedService : ResilientPollingHostedService<TidepoolConnectorService, TidepoolConnectorConfiguration>
{
    private readonly TidepoolConnectorConfiguration _config;

    public TidepoolHostedService(
        IServiceProvider serviceProvider,
        IOptions<TidepoolConnectorConfiguration> config,
        ILogger<TidepoolHostedService> logger
    ) : base(serviceProvider, logger, config.Value)
    {
        _config = config.Value;
    }

    protected override string ConnectorName => "Tidepool";

    protected override TimeSpan NormalPollingInterval =>
        TimeSpan.FromMinutes(_config.SyncIntervalMinutes);

    protected override async Task<bool> ExecuteSyncAsync(
        TidepoolConnectorService connector,
        DateTime? backfillFrom,
        CancellationToken cancellationToken)
    {
        return await connector.SyncDataAsync(_config, cancellationToken, backfillFrom);
    }
}
