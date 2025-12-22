using System.Net.Http.Json;

using Nocturne.Connectors.Core.Services;
using Nocturne.Core.Constants;
using Nocturne.Core.Models.Services;

namespace Nocturne.API.Services;

/// <summary>
/// Service for triggering manual data synchronization via connector sidecar services
/// </summary>
public class ConnectorSyncService : IConnectorSyncService
{

    private readonly ILogger<ConnectorSyncService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ConnectorSyncService(
        ILogger<ConnectorSyncService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration
    )
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }



    /// <inheritdoc />
    public bool HasEnabledConnectors()
    {
        var connectors = ConnectorMetadataService.GetAll();
        foreach (var connector in connectors)
        {
            if (IsConnectorConfiguredInternal(connector.ConnectorName))
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public bool IsConnectorConfigured(string connectorId)
    {
        if (string.IsNullOrEmpty(connectorId))
        {
            return false;
        }

        return IsConnectorConfiguredInternal(connectorId);
    }

    private bool IsConnectorConfiguredInternal(string connectorName)
    {
        var section = _configuration.GetSection($"Parameters:Connectors:{connectorName}");

        if (!section.Exists())
        {
            return false;
        }

        // Check if explicitly enabled (defaults to true in BaseConnectorConfiguration)
        return section.GetValue("Enabled", true);
    }

    /// <inheritdoc />
    public async Task<Nocturne.Connectors.Core.Models.SyncResult> TriggerConnectorSyncAsync(
        string connectorId,
        Nocturne.Connectors.Core.Models.SyncRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // Find connector metadata
        var connector = ConnectorMetadataService.GetAll()
            .FirstOrDefault(c => c.ConnectorName.Equals(connectorId, StringComparison.OrdinalIgnoreCase)
                             || c.ServiceName.Equals(connectorId, StringComparison.OrdinalIgnoreCase));

        if (connector == null)
        {
            return new Nocturne.Connectors.Core.Models.SyncResult
            {
                Success = false,
                Message = $"Connector '{connectorId}' not found"
            };
        }

        if (!IsConnectorConfiguredInternal(connector.ConnectorName))
        {
             return new Nocturne.Connectors.Core.Models.SyncResult
            {
                Success = false,
                Message = $"Connector '{connector.DisplayName}' is not configured or enabled"
            };
        }

        return await SyncConnectorAsync(connector.DisplayName, connector.ServiceName, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Nocturne.Connectors.Core.Models.SyncDataType>> GetConnectorCapabilitiesAsync(
        string connectorId,
        CancellationToken cancellationToken = default
    )
    {
        var connector = ConnectorMetadataService.GetAll()
            .FirstOrDefault(c => c.ConnectorName.Equals(connectorId, StringComparison.OrdinalIgnoreCase)
                             || c.ServiceName.Equals(connectorId, StringComparison.OrdinalIgnoreCase));

        if (connector == null || string.IsNullOrEmpty(connector.ServiceName))
        {
            return new List<Nocturne.Connectors.Core.Models.SyncDataType>();
        }

        try
        {
            var url = $"http://{connector.ServiceName}/capabilities";
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var capabilities = await response.Content.ReadFromJsonAsync<List<Nocturne.Connectors.Core.Models.SyncDataType>>(cancellationToken: cancellationToken);
                return capabilities ?? new List<Nocturne.Connectors.Core.Models.SyncDataType>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching capabilities for {ConnectorId}", connectorId);
        }

        return new List<Nocturne.Connectors.Core.Models.SyncDataType>();
    }



    /// <summary>
    /// Syncs a single connector by modifying the sidecar service
    /// </summary>
    private async Task<Nocturne.Connectors.Core.Models.SyncResult> SyncConnectorAsync(
        string displayName,
        string serviceName,
        Nocturne.Connectors.Core.Models.SyncRequest request,
        CancellationToken cancellationToken
    )
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var url = $"http://{serviceName}/sync";

            _logger.LogInformation("Triggering sidecar sync for {ConnectorName} at {Url} with request {@Request}", displayName, url, request);

            var client = _httpClientFactory.CreateClient();

            // Send SyncRequest as JSON
            var response = await client.PostAsJsonAsync(url, request, cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var syncResult = await response.Content.ReadFromJsonAsync<Nocturne.Connectors.Core.Models.SyncResult>(cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Sidecar sync successful for {ConnectorName}. Items: {@ItemsSynced}",
                    displayName,
                    syncResult?.ItemsSynced
                );

                return syncResult ?? new Nocturne.Connectors.Core.Models.SyncResult
                {
                    Success = true,
                    StartTime = startTime,
                    EndTime = DateTimeOffset.UtcNow
                };
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Sidecar sync failed for {ConnectorName}: {StatusCode} - {Error}",
                    displayName,
                    response.StatusCode,
                    error
                );

                return new Nocturne.Connectors.Core.Models.SyncResult
                {
                    Success = false,
                    Message = $"Sidecar returned {response.StatusCode}: {error}",
                    StartTime = startTime,
                    EndTime = DateTimeOffset.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error triggering sidecar sync for {ConnectorName}: {Message}",
                displayName,
                ex.Message
            );

            return new Nocturne.Connectors.Core.Models.SyncResult
            {
                Success = false,
                Message = ex.Message,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
