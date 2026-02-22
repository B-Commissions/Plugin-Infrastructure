using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace BlueBeard.Zones.Commands.NodeCmds;

internal class NodeFinishCommand : SubCommand
{
    public override string Name => "finish";
    public override string Permission => "zone.node.finish";
    public override string Help => "Create polygon zone from collected nodes.";
    public override string Syntax => "";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (caller is not UnturnedPlayer player)
        {
            CommandBase.Reply(caller, "This command can only be used by players.", Color.red);
            return;
        }

        var builder = ZonesPlugin.Instance.ZoneBuilderManager;
        var session = builder.GetSession(player.Player);
        if (session == null)
        {
            CommandBase.Reply(caller, "No active build session.", Color.red);
            return;
        }

        if (session.Nodes.Count < 3)
        {
            CommandBase.Reply(caller, $"Need at least 3 nodes to create a polygon zone. You have {session.Nodes.Count}.", Color.red);
            return;
        }

        var nodeCount = session.Nodes.Count;
        var zoneId = session.ZoneId;
        var result = await builder.FinishSession(player.Player);
        if (result)
            CommandBase.Reply(caller, $"Polygon zone '{zoneId}' created with {nodeCount} vertices.", Color.green);
        else
            CommandBase.Reply(caller, "Failed to create zone.", Color.red);
    }
}
