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
            "Blocks damage in the zone.",
            ZoneFlag.NoDamage, ZoneFlag.NoPlayerDamage, ZoneFlag.NoVehicleDamage,
            ZoneFlag.NoTireDamage, ZoneFlag.NoAnimalDamage, ZoneFlag.NoZombieDamage, ZoneFlag.NoPvP);
        _registry.RegisterBuiltInHandler(new AccessFlagHandler(zoneManager, playerTracker),
            "Restricts entering, leaving, or carjacking inside the zone.",
            ZoneFlag.NoEnter, ZoneFlag.NoLeave, ZoneFlag.NoVehicleCarjack);
        _registry.RegisterBuiltInHandler(new BuildFlagHandler(zoneManager, playerTracker, blockListManager),
            "Blocks building in the zone; flag value may name a block list.",
            ZoneFlag.NoBuild);
        _registry.RegisterBuiltInHandler(new ItemEquipFlagHandler(zoneManager, playerTracker, blockListManager),
            "Dequips blocked items in the zone; flag value may name a block list.",
            ZoneFlag.NoItemEquip);
        _registry.RegisterBuiltInHandler(new LockpickFlagHandler(zoneManager, playerTracker),
            "Blocks vehicle lockpicking inside the zone.",
            ZoneFlag.NoLockpick);
        _registry.RegisterBuiltInHandler(new EnvironmentFlagHandler(zoneManager, playerTracker),
            "Environmental controls inside the zone.",
            ZoneFlag.NoZombie, ZoneFlag.NoVehicleSiphoning, ZoneFlag.InfiniteGenerator);
        _registry.RegisterBuiltInHandler(new NotificationFlagHandler(zoneManager, playerTracker),
            "Sends a chat message on enter/leave.",
            ZoneFlag.EnterMessage, ZoneFlag.LeaveMessage);
        _registry.RegisterBuiltInHandler(new EffectFlagHandler(zoneManager, playerTracker),
            "Plays Unturned effects on enter/leave.",
            ZoneFlag.EnterAddEffect, ZoneFlag.LeaveAddEffect, ZoneFlag.EnterRemoveEffect, ZoneFlag.LeaveRemoveEffect);
        _registry.RegisterBuiltInHandler(new GroupFlagHandler(zoneManager, playerTracker),
            "Adds or removes Rocket permission groups on enter/leave.",
            ZoneFlag.EnterAddGroup, ZoneFlag.EnterRemoveGroup, ZoneFlag.LeaveAddGroup, ZoneFlag.LeaveRemoveGroup);
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
