using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nocturne.Connectors.Core.Interfaces;
using Nocturne.Connectors.Core.Services;

namespace Nocturne.Connectors.Core.Extensions
{
    public static class ConnectorServiceCollectionExtensions
    {
        public static IServiceCollection AddBaseConnectorServices(this IServiceCollection services)
        {
            // Core state and metrics services
            services.TryAddSingleton<IConnectorStateService, ConnectorStateService>();
            services.TryAddSingleton<IConnectorMetricsTracker, ConnectorMetricsTracker>();

            // Default strategies
            services.TryAddSingleton<IRetryDelayStrategy, ProductionRetryDelayStrategy>();
            services.TryAddSingleton<IRateLimitingStrategy, ProductionRateLimitingStrategy>();

            return services;
        }

        public static IServiceCollection AddConnectorApiDataSubmitter(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var apiUrl = configuration["NocturneApiUrl"];
            var apiSecret = configuration["ApiSecret"];

            services.AddSingleton<IApiDataSubmitter>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("NocturneApi");
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApiDataSubmitter>>();
                if (string.IsNullOrEmpty(apiUrl))
                {
                    throw new InvalidOperationException("NocturneApiUrl configuration is missing.");
                }
                return new ApiDataSubmitter(httpClient, apiUrl, apiSecret, logger);
            });

            return services;
        }
    }
}
