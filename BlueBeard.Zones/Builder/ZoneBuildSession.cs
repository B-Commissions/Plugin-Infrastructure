using System.Collections.Generic;
using UnityEngine;

namespace BlueBeard.Zones.Builder;

public class ZoneBuildSession
{
    public string ZoneId { get; set; }
    public List<Vector3> Nodes { get; } = new();
    public float Height { get; set; } = 30f;
}
