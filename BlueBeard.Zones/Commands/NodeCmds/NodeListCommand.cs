using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace BlueBeard.Zones.Commands.NodeCmds;

internal class NodeListCommand : SubCommand
{
    public override string Name => "list";
    public override string Permission => "zone.node.list";
    public override string Help => "Show current session nodes.";
    public override string Syntax => "";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (caller is not UnturnedPlayer player)
        {
            CommandBase.Reply(caller, "This command can only be used by players.", Color.red);
            return Task.CompletedTask;
        }

        var builder = ZonesPlugin.Instance.ZoneBuilderManager;
        var session = builder.GetSession(player.Player);
        if (session == null)
        {
            CommandBase.Reply(caller, "No active build session.", Color.red);
            return Task.CompletedTask;
        }

        CommandBase.Reply(caller, $"Zone '{session.ZoneId}' - {session.Nodes.Count} node(s), height={session.Height}:", Color.cyan);
        for (var i = 0; i < session.Nodes.Count; i++)
        {
            var node = session.Nodes[i];
            CommandBase.Reply(caller, $"  #{i + 1}: ({node.x:F1}, {node.y:F1}, {node.z:F1})", Color.white);
        }
        return Task.CompletedTask;
    }
}
