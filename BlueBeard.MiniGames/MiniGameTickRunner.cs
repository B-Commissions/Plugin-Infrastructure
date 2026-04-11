using UnityEngine;

namespace BlueBeard.MiniGames;

/// <summary>
/// MonoBehaviour that drives a single <see cref="MiniGameInstance"/>. Attached by
/// <see cref="MiniGameManager.Start"/> and destroyed when the mini-game ends. The manager
/// owns lifecycle — don't add or destroy this component directly.
/// </summary>
internal class MiniGameTickRunner : MonoBehaviour
{
    internal MiniGameInstance Instance;
    internal IMiniGameHandler Handler;
    internal MiniGameManager Manager;

    private void Update()
    {
        if (Instance == null || Handler == null || Manager == null) return;
        if (Instance.State != MiniGameState.Running) return;

        var dt = Time.deltaTime;
        Instance.TimeRemaining -= dt;
        Handler.OnTick(Instance, dt);

        if (Instance.TimeRemaining <= 0f && Instance.State == MiniGameState.Running)
        {
            Manager.Complete(Instance, MiniGameState.TimedOut);
        }
    }
}
