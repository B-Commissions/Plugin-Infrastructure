using SDG.Unturned;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Server-side behaviour keyed by zombie type (e.g. normal, mega, crawler). Registered with
/// <c>ZombieBehaviourManager.Register</c>. Inherit <see cref="ZombieBehaviourBase"/>
/// for virtual no-op defaults.
/// </summary>
public interface IZombieBehaviour
{
    /// <summary>Damage is about to be applied. Return false to prevent.</summary>
    bool OnDamageRequested(ref DamageZombieParameters parameters);

    /// <summary>Zombie was killed. Invoked manually via <see cref="ZombieBehaviourManager.NotifyKilled"/>.</summary>
    void OnKilled(Zombie zombie);

    /// <summary>Zombie was spawned. Invoked manually via <see cref="ZombieBehaviourManager.NotifySpawned"/>.</summary>
    void OnSpawned(Zombie zombie);
}
