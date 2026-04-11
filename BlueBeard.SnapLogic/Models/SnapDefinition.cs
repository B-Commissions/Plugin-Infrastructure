using System.Collections.Generic;

namespace BlueBeard.SnapLogic.Models;

/// <summary>
/// Declares a barricade type as a snap host with its available attachment points.
/// </summary>
public class SnapDefinition
{
    /// <summary>
    /// Unique identifier for this snap definition.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The Unturned asset ID of the host barricade.
    /// </summary>
    public ushort HostAssetId { get; set; }

    /// <summary>
    /// Maximum distance from the host origin for snap detection.
    /// </summary>
    public float SnapRadius { get; set; } = 2.0f;

    /// <summary>
    /// The attachment points available on this host barricade.
    /// </summary>
    public List<SnapPoint> SnapPoints { get; set; } = [];
}
