using Microsoft.Extensions.Configuration;
using Nocturne.Connectors.Core.Models;

#nullable enable

namespace Nocturne.Connectors.Core.Extensions
{
    /// <summary>
    /// Extension methods for simplified connector configuration binding
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Binds connector configuration from multiple sources in order of precedence:
        /// 1. Environment variables (highest priority)
        /// 2. Parameters:Connectors:{ConnectorName} section
        /// 3. Connectors:{ConnectorName} section (fallback)
        /// </summary>
        /// <typeparam name="T">The configuration type</typeparam>
        /// <param name="configuration">The IConfiguration instance</param>
        /// <param name="config">The configuration object to bind to</param>
        /// <param name="connectorName">The connector name (e.g., "Glooko", "Dexcom")</param>
        public static void BindConnectorConfiguration<T>(
            this IConfiguration configuration,
            T config,
            string connectorName
        )
            where T : BaseConnectorConfiguration
        {
            // Try primary configuration path
            var section = configuration.GetSection($"Parameters:Connectors:{connectorName}");

            // Fallback to alternate path if not found
            if (!section.Exists())
            {
                section = configuration.GetSection($"Connectors:{connectorName}");
            }

            // Bind the section if it exists
            if (section.Exists())
            {
                section.Bind(config);
            }

            // Override with environment variables (these take precedence)
            // Common base configuration properties
            if (bool.TryParse(configuration["SaveRawData"], out var saveRawData))
            {
                config.SaveRawData = saveRawData;
            }

            if (bool.TryParse(configuration["LoadFromFile"], out var loadFromFile))
            {
                config.LoadFromFile = loadFromFile;
            }

            var dataDirectory = configuration["DataDirectory"];
            if (!string.IsNullOrEmpty(dataDirectory))
            {
                config.DataDirectory = dataDirectory;
            }

            var loadFilePath = configuration["LoadFilePath"];
            if (!string.IsNullOrEmpty(loadFilePath))
            {
                config.LoadFilePath = loadFilePath;
            }

            if (bool.TryParse(configuration["DeleteAfterUpload"], out var deleteAfterUpload))
            {
                config.DeleteAfterUpload = deleteAfterUpload;
            }

            if (bool.TryParse(configuration["UseAsyncProcessing"], out var useAsyncProcessing))
            {
                config.UseAsyncProcessing = useAsyncProcessing;
            }

            if (bool.TryParse(configuration["FallbackToDirectApi"], out var fallbackToDirectApi))
            {
                config.FallbackToDirectApi = fallbackToDirectApi;
            }

            if (int.TryParse(configuration["BatchSize"], out var batchSize))
            {
                config.BatchSize = batchSize;
            }

            if (int.TryParse(configuration["SyncIntervalMinutes"], out var syncIntervalMinutes))
            {
                config.SyncIntervalMinutes = syncIntervalMinutes;
            }

            var nightscoutUrl = configuration["NightscoutUrl"];
            if (!string.IsNullOrEmpty(nightscoutUrl))
            {
                config.NightscoutUrl = nightscoutUrl;
            }

            var nightscoutApiSecret = configuration["NightscoutApiSecret"];
            if (!string.IsNullOrEmpty(nightscoutApiSecret))
            {
                config.NightscoutApiSecret = nightscoutApiSecret;
            }

            var apiSecret = configuration["ApiSecret"];
            if (!string.IsNullOrEmpty(apiSecret))
            {
                config.ApiSecret = apiSecret;
            }
        }
    }
}
