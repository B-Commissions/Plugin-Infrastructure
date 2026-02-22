using System;
using System.Linq;
using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.Zones;

public class ZoneComponent : MonoBehaviour
{
    public ZoneDefinition Definition { get; set; }
    public Action<Player, ZoneDefinition> PlayerEntered { get; set; }
    public Action<Player, ZoneDefinition> PlayerExited { get; set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<Player>(out var player))
            PlayerEntered?.Invoke(player, Definition);
        else if (other.CompareTag("Vehicle") && other.TryGetComponent<InteractableVehicle>(out var vehicle))
            foreach (var p in vehicle.passengers.Where(p => p?.player?.player != null))
                PlayerEntered?.Invoke(p.player.player, Definition);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<Player>(out var player))
            PlayerExited?.Invoke(player, Definition);
        else if (other.CompareTag("Vehicle") && other.TryGetComponent<InteractableVehicle>(out var vehicle))
            foreach (var p in vehicle.passengers.Where(p => p?.player?.player != null))
                PlayerExited?.Invoke(p.player.player, Definition);
    }
}
