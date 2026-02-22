using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.BlockListCmds;

internal class BlockListAddItemCommand : SubCommand
{
    public override string Name => "additem";
    public override string Permission => "zone.blocklist.additem";
    public override string Help => "Add an item to a block list.";
    public override string Syntax => "<name> <itemId>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 2)
        {
            CommandBase.Reply(caller, $"Usage: /zone blocklist additem {Syntax}", Color.yellow);
            return;
        }

        var manager = ZonesPlugin.Instance.BlockListManager;
        if (manager.GetBlockList(args[0]) == null)
        {
            CommandBase.Reply(caller, $"Block list '{args[0]}' not found.", Color.red);
            return;
        }

        if (!ushort.TryParse(args[1], out var itemId))
        {
            CommandBase.Reply(caller, "Invalid item ID. Must be a number.", Color.red);
            return;
        }

        await manager.AddItemAsync(args[0], itemId);
        CommandBase.Reply(caller, $"Item {itemId} added to block list '{args[0]}'.", Color.green);
    }
}
