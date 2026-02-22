using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.BlockListCmds;

internal class BlockListListCommand : SubCommand
{
    public override string Name => "list";
    public override string Permission => "zone.blocklist.list";
    public override string Help => "List all block lists.";
    public override string Syntax => "";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        var lists = ZonesPlugin.Instance.BlockListManager.GetAllBlockLists();
        if (lists.Count == 0)
        {
            CommandBase.Reply(caller, "No block lists.", Color.yellow);
            return Task.CompletedTask;
        }

        foreach (var kvp in lists)
            CommandBase.Reply(caller, $"{kvp.Key} ({kvp.Value.Items.Count} items)", Color.cyan);

        return Task.CompletedTask;
    }
}
