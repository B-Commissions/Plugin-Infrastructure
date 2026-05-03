using System;
using System.Collections.Generic;
using System.Linq;
using BlueBeard.Core;

namespace BlueBeard.Cooldowns;

/// <summary>
/// Centralised cooldown tracker. Stores expiry timestamps keyed by arbitrary strings —
/// callers choose the key convention (typically <c>{domain}.{entityId}</c>, e.g.
/// <c>hotwire.76561198012345678</c>).
///
/// All operations are synchronous and in-memory. Lazy cleanup: expired entries are removed
/// when next accessed via <see cref="IsActive"/> or <see cref="GetRemaining"/>.
///
/// The clock used for expiry comparisons can be injected via the constructor (for tests);
/// defaults to <see cref="DateTime.UtcNow"/>.
/// </summary>
public class CooldownManager(Func<DateTime> utcNow) : IManager
{
    private readonly Dictionary<string, DateTime> _cooldowns = new();
    private readonly Func<DateTime> _utcNow = utcNow ?? (() => DateTime.UtcNow);

    public CooldownManager() : this(null) { }

    /// <summary>Start or overwrite a cooldown lasting <paramref name="durationSeconds"/>.</summary>
    public virtual void Start(string key, float durationSeconds)
    {
        _cooldowns[key] = _utcNow().AddSeconds(durationSeconds);
    }

    /// <summary>Start or overwrite a cooldown lasting <paramref name="duration"/>.</summary>
    public virtual void Start(string key, TimeSpan duration)
    {
        _cooldowns[key] = _utcNow().Add(duration);
    }

    /// <summary>
    /// Returns true if <paramref name="key"/> exists and has not yet expired.
    /// Lazy-removes the entry and returns false if it has expired.
    /// </summary>
    public virtual bool IsActive(string key)
    {
        if (!_cooldowns.TryGetValue(key, out var expiry))
            return false;

        if (_utcNow() >= expiry)
        {
            _cooldowns.Remove(key);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Seconds remaining for <paramref name="key"/>, or 0 if expired / not found.
    /// </summary>
    public virtual float GetRemaining(string key)
    {
        if (!_cooldowns.TryGetValue(key, out var expiry))
            return 0f;

        var remaining = (float)(expiry - _utcNow()).TotalSeconds;
        if (remaining <= 0f)
        {
            _cooldowns.Remove(key);
            return 0f;
        }
        return remaining;
    }

    /// <summary>
    /// Atomic check-and-start. If the key is not on cooldown, starts it for
    /// <paramref name="durationSeconds"/> and returns true. Otherwise returns false
    /// and the existing cooldown is left untouched.
    /// </summary>
    public virtual bool TryUse(string key, float durationSeconds)
    {
        if (IsActive(key))
            return false;
        Start(key, durationSeconds);
        return true;
    }

    /// <summary>Remove a single cooldown immediately.</summary>
    public virtual void Cancel(string key)
    {
        _cooldowns.Remove(key);
    }

    /// <summary>
    /// Remove every cooldown whose key starts with <paramref name="prefix"/>.
    /// Use for bulk clearing by domain or entity — e.g. <c>CancelByPrefix("hotwire.")</c>
    /// to clear every hotwire cooldown for every player.
    /// </summary>
    public virtual void CancelByPrefix(string prefix)
    {
        var toRemove = _cooldowns.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var key in toRemove)
            _cooldowns.Remove(key);
    }

    public virtual void Load() { }

    public virtual void Unload()
    {
        _cooldowns.Clear();
    }

    /// <summary>Current in-memory cooldown count (diagnostics / testing).</summary>
    public int Count => _cooldowns.Count;

    /// <summary>
    /// Snapshot of the current cooldown keys. Used by subclasses (e.g.
    /// <c>PersistentCooldownManager</c>) to iterate keys for bulk operations without
    /// exposing the backing dictionary.
    /// </summary>
    protected List<string> GetKeysSnapshot()
    {
        return _cooldowns.Keys.ToList();
    }
}
