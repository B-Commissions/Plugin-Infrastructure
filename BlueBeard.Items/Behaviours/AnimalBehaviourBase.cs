using SDG.Unturned;

namespace BlueBeard.Items.Behaviours;

public abstract class AnimalBehaviourBase : IAnimalBehaviour
{
    public virtual bool OnDamageRequested(ref DamageAnimalParameters parameters) => true;
    public virtual void OnKilled(Animal animal) { }
    public virtual void OnSpawned(Animal animal) { }
}
