using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using BlueBeard.Zones.Flags;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.GroupCmds;

internal class GroupRemoveCommand : SubCommand
{
    public override string Name => "remove";
    public override string Permission => "zone.group.remove";
    public override string Help => "Remove a group action from a zone.";
    public override string Syntax => "<zoneId> <enter|leave> <add|remove>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 3)
        {
            CommandBase.Reply(caller, $"Usage: /zone group remove {Syntax}", Color.yellow);
            return;
        }

        var zone = ZonesPlugin.Instance.ZoneManager.GetZone(args[0]);
        if (zone == null)
        {
            CommandBase.Reply(caller, $"Zone '{args[0]}' not found.", Color.red);
            return;
        }

        var flagName = (args[1].ToLowerInvariant(), args[2].ToLowerInvariant()) switch
        {
            ("enter", "add") => ZoneFlag.EnterAddGroup,
            ("enter", "remove") => ZoneFlag.EnterRemoveGroup,
            ("leave", "add") => ZoneFlag.LeaveAddGroup,
            ("leave", "remove") => ZoneFlag.LeaveRemoveGroup,
            _ => null
        };

        if (flagName == null)
        {
            CommandBase.Reply(caller, "Must be <enter|leave> <add|remove>.", Color.red);
            return;
        }

        if (zone.Flags == null || !zone.Flags.Remove(flagName))
        {
            CommandBase.Reply(caller, $"No {args[1]} {args[2]} group action on zone '{args[0]}'.", Color.red);
            return;
        }

        await ZonesPlugin.Instance.ZoneManager.SaveZoneAsync(zone);
        CommandBase.Reply(caller, $"Group action removed from zone '{args[0]}'.", Color.green);
    }
}
