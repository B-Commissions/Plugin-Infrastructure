using System.Linq;
using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace BlueBeard.Zones.Commands;

internal class InZoneCommand : SubCommand
{
    public override string Name => "inzone";
    public override string Permission => "zone.inzone";
    public override string Help => "Show which zones you are currently in.";
    public override string Syntax => "";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (caller is not UnturnedPlayer player)
        {
            CommandBase.Reply(caller, "This command can only be used by players.", Color.red);
            return Task.CompletedTask;
        }

        var tracker = ZonesPlugin.Instance.PlayerTracker;
        if (tracker == null)
        {
            CommandBase.Reply(caller, "Player tracking is not available.", Color.red);
            return Task.CompletedTask;
        }

        var zones = tracker.GetZonesForPlayer(player.Player);
        if (zones.Count == 0)
        {
            CommandBase.Reply(caller, "You are not in any zones.", Color.yellow);
            return Task.CompletedTask;
        }

        var zoneNames = string.Join(", ", zones.Select(z => z.Id));
        CommandBase.Reply(caller, $"You are in: {zoneNames}", Color.cyan);
        return Task.CompletedTask;
    }
}
