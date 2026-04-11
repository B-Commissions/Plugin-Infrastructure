using SDG.Unturned;

namespace BlueBeard.Items;

/// <summary>
/// Convenience base class for <see cref="IItemBehaviour"/> implementations. Every hook is a
/// virtual no-op so subclasses override only the ones they care about. <see cref="OnPickedUp"/>
/// defaults to true (allow pickup).
/// </summary>
public abstract class ItemBehaviourBase : IItemBehaviour
{
    public virtual void OnEquipped(Player player, ItemJar jar) { }
    public virtual void OnDequipped(Player player, ItemJar jar) { }
    public virtual void OnUsed(Player player, ItemJar jar) { }
    public virtual void OnDropped(Player player, ItemJar jar) { }
    public virtual bool OnPickedUp(Player player, ItemJar jar) => true;
}
