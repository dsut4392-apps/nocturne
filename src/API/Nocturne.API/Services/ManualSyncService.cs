using Microsoft.Extensions.Options;
using Nocturne.API.Configuration;
using Nocturne.Connectors.Configurations;
using Nocturne.Connectors.Dexcom.Services;
using Nocturne.Connectors.FreeStyle.Services;
using Nocturne.Connectors.Glooko.Services;
using Nocturne.Connectors.MiniMed.Services;
using Nocturne.Connectors.MyFitnessPal.Services;
using Nocturne.Connectors.Nightscout.Services;
using Nocturne.Core.Models.Services;

namespace Nocturne.API.Services;

/// <summary>
/// Service for manually triggering data synchronization across all enabled connectors
/// </summary>
public class ManualSyncService : IManualSyncService
{
    private readonly ManualSyncSettings _settings;
    private readonly ILogger<ManualSyncService> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Connector configurations
    private readonly DexcomConnectorConfiguration? _dexcomConfig;
    private readonly GlookoConnectorConfiguration? _glookoConfig;
    private readonly LibreLinkUpConnectorConfiguration? _libreConfig;
    private readonly CareLinkConnectorConfiguration? _carelinkConfig;
    private readonly NightscoutConnectorConfiguration? _nightscoutConfig;
    private readonly MyFitnessPalConnectorConfiguration? _mfpConfig;

    public ManualSyncService(
        IOptions<ManualSyncSettings> settings,
        ILogger<ManualSyncService> logger,
        IServiceProvider serviceProvider,
        DexcomConnectorConfiguration? dexcomConfig = null,
        GlookoConnectorConfiguration? glookoConfig = null,
        LibreLinkUpConnectorConfiguration? libreConfig = null,
        CareLinkConnectorConfiguration? carelinkConfig = null,
        NightscoutConnectorConfiguration? nightscoutConfig = null,
        MyFitnessPalConnectorConfiguration? mfpConfig = null
    )
    {
        _settings = settings.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _dexcomConfig = dexcomConfig;
        _glookoConfig = glookoConfig;
        _libreConfig = libreConfig;
        _carelinkConfig = carelinkConfig;
        _nightscoutConfig = nightscoutConfig;
        _mfpConfig = mfpConfig;
    }

    /// <inheritdoc />
    public bool IsEnabled()
    {
        return _settings.IsEnabled;
    }

    /// <inheritdoc />
    public async Task<ManualSyncResult> TriggerManualSyncAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = new ManualSyncResult
        {
            StartTime = DateTimeOffset.UtcNow,
            Success = true,
        };

        if (!_settings.IsEnabled)
        {
            _logger.LogWarning("Manual sync is not enabled - BackfillDays is not configured");
            result.Success = false;
            result.ErrorMessage = "Manual sync is not enabled";
            result.EndTime = DateTimeOffset.UtcNow;
            return result;
        }

        _logger.LogInformation(
            "Starting manual sync for all connectors with {LookbackDays} day lookback",
            _settings.BackfillDays
        );

        var connectorTasks = new List<Task<ConnectorSyncResult>>();

        // Queue up all enabled connectors
        if (_dexcomConfig != null && _dexcomConfig.SyncIntervalMinutes > 0)
        {
            connectorTasks.Add(SyncConnectorAsync<DexcomConnectorService>("Dexcom", cancellationToken));
        }

        if (_glookoConfig != null && _glookoConfig.SyncIntervalMinutes > 0)
        {
            connectorTasks.Add(SyncConnectorAsync<GlookoConnectorService>("Glooko", cancellationToken));
        }

        if (_libreConfig != null && _libreConfig.SyncIntervalMinutes > 0)
        {
            connectorTasks.Add(
                SyncConnectorAsync<LibreConnectorService>("LibreLinkUp", cancellationToken)
            );
        }

        if (_carelinkConfig != null && _carelinkConfig.SyncIntervalMinutes > 0)
        {
            connectorTasks.Add(
                SyncConnectorAsync<CareLinkConnectorService>("CareLink", cancellationToken)
            );
        }

