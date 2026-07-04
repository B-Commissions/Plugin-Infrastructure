using System;
using System.Linq;
using BlueBeard.Effects.Patterns;
using UnityEngine;
using Xunit;

namespace BlueBeard.Tests.Effects;

public class CirclePatternTests
{
    [Fact]
    public void Produces_Requested_Point_Count_On_Radius()
    {
        var points = new CirclePattern(5f, 8).GetPoints().ToList();

        Assert.Equal(8, points.Count);
        Assert.All(points, p =>
        {
            Assert.Equal(0f, p.y);
            Assert.Equal(5f, new Vector2(p.x, p.z).magnitude, precision: 3);
        });
    }

    [Fact]
    public void Points_Are_Evenly_Spaced()
    {
        var points = new CirclePattern(10f, 4).GetPoints().ToList();

        // 4 points on a circle: consecutive points are 90° apart.
        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            var dot = (a.x * b.x + a.z * b.z) / (100f); // both radius 10
            Assert.Equal(0f, dot, precision: 3);
        }
    }

    [Theory]
    [InlineData(0f, 5)]
    [InlineData(-1f, 5)]
    public void Invalid_Radius_Throws(float radius, int count)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new CirclePattern(radius, count));

    [Fact]
    public void Invalid_Count_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new CirclePattern(5f, 0));
}

public class SquarePatternTests
{
    [Fact]
    public void Produces_Four_Sides_Of_Points_On_The_Perimeter()
    {
        var pattern = new SquarePattern(10f, 5);
        var points = pattern.GetPoints().ToList();

        Assert.Equal(20, points.Count);
        Assert.All(points, p =>
        {
            var onVerticalEdge = Math.Abs(Math.Abs(p.x) - 5f) < 0.001f;
            var onHorizontalEdge = Math.Abs(Math.Abs(p.z) - 5f) < 0.001f;
            Assert.True(onVerticalEdge || onHorizontalEdge,
                $"({p.x}, {p.z}) is not on the square's perimeter");
        });
    }

    [Fact]
    public void Perimeter_Points_Are_Distinct()
    {
        var points = new SquarePattern(8f, 4).GetPoints().ToList();
        Assert.Equal(points.Count, points.Distinct().Count());
    }
}
