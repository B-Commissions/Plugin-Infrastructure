# Events

`SnapManager` exposes four events for hooking into the snap lifecycle.

## OnItemSnapped

Fired when a barricade is snapped to a point on a host.

```csharp
snapManager.OnItemSnapped += (SnapHost host, SnapAttachment attachment) =>
{
    Logger.Log($"Barricade {attachment.AssetId} snapped to {attachment.PointName} on host {host.HostInstanceId}");
};
```

**Parameters:**
- `host` -- The `SnapHost` the item was snapped to.
- `attachment` -- The `SnapAttachment` record with point name, asset ID, instance ID, and drop reference.

## OnItemUnsnapped

Fired when a snapped barricade is removed from a host (via `Unsnap()` or `ClearHost()`).

```csharp
snapManager.OnItemUnsnapped += (SnapHost host, SnapAttachment attachment) =>
{
    Logger.Log($"Barricade {attachment.AssetId} removed from {attachment.PointName}");
};
```

## OnHostRegistered

Fired when a new host barricade is registered (either automatically on barricade spawn or via `RegisterHost()`).

```csharp
snapManager.OnHostRegistered += (SnapHost host) =>
{
    Logger.Log($"New snap host registered: {host.DefinitionId} (instance {host.HostInstanceId})");
};
```

## OnHostDestroyed

Fired when a host barricade is destroyed or unregistered. Children may already be cleared at this point depending on `DestroyChildrenWithHost`.

```csharp
snapManager.OnHostDestroyed += (SnapHost host) =>
{
    Logger.Log($"Snap host destroyed: {host.DefinitionId} (instance {host.HostInstanceId})");
};
```

## Event Order

When a host is salvaged with `DestroyChildrenWithHost = true`:
1. `OnItemUnsnapped` fires for each child attachment.
2. Child barricades are destroyed.
3. `OnHostDestroyed` fires for the host.
