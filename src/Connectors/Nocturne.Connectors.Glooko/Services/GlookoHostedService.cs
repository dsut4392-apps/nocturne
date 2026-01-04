using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nocturne.Connectors.Core.Services;
using Nocturne.Connectors.Configurations;
using Nocturne.Connectors.Glooko.Services;

namespace Nocturne.Connectors.Glooko;

/// <summary>
/// Hosted service that runs the Glooko connector in the background with resilient polling.
/// Features adaptive polling (fast reconnection) and automatic backfill on recovery.
/// </summary>
public class GlookoHostedService : ResilientPollingHostedService<GlookoConnectorService, GlookoConnectorConfiguration>
{
    private readonly GlookoConnectorConfiguration _config;

    public GlookoHostedService(
        IServiceProvider serviceProvider,
        ILogger<GlookoHostedService> logger,
        IOptions<GlookoConnectorConfiguration> config
    ) : base(serviceProvider, logger, config.Value)
    {
        _config = config.Value;
    }

    protected override string ConnectorName => "Glooko";

    protected override TimeSpan NormalPollingInterval =>
        TimeSpan.FromMinutes(_config.SyncIntervalMinutes);

    protected override async Task<bool> ExecuteSyncAsync(
        GlookoConnectorService connector,
        DateTime? backfillFrom,
        CancellationToken cancellationToken)
    {
        return await connector.SyncDataAsync(_config, cancellationToken, backfillFrom);
    }
}
