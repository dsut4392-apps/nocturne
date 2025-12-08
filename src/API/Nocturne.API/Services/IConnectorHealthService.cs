using Nocturne.API.Models;

namespace Nocturne.API.Services;

public interface IConnectorHealthService
{
    Task<IEnumerable<ConnectorStatusDto>> GetConnectorStatusesAsync(CancellationToken cancellationToken = default);
}
