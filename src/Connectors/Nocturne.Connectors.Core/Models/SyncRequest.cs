using System;
using System.Collections.Generic;

namespace Nocturne.Connectors.Core.Models;

public class SyncRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public List<SyncDataType> DataTypes { get; set; } = new();
}
