using System.Collections.Generic;
using BlueBeard.Core.Commands;
using BlueBeard.Zones.Commands.BlockListCmds;
using BlueBeard.Zones.Commands.EffectCmds;
using BlueBeard.Zones.Commands.FlagCmds;
using BlueBeard.Zones.Commands.GroupCmds;
using BlueBeard.Zones.Commands.HeightCmds;
using BlueBeard.Zones.Commands.MessageCmds;
using BlueBeard.Zones.Commands.NodeCmds;
using Rocket.API;

namespace BlueBeard.Zones.Commands;

internal class ZoneCommand : CommandBase
{
    public override AllowedCaller AllowedCaller => AllowedCaller.Both;
    public override string Name => "zone";
    public override string Help => "Zone management commands.";
    public override string Syntax => "/zone <create | destroy | list | tp | inzone | node | flag | height | blocklist | message | effect | group>";
    public override List<string> Aliases => [];
    public override List<string> Permissions => ["zone"];

    public override SubCommand[] Children =>
    [
        new CreateZoneCommand(),
        new DestroyZoneCommand(),
        new ListZonesCommand(),
        new TeleportToZoneCommand(),
        new InZoneCommand(),
        new SubCommandGroup("node", [], "zone.node",
        [
            new NodeStartCommand(),
            new NodeAddCommand(),
            new NodeUndoCommand(),
            new NodeListCommand(),
            new NodeFinishCommand(),
            new NodeCancelCommand()
        ]),
        new SubCommandGroup("flag", [], "zone.flag",
        [
            new FlagAddCommand(),
            new FlagRemoveCommand(),
            new FlagListCommand()
        ]),
        new SubCommandGroup("height", [], "zone.height",
        [
            new HeightSetCommand(),
            new HeightRemoveCommand()
        ]),
        new SubCommandGroup("blocklist", ["bl"], "zone.blocklist",
        [
            new BlockListCreateCommand(),
            new BlockListDeleteCommand(),
            new BlockListListCommand(),
            new BlockListAddItemCommand(),
            new BlockListRemoveItemCommand(),
            new BlockListItemsCommand()
        ]),
        new SubCommandGroup("message", ["msg"], "zone.message",
        [
            new MessageSetCommand(),
            new MessageRemoveCommand()
        ]),
        new SubCommandGroup("effect", ["fx"], "zone.effect",
        [
            new EffectAddCommand(),
            new EffectRemoveCommand()
        ]),
        new SubCommandGroup("group", ["grp"], "zone.group",
        [
            new GroupAddCommand(),
            new GroupRemoveCommand()
        ])
    ];
}
