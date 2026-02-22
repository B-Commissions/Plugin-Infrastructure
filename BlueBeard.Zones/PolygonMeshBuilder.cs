using UnityEngine;

namespace BlueBeard.Zones;

public static class PolygonMeshBuilder
{
    public static Mesh Build(Vector2[] localPoints, float height)
    {
        var n = localPoints.Length;
        var halfHeight = height / 2f;
        var vertices = new Vector3[6 * n];
        var triangles = new int[(n - 2) * 6 + n * 6];

        for (var i = 0; i < n; i++)
            vertices[i] = new Vector3(localPoints[i].x, -halfHeight, localPoints[i].y);
        for (var i = 0; i < n; i++)
            vertices[n + i] = new Vector3(localPoints[i].x, halfHeight, localPoints[i].y);
        for (var i = 0; i < n; i++)
        {
            var next = (i + 1) % n;
            var baseIndex = 2 * n + 4 * i;
            vertices[baseIndex] = new Vector3(localPoints[i].x, -halfHeight, localPoints[i].y);
            vertices[baseIndex + 1] = new Vector3(localPoints[next].x, -halfHeight, localPoints[next].y);
            vertices[baseIndex + 2] = new Vector3(localPoints[i].x, halfHeight, localPoints[i].y);
            vertices[baseIndex + 3] = new Vector3(localPoints[next].x, halfHeight, localPoints[next].y);
        }

        var tri = 0;
        for (var i = 1; i < n - 1; i++) { triangles[tri++] = 0; triangles[tri++] = i + 1; triangles[tri++] = i; }
        for (var i = 1; i < n - 1; i++) { triangles[tri++] = n; triangles[tri++] = n + i; triangles[tri++] = n + i + 1; }
        for (var i = 0; i < n; i++)
        {
            var baseIndex = 2 * n + 4 * i;
            triangles[tri++] = baseIndex; triangles[tri++] = baseIndex + 2; triangles[tri++] = baseIndex + 1;
            triangles[tri++] = baseIndex + 1; triangles[tri++] = baseIndex + 2; triangles[tri++] = baseIndex + 3;
        }

        var mesh = new Mesh { vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
