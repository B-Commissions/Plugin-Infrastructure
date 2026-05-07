using SDG.Unturned;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Registry keyed by animal asset id. Damage is auto-dispatched; spawn and kill are manual.
/// </summary>
public class AnimalBehaviourManager : EntityBehaviourRegistry<ushort, IAnimalBehaviour>
{
    public override void Load()
    {
        DamageTool.damageAnimalRequested += OnDamageRequested;
    }

    public override void Unload()
    {
        DamageTool.damageAnimalRequested -= OnDamageRequested;
        base.Unload();
    }

    /// <summary>Manually fire <see cref="IAnimalBehaviour.OnSpawned"/>.</summary>
    public void NotifySpawned(Animal animal)
    {
        if (animal?.asset == null) return;
        if (Behaviours.TryGetValue(animal.asset.id, out var b))
            b.OnSpawned(animal);
    }

    /// <summary>Manually fire <see cref="IAnimalBehaviour.OnKilled"/>.</summary>
    public void NotifyKilled(Animal animal)
    {
        if (animal?.asset == null) return;
        if (Behaviours.TryGetValue(animal.asset.id, out var b))
            b.OnKilled(animal);
    }

    private void OnDamageRequested(ref DamageAnimalParameters parameters, ref bool shouldAllow)
    {
        if (!shouldAllow) return;
        var animal = parameters.animal;
        if (animal?.asset == null) return;
        if (Behaviours.TryGetValue(animal.asset.id, out var b))
        {
            if (!b.OnDamageRequested(ref parameters))
                shouldAllow = false;
        }
    }
}
