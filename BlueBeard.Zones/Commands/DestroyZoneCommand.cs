using System;
using System.Linq;
using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands;

internal class DestroyZoneCommand : SubCommand
{
    public override string Name => "destroy";
    public override string Permission => "zone.destroy";
    public override string Help => "Destroy a zone by ID or all zones.";
    public override string Syntax => "<id | all>";

    public override async Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 1)
        {
            CommandBase.Reply(caller, $"Usage: /zone destroy {Syntax}", Color.yellow);
            return;
        }
        var manager = ZonesPlugin.Instance.ZoneManager;
        if (args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var keys = manager.Zones.Keys.ToList();
            foreach (var key in keys)
                await manager.DestroyAndDeleteZoneAsync(key);
            CommandBase.Reply(caller, $"Destroyed {keys.Count} zone(s).", Color.green);
            return;
        }
        var id = args[0];
        if (!manager.Zones.ContainsKey(id))
        {
            CommandBase.Reply(caller, $"Zone '{id}' not found.", Color.red);
            return;
        }
        await manager.DestroyAndDeleteZoneAsync(id);
        CommandBase.Reply(caller, $"Zone '{id}' destroyed.", Color.green);
    }
}
