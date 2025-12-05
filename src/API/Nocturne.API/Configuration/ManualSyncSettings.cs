namespace Nocturne.API.Configuration;

/// <summary>
/// Configuration options for manual data synchronization
/// </summary>
public class ManualSyncSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Parameters:Connectors:Settings";

    /// <summary>
    /// Gets or sets the number of days to look back when performing a manual sync.
    /// If null or not set, manual sync feature will not be available.
    /// </summary>
    public int? ManualSyncLookbackDays { get; set; }

    /// <summary>
    /// Gets whether manual sync is configured and available
    /// </summary>
    public bool IsEnabled => ManualSyncLookbackDays.HasValue && ManualSyncLookbackDays.Value > 0;
}
