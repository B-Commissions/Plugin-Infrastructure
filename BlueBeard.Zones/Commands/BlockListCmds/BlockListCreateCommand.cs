using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.BlockListCmds;

internal class BlockListCreateCommand : SubCommand
{
    public override string Name => "create";
    public override string Permission => "zone.blocklist.create";
    public override string Help => "Create a new block list.";
    public override string Syntax => "<name>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 1)
        {
            CommandBase.Reply(caller, $"Usage: /zone blocklist create {Syntax}", Color.yellow);
            return;
        }

        var manager = ZonesPlugin.Instance.BlockListManager;
        if (manager.GetBlockList(args[0]) != null)
        {
            CommandBase.Reply(caller, $"Block list '{args[0]}' already exists.", Color.red);
            return;
        }

        await manager.CreateBlockListAsync(args[0]);
        CommandBase.Reply(caller, $"Block list '{args[0]}' created.", Color.green);
    }
}
