using UnityEngine;

namespace BlueBeard.SnapLogic.Models;

/// <summary>
/// Defines a single attachment location on a host barricade where a child barricade can snap to.
/// </summary>
public class SnapPoint
{
    /// <summary>
    /// Unique name identifying this snap point on the host (e.g. "slot_1", "left_hook").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Position offset relative to the host barricade's origin.
    /// </summary>
    public Vector3 PositionOffset { get; set; }

    /// <summary>
    /// Asset IDs of barricades that can snap to this point.
    /// Empty or null means any barricade is accepted.
    /// </summary>
    public ushort[] AcceptedAssetIds { get; set; }

    /// <summary>
    /// Returns true if the given asset ID is accepted by this snap point.
    /// </summary>
    public bool Accepts(ushort assetId)
    {
        return AcceptedAssetIds == null || AcceptedAssetIds.Length == 0 ||
               System.Array.IndexOf(AcceptedAssetIds, assetId) >= 0;
    }
}
