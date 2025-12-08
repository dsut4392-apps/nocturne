using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nocturne.API.Models;
using Nocturne.Core.Constants;

namespace Nocturne.API.Services;

public class ConnectorHealthService : IConnectorHealthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConnectorHealthService> _logger;

    private record ConnectorDefinition(string ServiceName, string HealthCheckName, string DisplayName);

    private static readonly ConnectorDefinition[] Connectors = new[]
    {
        new ConnectorDefinition(ServiceNames.NightscoutConnector, "nightscout", "Nightscout"),
        new ConnectorDefinition(ServiceNames.DexcomConnector, "dexcom", "Dexcom API"),
        new ConnectorDefinition(ServiceNames.LibreConnector, "freestyle", "LibreLinkUp"),
        new ConnectorDefinition(ServiceNames.GlookoConnector, "glooko", "Glooko"),
        new ConnectorDefinition(ServiceNames.MiniMedConnector, "minimed", "MiniMed CareLink"),
        new ConnectorDefinition(ServiceNames.MyFitnessPalConnector, "myfitnesspal", "MyFitnessPal")
    };

    public ConnectorHealthService(IHttpClientFactory httpClientFactory, ILogger<ConnectorHealthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<ConnectorStatusDto>> GetConnectorStatusesAsync(CancellationToken cancellationToken = default)
    {
        var tasks = Connectors.Select(c => CheckConnectorStatusAsync(c, cancellationToken));
        return await Task.WhenAll(tasks);
    }

    private async Task<ConnectorStatusDto> CheckConnectorStatusAsync(ConnectorDefinition connector, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            // Aspire service discovery handles the hostname resolution
            var url = $"http://{connector.ServiceName}/health";

            _logger.LogDebug("Checking health for {Connector} at {Url}", connector.DisplayName, url);

            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new ConnectorStatusDto
                {
                    Name = connector.DisplayName,
                    Status = "Unhealthy",
                    Description = $"HTTP {response.StatusCode}",
                    IsHealthy = false
                };
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            // Navigate to results -> {checkName}
            if (doc.RootElement.TryGetProperty("results", out var results) &&
                results.TryGetProperty(connector.HealthCheckName, out var checkResult))
            {
                var status = checkResult.GetProperty("status").GetString() ?? "Unknown";
                var description = checkResult.TryGetProperty("description", out var desc) ? desc.GetString() : null;

                long totalEntries = 0;
                DateTime? lastEntryTime = null;
                int entriesLast24h = 0;

                if (checkResult.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("TotalEntries", out var msgEl) && msgEl.ValueKind == JsonValueKind.Number)
                    {
                         totalEntries = msgEl.GetInt64();
                    }
                    else // Fallback if it was serialized as string or other
                    {
                        // Some serializers might output numbers as strings if configured loosely, but Utf8JsonWriter usually strong types
                    }

                    if (data.TryGetProperty("LastEntryTime", out var timeEl))
                    {
                         if(timeEl.ValueKind == JsonValueKind.String && DateTime.TryParse(timeEl.GetString(), out var dt))
                         {
                             lastEntryTime = dt;
                         }
                    }

                    if (data.TryGetProperty("EntriesLast24Hours", out var countEl) && countEl.ValueKind == JsonValueKind.Number)
                    {
                        entriesLast24h = countEl.GetInt32();
                    }
                }

                return new ConnectorStatusDto
                {
                    Name = connector.DisplayName,
                    Status = status,
                    Description = description,
                    TotalEntries = totalEntries,
                    LastEntryTime = lastEntryTime,
                    EntriesLast24Hours = entriesLast24h,
                    IsHealthy = status == "Healthy"
                };
            }

            // If we have a healthy root status but missing specific check results
            var rootStatus = doc.RootElement.GetProperty("status").GetString();
             return new ConnectorStatusDto
            {
                Name = connector.DisplayName,
                Status = rootStatus ?? "Unknown",
                Description = "Detailed metrics unavailable",
                IsHealthy = rootStatus == "Healthy"
            };

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check status for {Connector}", connector.DisplayName);
            return new ConnectorStatusDto
            {
                Name = connector.DisplayName,
                Status = "Unreachable",
                Description = ex.Message,
                IsHealthy = false
            };
        }
    }
}
