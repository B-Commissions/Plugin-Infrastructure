using BlueBeard.Zones.BlockLists;
using BlueBeard.Zones.Tracking;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.Zones.Flags.Handlers;

public class BuildFlagHandler : FlagHandlerBase
{
    private readonly BlockListManager _blockListManager;

    public BuildFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker, BlockListManager blockListManager)
        : base(zoneManager, playerTracker)
    {
        _blockListManager = blockListManager;
    }

    public override string FlagName => ZoneFlag.NoBuild;

    public override void Subscribe()
    {
        BarricadeManager.onDeployBarricadeRequested += OnDeployBarricade;
        StructureManager.onDeployStructureRequested += OnDeployStructure;
    }

    public override void Unsubscribe()
    {
        BarricadeManager.onDeployBarricadeRequested -= OnDeployBarricade;
        StructureManager.onDeployStructureRequested -= OnDeployStructure;
    }

    private void OnDeployBarricade(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angle_x, ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
    {
        if (!shouldAllow) return;

        if (IsPositionInZoneWithFlag(point, ZoneFlag.NoBuild, out var zone, out var flagValue))
        {
            var player = PlayerTool.getPlayer(new CSteamID(owner));
            if (player != null && HasOverridePermission(player, ZoneFlag.NoBuild, zone.Id)) return;

            // If flag value is a block list name, only block items in that list
            if (!string.IsNullOrEmpty(flagValue))
            {
                if (!_blockListManager.IsItemInBlockList(flagValue, asset.id))
                    return; // Item not in block list, allow
            }

            shouldAllow = false;
        }
    }

    private void OnDeployStructure(Structure structure, ItemStructureAsset asset, ref Vector3 point, ref float angle_x, ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
    {
        if (!shouldAllow) return;

        if (IsPositionInZoneWithFlag(point, ZoneFlag.NoBuild, out var zone, out var flagValue))
        {
            var player = PlayerTool.getPlayer(new CSteamID(owner));
            if (player != null && HasOverridePermission(player, ZoneFlag.NoBuild, zone.Id)) return;

            if (!string.IsNullOrEmpty(flagValue))
            {
                if (!_blockListManager.IsItemInBlockList(flagValue, asset.id))
                    return;
            }

            shouldAllow = false;
        }
    }
}
