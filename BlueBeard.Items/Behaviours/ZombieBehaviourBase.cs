using SDG.Unturned;

namespace BlueBeard.Items.Behaviours;

public abstract class ZombieBehaviourBase : IZombieBehaviour
{
    public virtual bool OnDamageRequested(ref DamageZombieParameters parameters) => true;
    public virtual void OnKilled(Zombie zombie) { }
    public virtual void OnSpawned(Zombie zombie) { }
}
