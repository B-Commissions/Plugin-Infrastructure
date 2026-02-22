using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlueBeard.Zones.Tracking;
using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.Zones.Flags.Handlers;

public class EnvironmentFlagHandler : FlagHandlerBase
{
    private Coroutine _zombieCleanupCoroutine;
    private Coroutine _generatorRefuelCoroutine;
    private GameObject _coroutineHost;

    public EnvironmentFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker)
        : base(zoneManager, playerTracker) { }

    public override string FlagName => "environment";

    public override void Subscribe()
    {
        VehicleManager.onSiphonVehicleRequested += OnSiphonVehicle;

        _coroutineHost = new GameObject("ZoneEnvironmentHandler");
        Object.DontDestroyOnLoad(_coroutineHost);
        var runner = _coroutineHost.AddComponent<CoroutineRunner>();
        _zombieCleanupCoroutine = runner.StartCoroutine(ZombieCleanupLoop());
        _generatorRefuelCoroutine = runner.StartCoroutine(GeneratorRefuelLoop());
    }

    public override void Unsubscribe()
    {
        VehicleManager.onSiphonVehicleRequested -= OnSiphonVehicle;

        if (_coroutineHost != null)
            Object.Destroy(_coroutineHost);
        _coroutineHost = null;
        _zombieCleanupCoroutine = null;
        _generatorRefuelCoroutine = null;
    }

    private void OnSiphonVehicle(InteractableVehicle vehicle, Player instigatingPlayer, ref bool shouldAllow, ref ushort desiredAmount)
    {
        if (!shouldAllow) return;
        if (instigatingPlayer == null) return;

        if (IsPlayerInZoneWithFlag(instigatingPlayer, ZoneFlag.NoVehicleSiphoning, out var zone, out _))
        {
            if (HasOverridePermission(instigatingPlayer, ZoneFlag.NoVehicleSiphoning, zone.Id)) return;
            shouldAllow = false;
        }
    }

    private IEnumerator ZombieCleanupLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            var definitions = ZoneManager.GetAllDefinitions();
            var noZombieZones = definitions.Where(d => d.Flags != null && d.Flags.ContainsKey(ZoneFlag.NoZombie)).ToList();

            if (noZombieZones.Count == 0) continue;

            for (var r = 0; r < ZombieManager.regions.Length; r++)
            {
                var region = ZombieManager.regions[r];
                for (var i = region.zombies.Count - 1; i >= 0; i--)
                {
                    var zombie = region.zombies[i];
                    if (zombie == null || zombie.isDead) continue;

                    foreach (var zone in noZombieZones)
                    {
                        if (PlayerTracker.IsPositionInZoneWithFlag(zombie.transform.position, ZoneFlag.NoZombie, out _, out _))
                        {
                            ZombieManager.sendZombieDead(zombie, Vector3.zero);
                            break;
                        }
                    }
                }
            }
        }
    }

    private IEnumerator GeneratorRefuelLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);

            var definitions = ZoneManager.GetAllDefinitions();
            var generatorZones = definitions.Where(d => d.Flags != null && d.Flags.ContainsKey(ZoneFlag.InfiniteGenerator)).ToList();

            if (generatorZones.Count == 0) continue;

            foreach (var region in BarricadeManager.regions)
            {
                foreach (var drop in region.drops)
                {
                    if (drop.interactable is not InteractableGenerator generator) continue;
                    if (generator.fuel >= generator.capacity) continue;

                    if (PlayerTracker.IsPositionInZoneWithFlag(drop.model.position, ZoneFlag.InfiniteGenerator, out _, out _))
                    {
                        generator.askFill(generator.capacity);
                        BarricadeManager.sendFuel(drop.model, generator.fuel);
                    }
                }
            }
        }
    }

    private class CoroutineRunner : MonoBehaviour { }
}
