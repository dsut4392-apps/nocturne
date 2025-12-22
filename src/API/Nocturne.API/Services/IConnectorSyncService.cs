using Nocturne.Core.Models.Services;

namespace Nocturne.API.Services;

/// <summary>
/// Service for manually triggering data synchronization across all enabled connectors
/// </summary>
public interface IConnectorSyncService
{


    /// <summary>
    /// Checks if there are any connectors configured and enabled
    /// </summary>
    bool HasEnabledConnectors();

    /// <summary>
    /// Checks if a specific connector is configured and enabled
    /// </summary>
    /// <param name="connectorId">The connector ID (e.g., "dexcom", "glooko")</param>
    bool IsConnectorConfigured(string connectorId);
    /// <summary>
    /// Triggers a granular sync for a specific connector
    /// </summary>
    /// <param name="connectorId">The connector identifier/name</param>
    /// <param name="request">The sync request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the sync operation</returns>
    Task<Nocturne.Connectors.Core.Models.SyncResult> TriggerConnectorSyncAsync(string connectorId, Nocturne.Connectors.Core.Models.SyncRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the supported sync data types for a connector
    /// </summary>
    /// <param name="connectorId">The connector identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supported data types</returns>
    Task<List<Nocturne.Connectors.Core.Models.SyncDataType>> GetConnectorCapabilitiesAsync(string connectorId, CancellationToken cancellationToken = default);
}
