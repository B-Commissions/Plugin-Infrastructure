using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using BlueBeard.Zones.Flags;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.MessageCmds;

internal class MessageSetCommand : SubCommand
{
    public override string Name => "set";
    public override string Permission => "zone.message.set";
    public override string Help => "Set an enter or leave message on a zone.";
    public override string Syntax => "<zoneId> <enter|leave> <text>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 3)
        {
            CommandBase.Reply(caller, $"Usage: /zone message set {Syntax}", Color.yellow);
            return;
        }

        var zone = ZonesPlugin.Instance.ZoneManager.GetZone(args[0]);
        if (zone == null)
        {
            CommandBase.Reply(caller, $"Zone '{args[0]}' not found.", Color.red);
            return;
        }

        var flagName = args[1].ToLowerInvariant() switch
        {
            "enter" => ZoneFlag.EnterMessage,
            "leave" => ZoneFlag.LeaveMessage,
            _ => null
        };

        if (flagName == null)
        {
            CommandBase.Reply(caller, "Type must be 'enter' or 'leave'.", Color.red);
            return;
        }

        var message = string.Join(" ", args, 2, args.Length - 2);
        zone.Flags ??= new();
        zone.Flags[flagName] = message;
        await ZonesPlugin.Instance.ZoneManager.SaveZoneAsync(zone);
        CommandBase.Reply(caller, $"{args[1]} message set on zone '{args[0]}'.", Color.green);
    }
}
