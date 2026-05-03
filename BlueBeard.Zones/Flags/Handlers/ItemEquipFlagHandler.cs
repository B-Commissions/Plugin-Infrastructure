using BlueBeard.Zones.BlockLists;
using BlueBeard.Zones.Tracking;
using SDG.Unturned;

namespace BlueBeard.Zones.Flags.Handlers;

public class ItemEquipFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker, BlockListManager blockListManager) : FlagHandlerBase(zoneManager, playerTracker)
{
    public override string FlagName => ZoneFlag.NoItemEquip;

    public override void Subscribe()
    {
        ZoneManager.PlayerEnteredZone += OnPlayerEntered;
    }

    public override void Unsubscribe()
    {
        ZoneManager.PlayerEnteredZone -= OnPlayerEntered;
    }

    private void OnPlayerEntered(Player player, ZoneDefinition definition)
    {
        if (definition.Flags == null || !definition.Flags.TryGetValue(ZoneFlag.NoItemEquip, out var flagValue))
            return;

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
