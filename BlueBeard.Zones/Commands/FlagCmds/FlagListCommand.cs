using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BlueBeard.Zones.Commands.FlagCmds;

internal class FlagListCommand : SubCommand
{
    public override string Name => "list";
    public override string Permission => "zone.flag.list";
    public override string Help => "List flags on a zone.";
    public override string Syntax => "[zoneId]";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 1)
        {
            CommandBase.Reply(caller, "Available flags: noDamage, noPlayerDamage, noVehicleDamage, noTireDamage, noAnimalDamage, noZombieDamage, noEnter, noLeave, noVehicleCarjack, noPvP, noBuild, noItemEquip, noLockpick, noZombie, noVehicleSiphoning, infiniteGenerator, enterMessage, leaveMessage, enterAddEffect, leaveAddEffect, enterRemoveEffect, leaveRemoveEffect, enterAddGroup, enterRemoveGroup, leaveAddGroup, leaveRemoveGroup", Color.cyan);
            return Task.CompletedTask;
        }

        var zone = ZonesPlugin.Instance.ZoneManager.GetZone(args[0]);
        if (zone == null)
        {
            CommandBase.Reply(caller, $"Zone '{args[0]}' not found.", Color.red);
            return Task.CompletedTask;
        }

        if (zone.Flags == null || zone.Flags.Count == 0)
        {
            CommandBase.Reply(caller, $"Zone '{args[0]}' has no flags.", Color.yellow);
            return Task.CompletedTask;
        }

        CommandBase.Reply(caller, $"Flags on zone '{args[0]}':", Color.cyan);
        foreach (var kvp in zone.Flags)
        {
            var value = string.IsNullOrEmpty(kvp.Value) ? "" : $" = {kvp.Value}";
            CommandBase.Reply(caller, $"  {kvp.Key}{value}", Color.white);
        }
        return Task.CompletedTask;
    }
}
