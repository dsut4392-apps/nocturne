using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nocturne.Connectors.Core.Interfaces;
using Nocturne.Connectors.Core.Models;
using System;
using System.Linq;
using System.Threading;

namespace Nocturne.Connectors.Core.Extensions
{
    public static class ConnectorEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps standard connector endpoints including:
        /// - POST /sync
        /// - GET /capabilities
        /// - GET /health/data
        /// - GET /metrics (optional, TODO: standardize metrics endpoint structure if needed)
        /// </summary>
        /// <typeparam name="TService">The connector service type implementing IConnectorService<TConfig></typeparam>
        /// <typeparam name="TConfig">The connector configuration type implementing IConnectorConfiguration</typeparam>
        /// <param name="app">The WebApplication builder</param>
        /// <param name="connectorDisplayName">Display name for the connector (e.g. "Nightscout Connector")</param>
        /// <returns>The WebApplication for chaining</returns>
        public static WebApplication MapConnectorEndpoints<TService, TConfig>(
            this WebApplication app,
            string connectorDisplayName)
            where TService : class, IConnectorService<TConfig>
            where TConfig : class, IConnectorConfiguration
        {
            // Configure manual sync endpoint
            app.MapPost(
                "/sync",
                async (
                    [FromBody] SyncRequest request,
                    IServiceProvider serviceProvider,
                    CancellationToken cancellationToken
                ) =>
                {
                    // Use a generic logger for the endpoint or dynamic logger based on TService if preferred
                    // But typically Program logger or similar is used in original code.
                    // Let's use ILogger<TService> as it's more specific than Program.
                    var logger = serviceProvider.GetRequiredService<ILogger<TService>>();
                    var config = serviceProvider.GetRequiredService<TConfig>();

                    try
                    {
                        using var scope = serviceProvider.CreateScope();
                        var connectorService = scope.ServiceProvider.GetRequiredService<TService>();

                        logger.LogInformation(
                            "Manual sync triggered for {ConnectorName}. Request: {@Request}",
                            connectorDisplayName,
                            request
                        );

                        var result = await connectorService.SyncDataAsync(
                            request,
                            config,
                            cancellationToken
                        );

                        return Results.Ok(result);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error during manual sync for {ConnectorName}", connectorDisplayName);
                        return Results.Problem("Sync failed with error: " + ex.Message);
                    }
                }
            );

            // Configure capabilities endpoint
            app.MapGet(
                "/capabilities",
                (IServiceProvider serviceProvider) =>
                {
                    using var scope = serviceProvider.CreateScope();
                    var connectorService = scope.ServiceProvider.GetRequiredService<TService>();

                    return Results.Ok(new
                    {
                        supportedDataTypes = connectorService.SupportedDataTypes
                    });
                }
            );

            // Configure health/data endpoint
            app.MapGet(
                "/health/data",
                (IServiceProvider serviceProvider) =>
                {
                    var metricsTracker = serviceProvider.GetService<IConnectorMetricsTracker>();

                    // We need to resolve IOptionsSnapshot<TConfig> or TConfig directly.
                    // The original code used IOptionsSnapshot<TConfig> in Nightscout, but TConfig in Glooko/Dexcom.
                    // TConfig as a singleton (which they all seem to register) is safer if registered directly.
                    // Let's try to get TConfig directly since it's registered as singleton in all reviewed Programs.
                    var config = serviceProvider.GetRequiredService<TConfig>();

                    if (metricsTracker == null)
                    {
                        return Results.Ok(
                            new
                            {
                                connectorName = connectorDisplayName,
                                status = "running",
                                message = "Metrics tracking not available",
                            }
                        );
                    }

                    var recentTimestamps = metricsTracker.GetRecentEntryTimestamps(10);

                    return Results.Ok(
                        new
                        {
                            connectorName = connectorDisplayName,
                            status = "running",
                            metrics = new
                            {
                                totalEntries = metricsTracker.TotalEntries,
                                lastEntryTime = metricsTracker.LastEntryTime,
                                entriesLast24Hours = metricsTracker.EntriesLast24Hours,
                                lastSyncTime = metricsTracker.LastSyncTime,
                            },
                            recentEntries = recentTimestamps.Select(t => new { timestamp = t }).ToArray(),
                            configuration = new
                            {
                                syncIntervalMinutes = config.SyncIntervalMinutes,
                                connectSource = config.ConnectSource,
                            },
                        }
                    );
                }
            );

            return app;
        }
    }
}
