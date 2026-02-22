# Shapes

Shapes define the physical boundary of a zone using Unity trigger colliders. BlueBeard.Zones includes two built-in shapes and supports custom shapes via the `IZoneShape` interface.

## Built-in Shapes

### RadiusZoneShape

A cylindrical zone defined by a radius and height. Uses a `CapsuleCollider`.

```csharp
var shape = new RadiusZoneShape(radius: 50f, height: 30f);
```

| Property | Type | Description |
|---|---|---|
| `Radius` | `float` | The horizontal radius of the cylinder. |
| `Height` | `float` | The vertical extent of the cylinder. |
| `ShapeType` | `string` | Always `"radius"`. Used for serialization. |

### PolygonZoneShape

An arbitrary polygon extruded to a height. Uses a `MeshCollider` (convex).

```csharp
var points = new[]
{
    new Vector3(0, 0, 0),
    new Vector3(100, 0, 0),
    new Vector3(100, 0, 80),
    new Vector3(50, 0, 120),
    new Vector3(0, 0, 80)
};

var shape = new PolygonZoneShape(points, height: 40f);
```

| Property | Type | Description |
|---|---|---|
| `WorldPoints` | `Vector3[]` | The vertices of the polygon in world coordinates. |
| `Height` | `float` | The vertical extent of the extruded polygon. |
| `ShapeType` | `string` | Always `"polygon"`. Used for serialization. |

**Note:** Unity's `MeshCollider` requires the mesh to be convex when used as a trigger. If your polygon is concave, Unity will approximate it as a convex hull. For best results, use convex polygons.

## IZoneShape Interface

```csharp
public interface IZoneShape
{
    string ShapeType { get; }
    void ApplyCollider(GameObject gameObject);
}
```

| Member | Description |
|---|---|
| `ShapeType` | A unique string identifier used for serialization/deserialization (e.g., `"radius"`, `"polygon"`). |
| `ApplyCollider` | Called once when the zone is created. Attach a trigger collider to the provided GameObject. |

## Custom Shapes

You can create your own shapes by implementing `IZoneShape`:

```csharp
using BlueBeard.Zones.Shapes;
using UnityEngine;

public class BoxZoneShape : IZoneShape
{
    private readonly Vector3 _size;

    public string ShapeType => "box";
    public Vector3 Size => _size;

    public BoxZoneShape(Vector3 size) { _size = size; }

    public void ApplyCollider(GameObject gameObject)
    {
        var collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = _size;
    }
}
```

**Important:** If you create a custom shape and want it to persist through storage, you'll need to extend `ZoneStorageMapper` to handle serialization/deserialization of your shape type. The mapper uses the `ShapeType` string to determine how to reconstruct the shape from stored data.

## How Colliders Work

When a zone is created, the `ZoneManager`:

1. Creates a new `GameObject` at the zone's center position
2. Adds a kinematic `Rigidbody` (required for trigger events)
3. Calls `shape.ApplyCollider(gameObject)` to attach the trigger collider
4. Adds a `ZoneComponent` that listens for `OnTriggerEnter` and `OnTriggerExit`

The `ZoneComponent` detects both players (by the `"Player"` tag) and vehicles (by the `"Vehicle"` tag). For vehicles, events fire for each passenger individually.
