using System.Collections.Generic;
using System.Linq;
using SDG.Unturned;

namespace BlueBeard.SnapLogic.Models;

/// <summary>
/// Runtime state tracking a placed host barricade and its occupied snap points.
/// </summary>
public class SnapHost
{
    /// <summary>
    /// The ID of the <see cref="SnapDefinition"/> this host was registered from.
    /// </summary>
    public string DefinitionId { get; set; }

    /// <summary>
    /// The Unturned instance ID of the host barricade.
    /// </summary>
    public uint HostInstanceId { get; set; }

    /// <summary>
    /// The Steam ID of the barricade owner.
    /// </summary>
    public ulong OwnerId { get; set; }

    /// <summary>
    /// The Steam group ID of the barricade.
    /// </summary>
    public ulong GroupId { get; set; }

    /// <summary>
    /// Reference to the host barricade drop.
    /// </summary>
    public BarricadeDrop HostDrop { get; set; }

    /// <summary>
    /// The snap points available on this host (copied from the definition).
    /// </summary>
    public List<SnapPoint> SnapPoints { get; set; } = new();

    /// <summary>
    /// The snap radius for this host (copied from the definition).
    /// </summary>
    public float SnapRadius { get; set; }

    /// <summary>
    /// Currently occupied snap points, keyed by point name.
    /// </summary>
    public Dictionary<string, SnapAttachment> Attachments { get; set; } = new();

    /// <summary>
    /// Number of unoccupied snap points.
    /// </summary>
    public int AvailablePoints => SnapPoints.Count - Attachments.Count;

    /// <summary>
    /// True if all snap points are occupied.
    /// </summary>
    public bool IsFull => Attachments.Count >= SnapPoints.Count;

    /// <summary>
    /// Returns the first unoccupied snap point that accepts the given asset ID,
    /// or null if none available.
    /// </summary>
    public SnapPoint FindAvailablePoint(ushort assetId)
    {
        return SnapPoints.FirstOrDefault(p => !Attachments.ContainsKey(p.Name) && p.Accepts(assetId));
    }

    /// <summary>
    /// Returns a specific snap point by name, or null if not found.
    /// </summary>
    public SnapPoint GetPoint(string pointName)
    {
        return SnapPoints.FirstOrDefault(p => p.Name == pointName);
    }
}
