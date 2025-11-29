using Nocturne.Core.Contracts;
using Nocturne.Infrastructure.Data.Abstractions;

namespace Nocturne.API.Services;

public class DemoDataBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DemoDataBackgroundService> _logger;
    private readonly DemoModeConfiguration _config;
    private bool _historicalDataGenerated = false;

    public DemoDataBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<DemoDataBackgroundService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config =
            configuration.GetSection("DemoMode").Get<DemoModeConfiguration>()
            ?? new DemoModeConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Demo mode is disabled, background service will not run");

            // Clean up any existing demo data when demo mode is disabled
            await CleanupDemoDataAsync(stoppingToken);
            return;
        }

        _logger.LogInformation(
            "Starting demo data generation with {IntervalMinutes} minute intervals",
            _config.IntervalMinutes
        );

        // Generate historical data first (if not already done)
        if (!_historicalDataGenerated)
        {
            await GenerateHistoricalDataAsync(stoppingToken);
            _historicalDataGenerated = true;
        }

        // Generate initial entry immediately
        await GenerateAndSaveEntryAsync(stoppingToken);

        // Set up timer for regular generation
        var interval = TimeSpan.FromMinutes(_config.IntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await GenerateAndSaveEntryAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Demo data generation service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating demo data");
                // Continue running even if one generation fails
            }
        }
    }

    private async Task GenerateHistoricalDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var demoDataService = scope.ServiceProvider.GetRequiredService<IDemoDataService>();
        var entryService = scope.ServiceProvider.GetRequiredService<IEntryService>();
        var treatmentService = scope.ServiceProvider.GetRequiredService<ITreatmentService>();

        try
        {
            // First, check if we already have demo data
            var existingEntries = await entryService.GetEntriesAsync(
                find: "{\"is_demo\":true}",
                count: 1,
                cancellationToken: cancellationToken
            );

            if (existingEntries.Any())
            {
                _logger.LogInformation(
                    "Demo data already exists, skipping historical data generation"
                );
                return;
            }

            _logger.LogInformation(
                "Generating {Months} months of historical demo data...",
                _config.HistoryMonths
            );

            var startTime = DateTime.UtcNow;

            // Generate the data
            var (entries, treatments) = demoDataService.GenerateHistoricalData();

            _logger.LogInformation(
                "Generated {Entries} entries and {Treatments} treatments, saving to database...",
                entries.Count,
                treatments.Count
            );

            // Save in batches to avoid memory issues and provide progress
            const int batchSize = 5000;

            // Save entries in batches
            for (var i = 0; i < entries.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = entries.Skip(i).Take(batchSize).ToList();
                await entryService.CreateEntriesAsync(batch, cancellationToken);

                var progress = Math.Min(100, (i + batchSize) * 100 / entries.Count);
                _logger.LogDebug("Entry save progress: {Progress}%", progress);
            }

            // Save treatments in batches
            for (var i = 0; i < treatments.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = treatments.Skip(i).Take(batchSize).ToList();
                await treatmentService.CreateTreatmentsAsync(batch, cancellationToken);

                var progress = Math.Min(100, (i + batchSize) * 100 / treatments.Count);
                _logger.LogDebug("Treatment save progress: {Progress}%", progress);
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Completed historical demo data generation: {Entries} entries, {Treatments} treatments in {Duration}",
                entries.Count,
                treatments.Count,
                duration
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate historical demo data");
            throw;
        }
    }

    private async Task CleanupDemoDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        try
        {
            var postgreSqlService = scope.ServiceProvider.GetRequiredService<IPostgreSqlService>();

            _logger.LogInformation("Cleaning up demo data...");

            var entriesDeleted = await postgreSqlService.DeleteDemoEntriesAsync(cancellationToken);
            var treatmentsDeleted = await postgreSqlService.DeleteDemoTreatmentsAsync(
                cancellationToken
            );

            _logger.LogInformation(
                "Demo data cleanup complete: {Entries} entries and {Treatments} treatments deleted",
                entriesDeleted,
                treatmentsDeleted
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup demo data");
        }
    }

    private async Task GenerateAndSaveEntryAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var demoDataService = scope.ServiceProvider.GetRequiredService<IDemoDataService>();
        var entryService = scope.ServiceProvider.GetRequiredService<IEntryService>();
        var broadcastService = scope.ServiceProvider.GetRequiredService<ISignalRBroadcastService>();

        try
        {
            var entry = await demoDataService.GenerateEntryAsync(cancellationToken);

            _logger.LogInformation(
                "Demo data: Generated entry SGV={Sgv}, Direction={Direction}, IsDemo={IsDemo}",
                entry.Sgv,
                entry.Direction,
                entry.IsDemo
            );

            await entryService.CreateEntriesAsync(new[] { entry }, cancellationToken);

            _logger.LogInformation(
                "Demo data: Entry saved to database and should trigger WebSocket broadcasts"
            );

            // Additional diagnostic: Try direct broadcast to verify SignalR is working
            try
            {
                await broadcastService.BroadcastDataUpdateAsync(new[] { entry });
                _logger.LogInformation("Demo data: Direct dataUpdate broadcast sent successfully");
            }
            catch (Exception broadcastEx)
            {
                _logger.LogError(
                    broadcastEx,
                    "Demo data: Direct dataUpdate broadcast failed - WebSocket bridge may not be receiving events"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate and save demo entry");
            throw;
        }
    }
}
