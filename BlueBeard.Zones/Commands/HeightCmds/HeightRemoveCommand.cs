using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.HeightCmds;

internal class HeightRemoveCommand : SubCommand
{
    public override string Name => "remove";
    public override string Permission => "zone.height.remove";
    public override string Help => "Remove height bounds from a zone.";
    public override string Syntax => "<zoneId>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 1)
        {
            CommandBase.Reply(caller, $"Usage: /zone height remove {Syntax}", Color.yellow);
            return;
        }

        var zone = ZonesPlugin.Instance.ZoneManager.GetZone(args[0]);
        if (zone == null)
        {
            CommandBase.Reply(caller, $"Zone '{args[0]}' not found.", Color.red);
            return;
        }

        zone.LowerHeight = null;
        zone.UpperHeight = null;
        await ZonesPlugin.Instance.ZoneManager.SaveZoneAsync(zone);
        CommandBase.Reply(caller, $"Height bounds removed from zone '{args[0]}'.", Color.green);
    }
}
