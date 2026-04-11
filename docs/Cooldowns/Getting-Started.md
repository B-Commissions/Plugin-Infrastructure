# Getting Started

## Installation

Add a project reference in your plugin's `.csproj`:

```xml
<ProjectReference Include="..\BlueBeard.Cooldowns\BlueBeard.Cooldowns.csproj" />
```

BlueBeard.Cooldowns depends on `BlueBeard.Core` and `BlueBeard.Database`. The Database reference is only needed if you use `PersistentCooldownManager`; it's pulled in anyway so both variants are available from a single package.

## Core Concepts

### Key
An arbitrary string the caller chooses. The manager stores the expiry timestamp against this key. Recommended convention is `{domain}.{entityId}`:

- `hotwire.76561198012345678` -- a hotwire cooldown for a specific Steam ID
- `shop.restock.12345` -- restock cooldown for shop id 12345
- `event.raid.global` -- a server-wide raid event cooldown

The key namespace is entirely under your control; the manager just matches strings.

### Expiry
Every key maps to a `DateTime` (UTC). `IsActive` returns true while `UtcNow < expiry`.

### Lazy cleanup
Expired entries are never removed by a background sweep. They are dropped when the next `IsActive` or `GetRemaining` call discovers them. For bulk cleanup, call `CancelByPrefix`.

## Basic Setup

```csharp
using BlueBeard.Cooldowns;

public class MyPlugin : RocketPlugin
{
    public static CooldownManager Cooldowns { get; private set; }

    protected override void Load()
    {
        Cooldowns = new CooldownManager();
        Cooldowns.Load();
    }

    protected override void Unload()
    {
        Cooldowns.Unload();
    }
}
```

## The TryUse Pattern

The most common cooldown pattern is "allow the action if not on cooldown, otherwise reject". `TryUse` wraps this in a single atomic call:

```csharp
public void Dash(UnturnedPlayer player)
{
    var key = $"dash.{player.CSteamID.m_SteamID}";
    if (!MyPlugin.Cooldowns.TryUse(key, 8f))
    {
        var remaining = MyPlugin.Cooldowns.GetRemaining(key);
        UnturnedChat.Say(player, $"Dash on cooldown ({remaining:F1}s)");
        return;
    }

    GrantDash(player);
}
```

Without `TryUse` you would need a `IsActive` check followed by a `Start` call, which is not atomic and introduces a small race window in multi-threaded call paths.

## Tests with Injected Clock

Pass a `Func<DateTime>` to the constructor so tests can advance time without sleeping:

```csharp
var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var mgr = new CooldownManager(() => now);
mgr.Start("a", 5f);
now = now.AddSeconds(10);
Assert.False(mgr.IsActive("a"));
```

## Quick Reference

| Method | Purpose |
|--------|---------|
| `Start(key, seconds)` | Set or overwrite a cooldown |
| `Start(key, timespan)` | Set or overwrite a cooldown |
| `IsActive(key)` | True if the key exists and has not expired (removes the key if expired) |
| `GetRemaining(key)` | Seconds remaining, or 0 if expired / not found |
| `TryUse(key, seconds)` | Atomic check-and-start; returns true on first call, false while active |
| `Cancel(key)` | Remove a single key immediately |
| `CancelByPrefix(prefix)` | Remove every key whose name starts with `prefix` |
| `Count` | Diagnostics -- current number of tracked cooldowns |
