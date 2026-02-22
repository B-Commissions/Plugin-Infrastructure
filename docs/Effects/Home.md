# BlueBeard.Effects

BlueBeard.Effects is a managed effect emitter system for Unturned. It provides a clean abstraction over the native `EffectManager.sendEffectReliable` API, letting you spawn visual effects at world positions with fine-grained control over spatial layout and audience targeting.

## Features

- **Spawn visual effects at world positions** -- send any Unturned effect asset to exact coordinates on the map.
- **Spatial patterns** -- arrange effect spawn points using built-in layouts: `SinglePoint`, `Circle`, `Square`, and `Scatter`.
- **Audience targeting** -- control who sees the effect: `AllPlayers`, `SinglePlayer`, or a filtered `PlayerGroup`.
- **Automatic lifecycle management** -- each emitter runs inside a Unity coroutine on its own `GameObject`, so timing and cleanup are handled for you.
- **One-shot and repeating modes** -- fire an effect once and auto-dispose, or repeat on an interval until you stop it.
- **Surface snapping** -- optionally snap effect positions to the ground surface via raycast (enabled by default).

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](Getting-Started.md) | Project setup, creating the manager, starting and stopping emitters. |
| [Patterns](Patterns.md) | Spatial patterns (`SinglePoint`, `Circle`, `Square`, `Scatter`) and how to write your own. |
| [Audiences](Audiences.md) | Audience types (`AllPlayers`, `SinglePlayer`, `PlayerGroup`) and how to write your own. |
| [Examples](Examples.md) | Complete code examples for common scenarios. |

## Architecture at a Glance

```
EffectEmitterManager
  |-- Start(definition, audience) --> creates a GameObject with an EffectEmitter
  |-- Stop(emitter)               --> stops the coroutine and destroys the GameObject
  |-- Unload()                    --> stops all active emitters

EffectEmitter (MonoBehaviour)
  |-- Definition  : EffectDefinition  (effect ID, pattern, origin, interval, options)
  |-- Audience    : IEffectAudience   (who receives the effect packets)
  |-- Begin()     : starts the emit coroutine
  |-- End()       : stops the coroutine
  |-- Completed   : event raised when a one-shot emitter finishes

EffectDefinition
  |-- EffectId       : ushort          (Unturned effect asset ID)
  |-- Pattern        : IEffectPattern  (spatial layout of spawn points)
  |-- Origin         : Vector3         (world-space origin)
  |-- Interval       : float           (seconds between repeats)
  |-- SnapToSurface  : bool            (raycast to ground, default true)
  |-- OneShot        : bool            (fire once then auto-stop)
```

## Namespace Map

| Namespace | Contents |
|-----------|----------|
| `BlueBeard.Effects` | `EffectEmitterManager`, `EffectEmitter`, `EffectDefinition` |
| `BlueBeard.Effects.Patterns` | `IEffectPattern`, `SinglePointPattern`, `CirclePattern`, `SquarePattern`, `ScatterPattern` |
| `BlueBeard.Effects.Audiences` | `IEffectAudience`, `AllPlayersAudience`, `SinglePlayerAudience`, `PlayerGroupAudience` |
