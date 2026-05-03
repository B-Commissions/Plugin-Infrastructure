using UnityEngine;

namespace BlueBeard.Zones.Shapes;

public class RadiusZoneShape(float radius, float height) : IZoneShape
{
    public string ShapeType => "radius";
    public float Radius => radius;
    public float Height => height;

    public void ApplyCollider(GameObject gameObject)
    {
        var collider = gameObject.AddComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.radius = radius;
        collider.height = height;
    }
}
