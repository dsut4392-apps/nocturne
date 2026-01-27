using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Injectables;

/// <summary>
/// Represents an insulin pen or vial in inventory for tracking purposes.
/// </summary>
public class PenVial
{
    /// <summary>
    /// Gets or sets the unique identifier for this pen/vial.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the reference to the injectable medication this pen/vial contains.
    /// </summary>
    [JsonPropertyName("injectableMedicationId")]
    public Guid InjectableMedicationId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this pen/vial was opened in Unix milliseconds.
    /// </summary>
    [JsonPropertyName("openedAt")]
    public long? OpenedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiration timestamp in Unix milliseconds.
    /// Typically calculated as opened date plus 28 days for most insulins.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public long? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the initial units in this pen/vial (e.g., 300u pen, 1000u vial).
    /// </summary>
    [JsonPropertyName("initialUnits")]
    public double? InitialUnits { get; set; }

    /// <summary>
    /// Gets or sets the remaining units in this pen/vial.
    /// Decremented when doses are logged.
    /// </summary>
    [JsonPropertyName("remainingUnits")]
    public double? RemainingUnits { get; set; }

    /// <summary>
    /// Gets or sets the lot number for tracking purposes.
    /// </summary>
    [JsonPropertyName("lotNumber")]
    public string? LotNumber { get; set; }

    /// <summary>
    /// Gets or sets the current status of this pen/vial.
    /// </summary>
    [JsonPropertyName("status")]
    public PenVialStatus Status { get; set; }

    /// <summary>
    /// Gets or sets optional notes about this pen/vial.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether this pen/vial is archived (soft delete).
    /// </summary>
    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; set; }
}
