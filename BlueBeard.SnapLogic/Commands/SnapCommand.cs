#if DEBUG
using System.Collections.Generic;
using BlueBeard.Core.Commands;
using Rocket.API;

namespace BlueBeard.SnapLogic.Commands;

public class SnapCommand : CommandBase
{
    public override AllowedCaller AllowedCaller => AllowedCaller.Player;
    public override string Name => "snap";
    public override string Help => "SnapLogic debug commands.";
    public override string Syntax => "/snap <dump>";
    public override List<string> Aliases => [];
    public override List<string> Permissions => ["snap"];
    public override SubCommand[] Children =>
    [
        new DumpSubCommand()
    ];
}
#endif
