using SDG.Unturned;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Registry keyed by zombie type (the <see cref="Zombie.type"/> byte). Damage is auto-dispatched;
/// spawn and kill are manual since Unturned exposes no clean global hooks for them.
/// </summary>
public class ZombieBehaviourManager : EntityBehaviourRegistry<byte, IZombieBehaviour>
{
    public override void Load()
    {
        DamageTool.damageZombieRequested += OnDamageRequested;
    }

    public override void Unload()
    {
        DamageTool.damageZombieRequested -= OnDamageRequested;
        base.Unload();
    }

    /// <summary>Manually fire <see cref="IZombieBehaviour.OnSpawned"/>.</summary>
    public void NotifySpawned(Zombie zombie)
    {
        if (zombie == null) return;
        if (Behaviours.TryGetValue(zombie.type, out var b))
            b.OnSpawned(zombie);
    }

    /// <summary>Manually fire <see cref="IZombieBehaviour.OnKilled"/>.</summary>
    public void NotifyKilled(Zombie zombie)
    {
        if (zombie == null) return;
        if (Behaviours.TryGetValue(zombie.type, out var b))
            b.OnKilled(zombie);
    }

    private void OnDamageRequested(ref DamageZombieParameters parameters, ref bool shouldAllow)
    {
        if (!shouldAllow) return;
        var zombie = parameters.zombie;
        if (zombie == null) return;
        if (Behaviours.TryGetValue(zombie.type, out var b))
        {
            if (!b.OnDamageRequested(ref parameters))
                shouldAllow = false;
        }
    }
}
