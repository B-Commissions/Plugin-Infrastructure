using System;
using BlueBeard.Cooldowns;
using Xunit;

namespace BlueBeard.Tests.Cooldowns;

public class CooldownDiagnosticsTests
{
    [Fact]
    public void Snapshot_Reports_Keys_And_Expiries()
    {
        var now = new DateTime(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc);
        var mgr = new CooldownManager(() => now);
        mgr.Start("a", 30f);
        mgr.Start("b", TimeSpan.FromMinutes(5));

        var snapshot = mgr.Snapshot();

        Assert.Equal(2, snapshot.Count);
        Assert.Equal(now.AddSeconds(30), snapshot["a"]);
        Assert.Equal(now.AddMinutes(5), snapshot["b"]);
    }

    [Fact]
    public void Sweep_Removes_Only_Expired_Entries()
    {
        var now = new DateTime(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc);
        var mgr = new CooldownManager(() => now);
        mgr.Start("short", 10f);
        mgr.Start("long", 300f);

        now = now.AddSeconds(60);

        var removed = mgr.Sweep();

        Assert.Equal(1, removed);
        Assert.Equal(1, mgr.Count);
        Assert.True(mgr.IsActive("long"));
    }

    [Fact]
    public void Sweep_On_Empty_Manager_Is_Zero()
    {
        var mgr = new CooldownManager(() => DateTime.UtcNow);
        Assert.Equal(0, mgr.Sweep());
    }
}
