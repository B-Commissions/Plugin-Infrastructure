using System;
using System.Collections.Generic;
using System.Linq;
using BlueBeard.Core.Helpers;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace BlueBeard.Core.Commands;

public abstract class CommandBase : IRocketCommand
{
    public abstract AllowedCaller AllowedCaller { get; }
    public abstract string Name { get; }
    public abstract string Help { get; }
    public abstract string Syntax { get; }
    public abstract List<string> Aliases { get; }
    public abstract List<string> Permissions { get; }
    public abstract SubCommand[] Children { get; }

    public void Execute(IRocketPlayer caller, string[] command)
    {
        ExecuteRouter(caller, command);
    }

    private async void ExecuteRouter(IRocketPlayer caller, string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                var available = string.Join(" | ", Children.Select(c => c.Name));
                Reply(caller, $"Usage: /{Name} <{available}>", Color.yellow);
                return;
            }

            var token = args[0];
            var child = Children.FirstOrDefault(c => c.Matches(token));

            if (child == null)
            {
                var available = string.Join(" | ", Children.Select(c => c.Name));
                Reply(caller, $"Unknown sub-command '{token}'. Available: {available}", Color.red);
                return;
            }

            if (caller is UnturnedPlayer player && !player.HasPermission(child.Permission))
            {
                Reply(caller, "You do not have permission to use this command.", Color.red);
                return;
            }

            var remaining = args.Skip(1).ToArray();
            await child.Execute(caller, remaining);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            Reply(caller, "An error occurred while executing the command.", Color.red);
        }
    }

    /// <summary>
    /// Send a reply to the caller. Safe from any thread: continuations after an <c>await</c>
    /// in a subcommand resume on the thread pool (Unturned installs no
    /// SynchronizationContext), and UnturnedChat.Say is main-thread-only — off-thread calls
    /// are marshalled back automatically.
    /// </summary>
    public static void Reply(IRocketPlayer caller, string message, Color color = default)
    {
        if (color == default)
            color = Color.white;

        if (caller is UnturnedPlayer player)
        {
            if (IsGameThread())
                UnturnedChat.Say(player, message, color);
            else
                ThreadHelper.RunSynchronously(() => UnturnedChat.Say(player, message, color));
        }
        else
        {
            Logger.Log(message);
        }
    }

    private static bool IsGameThread()
    {
        try { ThreadUtil.assertIsGameThread(); return true; }
        catch { return false; }
    }
}
