using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.HeightCmds;

internal class HeightSetCommand : SubCommand
{
    public override string Name => "set";
    public override string Permission => "zone.height.set";
    public override string Help => "Set height bounds on a zone.";
    public override string Syntax => "<zoneId> <lower> <upper>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 3)
        {
            CommandBase.Reply(caller, $"Usage: /zone height set {Syntax}", Color.yellow);
            return;
        }

        var zone = ZonesPlugin.Instance.ZoneManager.GetZone(args[0]);
        if (zone == null)
        {
            CommandBase.Reply(caller, $"Zone '{args[0]}' not found.", Color.red);
            return;
        }

        if (!float.TryParse(args[1], out var lower) || !float.TryParse(args[2], out var upper))
        {
            CommandBase.Reply(caller, "Lower and upper must be numbers.", Color.red);
            return;
        }

        zone.LowerHeight = lower;
        zone.UpperHeight = upper;
        await ZonesPlugin.Instance.ZoneManager.SaveZoneAsync(zone);
        CommandBase.Reply(caller, $"Height bounds set on zone '{args[0]}': lower={lower}, upper={upper}.", Color.green);
    }
}
