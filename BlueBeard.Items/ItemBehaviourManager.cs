using System.Collections.Generic;
using BlueBeard.Core;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace BlueBeard.Items;

/// <summary>
/// Registry that routes server-side item events (equip, dequip, use, drop, pickup) to
/// per-asset-id handlers. Register a handler via <see cref="Register"/>; dispatch happens
/// automatically for events where Unturned exposes a reliable global hook.
///
/// Hooks wired automatically:
///  - Equip / dequip via <see cref="PlayerEquipment.OnUseableChanged_Global"/>. Dequip is
///    detected by diffing the current useable asset id against the previously-seen id per
///    player (Unturned does not expose a dedicated dequip event).
///
/// Hooks callers invoke manually (no universal Unturned event exists):
///  - <see cref="NotifyUsed"/>, <see cref="NotifyDropped"/>, <see cref="NotifyPickedUp"/>.
///    Call these from a plugin-specific hook (command, consumable handler, container
///    interaction, etc.) when you want the behaviour invoked.
/// </summary>
public class ItemBehaviourManager : IManager
{
    private readonly Dictionary<ushort, IItemBehaviour> _behaviours = new();
    private readonly Dictionary<ulong, ushort> _lastEquipped = new();

    public void Register(ushort assetId, IItemBehaviour behaviour)
    {
        _behaviours[assetId] = behaviour;
    }

    public void Unregister(ushort assetId)
    {
        _behaviours.Remove(assetId);
    }

    public IItemBehaviour GetBehaviour(ushort assetId)
    {
        return _behaviours.TryGetValue(assetId, out var b) ? b : null;
    }

    public void Load()
    {
        PlayerEquipment.OnUseableChanged_Global += OnUseableChanged;
        U.Events.OnPlayerConnected += OnPlayerConnected;
        U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
    }

    public void Unload()
    {
        PlayerEquipment.OnUseableChanged_Global -= OnUseableChanged;
        U.Events.OnPlayerConnected -= OnPlayerConnected;
        U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;

        _behaviours.Clear();
        _lastEquipped.Clear();
    }

    // -----------------------------------------------------------------------
    // Public manual-dispatch hooks
    // -----------------------------------------------------------------------

    /// <summary>Call this to invoke <see cref="IItemBehaviour.OnUsed"/> for the given item.</summary>
    public void NotifyUsed(Player player, ItemJar jar)
    {
        if (player == null || jar?.item == null) return;
        if (_behaviours.TryGetValue(jar.item.id, out var behaviour))
            behaviour.OnUsed(player, jar);
    }

    /// <summary>Call this to invoke <see cref="IItemBehaviour.OnDropped"/> for the given item.</summary>
    public void NotifyDropped(Player player, ItemJar jar)
    {
        if (player == null || jar?.item == null) return;
        if (_behaviours.TryGetValue(jar.item.id, out var behaviour))
            behaviour.OnDropped(player, jar);
    }

    /// <summary>
    /// Call this to invoke <see cref="IItemBehaviour.OnPickedUp"/>. Returns the handler's
    /// decision (true = allow, false = deny) so the caller can enforce the veto.
    /// </summary>
    public bool NotifyPickedUp(Player player, ItemJar jar)
    {
        if (player == null || jar?.item == null) return true;
        if (_behaviours.TryGetValue(jar.item.id, out var behaviour))
            return behaviour.OnPickedUp(player, jar);
        return true;
    }

    // -----------------------------------------------------------------------
    // Equip / dequip dispatch
    // -----------------------------------------------------------------------

    private void OnUseableChanged(PlayerEquipment equipment)
    {
        if (equipment == null || equipment.player == null) return;

        var steamId = equipment.player.channel.owner.playerID.steamID.m_SteamID;
        _lastEquipped.TryGetValue(steamId, out var previousId);

        var currentId = equipment.asset?.id ?? 0;

        // Dequip: previous -> something else (or nothing)
        if (previousId != 0 && previousId != currentId)
        {
            if (_behaviours.TryGetValue(previousId, out var prevBehaviour))
            {
                // We don't have the original ItemJar anymore; pass null. Handlers that need
                // the jar reference should capture it in OnEquipped.
                prevBehaviour.OnDequipped(equipment.player, null);
            }
        }

        // Equip: something new is in hand
        if (currentId != 0 && currentId != previousId)
        {
            if (_behaviours.TryGetValue(currentId, out var currBehaviour))
            {
                var jar = BuildEquippedJar(equipment);
                currBehaviour.OnEquipped(equipment.player, jar);
            }
        }

        _lastEquipped[steamId] = currentId;
    }

    private static ItemJar BuildEquippedJar(PlayerEquipment equipment)
    {
        var inv = equipment.player.inventory;
        if (inv == null) return null;
        if (equipment.equippedPage >= inv.items.Length) return null;
        var index = inv.getIndex(equipment.equippedPage, equipment.equipped_x, equipment.equipped_y);
        return inv.getItem(equipment.equippedPage, index);
    }

    private void OnPlayerConnected(UnturnedPlayer player)
    {
        _lastEquipped[player.CSteamID.m_SteamID] = 0;
    }

    private void OnPlayerDisconnected(UnturnedPlayer player)
    {
        _lastEquipped.Remove(player.CSteamID.m_SteamID);
    }
}
