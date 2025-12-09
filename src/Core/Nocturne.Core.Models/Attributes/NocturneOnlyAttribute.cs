using System;

namespace Nocturne.Core.Models.Attributes;

/// <summary>
/// Attribute to mark properties that are specific to Nocturne and should strictly NOT be included
/// in legacy V1-V3 API responses (even if not null).
/// These properties will only be serialized for V4+ endpoints.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NocturneOnlyAttribute : Attribute
{
}
