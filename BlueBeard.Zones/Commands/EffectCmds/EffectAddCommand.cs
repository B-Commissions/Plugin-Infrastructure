using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using BlueBeard.Zones.Flags;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.EffectCmds;

internal class EffectAddCommand : SubCommand
{
    public override string Name => "add";
    public override string Permission => "zone.effect.add";
    public override string Help => "Add an effect trigger to a zone.";
    public override string Syntax => "<zoneId> <enter|leave> <effectId>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 3)
        {
            CommandBase.Reply(caller, $"Usage: /zone effect add {Syntax}", Color.yellow);
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

        if (!ushort.TryParse(args[2], out _))
        {
            CommandBase.Reply(caller, "Effect ID must be a number.", Color.red);
            return;
        }

        zone.Flags ??= new();
        zone.Flags[flagName] = args[2];
        await ZonesPlugin.Instance.ZoneManager.SaveZoneAsync(zone);
        CommandBase.Reply(caller, $"Effect {args[2]} set as {args[1]} effect on zone '{args[0]}'.", Color.green);
    }
}
