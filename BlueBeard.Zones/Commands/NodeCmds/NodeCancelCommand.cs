using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace BlueBeard.Zones.Commands.NodeCmds;

internal class NodeCancelCommand : SubCommand
{
    public override string Name => "cancel";
    public override string Permission => "zone.node.cancel";
    public override string Help => "Discard the current build session.";
    public override string Syntax => "";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (caller is not UnturnedPlayer player)
        {
            CommandBase.Reply(caller, "This command can only be used by players.", Color.red);
            return Task.CompletedTask;
        }

        var builder = ZonesPlugin.Instance.ZoneBuilderManager;
        if (!builder.CancelSession(player.Player))
        {
            CommandBase.Reply(caller, "No active build session.", Color.red);
            return Task.CompletedTask;
        }

        CommandBase.Reply(caller, "Build session cancelled.", Color.green);
        return Task.CompletedTask;
    }
}
