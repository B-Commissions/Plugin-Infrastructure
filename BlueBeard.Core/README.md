# BlueBeard.Core

Shared foundation library for all BlueBeard plugins. Provides configuration management, common helpers, and the `IManager` lifecycle interface.

## Installation

Add a project reference:

```xml
<ProjectReference Include="..\BlueBeard.Core\BlueBeard.Core.csproj" />
```

## IManager

Standard lifecycle interface for all managers:

```csharp
public interface IManager
{
    void Load();
    void Unload();
}
```

## ConfigManager

XML-based configuration system with automatic validation and migration. New properties added to a config class are automatically populated with defaults on next load. Removed properties are cleaned up on save.

### Setup

```csharp
using BlueBeard.Core.Configs;

// In your plugin's Load():
var configManager = new ConfigManager();
configManager.Initialize(Directory); // RocketPlugin.Directory
configManager.LoadConfig<MyConfig>();
```

### Defining a Config

```csharp
using BlueBeard.Core.Configs;

public class MyConfig : IConfig
{
    public ushort SpawnEffectId { get; set; }
    public float CooldownSeconds { get; set; }
    public int MaxPlayers { get; set; }

    public void LoadDefaults()
    {
        SpawnEffectId = 1234;
        CooldownSeconds = 30f;
        MaxPlayers = 10;
    }
}
```

### Reading and Reloading

```csharp
// Read a loaded config:
var config = configManager.GetConfig<MyConfig>();

// Hot-reload from disk:
var updated = configManager.ReloadConfig<MyConfig>();
```

Configs are saved as `{TypeName}.configuration.xml` inside a `Configs/` subfolder of the plugin directory.

## Helpers

### ThreadHelper

Bridge between background threads and Unity's main thread:

```csharp
using BlueBeard.Core.Helpers;

// Run work off the main thread:
ThreadHelper.RunAsynchronously(() =>
{
    var data = LoadExpensiveData();
    // Return to main thread to use Unturned APIs:
    ThreadHelper.RunSynchronously(() =>
    {
        UnturnedChat.Say(player, $"Loaded {data.Count} items");
    });
});

// Async/await version:
ThreadHelper.RunAsynchronously(async () =>
{
    var data = await db.Table<MyEntity>().QueryAsync();
    ThreadHelper.RunSynchronously(() => ProcessOnMainThread(data));
});
```

### MessageHelper

Thread-safe chat messaging (automatically dispatches to main thread):

```csharp
using BlueBeard.Core.Helpers;

// Safe to call from any thread:
MessageHelper.Say(player, "Hello!", Color.green);
MessageHelper.Say("Server broadcast", Color.yellow);
```

### SurfaceHelper

Snap a world position down to the terrain/structure surface:

```csharp
using BlueBeard.Core.Helpers;

var groundPos = SurfaceHelper.SnapPositionToSurface(targetPosition);
// Optionally pass a custom layer mask:
var groundOnly = SurfaceHelper.SnapPositionToSurface(pos, RayMasks.GROUND);
```

### BarricadeHelper

Extract barricade info from a raycast hit and change barricade ownership:

```csharp
using BlueBeard.Core.Helpers;

if (BarricadeHelper.TryGetBarricadeFromHit(hitTransform, out var info))
{
    Logger.Log($"Hit barricade: {info.AssetName} (ID: {info.AssetId})");
}

BarricadeHelper.ChangeBarricadeOwner(transform, newOwnerSteamId, groupId);
```

### CommandDocGenerator

Auto-generate markdown documentation for all `CommandBase` commands in your plugin:

```csharp
using BlueBeard.Core.Helpers;

// In your plugin's Load() or OnLevelLoaded():
CommandDocGenerator.Generate(Directory);
// Writes one .md file per command to {pluginDirectory}/Commands/
```
