using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.FlagCmds;

internal class FlagAddCommand : SubCommand
{
    public override string Name => "add";
    public override string Permission => "zone.flag.add";
    public override string Help => "Add a flag to a zone.";
    public override string Syntax => "<zoneId> <flagName> [value]";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 2)
        {
            CommandBase.Reply(caller, $"Usage: /zone flag add {Syntax}", Color.yellow);
            return;
        }

        var zoneId = args[0];
        var flagName = args[1];
        var flagValue = args.Length >= 3 ? string.Join(" ", args, 2, args.Length - 2) : "";

        var zone = ZonesPlugin.Instance.ZoneManager.GetZone(zoneId);
        if (zone == null)
        {
            CommandBase.Reply(caller, $"Zone '{zoneId}' not found.", Color.red);
            return;
        }

        zone.Flags ??= new();
        zone.Flags[flagName] = flagValue;
        await ZonesPlugin.Instance.ZoneManager.SaveZoneAsync(zone);
        CommandBase.Reply(caller, $"Flag '{flagName}' added to zone '{zoneId}'.", Color.green);
    }
}
