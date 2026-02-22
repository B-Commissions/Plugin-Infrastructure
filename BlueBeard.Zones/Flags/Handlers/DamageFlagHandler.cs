using BlueBeard.Zones.Tracking;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.Zones.Flags.Handlers;

public class DamageFlagHandler : FlagHandlerBase
{
    public DamageFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker)
        : base(zoneManager, playerTracker) { }

    public override string FlagName => "damage";

    public override void Subscribe()
    {
        BarricadeManager.onDamageBarricadeRequested += OnDamageBarricade;
        StructureManager.onDamageStructureRequested += OnDamageStructure;
        DamageTool.damagePlayerRequested += OnDamagePlayer;
        VehicleManager.onDamageVehicleRequested += OnDamageVehicle;
        VehicleManager.onDamageTireRequested += OnDamageTire;
        DamageTool.damageAnimalRequested += OnDamageAnimal;
        DamageTool.damageZombieRequested += OnDamageZombie;
    }

    public override void Unsubscribe()
    {
        BarricadeManager.onDamageBarricadeRequested -= OnDamageBarricade;
        StructureManager.onDamageStructureRequested -= OnDamageStructure;
        DamageTool.damagePlayerRequested -= OnDamagePlayer;
        VehicleManager.onDamageVehicleRequested -= OnDamageVehicle;
        VehicleManager.onDamageTireRequested -= OnDamageTire;
        DamageTool.damageAnimalRequested -= OnDamageAnimal;
        DamageTool.damageZombieRequested -= OnDamageZombie;
    }

    private void OnDamageBarricade(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (!shouldAllow) return;
        if (IsPositionInZoneWithFlag(barricadeTransform.position, ZoneFlag.NoDamage, out var zone, out _))
        {
            var player = PlayerTool.getPlayer(instigatorSteamID);
            if (player != null && HasOverridePermission(player, ZoneFlag.NoDamage, zone.Id)) return;
            shouldAllow = false;
        }
    }

    private void OnDamageStructure(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (!shouldAllow) return;
        if (IsPositionInZoneWithFlag(structureTransform.position, ZoneFlag.NoDamage, out var zone, out _))
        {
            var player = PlayerTool.getPlayer(instigatorSteamID);
            if (player != null && HasOverridePermission(player, ZoneFlag.NoDamage, zone.Id)) return;
            shouldAllow = false;
        }
    }

    private void OnDamagePlayer(ref DamagePlayerParameters parameters, ref bool shouldAllow)
    {
        if (!shouldAllow) return;

        var victim = parameters.player;
        if (victim == null) return;

        // Check noDamage (all damage blocked)
        if (IsPlayerInZoneWithFlag(victim, ZoneFlag.NoDamage, out var zone, out _))
        {
            if (!HasOverridePermission(victim, ZoneFlag.NoDamage, zone.Id))
            {
                shouldAllow = false;
                return;
            }
        }

        // Check noPlayerDamage (player-on-player)
        if (IsPlayerInZoneWithFlag(victim, ZoneFlag.NoPlayerDamage, out zone, out _))
        {
            if (!HasOverridePermission(victim, ZoneFlag.NoPlayerDamage, zone.Id))
            {
                shouldAllow = false;
                return;
            }
        }

        // Check noPvP
        if (IsPlayerInZoneWithFlag(victim, ZoneFlag.NoPvP, out zone, out _))
        {
            if (!HasOverridePermission(victim, ZoneFlag.NoPvP, zone.Id))
            {
                shouldAllow = false;
                return;
            }
        }
    }

    private void OnDamageVehicle(CSteamID instigatorSteamID, InteractableVehicle vehicle, ref ushort pendingTotalDamage, ref bool canRepair, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (!shouldAllow) return;

        if (IsPositionInZoneWithFlag(vehicle.transform.position, ZoneFlag.NoDamage, out var zone, out _) ||
            IsPositionInZoneWithFlag(vehicle.transform.position, ZoneFlag.NoVehicleDamage, out zone, out _))
        {
            var player = PlayerTool.getPlayer(instigatorSteamID);
            if (player != null && HasOverridePermission(player, ZoneFlag.NoVehicleDamage, zone.Id)) return;
            shouldAllow = false;
        }
    }

    private void OnDamageTire(CSteamID instigatorSteamID, InteractableVehicle vehicle, int tireIndex, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (!shouldAllow) return;

        if (IsPositionInZoneWithFlag(vehicle.transform.position, ZoneFlag.NoDamage, out var zone, out _) ||
            IsPositionInZoneWithFlag(vehicle.transform.position, ZoneFlag.NoTireDamage, out zone, out _))
        {
            var player = PlayerTool.getPlayer(instigatorSteamID);
            if (player != null && HasOverridePermission(player, ZoneFlag.NoTireDamage, zone.Id)) return;
            shouldAllow = false;
        }
    }

    private void OnDamageAnimal(ref DamageAnimalParameters parameters, ref bool shouldAllow)
    {
        if (!shouldAllow) return;

        var animal = parameters.animal;
        if (animal == null) return;

        if (IsPositionInZoneWithFlag(animal.transform.position, ZoneFlag.NoDamage, out _, out _) ||
            IsPositionInZoneWithFlag(animal.transform.position, ZoneFlag.NoAnimalDamage, out _, out _))
        {
            shouldAllow = false;
        }
    }

    private void OnDamageZombie(ref DamageZombieParameters parameters, ref bool shouldAllow)
    {
        if (!shouldAllow) return;

        var zombie = parameters.zombie;
        if (zombie == null) return;

        if (IsPositionInZoneWithFlag(zombie.transform.position, ZoneFlag.NoDamage, out _, out _) ||
            IsPositionInZoneWithFlag(zombie.transform.position, ZoneFlag.NoZombieDamage, out _, out _))
        {
            shouldAllow = false;
        }
    }
}
