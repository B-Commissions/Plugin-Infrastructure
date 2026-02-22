# Examples

Complete code examples demonstrating common BlueBeard.Effects usage patterns. All examples assume an `EffectEmitterManager` has been created and loaded:

```csharp
private readonly EffectEmitterManager _effects = new();

// In your plugin load:
_effects.Load();
```

---

## One-Shot Explosion Effect at a Position

Fire a single effect at an exact world position, visible to all players. The emitter automatically stops and cleans itself up after the single emission.

```csharp
using BlueBeard.Effects;
using BlueBeard.Effects.Audiences;
using BlueBeard.Effects.Patterns;
using UnityEngine;

public void SpawnExplosion(Vector3 position)
{
    var definition = new EffectDefinition
    {
        EffectId      = 394,                      // Unturned effect asset ID
        Pattern       = new SinglePointPattern(),  // single point at origin
        Origin        = position,
        SnapToSurface = true,                      // snap to ground
        OneShot       = true                       // fire once, then auto-dispose
    };

    _effects.Start(definition, new AllPlayersAudience());
    // No need to store the return value -- the manager auto-stops one-shot emitters.
}
```

---

## Repeating Circle Effect Around a Zone Border

Show a ring of effects around a zone center that repeats every 3 seconds. This runs indefinitely until you stop it.

```csharp
using BlueBeard.Effects;
using BlueBeard.Effects.Audiences;
using BlueBeard.Effects.Patterns;
using UnityEngine;

private EffectEmitter _zoneBorderEmitter;

public void StartZoneBorder(Vector3 zoneCenter, float zoneRadius)
{
    var definition = new EffectDefinition
    {
        EffectId      = 120,
        Pattern       = new CirclePattern(radius: zoneRadius, pointCount: 24),
        Origin        = zoneCenter,
        SnapToSurface = true,
        OneShot       = false,       // repeating
        Interval      = 3f           // every 3 seconds
    };

    _zoneBorderEmitter = _effects.Start(definition, new AllPlayersAudience());
}

public void StopZoneBorder()
{
    if (_zoneBorderEmitter != null)
    {
        _effects.Stop(_zoneBorderEmitter);
        _zoneBorderEmitter = null;
    }
}
```

---

## Effect Visible to Only One Player

Send a personal effect that only a specific player can see -- useful for hit markers, UI feedback, or private indicators.

```csharp
using BlueBeard.Effects;
using BlueBeard.Effects.Audiences;
using BlueBeard.Effects.Patterns;
using SDG.Unturned;
using UnityEngine;

public void SpawnPlayerEffect(Player player, Vector3 position)
{
    var definition = new EffectDefinition
    {
        EffectId      = 250,
        Pattern       = new SinglePointPattern(),
        Origin        = position,
        SnapToSurface = false,       // keep exact position (e.g., mid-air hit marker)
        OneShot       = true
    };

    var audience = new SinglePlayerAudience(player);
    _effects.Start(definition, audience);
}
```

---

## Scatter Effect for a Debris Field

Spawn random effects across an area. The scatter pattern pre-computes positions with surface snapping and minimum spacing, so the result looks natural.

```csharp
using BlueBeard.Effects;
using BlueBeard.Effects.Audiences;
using BlueBeard.Effects.Patterns;
using UnityEngine;

public void SpawnDebrisField(Vector3 center)
{
    var pattern = ScatterPattern.Random(
        origin:      center,
        count:       12,
        radius:      20f,
        minDistance:  3f       // at least 3 units between points
    );

    var definition = new EffectDefinition
    {
        EffectId      = 305,
        Pattern       = pattern,
        Origin        = center,
        SnapToSurface = true,
        OneShot       = true
    };

    _effects.Start(definition, new AllPlayersAudience());
}
```

---

## Square Perimeter Effect

Outline a rectangular area -- useful for base boundaries, event arenas, or restricted zones.

```csharp
using BlueBeard.Effects;
using BlueBeard.Effects.Audiences;
using BlueBeard.Effects.Patterns;
using UnityEngine;

private EffectEmitter _perimeterEmitter;

public void StartPerimeter(Vector3 center, float size)
{
    var definition = new EffectDefinition
    {
        EffectId      = 180,
        Pattern       = new SquarePattern(size: size, pointsPerSide: 8),  // 32 points total
        Origin        = center,
        SnapToSurface = true,
        OneShot       = false,
        Interval      = 5f
    };

    _perimeterEmitter = _effects.Start(definition, new AllPlayersAudience());
}
```

---

## Custom Pattern: Cross Shape

Create a cross (plus sign) pattern by implementing `IEffectPattern`. This yields points along two perpendicular lines through the origin.

```csharp
using System.Collections.Generic;
using UnityEngine;
using BlueBeard.Effects.Patterns;

public class CrossPattern : IEffectPattern
{
    private readonly float _armLength;
    private readonly int _pointsPerArm;

    public CrossPattern(float armLength, int pointsPerArm)
    {
        _armLength = armLength;
        _pointsPerArm = pointsPerArm;
    }

    public IEnumerable<Vector3> GetPoints()
    {
        var step = _armLength / _pointsPerArm;

        // Horizontal arm (along X axis)
        for (var i = -_pointsPerArm; i <= _pointsPerArm; i++)
        {
            yield return new Vector3(step * i, 0f, 0f);
        }

        // Vertical arm (along Z axis), skip center to avoid duplicate
        for (var i = -_pointsPerArm; i <= _pointsPerArm; i++)
        {
            if (i == 0) continue;
            yield return new Vector3(0f, 0f, step * i);
        }
    }
}
```

**Usage:**

```csharp
var definition = new EffectDefinition
{
    EffectId      = 394,
    Pattern       = new CrossPattern(armLength: 10f, pointsPerArm: 5),
    Origin        = new Vector3(100f, 0f, 200f),
    SnapToSurface = true,
    OneShot       = true
};

_effects.Start(definition, new AllPlayersAudience());
```

---

## Custom Audience: Nearby Players Within Range

Create an audience that dynamically includes only players within a certain distance of a point. Because `GetRecipients()` is called every emit cycle, players who move in or out of range will be included or excluded in real time.

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

**Usage:**

```csharp
var center = new Vector3(100f, 0f, 200f);

var definition = new EffectDefinition
{
    EffectId      = 120,
    Pattern       = new CirclePattern(radius: 15f, pointCount: 16),
    Origin        = center,
    SnapToSurface = true,
    OneShot       = false,
    Interval      = 2f
};

// Only players within 50 units of the center will see the effect
var audience = new NearbyPlayersAudience(center, radius: 50f);
_effects.Start(definition, audience);
```

---

## Combining Patterns and Audiences

You can freely combine any pattern with any audience. Here is a repeating scatter effect visible only to a specific group:

```csharp
using BlueBeard.Effects;
using BlueBeard.Effects.Audiences;
using BlueBeard.Effects.Patterns;
using SDG.Unturned;
using UnityEngine;

public EffectEmitter StartTeamEffect(Vector3 center, CSteamID groupId)
{
    var pattern = ScatterPattern.Random(center, count: 6, radius: 10f, minDistance: 2f);

    var definition = new EffectDefinition
    {
        EffectId      = 200,
        Pattern       = pattern,
        Origin        = center,
        SnapToSurface = true,
        OneShot       = false,
        Interval      = 4f
    };

    var audience = new PlayerGroupAudience(client =>
        client.player.quests.groupID == groupId);

    return _effects.Start(definition, audience);
}
```
