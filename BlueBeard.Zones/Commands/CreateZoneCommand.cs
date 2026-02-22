using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using BlueBeard.Zones.Shapes;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace BlueBeard.Zones.Commands;

internal class CreateZoneCommand : SubCommand
{
    public override string Name => "create";
    public override string Permission => "zone.create";
    public override string Help => "Create a radius zone at your position.";
    public override string Syntax => "<id> <radius> [height]";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (caller is not UnturnedPlayer player)
        {
            CommandBase.Reply(caller, "This command can only be used by players.", Color.red);
            return;
        }

        if (args.Length < 2)
        {
            CommandBase.Reply(caller, $"Usage: /zone create {Syntax}", Color.yellow);
            return;
        }
        var id = args[0];
        if (!float.TryParse(args[1], out var radius))
        {
            CommandBase.Reply(caller, "Invalid radius. Must be a number.", Color.red);
            return;
        }
        var height = 30f;
        if (args.Length >= 3 && !float.TryParse(args[2], out height))
        {
            CommandBase.Reply(caller, "Invalid height. Must be a number.", Color.red);
            return;
        }
        var definition = new ZoneDefinition
        {
            Id = id, Center = player.Position, Shape = new RadiusZoneShape(radius, height)
        };
        await ZonesPlugin.Instance.ZoneManager.CreateAndSaveZoneAsync(definition);
        CommandBase.Reply(caller, $"Zone '{id}' created at {player.Position} (radius={radius}, height={height}).", Color.green);
    }
}
