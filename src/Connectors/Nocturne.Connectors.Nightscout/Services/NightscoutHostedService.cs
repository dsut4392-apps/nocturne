using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nocturne.Connectors.Core.Services;
using Nocturne.Connectors.Configurations;
using Nocturne.Connectors.Nightscout.Services;

namespace Nocturne.Connectors.Nightscout;

/// <summary>
/// Hosted service that runs the Nightscout-to-Nightscout connector in the background with resilient polling.
/// Features adaptive polling (fast reconnection) and automatic backfill on recovery.
/// </summary>
public class NightscoutHostedService : ResilientPollingHostedService<NightscoutConnectorService, NightscoutConnectorConfiguration>
{
    private readonly NightscoutConnectorConfiguration _config;

    public NightscoutHostedService(
        IServiceProvider serviceProvider,
        ILogger<NightscoutHostedService> logger,
        IOptions<NightscoutConnectorConfiguration> config
    ) : base(serviceProvider, logger, config.Value)
    {
        _config = config.Value;
    }

    protected override string ConnectorName => "Nightscout";

    protected override TimeSpan NormalPollingInterval =>
        TimeSpan.FromMinutes(Math.Max(1, _config.SyncIntervalMinutes));

    protected override async Task<bool> ExecuteSyncAsync(
        NightscoutConnectorService connector,
        DateTime? backfillFrom,
        CancellationToken cancellationToken)
    {
        return await connector.SyncDataAsync(_config, cancellationToken, backfillFrom);
    }
}
