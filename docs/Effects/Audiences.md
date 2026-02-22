# Audiences

Audiences control which players receive effect packets. Each audience type returns a set of `ITransportConnection` instances that the emitter sends the effect to via `EffectManager.sendEffectReliable`.

## The IEffectAudience Interface

```csharp
namespace BlueBeard.Effects.Audiences;

public interface IEffectAudience
{
    IEnumerable<ITransportConnection> GetRecipients();
}
```

`GetRecipients()` is called on every emit cycle (every iteration of the coroutine loop). This means audience membership is evaluated dynamically -- players who join, leave, or move in and out of range will be picked up on the next cycle.

---

## Built-in Audiences

### AllPlayersAudience

Sends the effect to every player currently connected to the server.

```csharp
namespace BlueBeard.Effects.Audiences;

public class AllPlayersAudience : IEffectAudience
{
    public IEnumerable<ITransportConnection> GetRecipients() =>
        Provider.clients.Select(client => client.transportConnection);
}
```

**Usage:**

```csharp
var audience = new AllPlayersAudience();
```

This iterates `Provider.clients` on every call, so it automatically includes players who connect after the emitter starts and excludes players who have disconnected.

Best for global events -- airdrops, server-wide announcements, map-wide weather effects.

---

### SinglePlayerAudience

Sends the effect to exactly one player.

```csharp
namespace BlueBeard.Effects.Audiences;

public class SinglePlayerAudience(Player player) : IEffectAudience
{
    public IEnumerable<ITransportConnection> GetRecipients()
    {
        if (player != null && player.channel != null && player.channel.owner != null)
            yield return player.channel.owner.transportConnection;
    }
}
```

**Usage:**

```csharp
var audience = new SinglePlayerAudience(player);
```

The constructor takes an Unturned `Player` reference. The null checks ensure the audience gracefully yields nothing if the player disconnects -- the emitter will simply send zero packets on that cycle.

Best for player-specific feedback -- hit confirmations, personal buff indicators, UI-linked effects.

---

### PlayerGroupAudience

Sends the effect to a filtered subset of connected players, selected by a predicate function.

```csharp
namespace BlueBeard.Effects.Audiences;

public class PlayerGroupAudience(Func<SteamPlayer, bool> predicate) : IEffectAudience
{
    private readonly Func<SteamPlayer, bool> _predicate =
        predicate ?? throw new ArgumentNullException(nameof(predicate));

    public IEnumerable<ITransportConnection> GetRecipients() =>
        from client in Provider.clients where _predicate(client) select client.transportConnection;
}
```

**Usage:**

```csharp
// Players on a specific group
var audience = new PlayerGroupAudience(client => client.player.quests.groupID.IsValid());

// Players who are admins
var audience = new PlayerGroupAudience(client => client.isAdmin);
```

**Validation:** Throws `ArgumentNullException` if the predicate is null.

The predicate receives a `SteamPlayer` (Unturned's server-side player wrapper) and returns `true` to include that player. Because `GetRecipients()` re-evaluates the predicate against `Provider.clients` on every cycle, the audience membership updates dynamically.

Best for team-specific effects, group markers, conditional visibility.

---

## Creating a Custom Audience

Implement `IEffectAudience` and return transport connections from `GetRecipients()`.

### Example: Nearby Players Within Range

```csharp
using System.Collections.Generic;
using System.Linq;
using SDG.NetTransport;
using SDG.Unturned;
using UnityEngine;
using BlueBeard.Effects.Audiences;

public class NearbyPlayersAudience : IEffectAudience
{
    private readonly Vector3 _center;
    private readonly float _radiusSqr;

    public NearbyPlayersAudience(Vector3 center, float radius)
    {
        _center = center;
        _radiusSqr = radius * radius;
    }

    public IEnumerable<ITransportConnection> GetRecipients()
    {
        return from client in Provider.clients
               let position = client.player.transform.position
               where (position - _center).sqrMagnitude <= _radiusSqr
               select client.transportConnection;
    }
}
```

### Guidelines for Custom Audiences

1. **Always return `ITransportConnection` instances.** The emitter passes them directly to `EffectManager.sendEffectReliable`.
2. **Handle disconnections gracefully.** Players can leave at any time. Null-check player references or rely on `Provider.clients` which only contains connected players.
3. **Re-evaluate on each call.** `GetRecipients()` is called on every emit cycle, so you can safely use dynamic conditions (distance checks, team membership, etc.) without worrying about stale data.
4. **Avoid expensive operations.** The method runs on the main thread inside a coroutine. Keep iteration simple. If you need spatial queries, use squared-distance comparisons (as shown above) rather than `Vector3.Distance` to avoid the square root.
