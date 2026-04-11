# Cooldown Manager Reference

## CooldownManager

In-memory cooldown tracker. All operations are synchronous and thread-affine -- call from the main thread unless you add your own synchronisation.

### Construction

```csharp
var mgr = new CooldownManager();                     // DateTime.UtcNow
var mgr = new CooldownManager(() => DateTime.UtcNow); // explicit clock
var mgr = new CooldownManager(TestClock.Read);       // deterministic test clock
```

### Lifecycle

`CooldownManager` implements `BlueBeard.Core.IManager`. `Load` is currently a no-op; `Unload` clears all tracked cooldowns.

### Methods

| Signature | Notes |
|-----------|-------|
| `void Start(string key, float seconds)` | Writes `UtcNow + seconds` as the expiry. Overwrites any existing entry. |
| `void Start(string key, TimeSpan duration)` | Same as above with an explicit `TimeSpan`. |
| `bool IsActive(string key)` | Returns true if the key exists and the expiry is in the future. If the entry is expired, it is removed lazily and `false` is returned. |
| `float GetRemaining(string key)` | Seconds until expiry, or 0 if the key is missing / expired. Expired entries are removed lazily. |
| `bool TryUse(string key, float seconds)` | Atomic: returns `false` if `IsActive(key)`, otherwise calls `Start(key, seconds)` and returns `true`. |
| `void Cancel(string key)` | Removes a single key from the dictionary. |
| `void CancelByPrefix(string prefix)` | Removes every key whose name starts with `prefix` (ordinal comparison). |
| `int Count` | Diagnostics -- number of currently tracked cooldowns. |
| `protected List<string> GetKeysSnapshot()` | Subclass hook; returns a copy of current keys for iteration without exposing the dictionary. |

### Threading

`CooldownManager` is not inherently thread-safe. All access should be from the main Unturned thread. If you must call from a background worker (e.g. a database callback), wrap the call in `ThreadHelper.RunSynchronously`.

### Lazy cleanup caveat

Entries are only freed when `IsActive` or `GetRemaining` is called for their key. If you create thousands of one-shot cooldowns that are never checked again, memory usage grows. Use `CancelByPrefix` to bulk-clear old domains when that becomes a concern.

### Subclassing

`CooldownManager` is designed for inheritance: every mutating method is `virtual` so subclasses can layer behaviour on top. The persistence variant (`PersistentCooldownManager`) uses this pattern to mirror mutations to a MySQL table. See [Persistence](Persistence.md).

## Key conventions

The manager treats keys as opaque strings, but a consistent naming scheme makes debugging and `CancelByPrefix` much easier.

| Pattern | Example | Use case |
|---------|---------|----------|
| `{domain}.{steamId}` | `dash.76561198012345678` | Per-player, single-purpose cooldown |
| `{domain}.{steamId}.{sub}` | `ability.76561198012345678.heal` | Per-player with multiple ability slots |
| `{domain}.{entityId}` | `shop.restock.12345` | Per-entity (shop, zone, event) |
| `{domain}.global` | `event.raid.global` | Server-wide |

For bulk clearing, pick prefixes that match what you want to cancel:

```csharp
cooldowns.CancelByPrefix($"ability.{steamId}.");  // all ability slots for one player
cooldowns.CancelByPrefix("shop.restock.");         // every shop's restock
cooldowns.CancelByPrefix("event.");                // every global event
```
