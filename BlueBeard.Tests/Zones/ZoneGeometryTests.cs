using BlueBeard.Zones;
using BlueBeard.Zones.Shapes;
using BlueBeard.Zones.Storage;
using BlueBeard.Zones.Tracking;
using System.Collections.Generic;
using UnityEngine;
using Xunit;

namespace BlueBeard.Tests.Zones;

public class PointInPolygonTests
{
    private static readonly Vector3[] Square =
    [
        new(0, 0, 0), new(10, 0, 0), new(10, 0, 10), new(0, 0, 10)
    ];

    // Concave "L" shape: a 10x10 square with the top-right 5x5 quadrant notched out.
    private static readonly Vector3[] LShape =
    [
        new(0, 0, 0), new(10, 0, 0), new(10, 0, 5), new(5, 0, 5), new(5, 0, 10), new(0, 0, 10)
    ];

    [Theory]
    [InlineData(5, 5, true)]    // center
    [InlineData(0.5f, 0.5f, true)]
    [InlineData(9.5f, 9.5f, true)]
    [InlineData(-1, 5, false)]
    [InlineData(11, 5, false)]
    [InlineData(5, -0.5f, false)]
    public void Square_Membership(float x, float z, bool expected)
        => Assert.Equal(expected, PlayerTracker.IsPointInPolygon(new Vector3(x, 0, z), Square));

    [Theory]
    [InlineData(2, 2, true)]     // in the base of the L
    [InlineData(2, 8, true)]     // in the vertical arm
    [InlineData(8, 2, true)]     // in the horizontal arm
    [InlineData(8, 8, false)]    // inside the notch — convex hull would say true
    [InlineData(6, 6, false)]    // just inside the notch
    public void Concave_L_Shape_Respects_The_Notch(float x, float z, bool expected)
        => Assert.Equal(expected, PlayerTracker.IsPointInPolygon(new Vector3(x, 0, z), LShape));
}

public class HeightBoundsTests
{
    private static ZoneDefinition Zone(float centerY, float? lower, float? upper) => new()
    {
        Id = "z",
        Center = new Vector3(0, centerY, 0),
        LowerHeight = lower,
        UpperHeight = upper
    };

    [Fact]
    public void No_Bounds_Always_Inside()
        => Assert.True(PlayerTracker.IsWithinHeightBounds(9999f, Zone(50, null, null)));

    [Theory]
    [InlineData(45, false)]  // below lower band (50 + -2 = 48)
    [InlineData(48, true)]   // exactly at lower bound
    [InlineData(55, true)]
    [InlineData(60, true)]   // exactly at upper bound (50 + 10)
    [InlineData(61, false)]
    public void Band_Is_Inclusive_And_Relative_To_Center(float y, bool expected)
        => Assert.Equal(expected, PlayerTracker.IsWithinHeightBounds(y, Zone(50, -2, 10)));

    [Fact]
    public void Zero_Offset_Is_A_Real_Bound()
    {
        // Regression: 0 used to round-trip as "no bound" through storage.
        var zone = Zone(50, 0, null);
        Assert.False(PlayerTracker.IsWithinHeightBounds(49.9f, zone));
        Assert.True(PlayerTracker.IsWithinHeightBounds(50f, zone));
    }
}

public class PositionInZoneTests
{
    [Fact]
    public void Radius_Zone_Uses_Horizontal_Distance()
    {
        var zone = new ZoneDefinition
        {
            Id = "r",
            Center = new Vector3(100, 50, 100),
            Shape = new RadiusZoneShape(10, 20)
        };

        Assert.True(PlayerTracker.IsPositionInZone(new Vector3(105, 50, 100), zone));
        Assert.True(PlayerTracker.IsPositionInZone(new Vector3(100, 500, 100), zone));  // no height bounds
        Assert.False(PlayerTracker.IsPositionInZone(new Vector3(111, 50, 100), zone));
    }

    [Fact]
    public void Height_Bounds_Apply_To_All_Shapes()
    {
        var zone = new ZoneDefinition
        {
            Id = "r",
            Center = new Vector3(0, 50, 0),
            Shape = new RadiusZoneShape(10, 20),
            LowerHeight = -5,
            UpperHeight = 5
        };

        Assert.True(PlayerTracker.IsPositionInZone(new Vector3(1, 52, 1), zone));
        Assert.False(PlayerTracker.IsPositionInZone(new Vector3(1, 60, 1), zone));
    }
}

public class ZoneStorageMapperTests
{
    [Fact]
    public void Radius_Zone_Round_Trips()
    {
        var original = new ZoneDefinition
        {
            Id = "arena",
            Center = new Vector3(10, 20, 30),
            Shape = new RadiusZoneShape(25, 40),
            Flags = new Dictionary<string, string> { ["noDamage"] = "", ["enterMessage"] = "hi" },
            Metadata = new Dictionary<string, string> { ["owner"] = "jack" },
            LowerHeight = 0,       // legitimate zero bound must survive
            UpperHeight = 12.5f,
            Priority = 7
        };

        var restored = ZoneStorageMapper.ToDefinition(ZoneStorageMapper.ToStorageData(original));

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Center, restored.Center);
        var shape = Assert.IsType<RadiusZoneShape>(restored.Shape);
        Assert.Equal(25, shape.Radius);
        Assert.Equal(40, shape.Height);
        Assert.Equal(original.Flags, restored.Flags);
        Assert.Equal(original.Metadata, restored.Metadata);
        Assert.Equal(0f, restored.LowerHeight);
        Assert.Equal(12.5f, restored.UpperHeight);
        Assert.Equal(7, restored.Priority);
    }

    [Fact]
    public void Polygon_Zone_Round_Trips()
    {
        var points = new[] { new Vector3(0, 0, 0), new Vector3(10, 0, 0), new Vector3(5, 0, 10) };
        var original = new ZoneDefinition
        {
            Id = "tri",
            Center = new Vector3(5, 0, 3),
            Shape = new PolygonZoneShape(points, 15)
        };

        var restored = ZoneStorageMapper.ToDefinition(ZoneStorageMapper.ToStorageData(original));

        var shape = Assert.IsType<PolygonZoneShape>(restored.Shape);
        Assert.Equal(points, shape.WorldPoints);
        Assert.Equal(15, shape.Height);
        Assert.Null(restored.LowerHeight);
        Assert.Null(restored.UpperHeight);
    }
}
