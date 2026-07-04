using System;
using System.Collections.Concurrent;
using BlueBeard.Core.Abstractions;
using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.OpenMod;

/// <summary>
/// Main-thread dispatcher for OpenMod environments. OpenMod itself doesn't expose a one-shot
/// "queue on main thread" helper in the same shape as Rocket's <c>TaskDispatcher</c>, so this
/// pre-creates a MonoBehaviour runner (on the main thread, during
/// <see cref="OpenModBootstrap.Install"/>) that drains a thread-safe queue in Update().
///
/// <see cref="QueueOnMainThread"/> itself only touches the queue, so it is safe to call from
/// any thread — which is the entire point of the API. Falls back to direct invocation when
/// already on the main thread with no delay.
/// </summary>
public sealed class OpenModTaskDispatcher : ITaskDispatcher
{
    private static MainThreadRunner _runner;

    /// <summary>
    /// Create the runner GameObject. MUST be called on the Unity main thread
    /// (OpenModBootstrap.Install does this); GameObject/AddComponent are main-thread-only.
    /// </summary>
    internal static void InitializeRunner()
    {
        if (_runner != null) return;
        var go = new GameObject("BlueBeard_OpenMod_MainThreadRunner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<MainThreadRunner>();
    }

    /// <summary>
    /// Destroy the runner. Called from OpenModBootstrap.Uninstall so hot-reloads don't
    /// stack orphaned runner objects holding references into the old assembly.
    /// </summary>
    internal static void DestroyRunner()
    {
        if (_runner == null) return;
        UnityEngine.Object.Destroy(_runner.gameObject);
        _runner = null;
    }

    public void QueueOnMainThread(System.Action action, float delaySeconds = 0)
    {
        if (action == null) return;

        if (delaySeconds <= 0 && IsOnMainThread())
        {
            try { action(); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            return;
        }

        var runner = _runner;
        if (runner == null)
        {
            // Bootstrap not installed (or already uninstalled). Only safe fallback is
            // direct invocation on the main thread; off-thread we must fail loudly rather
            // than corrupt Unity state.
            if (IsOnMainThread())
            {
                try { action(); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
                return;
            }
            throw new InvalidOperationException(
                "OpenModTaskDispatcher has no runner — call OpenModBootstrap.Install() before " +
                "queueing work from background threads.");
        }

        // Thread-safe: only enqueues. The runner computes the due time on the main thread
        // (UnityEngine.Time is main-thread-only).
        runner.Enqueue(action, delaySeconds);
    }

    private static bool IsOnMainThread()
    {
        try { ThreadUtil.assertIsGameThread(); return true; }
        catch { return false; }
    }

    private sealed class MainThreadRunner : MonoBehaviour
    {
        private readonly ConcurrentQueue<(System.Action Action, float DelaySeconds)> _incoming = new();
        private readonly System.Collections.Generic.List<(System.Action Action, float DueTime)> _delayed = [];

        public void Enqueue(System.Action action, float delaySeconds) =>
            _incoming.Enqueue((action, delaySeconds));

        private void Update()
        {
            while (_incoming.TryDequeue(out var item))
            {
                if (item.DelaySeconds <= 0) Run(item.Action);
                else _delayed.Add((item.Action, Time.unscaledTime + item.DelaySeconds));
            }

            for (var i = _delayed.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < _delayed[i].DueTime) continue;
                var action = _delayed[i].Action;
                _delayed.RemoveAt(i);
                Run(action);
            }
        }

        private static void Run(System.Action action)
        {
            try { action(); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
        }
    }
}
