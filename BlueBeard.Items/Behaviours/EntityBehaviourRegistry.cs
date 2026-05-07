using System;
using System.Collections.Generic;
using BlueBeard.Core;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Generic base for per-key behaviour registries. Stores one behaviour per key, exposes
/// Register / Unregister / GetBehaviour in the same shape as <see cref="ItemBehaviourManager"/>,
/// and lets concrete subclasses wire their own SDG event hooks in <see cref="Load"/>.
/// </summary>
public abstract class EntityBehaviourRegistry<TKey, TBehaviour> : IManager
    where TBehaviour : class
{
    protected readonly Dictionary<TKey, TBehaviour> Behaviours = new();

    public void Register(TKey key, TBehaviour behaviour)
    {
        if (behaviour == null) throw new ArgumentNullException(nameof(behaviour));
        Behaviours[key] = behaviour;
    }

    public void Unregister(TKey key) => Behaviours.Remove(key);

    public TBehaviour GetBehaviour(TKey key) =>
        Behaviours.TryGetValue(key, out var b) ? b : null;

    public IReadOnlyDictionary<TKey, TBehaviour> All => Behaviours;

    public bool TryGet(TKey key, out TBehaviour behaviour) => Behaviours.TryGetValue(key, out behaviour);

    public abstract void Load();

    public virtual void Unload()
    {
        Behaviours.Clear();
    }
}