        if (_nightscoutConfig != null && _nightscoutConfig.SyncIntervalMinutes > 0)
        {
            connectorTasks.Add(
                SyncConnectorAsync<NightscoutConnectorService>("Nightscout", cancellationToken)
            );
        }

        if (_mfpConfig != null && _mfpConfig.SyncIntervalMinutes > 0)
        {
            connectorTasks.Add(
                SyncConnectorAsync<MyFitnessPalConnectorService>("MyFitnessPal", cancellationToken)
            );
        }

        result.TotalConnectors = connectorTasks.Count;

        if (result.TotalConnectors == 0)
        {
            _logger.LogWarning("No connectors are enabled for manual sync");
            result.Success = false;
            result.ErrorMessage = "No connectors are enabled";
            result.EndTime = DateTimeOffset.UtcNow;
            return result;
        }

        // Execute all connectors in parallel
        _logger.LogInformation("Syncing {Count} connectors in parallel", result.TotalConnectors);
        var connectorResults = await Task.WhenAll(connectorTasks);

        result.ConnectorResults = connectorResults.ToList();
        result.SuccessfulConnectors = connectorResults.Count(r => r.Success);
        result.FailedConnectors = connectorResults.Count(r => !r.Success);
        result.Success = result.FailedConnectors == 0;
        result.EndTime = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Manual sync completed in {Duration}. Success: {SuccessCount}/{TotalCount}",
            result.Duration,
            result.SuccessfulConnectors,
            result.TotalConnectors
        );

        return result;
    }

    /// <summary>
    /// Syncs a single connector
    /// </summary>
    private async Task<ConnectorSyncResult> SyncConnectorAsync<TService>(
        string connectorName,
        CancellationToken cancellationToken
    )
        where TService : class
    {
        var result = new ConnectorSyncResult { ConnectorName = connectorName };
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            _logger.LogInformation("Starting manual sync for {ConnectorName}", connectorName);

            // Create a scope to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetService<TService>();

            if (service == null)
            {
                _logger.LogWarning(
                    "Connector service {ConnectorName} is not registered",
                    connectorName
                );
                result.Success = false;
                result.ErrorMessage = $"Service not registered: {connectorName}";
                result.Duration = DateTimeOffset.UtcNow - startTime;
                return result;
            }

            // Use reflection to call SyncDataAsync method
            var syncMethod = service
                .GetType()
                .GetMethod("SyncDataAsync", new[] { typeof(CancellationToken) });

            if (syncMethod == null)
            {
                _logger.LogWarning(
                    "Connector service {ConnectorName} does not have SyncDataAsync method",
                    connectorName
                );
                result.Success = false;
                result.ErrorMessage = $"SyncDataAsync method not found on {connectorName}";
                result.Duration = DateTimeOffset.UtcNow - startTime;
                return result;
            }

            // Invoke the sync method
            var task = syncMethod.Invoke(service, new object[] { cancellationToken }) as Task<bool>;

            if (task == null)
            {
                _logger.LogWarning(
                    "Failed to invoke SyncDataAsync on {ConnectorName}",
                    connectorName
                );
                result.Success = false;
                result.ErrorMessage = $"Failed to invoke SyncDataAsync on {connectorName}";
                result.Duration = DateTimeOffset.UtcNow - startTime;
                return result;
            }

            var success = await task;

            result.Success = success;
            result.Duration = DateTimeOffset.UtcNow - startTime;

            if (success)
            {
                _logger.LogInformation(
                    "Manual sync completed successfully for {ConnectorName} in {Duration}",
                    connectorName,
                    result.Duration
                );
            }
            else
            {
                _logger.LogWarning(
                    "Manual sync failed for {ConnectorName} in {Duration}",
                    connectorName,
                    result.Duration
                );
                result.ErrorMessage = "Sync returned false";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during manual sync for {ConnectorName}: {Message}",
                connectorName,
                ex.Message
            );
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = DateTimeOffset.UtcNow - startTime;
        }

        return result;
    }
}
