using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nocturne.Connectors.Core.Services;
using Nocturne.Connectors.Configurations;
using Nocturne.Connectors.FreeStyle.Services;

namespace Nocturne.Connectors.FreeStyle;

/// <summary>
/// Hosted service that runs the FreeStyle LibreLinkUp connector in the background with resilient polling.
/// Features adaptive polling (fast reconnection) and automatic backfill on recovery.
/// </summary>
public class FreeStyleHostedService : ResilientPollingHostedService<LibreConnectorService, LibreLinkUpConnectorConfiguration>
{
    private readonly LibreLinkUpConnectorConfiguration _config;

    public FreeStyleHostedService(
        IServiceProvider serviceProvider,
        ILogger<FreeStyleHostedService> logger,
        IOptions<LibreLinkUpConnectorConfiguration> config
    ) : base(serviceProvider, logger, config.Value)
    {
        _config = config.Value;
    }

    protected override string ConnectorName => "FreeStyle";

    protected override TimeSpan NormalPollingInterval =>
        TimeSpan.FromMinutes(_config.SyncIntervalMinutes);

    protected override async Task<bool> ExecuteSyncAsync(
        LibreConnectorService connector,
        DateTime? backfillFrom,
        CancellationToken cancellationToken)
    {
        return await connector.SyncDataAsync(_config, cancellationToken, backfillFrom);
    }
}
