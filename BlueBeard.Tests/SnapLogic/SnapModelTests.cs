using BlueBeard.SnapLogic.Models;
using UnityEngine;
using Xunit;

namespace BlueBeard.Tests.SnapLogic;

public class SnapPointTests
{
    [Fact]
    public void Null_Or_Empty_Accept_List_Accepts_Anything()
    {
        Assert.True(new SnapPoint { Name = "a" }.Accepts(123));
        Assert.True(new SnapPoint { Name = "a", AcceptedAssetIds = [] }.Accepts(123));
    }

    [Fact]
    public void Accept_List_Filters_By_Asset_Id()
    {
        var point = new SnapPoint { Name = "a", AcceptedAssetIds = [100, 200] };
        Assert.True(point.Accepts(100));
        Assert.True(point.Accepts(200));
        Assert.False(point.Accepts(300));
    }
}

public class SnapHostTests
{
    private static SnapHost Host(params SnapPoint[] points) => new()
    {
        DefinitionId = "rack",
        HostInstanceId = 1,
        SnapPoints = [.. points]
    };

    [Fact]
    public void FindAvailablePoint_Skips_Occupied_Slots()
    {
        var host = Host(
            new SnapPoint { Name = "slot1" },
            new SnapPoint { Name = "slot2" });
        host.Attachments["slot1"] = new SnapAttachment { PointName = "slot1", InstanceId = 42 };

        var found = host.FindAvailablePoint(999);

        Assert.NotNull(found);
        Assert.Equal("slot2", found.Name);
    }

    [Fact]
    public void FindAvailablePoint_Honours_Accept_Filters()
    {
        var host = Host(
            new SnapPoint { Name = "guns", AcceptedAssetIds = [100] },
            new SnapPoint { Name = "melee", AcceptedAssetIds = [200] });

        Assert.Equal("melee", host.FindAvailablePoint(200)?.Name);
        Assert.Null(host.FindAvailablePoint(300));
    }

    [Fact]
    public void IsFull_And_AvailablePoints_Track_Occupancy()
    {
        var host = Host(new SnapPoint { Name = "only" });

        Assert.False(host.IsFull);
        Assert.Equal(1, host.AvailablePoints);

        host.Attachments["only"] = new SnapAttachment { PointName = "only", InstanceId = 7 };

        Assert.True(host.IsFull);
        Assert.Equal(0, host.AvailablePoints);
        Assert.Null(host.FindAvailablePoint(1));
    }
}
