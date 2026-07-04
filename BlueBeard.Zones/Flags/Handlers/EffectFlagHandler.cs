using BlueBeard.Zones.Tracking;
using SDG.Unturned;
using UnturnedEffectManager = SDG.Unturned.EffectManager;

namespace BlueBeard.Zones.Flags.Handlers;

public class EffectFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker) : FlagHandlerBase(zoneManager, playerTracker)
{
    public override string FlagName => "effect";

    public override void Subscribe()
    {
        // Tracker events are height/shape-filtered; raw ZoneManager events are not.
        PlayerTracker.PlayerEnteredZone += OnPlayerEntered;
        PlayerTracker.PlayerExitedZone += OnPlayerExited;
    }

    public override void Unsubscribe()
    {
        PlayerTracker.PlayerEnteredZone -= OnPlayerEntered;
        PlayerTracker.PlayerExitedZone -= OnPlayerExited;
    }

    private void OnPlayerEntered(Player player, ZoneDefinition definition)
    {
        if (definition.Flags == null) return;
        var connection = player.channel.owner.transportConnection;

        if (definition.Flags.TryGetValue(ZoneFlag.EnterAddEffect, out var addEffectId) &&
            ushort.TryParse(addEffectId, out var effectId))
        {
            UnturnedEffectManager.sendEffectReliable(effectId, connection, player.transform.position);
        }

        if (definition.Flags.TryGetValue(ZoneFlag.EnterRemoveEffect, out var removeEffectId) &&
            ushort.TryParse(removeEffectId, out var removeId))
        {
            UnturnedEffectManager.askEffectClearByID(removeId, connection);
        }
    }

    private void OnPlayerExited(Player player, ZoneDefinition definition)
    {
        if (definition.Flags == null) return;
        var connection = player.channel.owner.transportConnection;

        if (definition.Flags.TryGetValue(ZoneFlag.LeaveAddEffect, out var addEffectId) &&
            ushort.TryParse(addEffectId, out var effectId))
        {
            UnturnedEffectManager.sendEffectReliable(effectId, connection, player.transform.position);
        }

        if (definition.Flags.TryGetValue(ZoneFlag.LeaveRemoveEffect, out var removeEffectId) &&
            ushort.TryParse(removeEffectId, out var removeId))
        {
            UnturnedEffectManager.askEffectClearByID(removeId, connection);
        }
    }
}
