using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nocturne.Connectors.Core.Services;
using Nocturne.Connectors.Configurations;
using Nocturne.Connectors.MiniMed.Services;

namespace Nocturne.Connectors.MiniMed;

/// <summary>
/// Hosted service that runs the MiniMed CareLink connector in the background with resilient polling.
/// Features adaptive polling (fast reconnection) and automatic backfill on recovery.
/// </summary>
public class MiniMedHostedService : ResilientPollingHostedService<CareLinkConnectorService, CareLinkConnectorConfiguration>
{
    private readonly CareLinkConnectorConfiguration _config;

    public MiniMedHostedService(
        IServiceProvider serviceProvider,
        ILogger<MiniMedHostedService> logger,
        IOptions<CareLinkConnectorConfiguration> config
    ) : base(serviceProvider, logger, config.Value)
    {
        _config = config.Value;
    }

    protected override string ConnectorName => "MiniMed";

    protected override TimeSpan NormalPollingInterval =>
        TimeSpan.FromMinutes(_config.SyncIntervalMinutes);

    protected override async Task<bool> ExecuteSyncAsync(
        CareLinkConnectorService connector,
        DateTime? backfillFrom,
        CancellationToken cancellationToken)
    {
        return await connector.SyncDataAsync(_config, cancellationToken, backfillFrom);
    }
}
