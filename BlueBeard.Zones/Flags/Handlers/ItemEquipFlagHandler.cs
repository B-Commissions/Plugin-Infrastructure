using BlueBeard.Zones.BlockLists;
using BlueBeard.Zones.Tracking;
using SDG.Unturned;

namespace BlueBeard.Zones.Flags.Handlers;

public class ItemEquipFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker, BlockListManager blockListManager) : FlagHandlerBase(zoneManager, playerTracker)
{
    public override string FlagName => ZoneFlag.NoItemEquip;

    public override void Subscribe()
    {
        // Tracker events are height/shape-filtered; raw ZoneManager events are not.
        PlayerTracker.PlayerEnteredZone += OnPlayerEntered;
        // Entry-only enforcement was trivially bypassed by equipping AFTER entering.
        PlayerEquipment.OnUseableChanged_Global += OnUseableChanged;
    }

    public override void Unsubscribe()
    {
        PlayerTracker.PlayerEnteredZone -= OnPlayerEntered;
        PlayerEquipment.OnUseableChanged_Global -= OnUseableChanged;
    }

    private void OnPlayerEntered(Player player, ZoneDefinition definition)
    {
        if (definition.Flags == null || !definition.Flags.TryGetValue(ZoneFlag.NoItemEquip, out var flagValue))
            return;

        EnforceForPlayer(player, definition, flagValue);
    }

    private void OnUseableChanged(PlayerEquipment equipment)
    {
        var player = equipment.player;
        if (player == null || equipment.asset == null) return;

        if (IsPlayerInZoneWithFlag(player, ZoneFlag.NoItemEquip, out var zone, out var flagValue))
            EnforceForPlayer(player, zone, flagValue);
    }

    private void EnforceForPlayer(Player player, ZoneDefinition definition, string flagValue)
    {
        if (HasOverridePermission(player, ZoneFlag.NoItemEquip, definition.Id))
            return;

        // Check if the currently equipped item is in the block list
        var equipment = player.equipment;
        if (equipment.asset == null) return;

        if (!string.IsNullOrEmpty(flagValue))
        {
            if (!blockListManager.IsItemInBlockList(flagValue, equipment.asset.id))
                return;
        }

        // Force dequip the player's current item
        equipment.dequip();
    }
}
