using UnityEngine;

namespace BlueBeard.Zones.Shapes;

public class PolygonZoneShape : IZoneShape
{
    private readonly Vector3[] _worldPoints;
    private readonly float _height;

    public string ShapeType => "polygon";
    public Vector3[] WorldPoints => _worldPoints;
    public float Height => _height;

    public PolygonZoneShape(Vector3[] worldPoints, float height) { _worldPoints = worldPoints; _height = height; }

    public void ApplyCollider(GameObject gameObject)
    {
        var center = gameObject.transform.position;
        var localPoints = new Vector2[_worldPoints.Length];
        for (var i = 0; i < _worldPoints.Length; i++)
            localPoints[i] = new Vector2(_worldPoints[i].x - center.x, _worldPoints[i].z - center.z);

        var mesh = PolygonMeshBuilder.Build(localPoints, _height);
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        var meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;
        meshCollider.isTrigger = true;
    }
}
