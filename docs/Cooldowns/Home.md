# BlueBeard.Cooldowns

BlueBeard.Cooldowns is a centralised cooldown / timer tracking system for per-player, per-entity, or arbitrary keyed timers. In-memory by default, with an optional database-backed variant for cooldowns that must survive server restarts.

## Features

- **Synchronous in-memory storage** -- Simple `Dictionary<string, DateTime>` keyed by caller-supplied strings.
- **Atomic TryUse** -- `TryUse(key, duration)` is the single-call pattern for "once per N seconds" gameplay rules.
- **Lazy cleanup** -- Expired entries are removed the next time they're accessed; no background sweep.
- **Bulk cancel** -- `CancelByPrefix` clears every matching key for a domain or entity.
- **Clock injection** -- Constructor accepts `Func<DateTime>` for deterministic tests without `Thread.Sleep`.
- **Optional persistence** -- `PersistentCooldownManager` wraps `BlueBeard.Database` to write rows to a `bb_cooldowns` table and replay them on `Load`.
- **Key conventions** -- Caller chooses the key format. Recommended: `{domain}.{entityId}`, e.g. `hotwire.76561198012345678`.

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](Getting-Started.md) | Setup, basic usage, `TryUse` patterns |
| [Cooldown Manager](Cooldown-Manager.md) | Full `CooldownManager` API reference |
| [Persistence](Persistence.md) | Enabling `PersistentCooldownManager` with `BlueBeard.Database` |
| [Examples](Examples.md) | Dash, ability throttles, per-entity unlock timers |

## Source Classes

| Class | Role |
|-------|------|
| `CooldownManager` | In-memory cooldown tracker; `IManager` lifecycle |
| `PersistentCooldownManager` | Extends `CooldownManager` with MySQL persistence |
| `BBCooldownRow` | Row entity for the `bb_cooldowns` table |
