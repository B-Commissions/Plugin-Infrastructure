using UnityEngine;

namespace BlueBeard.Zones.Shapes;

public interface IZoneShape
{
    string ShapeType { get; }
    void ApplyCollider(GameObject gameObject);
}
