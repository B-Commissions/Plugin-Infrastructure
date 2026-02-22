using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.FlagCmds;

internal class FlagRemoveCommand : SubCommand
{
    public override string Name => "remove";
    public override string Permission => "zone.flag.remove";
    public override string Help => "Remove a flag from a zone.";
    public override string Syntax => "<zoneId> <flagName>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 2)
        {
            CommandBase.Reply(caller, $"Usage: /zone flag remove {Syntax}", Color.yellow);
            return;
        }

        var zoneId = args[0];
        var flagName = args[1];

        var zone = ZonesPlugin.Instance.ZoneManager.GetZone(zoneId);
        if (zone == null)
        {
            CommandBase.Reply(caller, $"Zone '{zoneId}' not found.", Color.red);
            return;
        }

        if (zone.Flags == null || !zone.Flags.Remove(flagName))
        {
            CommandBase.Reply(caller, $"Zone '{zoneId}' does not have flag '{flagName}'.", Color.red);
            return;
        }

        await ZonesPlugin.Instance.ZoneManager.SaveZoneAsync(zone);
        CommandBase.Reply(caller, $"Flag '{flagName}' removed from zone '{zoneId}'.", Color.green);
    }
}
