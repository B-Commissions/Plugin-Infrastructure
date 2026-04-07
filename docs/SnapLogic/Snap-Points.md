# Snap Points

## SnapPoint Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Unique name identifying this snap point on the host (e.g. `"slot_1"`, `"left_hook"`). |
| `PositionOffset` | `Vector3` | Position offset relative to the host barricade's origin. Converted to world position using the host's transform. |
| `AcceptedAssetIds` | `ushort[]` | Asset IDs of barricades that can snap to this point. Empty or null means any barricade is accepted. |

## Defining Snap Points

Snap points are defined as part of a `SnapDefinition`:

```csharp
new SnapDefinition
{
    Id = "workbench",
    HostAssetId = 50633,
    SnapRadius = 2.0f,
    SnapPoints = new List<SnapPoint>
    {
        new SnapPoint
        {
            Name = "tool_slot",
            PositionOffset = new Vector3(0.43f, -0.56f, 0.43f),
            AcceptedAssetIds = new ushort[] { 50632, 50634 }
        },
        new SnapPoint
        {
            Name = "material_slot",
            PositionOffset = new Vector3(-0.55f, -0.55f, 0.43f)
            // No AcceptedAssetIds = accepts any barricade
        }
    }
}
```

## Position Offsets

Offsets are relative to the host barricade's local coordinate system. They are converted to world coordinates using:

```csharp
Vector3 worldPos = hostTransform.TransformPoint(snapPoint.PositionOffset);
```

This means offsets are rotation-aware. If the host is rotated, the snap points rotate with it.

### Finding Offsets

Use the debug `/snap dump` command to capture positions:

1. Place a host barricade.
2. Manually position child barricades around it.
3. Look at the host and run `/snap dump`.
4. The generated file contains local offsets you can copy directly into your `SnapPoint` definitions.

## Asset Filtering

Each snap point can filter which barricade types are accepted:

```csharp
// Only specific barricades
new SnapPoint
{
    Name = "weapon_mount",
    PositionOffset = new Vector3(0, 0.5f, 0),
    AcceptedAssetIds = new ushort[] { 1234, 5678 }
}

// Any barricade
new SnapPoint
{
    Name = "open_slot",
    PositionOffset = new Vector3(0, 0.5f, 0)
    // AcceptedAssetIds is null/empty
}
```

The `SnapPoint.Accepts(ushort assetId)` method handles this logic.

## Snap Radius

The `SnapRadius` on `SnapDefinition` determines how far from the host origin a barricade can be placed and still trigger snap detection. If a barricade is placed within this radius, the manager will try to snap it to an available point.

## Automatic vs Manual Snapping

**Automatic:** When `AutoRegisterHosts` is enabled (default), hosts are registered on barricade spawn, and child barricades are automatically snapped when placed nearby.

**Manual:** You can also snap programmatically:

```csharp
var host = snapManager.GetHost(hostInstanceId);
var attachment = snapManager.TrySnap(host, childDrop);           // Nearest available
var attachment = snapManager.TrySnap(host, childDrop, "slot_1"); // Specific point
```
