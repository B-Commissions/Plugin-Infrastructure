using System.Collections.Generic;
using BlueBeard.Core;
using BlueBeard.Zones.BlockLists;
using BlueBeard.Zones.Flags.Handlers;
using BlueBeard.Zones.Tracking;

namespace BlueBeard.Zones.Flags;

public class FlagEnforcementManager : IManager
{
    private readonly List<IFlagHandler> _handlers = new();

    public void Initialize(ZoneManager zoneManager, PlayerTracker playerTracker, BlockListManager blockListManager)
    {
        _handlers.Add(new DamageFlagHandler(zoneManager, playerTracker));
        _handlers.Add(new AccessFlagHandler(zoneManager, playerTracker));
        _handlers.Add(new BuildFlagHandler(zoneManager, playerTracker, blockListManager));
        _handlers.Add(new ItemEquipFlagHandler(zoneManager, playerTracker, blockListManager));
        _handlers.Add(new LockpickFlagHandler(zoneManager, playerTracker));
        _handlers.Add(new EnvironmentFlagHandler(zoneManager, playerTracker));
        _handlers.Add(new NotificationFlagHandler(zoneManager, playerTracker));
        _handlers.Add(new EffectFlagHandler(zoneManager, playerTracker));
        _handlers.Add(new GroupFlagHandler(zoneManager, playerTracker));
    }

    public void Load()
    {
        foreach (var handler in _handlers)
            handler.Subscribe();
    }

    public void Unload()
    {
        foreach (var handler in _handlers)
            handler.Unsubscribe();
        _handlers.Clear();
    }
}
