using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using BlueBeard.Zones.Flags;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.EffectCmds;

internal class EffectRemoveCommand : SubCommand
{
    public override string Name => "remove";
    public override string Permission => "zone.effect.remove";
    public override string Help => "Remove an effect trigger from a zone.";
    public override string Syntax => "<zoneId> <enter|leave>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 2)
        {
            CommandBase.Reply(caller, $"Usage: /zone effect remove {Syntax}", Color.yellow);
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
            "enter" => ZoneFlag.EnterAddEffect,
            "leave" => ZoneFlag.LeaveAddEffect,
            _ => null
        };

        if (flagName == null)
        {
            CommandBase.Reply(caller, "Type must be 'enter' or 'leave'.", Color.red);
            return;
        }

        if (zone.Flags == null || !zone.Flags.Remove(flagName))
        {
            CommandBase.Reply(caller, $"No {args[1]} effect on zone '{args[0]}'.", Color.red);
            return;
        }

        await ZonesPlugin.Instance.ZoneManager.SaveZoneAsync(zone);
        CommandBase.Reply(caller, $"{args[1]} effect removed from zone '{args[0]}'.", Color.green);
    }
}
