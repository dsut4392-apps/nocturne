using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nocturne.Core.Constants;
using Xunit;
using Xunit.Abstractions;

namespace Nocturne.API.Tests.Integration.Infrastructure;

/// <summary>
/// Base class for Aspire-based integration tests.
/// Uses the AspireIntegrationTestFixture to provide a fully orchestrated
/// distributed application environment including database, API, and other services.
/// </summary>
[Collection("AspireIntegration")]
[Parity]
public abstract class AspireIntegrationTestBase : IAsyncLifetime
{
    protected readonly AspireIntegrationTestFixture Fixture;
    protected readonly ITestOutputHelper Output;
    protected readonly List<HubConnection> HubConnections = new();

    /// <summary>
    /// Pre-configured HttpClient for the Nocturne API
    /// </summary>
    protected HttpClient ApiClient => Fixture.ApiClient;

    protected AspireIntegrationTestBase(
        AspireIntegrationTestFixture fixture,
        ITestOutputHelper output
    )
    {
        Fixture = fixture;
        Output = output;
    }

    public virtual Task InitializeAsync()
    {
        using var _ = TestPerformanceTracker.MeasureTest($"{GetType().Name}.Initialize");

        // The fixture handles application initialization.
        // Override this method to add test-specific setup.
        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        using var _ = TestPerformanceTracker.MeasureTest($"{GetType().Name}.Dispose");

        // Clean up SignalR connections
        foreach (var connection in HubConnections)
        {
            if (connection.State == HubConnectionState.Connected)
            {
                await connection.StopAsync();
            }
            await connection.DisposeAsync();
        }
        HubConnections.Clear();

        // Note: Database cleanup is managed by the fixture's lifecycle
        // Individual tests should clean up their own test data if needed
    }

    /// <summary>
    /// Creates an HttpClient for a specific Aspire resource
    /// </summary>
    /// <param name="resourceName">Name of the resource (e.g., ServiceNames.NocturneApi)</param>
    protected HttpClient CreateHttpClient(string resourceName)
    {
        return Fixture.CreateHttpClient(resourceName);
    }

    /// <summary>
    /// Creates a SignalR connection to the Data Hub
    /// </summary>
    protected async Task<HubConnection> CreateDataHubConnectionAsync()
    {
        var baseAddress =
            ApiClient.BaseAddress
            ?? throw new InvalidOperationException("API client base address is not configured");

        var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(baseAddress, $"hubs/{ServiceNames.DataHub}"),
                options =>
                {
                    // Configure any authentication or headers as needed
                }
            )
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Warning);
            })
            .Build();

        HubConnections.Add(connection);
        await connection.StartAsync();
        return connection;
    }

    /// <summary>
    /// Creates a SignalR connection to the Notification Hub
    /// </summary>
    protected async Task<HubConnection> CreateNotificationHubConnectionAsync()
    {
        var baseAddress =
            ApiClient.BaseAddress
            ?? throw new InvalidOperationException("API client base address is not configured");

        var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(baseAddress, $"hubs/{ServiceNames.NotificationHub}"),
                options =>
                {
                    // Configure any authentication or headers as needed
                }
            )
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Warning);
            })
            .Build();

        HubConnections.Add(connection);
        await connection.StartAsync();
        return connection;
    }

    /// <summary>
    /// Gets the connection string for the PostgreSQL database
    /// </summary>
    protected async Task<string?> GetPostgresConnectionStringAsync()
    {
        return await Fixture.GetConnectionStringAsync(ServiceNames.PostgreSql);
    }

    /// <summary>
    /// Logs a message to the test output
    /// </summary>
    protected void Log(string message)
    {
        Output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
    }
}
