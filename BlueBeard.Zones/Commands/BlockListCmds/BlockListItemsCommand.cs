using System.Linq;
using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.BlockListCmds;

internal class BlockListItemsCommand : SubCommand
{
    public override string Name => "items";
    public override string Permission => "zone.blocklist.items";
    public override string Help => "List items in a block list.";
    public override string Syntax => "<name>";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 1)
        {
            CommandBase.Reply(caller, $"Usage: /zone blocklist items {Syntax}", Color.yellow);
            return Task.CompletedTask;
        }

        var list = ZonesPlugin.Instance.BlockListManager.GetBlockList(args[0]);
        if (list == null)
        {
            CommandBase.Reply(caller, $"Block list '{args[0]}' not found.", Color.red);
            return Task.CompletedTask;
        }

        if (list.Items.Count == 0)
        {
            CommandBase.Reply(caller, $"Block list '{args[0]}' is empty.", Color.yellow);
            return Task.CompletedTask;
        }

        var itemIds = string.Join(", ", list.Items.OrderBy(i => i));
        CommandBase.Reply(caller, $"Items in '{args[0]}': {itemIds}", Color.cyan);
        return Task.CompletedTask;
    }
}
