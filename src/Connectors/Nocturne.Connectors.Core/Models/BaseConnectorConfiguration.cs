using System;
using System.ComponentModel.DataAnnotations;
using Nocturne.Connectors.Core.Interfaces;

#nullable enable

namespace Nocturne.Connectors.Core.Models
{
    /// <summary>
    /// Base implementation of connector configuration with common properties
    /// </summary>
    public abstract class BaseConnectorConfiguration : IConnectorConfiguration
    {
        [Required]
        public ConnectSource ConnectSource { get; set; }

        public bool SaveRawData { get; set; } = false;

        public string DataDirectory { get; set; } = "./data";

        public bool LoadFromFile { get; set; } = false;

        public string? LoadFilePath { get; set; }

        public bool DeleteAfterUpload { get; set; } = false;

        public bool UseAsyncProcessing { get; set; } = true;

        public bool FallbackToDirectApi { get; set; } = true;

        public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromMinutes(5);

        public int MaxRetryAttempts { get; set; } = 3;

        public int BatchSize { get; set; } = 50;

        public string? RoutingKeyPrefix { get; set; }

        public int SyncIntervalMinutes { get; set; } = 5;

        public ConnectorMode Mode { get; set; } = ConnectorMode.Nocturne;

        public string NightscoutUrl { get; set; } = string.Empty;

        public string NightscoutApiSecret { get; set; } = string.Empty;

        public string ApiSecret { get; set; } = string.Empty;

        public virtual void Validate()
        {
            if (!Enum.IsDefined(typeof(ConnectSource), ConnectSource))
                throw new ArgumentException($"Invalid connector source: {ConnectSource}");

            if (UseAsyncProcessing)
            {
                if (MessageTimeout <= TimeSpan.Zero)
                    throw new ArgumentException("MessageTimeout must be greater than zero");

                if (MaxRetryAttempts < 0)
                    throw new ArgumentException("MaxRetryAttempts cannot be negative");

                if (BatchSize <= 0)
                    throw new ArgumentException("BatchSize must be greater than zero");

                if (!string.IsNullOrEmpty(RoutingKeyPrefix))
                {
                    if (
                        !System.Text.RegularExpressions.Regex.IsMatch(
                            RoutingKeyPrefix,
                            "^[a-zA-Z0-9.]*$"
                        )
                    )
                        throw new ArgumentException(
                            "RoutingKeyPrefix can only contain alphanumeric characters and dots"
                        );
                }
            }

            ValidateSourceSpecificConfiguration();
        }

        /// <summary>
        /// Override this method to validate connector-specific configuration
        /// </summary>
        protected abstract void ValidateSourceSpecificConfiguration();
    }
}
