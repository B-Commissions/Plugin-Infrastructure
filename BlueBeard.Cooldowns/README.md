# BlueBeard.Cooldowns

Centralised cooldown / timer tracking for Unturned plugins. In-memory by default, with an optional MySQL-backed variant for cooldowns that must survive server restarts.

## Features

- `CooldownManager` — synchronous, in-memory, lazy-cleanup
- `TryUse(key, duration)` — atomic check-and-start for "once per N seconds" gameplay rules
- `CancelByPrefix` — bulk clear by domain or entity
- Clock injection via constructor for deterministic tests
- `PersistentCooldownManager` — same API plus automatic MySQL persistence via `BlueBeard.Database`

## Quick example

```csharp
var cooldowns = new CooldownManager();
cooldowns.Load();

// Trigger an ability only if the player isn't already on cooldown:
var key = $"dash.{player.CSteamID.m_SteamID}";
if (cooldowns.TryUse(key, 8f))
    GrantDash(player);
else
    UnturnedChat.Say(player, $"Dash on cooldown ({cooldowns.GetRemaining(key):F1}s)");
```

## Persistent variant

```csharp
db.RegisterEntity<BBCooldownRow>();
db.Load();

var cooldowns = new PersistentCooldownManager();
cooldowns.Initialize(db);
cooldowns.Load();   // loads unexpired rows back into memory
```

Cooldowns set via `Start` / `TryUse` are then written to the `bb_cooldowns` table asynchronously and survive restarts.

See `docs/Cooldowns/` in the Infrastructure repo for full reference.
