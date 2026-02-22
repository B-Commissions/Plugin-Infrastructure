using System;
using System.Collections.Generic;
using BlueBeard.Core.Helpers;
using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.Effects.Patterns;

public class ScatterPattern : IEffectPattern
{
    private readonly List<Vector3> _offsets;
    public int Count => _offsets.Count;

    public ScatterPattern(Vector3 origin, IEnumerable<Vector3> absolutePositions)
    {
        _offsets = new List<Vector3>();
        foreach (var pos in absolutePositions)
        {
            var snapped = SurfaceHelper.SnapPositionToSurface(pos, RayMasks.GROUND | RayMasks.BARRICADE | RayMasks.STRUCTURE);
            var adjusted = new Vector3(snapped.x, snapped.y + 0.1f, snapped.z);
            _offsets.Add(adjusted - origin);
        }
    }

    public static ScatterPattern Random(Vector3 origin, int count, float radius, float minDistance = 0f)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
        if (minDistance < 0f) throw new ArgumentOutOfRangeException(nameof(minDistance));

        const int maxAttempts = 30;
        var minDistSqr = minDistance * minDistance;
        var positions = new List<Vector3>(count);

        for (var i = 0; i < count; i++)
        {
            var placed = false;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var r = UnityEngine.Random.insideUnitCircle * radius;
                var candidate = origin + new Vector3(r.x, 0f, r.y);
                if (minDistance > 0f && !IsFarEnough(candidate, positions, minDistSqr)) continue;
                positions.Add(candidate);
                placed = true;
                break;
            }
            if (!placed)
            {
                var r = UnityEngine.Random.insideUnitCircle * radius;
                positions.Add(origin + new Vector3(r.x, 0f, r.y));
            }
        }
        return new ScatterPattern(origin, positions);
    }

    private static bool IsFarEnough(Vector3 candidate, List<Vector3> existing, float minDistSqr)
    {
        for (var i = 0; i < existing.Count; i++)
        {
            var dx = candidate.x - existing[i].x;
            var dz = candidate.z - existing[i].z;
            if (dx * dx + dz * dz < minDistSqr) return false;
        }
        return true;
    }

    public IEnumerable<Vector3> GetPoints() => _offsets;
}
