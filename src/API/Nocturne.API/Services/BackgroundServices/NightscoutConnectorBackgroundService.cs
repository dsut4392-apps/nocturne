using Microsoft.Extensions.DependencyInjection;
using Nocturne.Connectors.Configurations;
using Nocturne.Connectors.Nightscout.Services;

namespace Nocturne.API.Services.BackgroundServices;

/// <summary>
/// Background service for Nightscout-to-Nightscout connector
/// </summary>
public class NightscoutConnectorBackgroundService
    : ConnectorBackgroundService<NightscoutConnectorConfiguration>
{
    public NightscoutConnectorBackgroundService(
        IServiceProvider serviceProvider,
        NightscoutConnectorConfiguration config,
        ILogger<NightscoutConnectorBackgroundService> logger
    )
        : base(serviceProvider, config, logger) { }

    protected override string ConnectorName => "Nightscout";

    protected override async Task<bool> PerformSyncAsync(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        var connectorService =
            scope.ServiceProvider.GetRequiredService<NightscoutConnectorService>();

        return await connectorService.SyncDataAsync(Config, cancellationToken);
    }
}
