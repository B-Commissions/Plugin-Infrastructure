using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace BlueBeard.Zones.Commands.NodeCmds;

internal class NodeUndoCommand : SubCommand
{
    public override string Name => "undo";
    public override string Permission => "zone.node.undo";
    public override string Help => "Remove the last added vertex.";
    public override string Syntax => "";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (caller is not UnturnedPlayer player)
        {
            CommandBase.Reply(caller, "This command can only be used by players.", Color.red);
            return Task.CompletedTask;
        }

        var builder = ZonesPlugin.Instance.ZoneBuilderManager;
        if (!builder.RemoveLastNode(player.Player))
        {
            CommandBase.Reply(caller, "No active session or no nodes to remove.", Color.red);
            return Task.CompletedTask;
        }

        var session = builder.GetSession(player.Player);
        CommandBase.Reply(caller, $"Last node removed. {session.Nodes.Count} node(s) remaining.", Color.green);
        return Task.CompletedTask;
    }
}
