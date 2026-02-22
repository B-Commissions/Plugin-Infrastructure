using System.Collections.Generic;
using UnityEngine;

namespace BlueBeard.Holograms;

public class HologramDefinition
{
    public Vector3 Position { get; set; }
    public float Radius { get; set; } = 15f;
    public float Height { get; set; } = 30f;
    public Dictionary<string, string> Metadata { get; set; }
}
