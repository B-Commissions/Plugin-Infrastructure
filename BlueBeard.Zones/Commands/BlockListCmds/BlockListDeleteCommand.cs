using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.BlockListCmds;

internal class BlockListDeleteCommand : SubCommand
{
    public override string Name => "delete";
    public override string Permission => "zone.blocklist.delete";
    public override string Help => "Delete a block list.";
    public override string Syntax => "<name>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 1)
        {
            CommandBase.Reply(caller, $"Usage: /zone blocklist delete {Syntax}", Color.yellow);
            return;
        }

        var manager = ZonesPlugin.Instance.BlockListManager;
        if (manager.GetBlockList(args[0]) == null)
        {
            CommandBase.Reply(caller, $"Block list '{args[0]}' not found.", Color.red);
            return;
        }

        await manager.DeleteBlockListAsync(args[0]);
        CommandBase.Reply(caller, $"Block list '{args[0]}' deleted.", Color.green);
    }
}
