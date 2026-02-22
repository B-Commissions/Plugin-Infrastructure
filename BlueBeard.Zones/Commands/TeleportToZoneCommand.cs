using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace BlueBeard.Zones.Commands;

internal class TeleportToZoneCommand : SubCommand
{
    public override string Name => "tp";
    public override string Permission => "zone.tp";
    public override string Help => "Teleport to a zone's center.";
    public override string Syntax => "<zoneId>";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (caller is not UnturnedPlayer player)
        {
            CommandBase.Reply(caller, "This command can only be used by players.", Color.red);
            return Task.CompletedTask;
        }

        if (args.Length < 1)
        {
            CommandBase.Reply(caller, $"Usage: /zone tp {Syntax}", Color.yellow);
            return Task.CompletedTask;
        }

        var zone = ZonesPlugin.Instance.ZoneManager.GetZone(args[0]);
        if (zone == null)
        {
            CommandBase.Reply(caller, $"Zone '{args[0]}' not found.", Color.red);
            return Task.CompletedTask;
        }

        player.Player.teleportToLocationUnsafe(zone.Center, player.Rotation);
        CommandBase.Reply(caller, $"Teleported to zone '{zone.Id}'.", Color.green);
        return Task.CompletedTask;
    }
}
