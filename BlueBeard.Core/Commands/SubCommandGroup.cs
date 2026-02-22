using System.Linq;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace BlueBeard.Core.Commands;

public class SubCommandGroup : SubCommand
{
    private readonly string _name;
    private readonly string[] _aliases;
    private readonly string _permission;
    private readonly SubCommand[] _children;

    public SubCommandGroup(string name, string[] aliases, string permission, SubCommand[] children)
    {
        _name = name;
        _aliases = aliases;
        _permission = permission;
        _children = children;
    }

    public override string Name => _name;
    public override string[] Aliases => _aliases;
    public override string Permission => _permission;
    public override string Help => $"Manage {_name}s";
    public override string Syntax => string.Join(" | ", _children.Select(c => c.Name));

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length == 0)
        {
            CommandBase.Reply(caller, $"Usage: {_name} <{Syntax}>", Color.yellow);
            return;
        }

        var token = args[0];
        var child = _children.FirstOrDefault(c => c.Matches(token));

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
