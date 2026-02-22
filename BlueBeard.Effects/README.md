# BlueBeard.Effects

A managed effect emitter system for Unturned. Spawn visual effects at world positions using spatial patterns and audience targeting, with automatic lifecycle management.

## Installation

Add a project reference:

```xml
<ProjectReference Include="..\BlueBeard.Effects\BlueBeard.Effects.csproj" />
```

## Setup

```csharp
using BlueBeard.Effects;

// In your plugin:
var effectManager = new EffectEmitterManager();
effectManager.Load();

// On unload:
effectManager.Unload(); // stops and destroys all active emitters
```

## Spawning Effects

### One-Shot Effect

Spawn an effect once at a position:

```csharp
using BlueBeard.Effects;
using BlueBeard.Effects.Patterns;
using BlueBeard.Effects.Audiences;

var definition = new EffectDefinition
{
    EffectId = 1234,
    Pattern = new SinglePointPattern(),
    Origin = position,
    OneShot = true,
    SnapToSurface = true
};

effectManager.Start(definition, new AllPlayersAudience());
```

### Repeating Effect

Spawn effects on a loop (e.g., zone border particles):

```csharp
var definition = new EffectDefinition
{
    EffectId = 5678,
    Pattern = new CirclePattern(radius: 15f, pointCount: 20),
    Origin = zoneCenter,
    OneShot = false,
    Interval = 2f, // seconds between each cycle
    SnapToSurface = true
};

var emitter = effectManager.Start(definition, new AllPlayersAudience());

// Stop it later:
effectManager.Stop(emitter);
```

## Patterns

Patterns define _where_ effects are spawned (as offsets from Origin).

| Pattern | Description | Constructor |
|---------|-------------|-------------|
| `SinglePointPattern` | Single effect at the origin | `new SinglePointPattern()` |
| `CirclePattern` | Points evenly spaced around a circle | `new CirclePattern(radius, pointCount)` |
| `SquarePattern` | Points along the edges of a square | `new SquarePattern(size, pointsPerSide)` |
| `ScatterPattern` | Random points within a radius | `ScatterPattern.Random(origin, count, radius, minDistance)` |

### Custom Patterns

Implement `IEffectPattern`:

```csharp
using BlueBeard.Effects.Patterns;

public class CrossPattern : IEffectPattern
{
    private readonly float _size;
    public CrossPattern(float size) { _size = size; }

    public IEnumerable<Vector3> GetPoints()
    {
        yield return new Vector3(-_size, 0, 0);
        yield return new Vector3(_size, 0, 0);
        yield return new Vector3(0, 0, -_size);
        yield return new Vector3(0, 0, _size);
        yield return Vector3.zero;
    }
}
```

## Audiences

Audiences define _who_ sees the effects.

| Audience | Description | Constructor |
|----------|-------------|-------------|
| `AllPlayersAudience` | Every connected player | `new AllPlayersAudience()` |
| `SinglePlayerAudience` | One specific player | `new SinglePlayerAudience(player)` |
| `PlayerGroupAudience` | Filtered subset of players | `new PlayerGroupAudience(predicate)` |

### Custom Audiences

Implement `IEffectAudience`:

```csharp
using BlueBeard.Effects.Audiences;

public class NearbyPlayersAudience : IEffectAudience
{
    private readonly Vector3 _center;
    private readonly float _range;

    public NearbyPlayersAudience(Vector3 center, float range)
    {
        _center = center;
        _range = range;
    }

    public IEnumerable<ITransportConnection> GetRecipients()
    {
        foreach (var client in Provider.clients)
        {
            if (Vector3.Distance(client.player.transform.position, _center) <= _range)
                yield return client.transportConnection;
        }
    }
}
```

## EffectDefinition

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EffectId` | `ushort` | 0 | The Unturned effect asset ID |
| `Pattern` | `IEffectPattern` | null | Spatial pattern for effect positions |
| `Origin` | `Vector3` | zero | World origin; pattern offsets are added to this |
| `Interval` | `float` | 0 | Seconds between cycles (for repeating effects) |
| `SnapToSurface` | `bool` | true | Raycast positions down to the ground |
| `OneShot` | `bool` | false | If true, emit once and auto-cleanup |

## Events

```csharp
// EffectEmitter fires Completed when a one-shot finishes:
emitter.Completed += e => Logger.Log("Effect finished");
```
