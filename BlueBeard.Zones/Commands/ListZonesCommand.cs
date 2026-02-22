using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands;

internal class ListZonesCommand : SubCommand
{
    public override string Name => "list";
    public override string Permission => "zone.list";
    public override string Help => "List all active zones.";
    public override string Syntax => "";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        var zones = ZonesPlugin.Instance.ZoneManager.Zones;
        if (zones.Count == 0)
        {
            CommandBase.Reply(caller, "No active zones.", Color.yellow);
            return Task.CompletedTask;
        }
        foreach (var kvp in zones)
        {
            var go = kvp.Value;
            var zone = go.GetComponent<ZoneComponent>();
            var shapeName = zone != null ? zone.Definition.Shape.GetType().Name : "Unknown";
            CommandBase.Reply(caller, $"{kvp.Key} Center={go.transform.position} Shape={shapeName}", Color.cyan);
        }
        return Task.CompletedTask;
    }
}
