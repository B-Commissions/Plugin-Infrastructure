using SDG.Unturned;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Server-side behaviour keyed by animal asset id (deer, cow, wolf, etc.). Registered with
/// <c>AnimalBehaviourManager.Register</c>. Inherit <see cref="AnimalBehaviourBase"/>
/// for virtual no-op defaults.
/// </summary>
public interface IAnimalBehaviour
{
    /// <summary>Damage is about to be applied. Return false to prevent.</summary>
    bool OnDamageRequested(ref DamageAnimalParameters parameters);

    /// <summary>Animal was killed. Invoked manually via <see cref="AnimalBehaviourManager.NotifyKilled"/>.</summary>
    void OnKilled(Animal animal);

    /// <summary>Animal was spawned. Invoked manually via <see cref="AnimalBehaviourManager.NotifySpawned"/>.</summary>
    void OnSpawned(Animal animal);
}
