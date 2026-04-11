# Examples

## Dash ability (ephemeral cooldown)

```csharp
public class DashCommand : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        var player = (UnturnedPlayer)caller;
        var key = $"dash.{player.CSteamID.m_SteamID}";

        if (!MyPlugin.Cooldowns.TryUse(key, 8f))
        {
            var remaining = MyPlugin.Cooldowns.GetRemaining(key);
            UnturnedChat.Say(player, $"Dash on cooldown ({remaining:F1}s)");
            return;
        }

        ApplyDashForce(player);
    }
}
```

## Ability bar with multiple slots

```csharp
public enum Ability { Dash, Heal, Shield }

public void Use(UnturnedPlayer player, Ability ability)
{
    var key = $"ability.{player.CSteamID.m_SteamID}.{ability}";
    var duration = ability switch
    {
        Ability.Dash => 8f,
        Ability.Heal => 30f,
        Ability.Shield => 45f,
        _ => 10f,
    };

    if (!MyPlugin.Cooldowns.TryUse(key, duration))
    {
        UnturnedChat.Say(player, $"{ability} on cooldown ({MyPlugin.Cooldowns.GetRemaining(key):F0}s)");
        return;
    }

    Trigger(player, ability);
}

// On respawn, clear every ability cooldown for the player:
public void OnPlayerRespawn(UnturnedPlayer player)
{
    MyPlugin.Cooldowns.CancelByPrefix($"ability.{player.CSteamID.m_SteamID}.");
}
```

## Per-shop restock timer (persistent)

A shop in a persistent world should not restock just because the server restarted. Use `PersistentCooldownManager`:

```csharp
public bool TryRestockShop(int shopId)
{
    var key = $"shop.restock.{shopId}";
    if (!MyPlugin.PersistentCooldowns.TryUse(key, TimeSpan.FromHours(6).TotalSeconds))
        return false;

    ResetShopInventory(shopId);
    return true;
}
```

When the server restarts, `PersistentCooldownManager.Load` pulls the row back into memory and `TryRestockShop` continues to deny restock attempts until the original expiry elapses.

## Global event lockout

Only one raid event can run at a time; after it ends there's a 2-hour cooldown before another can start:

```csharp
private const string RaidKey = "event.raid.global";

public bool CanStartRaid() => !MyPlugin.PersistentCooldowns.IsActive(RaidKey);

public void OnRaidCompleted()
{
    MyPlugin.PersistentCooldowns.Start(RaidKey, TimeSpan.FromHours(2));
}

public void AdminResetRaidLockout()
{
    MyPlugin.PersistentCooldowns.Cancel(RaidKey);
}
```

## Deterministic tests

Inject a controllable clock so the tests don't need `Thread.Sleep`:

```csharp
private sealed class TestClock
{
    public DateTime Now { get; set; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public DateTime Read() => Now;
    public void Advance(TimeSpan amount) => Now += amount;
}

[Fact]
public void Ability_Respects_Cooldown_And_Expires()
{
    var clock = new TestClock();
    var mgr = new CooldownManager(clock.Read);

    Assert.True(mgr.TryUse("dash.player1", 8f));
    Assert.False(mgr.TryUse("dash.player1", 8f));

    clock.Advance(TimeSpan.FromSeconds(10));
    Assert.True(mgr.TryUse("dash.player1", 8f));
}
```
