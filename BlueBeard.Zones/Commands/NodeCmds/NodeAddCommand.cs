using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace BlueBeard.Zones.Commands.NodeCmds;

internal class NodeAddCommand : SubCommand
{
    public override string Name => "add";
    public override string Permission => "zone.node.add";
    public override string Help => "Add your current position as a vertex.";
    public override string Syntax => "";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (caller is not UnturnedPlayer player)
        {
            CommandBase.Reply(caller, "This command can only be used by players.", Color.red);
            return Task.CompletedTask;
        }

        var builder = ZonesPlugin.Instance.ZoneBuilderManager;
        if (!builder.HasSession(player.Player))
        {
            CommandBase.Reply(caller, "No active build session. Use /zone node start <id> first.", Color.red);
            return Task.CompletedTask;
        }

        builder.AddNode(player.Player);
        var session = builder.GetSession(player.Player);
        CommandBase.Reply(caller, $"Node #{session.Nodes.Count} added at {player.Position}.", Color.green);
        return Task.CompletedTask;
    }
}
