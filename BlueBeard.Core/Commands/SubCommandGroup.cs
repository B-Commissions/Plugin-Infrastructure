using System.Linq;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace BlueBeard.Core.Commands;

public class SubCommandGroup(string name, string[] aliases, string permission, SubCommand[] children)
    : SubCommand
{
    public override string Name => name;
    public override string[] Aliases => aliases;
    public override string Permission => permission;
    public override string Help => $"Manage {name}s";
    public override string Syntax => string.Join(" | ", children.Select(c => c.Name));

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length == 0)
        {
            CommandBase.Reply(caller, $"Usage: {name} <{Syntax}>", Color.yellow);
            return;
        }

        var token = args[0];
        var child = children.FirstOrDefault(c => c.Matches(token));

        if (child == null)
        {
            CommandBase.Reply(caller, $"Unknown sub-command '{token}'. Available: {Syntax}", Color.red);
            return;
        }

        if (caller is UnturnedPlayer player && !player.HasPermission(child.Permission))
        {
            CommandBase.Reply(caller, "You do not have permission to use this command.", Color.red);
            return;
        }

        var remaining = args.Skip(1).ToArray();
        await child.Execute(caller, remaining);
    }
}
