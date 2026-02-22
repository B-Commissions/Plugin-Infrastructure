# Configuration System

BlueBeard.Core provides an XML-based configuration system through `ConfigManager` and the `IConfig` interface, both in the `BlueBeard.Core.Configs` namespace. The system handles serialization, deserialization, default values, and automatic migration when config schemas change.

## IConfig Interface

Every configuration class must implement `IConfig`:

```csharp
namespace BlueBeard.Core.Configs;

public interface IConfig
{
    void LoadDefaults();
}
```

`LoadDefaults()` is called whenever a fresh instance is created. It should populate every property with a sensible default value. This method is the single source of truth for defaults and is used both for first-run config generation and for migration of existing configs.

## ConfigManager

`ConfigManager` implements `IManager` and manages the full lifecycle of configuration objects. It maintains an internal dictionary mapping each config type to its loaded instance.

### Initialization

Before loading any configs, call `Initialize` with the plugin's directory path:

```csharp
var configManager = new ConfigManager();
configManager.Initialize(pluginDirectory);
```

This creates a `Configs/` subfolder inside the plugin directory if it does not already exist. All config files are stored in this subfolder.

### File Naming Convention

Config files are named after the type: `{TypeName}.configuration.xml`

For example, a class named `MyPluginConfig` produces the file:
```
{pluginDirectory}/Configs/MyPluginConfig.configuration.xml
```

### API Reference

| Method | Description |
|--------|-------------|
| `Initialize(string pluginDirectory)` | Sets the config directory. Must be called before any other method. |
| `LoadConfig<T>()` | Reads (or creates) the config file for type `T`, stores it internally, and saves it back to disk. |
| `GetConfig<T>()` | Returns the previously loaded config of type `T`. If none was loaded, returns a new instance with defaults. |
| `ReloadConfig<T>()` | Re-reads the config from disk, updates the internal cache, saves it back, and returns the new instance. |
| `SaveConfig<T>(T config)` | Serializes the given config instance to its XML file. |

### Type Constraints

All generic methods constrain `T` to `where T : IConfig, new()` (except `SaveConfig` which only requires `IConfig`). This means your config class must:

1. Implement `IConfig`
2. Have a public parameterless constructor

## Auto-Migration

When a config file already exists on disk, `ConfigManager` deserializes it and then runs a validation and migration pass. This handles three scenarios automatically:

### 1. New Properties (Added to the Class)

If a property exists on the class but is missing from the XML file, the property is set to its default value (from `LoadDefaults()`). A warning is logged:

```
[ConfigManager] Property 'NewProperty' missing from config file MyPluginConfig.configuration.xml. Using default value.
```

### 2. Removed Properties (No Longer on the Class)

If the XML file contains an element that does not correspond to any property on the class, a warning is logged and the element is stripped on the next save:

```
[ConfigManager] Config file MyPluginConfig.configuration.xml contains unknown element 'OldProperty'. It will be removed on save.
```

### 3. Null Reference-Type Properties

If a reference-type property was deserialized as `null` but the default is non-null, the default value is used:

```
[ConfigManager] Property 'ListOfItems' is null but default is non-null in MyPluginConfig.configuration.xml. Using default value.
```

After migration, the config is saved back to disk so the file always reflects the current schema.

## Example

### Defining a Config Class

```csharp
using System.Collections.Generic;
using BlueBeard.Core.Configs;

public class BountyConfig : IConfig
{
    public float RewardMultiplier { get; set; }
    public int MinBounty { get; set; }
    public int MaxBounty { get; set; }
    public List<string> ExcludedWeapons { get; set; }
    public string BroadcastFormat { get; set; }

    public void LoadDefaults()
    {
        RewardMultiplier = 1.5f;
        MinBounty = 100;
        MaxBounty = 10000;
        ExcludedWeapons = new List<string> { "Fists", "Umbrella" };
        BroadcastFormat = "{killer} claimed {amount} for eliminating {victim}!";
    }
}
```

### Using It in a Plugin

```csharp
using BlueBeard.Core.Configs;
using Rocket.Core.Plugins;

public class BountyPlugin : RocketPlugin
{
    private ConfigManager _configManager;

    protected override void Load()
    {
        _configManager = new ConfigManager();
        _configManager.Initialize(Directory);

        // Load (or create) the config
        _configManager.LoadConfig<BountyConfig>();

        // Read values
        var config = _configManager.GetConfig<BountyConfig>();
        Rocket.Core.Logging.Logger.Log($"Reward multiplier: {config.RewardMultiplier}");
    }

    public void OnSettingsChanged()
    {
        // Reload from disk at runtime
        var config = _configManager.ReloadConfig<BountyConfig>();
        Rocket.Core.Logging.Logger.Log($"Config reloaded. New max bounty: {config.MaxBounty}");
    }

    public void UpdateAndSave()
    {
        var config = _configManager.GetConfig<BountyConfig>();
        config.MaxBounty = 50000;
        _configManager.SaveConfig(config);
    }
}
```

### Resulting XML File

After the first load, `{pluginDirectory}/Configs/BountyConfig.configuration.xml` is created:

```xml
<?xml version="1.0" encoding="utf-8"?>
<BountyConfig>
  <RewardMultiplier>1.5</RewardMultiplier>
  <MinBounty>100</MinBounty>
  <MaxBounty>10000</MaxBounty>
  <ExcludedWeapons>
    <string>Fists</string>
    <string>Umbrella</string>
  </ExcludedWeapons>
  <BroadcastFormat>{killer} claimed {amount} for eliminating {victim}!</BroadcastFormat>
</BountyConfig>
```

Server operators can edit this file directly. On the next `LoadConfig` or `ReloadConfig`, any new properties are filled in from defaults, removed properties are cleaned up, and null reference-type properties are repaired.
