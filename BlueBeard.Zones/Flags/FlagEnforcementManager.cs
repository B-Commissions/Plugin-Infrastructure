using System.Collections.Generic;
using BlueBeard.Core;
using BlueBeard.Zones.BlockLists;
using BlueBeard.Zones.Flags.Handlers;
using BlueBeard.Zones.Tracking;

namespace BlueBeard.Zones.Flags;

public class FlagEnforcementManager : IManager
{
    private readonly FlagRegistry _registry;
    private readonly HashSet<IFlagHandler> _subscribed = [];
    private bool _isLoaded;

    public FlagEnforcementManager(FlagRegistry registry)
    {
        _registry = registry;
    }

    public void Initialize(ZoneManager zoneManager, PlayerTracker playerTracker, BlockListManager blockListManager)
    {
        _registry.RegisterBuiltInHandler(new DamageFlagHandler(zoneManager, playerTracker),
            "Blocks damage in the zone (see noPlayerDamage / noVehicleDamage / noTireDamage / noAnimalDamage / noZombieDamage / noPvP variants).");
        _registry.RegisterBuiltInHandler(new AccessFlagHandler(zoneManager, playerTracker),
            "Restricts entering, leaving, or carjacking inside the zone (noEnter / noLeave / noVehicleCarjack).");
        _registry.RegisterBuiltInHandler(new BuildFlagHandler(zoneManager, playerTracker, blockListManager),
            "Blocks building in the zone; flag value may name a block list.");
        _registry.RegisterBuiltInHandler(new ItemEquipFlagHandler(zoneManager, playerTracker, blockListManager),
            "Dequips items on entry; flag value may name a block list.");
        _registry.RegisterBuiltInHandler(new LockpickFlagHandler(zoneManager, playerTracker),
            "Blocks vehicle lockpicking inside the zone.");
        _registry.RegisterBuiltInHandler(new EnvironmentFlagHandler(zoneManager, playerTracker),
            "Environmental flags: noZombie, noVehicleSiphoning, infiniteGenerator.");
        _registry.RegisterBuiltInHandler(new NotificationFlagHandler(zoneManager, playerTracker),
            "Sends a chat message on enter/leave (enterMessage / leaveMessage).");
        _registry.RegisterBuiltInHandler(new EffectFlagHandler(zoneManager, playerTracker),
            "Plays Unturned effects on enter/leave (enterAddEffect / leaveAddEffect / enterRemoveEffect / leaveRemoveEffect).");
        _registry.RegisterBuiltInHandler(new GroupFlagHandler(zoneManager, playerTracker),
            "Adds or removes Rocket permission groups on enter/leave.");
    }

    public void Load()
    {
        foreach (var info in _registry.Flags)
        {
            if (info.Handler == null) continue;
            if (_subscribed.Add(info.Handler))
                info.Handler.Subscribe();
        }

        _registry.HandlerRegistered += OnHandlerRegistered;
        _registry.HandlerUnregistered += OnHandlerUnregistered;
        _isLoaded = true;
    }

    public void Unload()
    {
        _registry.HandlerRegistered -= OnHandlerRegistered;
        _registry.HandlerUnregistered -= OnHandlerUnregistered;

        foreach (var handler in _subscribed)
            handler.Unsubscribe();
        _subscribed.Clear();
        _isLoaded = false;
    }

    private void OnHandlerRegistered(FlagInfo info)
    {
        if (!_isLoaded || info.Handler == null) return;
        if (_subscribed.Add(info.Handler))
            info.Handler.Subscribe();
    }

    private void OnHandlerUnregistered(FlagInfo info)
    {
        if (info.Handler == null) return;
        if (_subscribed.Remove(info.Handler))
            info.Handler.Unsubscribe();
    }
}
