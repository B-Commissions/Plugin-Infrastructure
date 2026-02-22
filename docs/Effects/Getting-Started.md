# Getting Started

This guide walks through the minimum steps to start spawning effects with BlueBeard.Effects.

## 1. Add Project References

Your plugin project needs references to both **BlueBeard.Core** and **BlueBeard.Effects**. In your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\BlueBeard.Core\BlueBeard.Core.csproj" />
  <ProjectReference Include="..\BlueBeard.Effects\BlueBeard.Effects.csproj" />
</ItemGroup>
```

BlueBeard.Effects already depends on BlueBeard.Core internally, but your plugin needs a direct reference to Core so you can use the `IManager` interface and shared helpers like `SurfaceHelper`.

## 2. Create and Load the Manager

`EffectEmitterManager` implements `IManager`, so it follows the same Load/Unload lifecycle as every other BlueBeard manager.

```csharp
using BlueBeard.Effects;

public class MyPlugin
{
    private readonly EffectEmitterManager _effects = new();

    public void OnLoad()
    {
        _effects.Load();
    }

    public void OnUnload()
    {
        _effects.Unload();   // stops and destroys every active emitter
    }
}
```

`Load()` initializes the manager. `Unload()` iterates every tracked emitter, calls `End()` on it, destroys its `GameObject`, and clears the internal list. Always call `Unload()` when your plugin shuts down to avoid orphaned GameObjects.

## 3. Start an Emitter

Call `Start(definition, audience)` to create a new emitter. The manager creates a Unity `GameObject`, attaches an `EffectEmitter` `MonoBehaviour`, assigns the definition and audience, wires up the `Completed` event, begins the coroutine, and returns the emitter reference.

```csharp
using BlueBeard.Effects;
using BlueBeard.Effects.Audiences;
using BlueBeard.Effects.Patterns;
using UnityEngine;

var definition = new EffectDefinition
{
    EffectId       = 394,                        // Unturned effect asset ID
    Pattern        = new SinglePointPattern(),   // one point at the origin
    Origin         = new Vector3(100f, 0f, 200f),
    SnapToSurface  = true,                       // raycast to ground (default)
    OneShot        = true                        // fire once then auto-dispose
};

var audience = new AllPlayersAudience();

EffectEmitter emitter = _effects.Start(definition, audience);
```

### What happens behind the scenes

1. A new `GameObject` named `EffectEmitter_{EffectId}` is created.
2. An `EffectEmitter` component is added to it.
3. `Definition` and `Audience` are assigned.
4. The `Completed` event is subscribed so that when a one-shot emitter finishes, the manager automatically calls `Stop()` on it.
5. `Begin()` is called, which starts a Unity coroutine (`EmitLoop`).
6. The emitter is added to the manager's internal `_emitters` list.

### The Emit Loop

On each iteration the coroutine:

1. Collects the current recipients from `Audience.GetRecipients()`.
2. Iterates every offset point from `Definition.Pattern.GetPoints()`.
3. Adds each offset to `Definition.Origin` to get the world position.
4. If `SnapToSurface` is true, raycasts the position onto the ground via `SurfaceHelper.SnapPositionToSurface`.
5. Sends the effect to every recipient using `EffectManager.sendEffectReliable`.
6. If `OneShot` is false, waits `Definition.Interval` seconds, then loops again.
7. If `OneShot` is true, the loop exits and the `Completed` event fires.

## 4. Stop an Emitter

For repeating emitters (where `OneShot` is false), you must stop them manually when you no longer need them:

```csharp
_effects.Stop(emitter);
```

This calls `End()` on the emitter (which stops the coroutine), removes it from the tracked list, and destroys the `GameObject`. Passing `null` is safe -- the method returns immediately.

One-shot emitters stop themselves automatically via the `Completed` event handler that the manager wires up during `Start()`.

## 5. Unload Everything

When your plugin unloads, call `Unload()` on the manager:

```csharp
_effects.Unload();
```

This stops and destroys every active emitter and clears the internal list. After calling `Unload()`, you can safely call `Load()` again if needed to reinitialize.

## 6. Inspecting Active Emitters

The manager exposes a read-only view of all currently active emitters:

```csharp
IReadOnlyList<EffectEmitter> active = _effects.Emitters;
```

This can be useful for debugging or for building admin commands that list running effects.

## Quick Reference

| Method | Description |
|--------|-------------|
| `Load()` | Initializes the manager. |
| `Unload()` | Stops all emitters and clears state. |
| `Start(definition, audience)` | Creates and starts a new emitter. Returns the `EffectEmitter`. |
| `Stop(emitter)` | Stops a specific emitter and destroys its `GameObject`. |
| `Emitters` | Read-only list of all active `EffectEmitter` instances. |
