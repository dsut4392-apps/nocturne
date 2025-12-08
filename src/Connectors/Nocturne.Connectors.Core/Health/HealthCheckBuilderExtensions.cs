using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Nocturne.Connectors.Core.Health
{
    public static class HealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddConnectorHealthCheck(this IHealthChecksBuilder builder, string connectorName)
        {
            return builder.AddCheck<ConnectorHealthCheck>(
                connectorName,
                tags: new[] { "connector", "metrics" }
            );
        }
    }
}
