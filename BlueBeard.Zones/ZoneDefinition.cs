using System.Collections.Generic;
using BlueBeard.Zones.Shapes;
using UnityEngine;

namespace BlueBeard.Zones;

public class ZoneDefinition
{
    public string Id { get; set; }
    public Vector3 Center { get; set; }
    public IZoneShape Shape { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public Dictionary<string, string> Flags { get; set; } = new();
    public float? LowerHeight { get; set; }
    public float? UpperHeight { get; set; }
    public int Priority { get; set; }
}
