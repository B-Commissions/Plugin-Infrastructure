using UnityEngine;

namespace BlueBeard.Zones.Shapes;

public class RadiusZoneShape : IZoneShape
{
    private readonly float _radius;
    private readonly float _height;

    public string ShapeType => "radius";
    public float Radius => _radius;
    public float Height => _height;

    public RadiusZoneShape(float radius, float height) { _radius = radius; _height = height; }

    public void ApplyCollider(GameObject gameObject)
    {
        var collider = gameObject.AddComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.radius = _radius;
        collider.height = _height;
    }
}
