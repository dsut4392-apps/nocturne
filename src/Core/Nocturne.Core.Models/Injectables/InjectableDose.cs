using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Injectables;

/// <summary>
/// Represents a record of an administered injection dose.
/// </summary>
public class InjectableDose
{
    /// <summary>
    /// Gets or sets the unique identifier for this dose record.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the reference to the injectable medication that was administered.
    /// </summary>
    [JsonPropertyName("injectableMedicationId")]
    public Guid InjectableMedicationId { get; set; }

    /// <summary>
    /// Gets or sets the amount administered (units or mg based on medication's UnitType).
    /// </summary>
    [JsonPropertyName("units")]
    public double Units { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the dose was administered in Unix milliseconds.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the optional injection site where the dose was administered.
    /// </summary>
    [JsonPropertyName("injectionSite")]
    public InjectionSite? InjectionSite { get; set; }

    /// <summary>
    /// Gets or sets the optional reference to the pen/vial used for this dose.
    /// </summary>
    [JsonPropertyName("penVialId")]
    public Guid? PenVialId { get; set; }

    /// <summary>
    /// Gets or sets the optional lot number for tracking purposes.
    /// </summary>
    [JsonPropertyName("lotNumber")]
    public string? LotNumber { get; set; }

    /// <summary>
    /// Gets or sets optional notes about this dose.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets who entered this dose record (e.g., "user", "caregiver", "imported").
    /// </summary>
    [JsonPropertyName("enteredBy")]
    public string? EnteredBy { get; set; }

    /// <summary>
    /// Gets or sets the origin system if this dose was imported.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the original identifier from the source system for migration compatibility.
    /// </summary>
    [JsonPropertyName("originalId")]
    public string? OriginalId { get; set; }
}
