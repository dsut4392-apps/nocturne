using Nocturne.API.Services.BackgroundServices;

namespace Nocturne.API.Services;

/// <summary>
/// Interface for querying demo mode status.
/// </summary>
public interface IDemoModeService
{
    /// <summary>
    /// Whether demo mode is enabled and the external demo service is running.
    /// When true, only demo data should be shown to users.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Whether demo mode is configured (even if the service is not yet healthy).
    /// </summary>
    bool IsConfigured { get; }
}

/// <summary>
/// Service that provides demo mode status information.
/// This service reads the DemoService configuration passed from Aspire
/// and provides a unified way to check if demo mode is active.
/// </summary>
public class DemoModeService : IDemoModeService
{
    private readonly bool _isEnabled;
    private readonly bool _isConfigured;
    private readonly ILogger<DemoModeService> _logger;

    public DemoModeService(IConfiguration configuration, ILogger<DemoModeService> logger)
    {
        _logger = logger;

        // Read from DemoService section (set by Aspire environment variables)
        var demoServiceConfig =
            configuration
                .GetSection(DemoServiceConfiguration.SectionName)
                .Get<DemoServiceConfiguration>() ?? new DemoServiceConfiguration();

        // Also check Parameters:DemoMode:Enabled (set in appsettings.json for local dev)
        var parametersDemoModeEnabled = configuration.GetValue<bool>(
            "Parameters:DemoMode:Enabled",
            false
        );

        // Demo mode is enabled if either source says so
        _isEnabled = demoServiceConfig.Enabled || parametersDemoModeEnabled;
        _isConfigured = _isEnabled && !string.IsNullOrWhiteSpace(demoServiceConfig.Url);

        // Log all demo service configuration for debugging
        _logger.LogInformation(
            "Demo mode service initialized - DemoService:Enabled={DemoServiceEnabled}, Parameters:DemoMode:Enabled={ParametersEnabled}, Final IsEnabled={IsEnabled}",
            demoServiceConfig.Enabled,
            parametersDemoModeEnabled,
            _isEnabled
        );
    }

    /// <inheritdoc />
    public bool IsEnabled => _isEnabled;

    /// <inheritdoc />
    public bool IsConfigured => _isConfigured;
}
