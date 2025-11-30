using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Nocturne.Services.Demo.Services;

/// <summary>
/// Health check for the demo service. When this health check fails,
/// the API should clean up all demo data.
/// </summary>
public class DemoServiceHealthCheck : IHealthCheck
{
    private volatile bool _isHealthy = true;
    private readonly ILogger<DemoServiceHealthCheck> _logger;

    public DemoServiceHealthCheck(ILogger<DemoServiceHealthCheck> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets whether the service is healthy.
    /// Setting this to false will cause health checks to fail.
    /// </summary>
    public bool IsHealthy
    {
        get => _isHealthy;
        set
        {
            if (_isHealthy != value)
            {
                _isHealthy = value;
                _logger.LogInformation("Demo service health status changed to: {IsHealthy}", value);
            }
        }
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (_isHealthy)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Demo service is running"));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Demo service is stopping"));
    }
}
