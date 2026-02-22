using System;
using System.Linq;
using System.Threading.Tasks;
using Rocket.API;

namespace BlueBeard.Core.Commands;

public abstract class SubCommand
{
    public abstract string Name { get; }
    public virtual string[] Aliases => [];
    public abstract string Permission { get; }
    public abstract string Help { get; }
    public abstract string Syntax { get; }

    public bool Matches(string token)
    {
        return string.Equals(Name, token, StringComparison.OrdinalIgnoreCase) ||
               Aliases.Any(a => string.Equals(a, token, StringComparison.OrdinalIgnoreCase));
    }

    public abstract Task Execute(IRocketPlayer caller, string[] args);
}
