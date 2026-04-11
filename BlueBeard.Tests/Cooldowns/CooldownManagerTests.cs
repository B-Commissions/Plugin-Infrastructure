using System;
using BlueBeard.Cooldowns;
using Xunit;

namespace BlueBeard.Tests.Cooldowns;

public class CooldownManagerTests
{
    /// <summary>A controllable clock the tests can advance deterministically.</summary>
    private sealed class TestClock
    {
        public DateTime Now { get; set; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public DateTime Read() => Now;
        public void Advance(TimeSpan amount) => Now += amount;
    }

    [Fact]
    public void Start_Then_IsActive_Returns_True_Before_Expiry()
    {
        var clock = new TestClock();
        var mgr = new CooldownManager(clock.Read);

        mgr.Start("a", 10f);

        Assert.True(mgr.IsActive("a"));
    }

    [Fact]
    public void IsActive_Returns_False_After_Expiry_And_Removes_Key()
    {
        var clock = new TestClock();
        var mgr = new CooldownManager(clock.Read);

        mgr.Start("a", 10f);
        clock.Advance(TimeSpan.FromSeconds(11));

        Assert.False(mgr.IsActive("a"));
        Assert.Equal(0, mgr.Count);
    }

    [Fact]
    public void GetRemaining_Returns_Correct_Seconds()
    {
        var clock = new TestClock();
        var mgr = new CooldownManager(clock.Read);

        mgr.Start("a", 10f);
        clock.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal(7f, mgr.GetRemaining("a"), precision: 2);
    }

    [Fact]
    public void GetRemaining_Returns_Zero_When_Missing()
    {
        var mgr = new CooldownManager();
        Assert.Equal(0f, mgr.GetRemaining("nope"));
    }

    [Fact]
    public void GetRemaining_Returns_Zero_And_Removes_Expired_Key()
    {
        var clock = new TestClock();
        var mgr = new CooldownManager(clock.Read);

        mgr.Start("a", 5f);
        clock.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(0f, mgr.GetRemaining("a"));
        Assert.Equal(0, mgr.Count);
    }

    [Fact]
    public void TryUse_Returns_True_First_Time_Then_False_While_Active()
    {
        var clock = new TestClock();
        var mgr = new CooldownManager(clock.Read);

        Assert.True(mgr.TryUse("a", 5f));
        Assert.False(mgr.TryUse("a", 5f));
    }

    [Fact]
    public void TryUse_Returns_True_Again_After_Expiry()
    {
        var clock = new TestClock();
        var mgr = new CooldownManager(clock.Read);

        Assert.True(mgr.TryUse("a", 5f));
        clock.Advance(TimeSpan.FromSeconds(6));
        Assert.True(mgr.TryUse("a", 5f));
    }

    [Fact]
    public void Cancel_Immediately_Removes_Key()
    {
        var clock = new TestClock();
        var mgr = new CooldownManager(clock.Read);

        mgr.Start("a", 10f);
        mgr.Cancel("a");

        Assert.False(mgr.IsActive("a"));
    }

    [Fact]
    public void CancelByPrefix_Removes_Only_Matching_Keys()
    {
        var clock = new TestClock();
        var mgr = new CooldownManager(clock.Read);

        mgr.Start("hotwire.123", 10f);
        mgr.Start("hotwire.456", 10f);
        mgr.Start("dash.123", 10f);

        mgr.CancelByPrefix("hotwire.");

        Assert.False(mgr.IsActive("hotwire.123"));
        Assert.False(mgr.IsActive("hotwire.456"));
        Assert.True(mgr.IsActive("dash.123"));
    }

    [Fact]
    public void Overwriting_Existing_Cooldown_Resets_The_Timer()
    {
        var clock = new TestClock();
        var mgr = new CooldownManager(clock.Read);

        mgr.Start("a", 5f);
        clock.Advance(TimeSpan.FromSeconds(3));
        mgr.Start("a", 10f);

        Assert.Equal(10f, mgr.GetRemaining("a"), precision: 2);
    }

    [Fact]
    public void Unload_Clears_All_Cooldowns()
    {
        var mgr = new CooldownManager();
        mgr.Start("a", 10f);
        mgr.Start("b", 10f);

        mgr.Unload();

        Assert.Equal(0, mgr.Count);
    }

    [Fact]
    public void Start_With_TimeSpan_Overload_Works()
    {
        var clock = new TestClock();
        var mgr = new CooldownManager(clock.Read);

        mgr.Start("a", TimeSpan.FromSeconds(15));

        Assert.Equal(15f, mgr.GetRemaining("a"), precision: 2);
    }
}
