using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueBeard.Effects.Patterns;

public class CirclePattern : IEffectPattern
{
    public float Radius { get; }
    public int PointCount { get; }

    public CirclePattern(float radius, int pointCount)
    {
        if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
        if (pointCount <= 0) throw new ArgumentOutOfRangeException(nameof(pointCount));
        Radius = radius;
        PointCount = pointCount;
    }

    public IEnumerable<Vector3> GetPoints()
    {
        var step = 2f * Mathf.PI / PointCount;
        for (var i = 0; i < PointCount; i++)
        {
            var angle = step * i;
            yield return new Vector3(Mathf.Cos(angle) * Radius, 0f, Mathf.Sin(angle) * Radius);
        }
    }
}
