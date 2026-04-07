using SDG.Unturned;

namespace BlueBeard.SnapLogic.Models;

/// <summary>
/// Record of a child barricade occupying a snap point on a host.
/// </summary>
public class SnapAttachment
{
    /// <summary>
    /// The name of the snap point this child occupies.
    /// </summary>
    public string PointName { get; set; }

    /// <summary>
    /// The Unturned asset ID of the snapped child barricade.
    /// </summary>
    public ushort AssetId { get; set; }

    /// <summary>
    /// The Unturned instance ID of the snapped child barricade.
    /// </summary>
    public uint InstanceId { get; set; }

    /// <summary>
    /// Reference to the placed barricade drop.
    /// </summary>
    public BarricadeDrop Drop { get; set; }
}
