# Examples

## Basic: Weapon Rack

A barricade with two snap points for mounting weapons.

```csharp
using BlueBeard.SnapLogic;
using BlueBeard.SnapLogic.Models;
using Rocket.Core.Plugins;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

public class WeaponRackPlugin : RocketPlugin
{
    private SnapManager _snapManager;

    protected override void Load()
    {
        _snapManager = new SnapManager();

        _snapManager.RegisterDefinition(new SnapDefinition
        {
            Id = "weapon_rack",
            HostAssetId = 50633,
            SnapRadius = 1.5f,
            SnapPoints = new List<SnapPoint>
            {
                new SnapPoint
                {
                    Name = "left_mount",
                    PositionOffset = new Vector3(-0.5f, 0.3f, 0f),
                    AcceptedAssetIds = new ushort[] { 50632, 50634 }
                },
                new SnapPoint
                {
                    Name = "right_mount",
                    PositionOffset = new Vector3(0.5f, 0.3f, 0f),
                    AcceptedAssetIds = new ushort[] { 50632, 50634 }
                }
            }
        });

        _snapManager.OnItemSnapped += OnWeaponSnapped;
        _snapManager.Load();
    }

    private void OnWeaponSnapped(SnapHost host, SnapAttachment attachment)
    {
        Logger.Log($"Weapon {attachment.AssetId} mounted at {attachment.PointName}");
    }

    protected override void Unload()
    {
        _snapManager.OnItemSnapped -= OnWeaponSnapped;
        _snapManager.Unload();
    }
}
```

## Multiple Host Types

Register several definitions for different barricade types.

```csharp
_snapManager.RegisterDefinition(new SnapDefinition
{
    Id = "small_shelf",
    HostAssetId = 10001,
    SnapRadius = 1.0f,
    SnapPoints = new List<SnapPoint>
    {
        new SnapPoint { Name = "slot_1", PositionOffset = new Vector3(-0.3f, 0.5f, 0f) },
        new SnapPoint { Name = "slot_2", PositionOffset = new Vector3(0.3f, 0.5f, 0f) }
    }
});

_snapManager.RegisterDefinition(new SnapDefinition
{
    Id = "large_shelf",
    HostAssetId = 10002,
    SnapRadius = 2.0f,
    SnapPoints = new List<SnapPoint>
    {
        new SnapPoint { Name = "slot_1", PositionOffset = new Vector3(-0.8f, 1.0f, 0f) },
        new SnapPoint { Name = "slot_2", PositionOffset = new Vector3(0f, 1.0f, 0f) },
        new SnapPoint { Name = "slot_3", PositionOffset = new Vector3(0.8f, 1.0f, 0f) },
        new SnapPoint { Name = "slot_4", PositionOffset = new Vector3(-0.8f, 0.2f, 0f) },
        new SnapPoint { Name = "slot_5", PositionOffset = new Vector3(0f, 0.2f, 0f) },
        new SnapPoint { Name = "slot_6", PositionOffset = new Vector3(0.8f, 0.2f, 0f) }
    }
});
```

## Targeted Snapping

Snap a barricade to a specific named point programmatically.

```csharp
var host = _snapManager.GetHost(hostInstanceId);
if (host == null) return;

// Try a specific point
var attachment = _snapManager.TrySnap(host, childDrop, "slot_3");
if (attachment != null)
{
    Logger.Log($"Placed at slot_3");
}
else
{
    // Fallback to any available point
    attachment = _snapManager.TrySnap(host, childDrop);
}
```

## Querying State

```csharp
// Check if a barricade is snapped
if (_snapManager.IsSnapped(barricadeInstanceId))
{
    var attachment = _snapManager.GetAttachment(barricadeInstanceId);
    Logger.Log($"This barricade is snapped to {attachment.PointName}");
}

// Get all hosts
foreach (var host in _snapManager.GetAllHosts())
{
    Logger.Log($"Host {host.HostInstanceId}: {host.Attachments.Count}/{host.SnapPoints.Count} points used");
}

// Find nearest host for a given asset
var nearestHost = _snapManager.FindNearestHost(position, childAssetId);
```

## Manual Unsnap

```csharp
// Remove a specific child from its point
_snapManager.Unsnap(host, "slot_1");

// Remove all children (without destroying barricades)
_snapManager.ClearHost(host);

// Remove all children and destroy the barricades
_snapManager.ClearHost(host, destroyBarricades: true);
```
