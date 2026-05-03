using UnityEngine;

namespace BlueBeard.Zones.Shapes;

public class PolygonZoneShape(Vector3[] worldPoints, float height) : IZoneShape
{
    public string ShapeType => "polygon";
    public Vector3[] WorldPoints => worldPoints;
    public float Height => height;

    public void ApplyCollider(GameObject gameObject)
    {
        var center = gameObject.transform.position;
        var localPoints = new Vector2[worldPoints.Length];
        for (var i = 0; i < worldPoints.Length; i++)
            localPoints[i] = new Vector2(worldPoints[i].x - center.x, worldPoints[i].z - center.z);

        var mesh = PolygonMeshBuilder.Build(localPoints, height);
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        var meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;
        meshCollider.isTrigger = true;
    }
}
