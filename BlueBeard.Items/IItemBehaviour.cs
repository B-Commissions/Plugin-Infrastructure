using SDG.Unturned;

namespace BlueBeard.Items;

/// <summary>
/// Server-side behaviour attached to a specific item asset ID via
/// <see cref="ItemBehaviourManager.Register"/>. Implement the hooks you care about; inherit
/// <see cref="ItemBehaviourBase"/> for virtual no-op defaults.
/// </summary>
public interface IItemBehaviour
{
    /// <summary>Player equipped an item of this asset ID (switched it into their hands).</summary>
    void OnEquipped(Player player, ItemJar jar);

    /// <summary>Player dequipped an item of this asset ID (put it away or switched to another).</summary>
    void OnDequipped(Player player, ItemJar jar);

    /// <summary>Player performed the primary action with this item.</summary>
    void OnUsed(Player player, ItemJar jar);

    /// <summary>Player dropped this item from their inventory.</summary>
    void OnDropped(Player player, ItemJar jar);

    /// <summary>
    /// Player attempted to pick up this item. Return false to prevent the pickup.
    /// </summary>
    bool OnPickedUp(Player player, ItemJar jar);
}
