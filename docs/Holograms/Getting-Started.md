# Getting Started

This guide walks through adding BlueBeard.Holograms to your project, explains the core concepts, and shows how to register your first hologram.

## Project Setup

Add project references to both **BlueBeard.Holograms** and **BlueBeard.Core** in your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\BlueBeard.Core\BlueBeard.Core.csproj" />
  <ProjectReference Include="..\BlueBeard.Holograms\BlueBeard.Holograms.csproj" />
</ItemGroup>
```

BlueBeard.Holograms targets `net481` and depends on Unturned's `Assembly-CSharp`, `SDG.NetTransport`, `Rocket.Core`, and several Unity modules. These are resolved through the shared `Libs` folder.

## Core Concepts

### HologramDefinition

A `HologramDefinition` describes **where** a hologram exists in the world and what default data it carries.

```csharp
public class HologramDefinition
{
    public Vector3 Position { get; set; }                    // World position of the trigger zone
    public float Radius { get; set; } = 15f;                 // Capsule collider radius (default 15)
    public float Height { get; set; } = 30f;                 // Capsule collider height (default 30)
    public Dictionary<string, string> Metadata { get; set; } // Key-value pairs passed to the display
}
```

When a definition is registered, `HologramManager` creates a Unity `GameObject` at `Position` with a kinematic `Rigidbody` and a capsule `CapsuleCollider` (trigger mode) sized by `Radius` and `Height`. The `Metadata` dictionary is copied per-player on enter, so each player can have their own metadata state.

### Hologram

A `Hologram` is a single **pool slot**. It pairs two Unturned effect IDs:

```csharp
public class Hologram
{
    public ushort UI { get; set; }     // The UI effect ID (sent via EffectManager.sendUIEffect)
    public ushort Effect { get; set; } // The 3D world effect ID (sent via EffectManager.sendEffect)
}
```

- **UI** -- The screen-space overlay shown to the player. The effect ID is also used as the `key` (cast to `short`) for `sendUIEffectText`, `sendUIEffectVisibility`, etc.
- **Effect** -- The 3D visual spawned at the hologram's world position when a player enters the zone.

Both IDs must correspond to effects defined in your Unity asset bundle.

### IHologramDisplay

`IHologramDisplay` is the interface you implement to control what the player sees when they enter or leave a hologram zone.

```csharp
public interface IHologramDisplay
{
    void Show(ITransportConnection connection, short key,
        HologramDefinition definition, Dictionary<string, string> metadata);

    void Hide(ITransportConnection connection, short key,
        HologramDefinition definition);
}
```

- **Show** is called when a player enters the trigger zone (and again on `UpdatePlayer`/`UpdateAll`). Use `key` with `EffectManager.sendUIEffectText` and related methods to populate the UI.
- **Hide** is called when a player exits the zone or the definition is unregistered. Use it to hide or clear UI elements.

### HologramRegistration

`HologramRegistration` is a convenience class for **batch registration**. Instead of calling `RegisterDefinition` once per definition, you can bundle multiple definitions with one display and one pool.

```csharp
public class HologramRegistration
{
    public IHologramDisplay Display { get; set; }
    public List<Hologram> Holograms { get; set; }            // The pool
    public List<HologramDefinition> Definitions { get; set; } // All definitions sharing this pool
    public bool IsGlobal { get; set; }
}
```

All definitions in a single `HologramRegistration` share the same pool and display instance.

## Step-by-Step: First Hologram

### 1. Create and load the manager

`HologramManager` implements `IManager` from BlueBeard.Core. Call `Load()` to wire up the disconnect handler and `Unload()` to tear everything down.

```csharp
using BlueBeard.Holograms;

var hologramManager = new HologramManager();
hologramManager.Load();

// When your plugin unloads:
hologramManager.Unload();
```

### 2. Define your pool

Create a list of `Hologram` instances. Each entry is one slot -- the pool size determines the maximum number of holograms that can be simultaneously visible (globally or per-player, depending on mode).

```csharp
var pool = new List<Hologram>
{
    new() { UI = 50600, Effect = 50601 },
    new() { UI = 50602, Effect = 50603 },
    new() { UI = 50604, Effect = 50605 },
};
```

### 3. Implement IHologramDisplay

Write a class that populates the UI when a player enters and cleans it up when they leave.

```csharp
using BlueBeard.Holograms;
using SDG.NetTransport;
using SDG.Unturned;

public class MyDisplay : IHologramDisplay
{
    public void Show(ITransportConnection connection, short key,
        HologramDefinition definition, Dictionary<string, string> metadata)
    {
        EffectManager.sendUIEffectVisibility(key, connection, true, "Panel", true);
        EffectManager.sendUIEffectText(key, connection, true, "Panel/Title",
            metadata.GetValueOrDefault("title", "Hologram"));
    }

    public void Hide(ITransportConnection connection, short key,
        HologramDefinition definition)
    {
        EffectManager.sendUIEffectVisibility(key, connection, true, "Panel", false);
    }
}
```

### 4. Create definitions

```csharp
var definition = new HologramDefinition
{
    Position = new Vector3(100, 50, 200),
    Radius = 10f,
    Height = 20f,
    Metadata = new Dictionary<string, string>
    {
        { "title", "Welcome Zone" }
    }
};
```

### 5. Register

Use `RegisterDefinition` for a single definition, or `Register` with a `HologramRegistration` for batch registration.

```csharp
// Single registration
hologramManager.RegisterDefinition(definition, new MyDisplay(), pool, isGlobal: true);

// -- OR -- batch registration
var registration = new HologramRegistration
{
    Display = new MyDisplay(),
    Holograms = pool,
    Definitions = new List<HologramDefinition> { def1, def2, def3 },
    IsGlobal = true
};
hologramManager.Register(registration);
```

Once registered, the hologram is live. Players walking into the trigger zone will see the 3D effect and UI overlay automatically.
