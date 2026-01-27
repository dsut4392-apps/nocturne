using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Injectables;

/// <summary>
/// Unit of measurement for injectable medications.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UnitType
{
    /// <summary>
    /// Insulin units
    /// </summary>
    Units,

    /// <summary>
    /// Milligrams (GLP-1 agonists)
    /// </summary>
    Milligrams
}
