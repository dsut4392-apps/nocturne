using Microsoft.Extensions.Logging;
using Moq;
using Nocturne.Connectors.Core.Interfaces;
using Nocturne.Connectors.Core.Models;
using Nocturne.Connectors.Core.Services;
using Xunit;

namespace Nocturne.Connectors.Core.Tests.Services;

public class BaseConnectorServiceTests
{
    public class TestConnectorService : BaseConnectorService<TestConfig>
    {
        public TestConnectorService(
            HttpClient httpClient,
            ILogger<TestConnectorService> logger,
            IApiDataSubmitter? apiDataSubmitter = null)
            : base(httpClient, logger, apiDataSubmitter)
        {
        }

        public override string ConnectorSource => "test";
        public override string ServiceName => "Test";

        public override Task<bool> AuthenticateAsync() => Task.FromResult(true);
        public override Task<IEnumerable<Nocturne.Core.Models.Entry>> FetchGlucoseDataAsync(DateTime? since = null)
            => Task.FromResult(Enumerable.Empty<Nocturne.Core.Models.Entry>());
    }

    public class TestConfig : IConnectorConfiguration
    {
        public string NightscoutUrl { get; set; } = "";
        public string NightscoutApiSecret { get; set; } = "";
        public string ApiSecret { get; set; } = "";
        public int SyncIntervalMinutes { get; set; } = 5;
        public bool LoadFromFile { get; set; }
        public string? LoadFilePath { get; set; }
        public bool SaveRawData { get; set; }
        public string DataDirectory { get; set; } = "";
        public ConnectorMode Mode { get; set; } = ConnectorMode.Standalone;
        public bool FallbackToDirectApi { get; set; }
        public int BatchSize { get; set; } = 100;
        public bool UseAsyncProcessing { get; set; }
        public ConnectSource ConnectSource { get; set; } = ConnectSource.Dexcom;
        public bool DeleteAfterUpload { get; set; }
        public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxRetryAttempts { get; set; } = 3;
        public string? RoutingKeyPrefix { get; set; }

        public void Validate() { }
    }

    [Fact]
    public void Constructor_WithHttpClient_ShouldNotOwnHttpClient()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = Mock.Of<ILogger<TestConnectorService>>();

        // Act
        var service = new TestConnectorService(httpClient, logger);

        // Assert - HttpClient should not be disposed when service is disposed
        service.Dispose();

        // This will throw if HttpClient was disposed
        _ = httpClient.BaseAddress;
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = Mock.Of<ILogger<TestConnectorService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestConnectorService(null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TestConnectorService(httpClient, null!));
    }
}
