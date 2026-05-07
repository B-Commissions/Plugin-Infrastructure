using System;
using System.Collections;
using BlueBeard.Core.Abstractions;
using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.OpenMod;

/// <summary>
/// Main-thread dispatcher for OpenMod environments. OpenMod itself doesn't expose a one-shot
/// "queue on main thread" helper in the same shape as Rocket's <c>TaskDispatcher</c>, so this
/// uses Unturned's own coroutine runner via <see cref="ThreadUtil.assertIsGameThread"/> and a
/// MonoBehaviour scheduler. Falls back to direct invocation when already on the main thread.
/// </summary>
public sealed class OpenModTaskDispatcher : ITaskDispatcher
{
    private static MainThreadRunner _runner;

    public void QueueOnMainThread(System.Action action, float delaySeconds = 0)
    {
        if (action == null) return;

        if (delaySeconds <= 0 && IsOnMainThread())
        {
            try { action(); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            return;
        }

        EnsureRunner();
        _runner.Enqueue(action, delaySeconds);
    }

    private static bool IsOnMainThread()
    {
        try { ThreadUtil.assertIsGameThread(); return true; }
        catch { return false; }
    }

    private static void EnsureRunner()
    {
        if (_runner != null) return;
        var go = new GameObject("BlueBeard_OpenMod_MainThreadRunner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<MainThreadRunner>();
    }

    private sealed class MainThreadRunner : MonoBehaviour
    {
        public void Enqueue(System.Action action, float delaySeconds)
        {
            StartCoroutine(Run(action, delaySeconds));
        }

        private static IEnumerator Run(System.Action action, float delaySeconds)
        {
            if (delaySeconds > 0) yield return new WaitForSeconds(delaySeconds);
            try { action(); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
        }
    }
}
