using Xunit;

namespace Nocturne.API.Tests.Integration.Infrastructure;

/// <summary>
/// Test collection to ensure tests share the same Aspire application instance.
/// The AspireIntegrationTestFixture manages the complete distributed application lifecycle,
/// including PostgreSQL database, API service, and any other dependencies.
///
/// All integration tests should use [Collection("AspireIntegration")] to share
/// the same application instance for performance.
/// </summary>
[CollectionDefinition("AspireIntegration")]
public class AspireIntegrationTestCollection : ICollectionFixture<AspireIntegrationTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

/// <summary>
/// Legacy test collection for backward compatibility during migration.
/// Tests using this collection will continue using the CustomWebApplicationFactory approach.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<TestDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
