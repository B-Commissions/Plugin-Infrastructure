using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace BlueBeard.Zones.Commands.NodeCmds;

internal class NodeStartCommand : SubCommand
{
    public override string Name => "start";
    public override string Permission => "zone.node.start";
    public override string Help => "Begin a polygon zone build session.";
    public override string Syntax => "<id> [height]";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (caller is not UnturnedPlayer player)
        {
            CommandBase.Reply(caller, "This command can only be used by players.", Color.red);
            return Task.CompletedTask;
        }

        if (args.Length < 1)
        {
            CommandBase.Reply(caller, $"Usage: /zone node start {Syntax}", Color.yellow);
            return Task.CompletedTask;
        }

        var builder = ZonesPlugin.Instance.ZoneBuilderManager;
        if (builder.HasSession(player.Player))
        {
            CommandBase.Reply(caller, "You already have an active build session. Use /zone node cancel first.", Color.red);
            return Task.CompletedTask;
        }

        var id = args[0];
        var height = 30f;
        if (args.Length >= 2 && !float.TryParse(args[1], out height))
        {
            CommandBase.Reply(caller, "Invalid height. Must be a number.", Color.red);
            return Task.CompletedTask;
        }

        builder.StartSession(player.Player, id, height);
        CommandBase.Reply(caller, $"Build session started for zone '{id}' (height={height}). Walk to positions and use /zone node add.", Color.green);
        return Task.CompletedTask;
    }
}
