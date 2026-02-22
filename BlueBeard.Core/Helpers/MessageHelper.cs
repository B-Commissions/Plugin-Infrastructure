using Rocket.API;
using Rocket.Unturned.Chat;
using UnityEngine;

namespace BlueBeard.Core.Helpers;

public class MessageHelper
{
    public static void Say(IRocketPlayer caller, string message, Color color = default)
    {
        if (color == default)
            color = Color.white;

        ThreadHelper.RunSynchronously(() =>
        {
            UnturnedChat.Say(caller, message, color);
        });
    }

    public static void Say(string message, Color color = default)
    {
        if (color == default)
            color = Color.white;

        ThreadHelper.RunSynchronously(() =>
        {
            UnturnedChat.Say(message, color);
        });
    }
}
