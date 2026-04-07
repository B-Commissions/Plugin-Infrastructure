# Getting Started

## Installation

Add a project reference to `BlueBeard.SnapLogic` in your plugin's `.csproj`:

```xml
<ProjectReference Include="..\BlueBeard.SnapLogic\BlueBeard.SnapLogic.csproj" />
```

BlueBeard.SnapLogic depends on `BlueBeard.Core` (which is referenced automatically).

## Core Concepts

### Snap Point
A named position offset relative to a host barricade's origin. When a compatible child barricade is placed near the host, it gets moved to this position. Child barricades inherit the host's rotation.

### Snap Definition
Declares a barricade asset ID as a snap host and lists its available snap points. The definition also specifies a snap radius for detection.

### Snap Host
A runtime instance of a placed host barricade. Tracks which snap points are occupied and by which child barricades.

### Snap Attachment
A record of a child barricade occupying a snap point, storing the point name, asset ID, instance ID, and drop reference.

## Basic Setup

```csharp
using BlueBeard.SnapLogic;
using BlueBeard.SnapLogic.Models;
using UnityEngine;

public class MyPlugin : RocketPlugin
{
    private SnapManager _snapManager;

    protected override void Load()
    {
        _snapManager = new SnapManager();

        _snapManager.RegisterDefinition(new SnapDefinition
        {
            Id = "storage_shelf",
            HostAssetId = 12345,        // The host barricade asset ID
            SnapRadius = 2.0f,          // Detection radius
            SnapPoints = new List<SnapPoint>
            {
                new SnapPoint
                {
                    Name = "top_left",
                    PositionOffset = new Vector3(-0.5f, 1.0f, 0f)
                },
                new SnapPoint
                {
                    Name = "top_right",
                    PositionOffset = new Vector3(0.5f, 1.0f, 0f)
                },
                new SnapPoint
                {
                    Name = "bottom_left",
                    PositionOffset = new Vector3(-0.5f, 0.2f, 0f),
                    AcceptedAssetIds = new ushort[] { 67890 }  // Only accept specific items
                }
            }
        });

        _snapManager.Load();
    }

    protected override void Unload()
    {
        _snapManager.Unload();
    }
}
```

## How It Works

1. When a barricade matching a registered `HostAssetId` is placed, a `SnapHost` is automatically created.
2. When any other barricade is placed within `SnapRadius` of a host, the manager checks for available snap points.
3. If a compatible snap point is found, the barricade is moved to the snap point's world position using `BarricadeManager.ServerSetBarricadeTransform()`.
4. The child inherits the host's rotation.
5. The attachment is recorded and `OnItemSnapped` fires.

## Finding Snap Point Offsets

In debug builds, use the `/snap dump [radius]` command:

1. Manually arrange barricades around a host in the desired positions.
2. Look at the host barricade.
3. Run `/snap dump` (or `/snap dump 10` for a larger radius).
4. A `.txt` file is generated with each nearby barricade's asset ID and offset relative to the host.
5. Copy these offsets into your `SnapDefinition`.
