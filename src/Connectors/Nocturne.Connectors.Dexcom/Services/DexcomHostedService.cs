using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nocturne.Connectors.Core.Services;
using Nocturne.Connectors.Configurations;
using Nocturne.Connectors.Dexcom.Services;

namespace Nocturne.Connectors.Dexcom;

/// <summary>
/// Hosted service that runs the Dexcom connector in the background with resilient polling.
/// Features adaptive polling (fast reconnection) and automatic backfill on recovery.
/// </summary>
public class DexcomHostedService : ResilientPollingHostedService<DexcomConnectorService, DexcomConnectorConfiguration>
{
    private readonly DexcomConnectorConfiguration _config;

    public DexcomHostedService(
        IServiceProvider serviceProvider,
        ILogger<DexcomHostedService> logger,
        DexcomConnectorConfiguration config
    ) : base(serviceProvider, logger, config)
    {
        _config = config;
    }

    protected override string ConnectorName => "Dexcom";

    protected override TimeSpan NormalPollingInterval =>
        TimeSpan.FromMinutes(_config.SyncIntervalMinutes);

    protected override async Task<bool> ExecuteSyncAsync(
        DexcomConnectorService connector,
        DateTime? backfillFrom,
        CancellationToken cancellationToken)
    {
        return await connector.SyncDataAsync(_config, cancellationToken, backfillFrom);
    }
}
