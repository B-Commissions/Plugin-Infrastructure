using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.Core.Helpers;

public class SurfaceHelper
{
    private const float RaycastOriginHeight = 1024f;
    private const float RaycastMaxDistance = 2048f;
    private static readonly int SurfaceLayerMask =
        RayMasks.GROUND | RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.ENVIRONMENT;

    public static Vector3 SnapPositionToSurface(Vector3 position, int? layerMask = null)
    {
        var rayOrigin = new Vector3(position.x, RaycastOriginHeight, position.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, RaycastMaxDistance, layerMask ?? SurfaceLayerMask))
            return hit.point;
        return position;
    }
}
