#if DEBUG
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace BlueBeard.SnapLogic.Commands;

/// <summary>
/// Debug command: player looks at a barricade, and this dumps all nearby barricade
/// positions relative to it as offsets. Useful for discovering snap point positions.
/// </summary>
public class DumpSubCommand : SubCommand
{
    public override string Name => "dump";
    public override string Permission => "snap.dump";
    public override string Help => "Dumps nearby barricade positions relative to the looked-at barricade.";
    public override string Syntax => "/snap dump [radius]";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (caller is not UnturnedPlayer player)
            return Task.CompletedTask;

        var radius = 5f;
        if (args.Length > 0 && float.TryParse(args[0], out var parsedRadius))
            radius = parsedRadius;

        // Raycast from the player's camera to find the target barricade
        var look = player.Player.look;
        var ray = new Ray(look.aim.position, look.aim.forward);

        if (!Physics.Raycast(ray, out var hit, 10f, RayMasks.BARRICADE))
        {
            CommandBase.Reply(caller, "No barricade found. Look at a barricade and try again.", Color.red);
            return Task.CompletedTask;
        }

        var hostDrop = BarricadeManager.FindBarricadeByRootTransform(hit.transform);
        if (hostDrop == null)
        {
            CommandBase.Reply(caller, "Could not identify barricade. Try looking directly at it.", Color.red);
            return Task.CompletedTask;
        }

        var hostTransform = hostDrop.model;
        var hostPos = hostTransform.position;
        var hostAssetId = hostDrop.asset.id;

        var sb = new StringBuilder();
        sb.AppendLine($"Host: AssetId={hostAssetId} InstanceId={hostDrop.instanceID} Position=({hostPos.x:F2}, {hostPos.y:F2}, {hostPos.z:F2})");
        sb.AppendLine($"Rotation=({hostTransform.rotation.eulerAngles.x:F2}, {hostTransform.rotation.eulerAngles.y:F2}, {hostTransform.rotation.eulerAngles.z:F2})");
        sb.AppendLine("---");

        var count = 0;

        if (BarricadeManager.regions != null)
        {
            for (byte rx = 0; rx < Regions.WORLD_SIZE; rx++)
            {
                for (byte ry = 0; ry < Regions.WORLD_SIZE; ry++)
                {
                    var region = BarricadeManager.regions[rx, ry];
                    if (region?.drops == null)
                        continue;

                    foreach (var drop in region.drops)
                    {
                        if (drop?.asset == null || drop.model == null)
                            continue;

                        if (drop.instanceID == hostDrop.instanceID)
                            continue;

                        var childPos = drop.model.position;
                        if (Vector3.Distance(childPos, hostPos) > radius)
                            continue;

                        var localOffset = hostTransform.InverseTransformPoint(childPos);
                        sb.AppendLine($"AssetId={drop.asset.id} Offset=({localOffset.x:F2}, {localOffset.y:F2}, {localOffset.z:F2})");
                        count++;
                    }
                }
            }
        }

        if (count == 0)
        {
            CommandBase.Reply(caller, $"No barricades found within {radius} units of the target.", Color.yellow);
            return Task.CompletedTask;
        }

        try
        {
            var fileName = $"snap_dump_{hostAssetId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var directory = Rocket.Core.Environment.PluginsDirectory;
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, sb.ToString());

            CommandBase.Reply(caller, $"Dumped {count} barricade positions to {fileName}", Color.green);
            Logger.Log($"[SnapLogic] Snap dump saved to: {filePath}");
        }
        catch (Exception ex)
        {
            CommandBase.Reply(caller, "Failed to write dump file. Check server logs.", Color.red);
            Logger.LogException(ex);
        }

        return Task.CompletedTask;
    }
}
#endif
