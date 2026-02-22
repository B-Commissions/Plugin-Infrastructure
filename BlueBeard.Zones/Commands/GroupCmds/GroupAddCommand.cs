using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using BlueBeard.Zones.Flags;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.GroupCmds;

internal class GroupAddCommand : SubCommand
{
    public override string Name => "add";
    public override string Permission => "zone.group.add";
    public override string Help => "Add a group action to a zone.";
    public override string Syntax => "<zoneId> <enter|leave> <add|remove> <groupName>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 4)
        {
            CommandBase.Reply(caller, $"Usage: /zone group add {Syntax}", Color.yellow);
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

        zone.Flags ??= new();
        zone.Flags[flagName] = args[3];
        await ZonesPlugin.Instance.ZoneManager.SaveZoneAsync(zone);
        CommandBase.Reply(caller, $"Group action set: {args[1]} {args[2]} '{args[3]}' on zone '{args[0]}'.", Color.green);
    }
}
