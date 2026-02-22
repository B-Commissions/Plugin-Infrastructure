using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueBeard.Effects.Patterns;

public class SquarePattern : IEffectPattern
{
    public float Size { get; }
    public int PointsPerSide { get; }

    public SquarePattern(float size, int pointsPerSide)
    {
        if (size <= 0f) throw new ArgumentOutOfRangeException(nameof(size));
        if (pointsPerSide <= 0) throw new ArgumentOutOfRangeException(nameof(pointsPerSide));
        Size = size;
        PointsPerSide = pointsPerSide;
    }

    public IEnumerable<Vector3> GetPoints()
    {
        var half = Size / 2f;
        var step = Size / PointsPerSide;
        for (var i = 0; i < PointsPerSide; i++) yield return new Vector3(-half + step * i, 0f, half);
        for (var i = 0; i < PointsPerSide; i++) yield return new Vector3(half, 0f, half - step * i);
        for (var i = 0; i < PointsPerSide; i++) yield return new Vector3(half - step * i, 0f, -half);
        for (var i = 0; i < PointsPerSide; i++) yield return new Vector3(-half, 0f, -half + step * i);
    }
}
