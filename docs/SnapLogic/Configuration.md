# Configuration

## SnapLogicConfig

`SnapLogicConfig` implements `IConfig` and provides system-wide defaults.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultSnapRadius` | `float` | `2.0` | Default snap detection radius when not specified by a `SnapDefinition`. |
| `AutoRegisterHosts` | `bool` | `true` | Automatically register host barricades when they are placed (matching a registered definition). |
| `DestroyChildrenWithHost` | `bool` | `true` | Destroy all snapped children when a host barricade is destroyed or salvaged. |

## Usage

You can pass a `SnapLogicConfig` to `SnapManager.Initialize()`:

```csharp
var config = new SnapLogicConfig();
config.LoadDefaults();
config.DestroyChildrenWithHost = false;  // Override default

var snapManager = new SnapManager();
snapManager.Initialize(config);
snapManager.Load();
```

If you don't call `Initialize()`, the manager will use default values automatically.

## Using with ConfigManager

If your plugin uses `BlueBeard.Core.Configs.ConfigManager`, you can load the config from XML:

```csharp
var configManager = new ConfigManager();
configManager.Initialize(pluginDirectory);
configManager.LoadConfig<SnapLogicConfig>();

var config = configManager.GetConfig<SnapLogicConfig>();
snapManager.Initialize(config);
```

This generates a `SnapLogicConfig.configuration.xml` file that server operators can edit.
