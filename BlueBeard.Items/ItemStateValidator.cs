using SDG.Unturned;

namespace BlueBeard.Items;

/// <summary>
/// Gatekeeper for <see cref="ItemStateEncoder"/>. Unturned reads specific byte offsets in
/// weapon and attachment state for attachments, ammo count, fire mode, durability, etc.
/// Writing custom data into those states will corrupt them. Always check with this validator
/// before encoding.
/// </summary>
public static class ItemStateValidator
{
    /// <summary>
    /// Returns false if writing custom data into <paramref name="asset"/>'s state bytes
    /// would corrupt the item. Returns true for asset types where custom encoding is safe
    /// (generic items, storage, tools, etc.).
    /// </summary>
    public static bool IsSafeForCustomState(ItemAsset asset)
    {
        if (asset == null) return false;
        if (asset is ItemGunAsset) return false;
        if (asset is ItemMeleeAsset) return false;
        if (asset is ItemThrowableAsset) return false;
        if (asset is ItemMagazineAsset) return false;
        if (asset is ItemSightAsset) return false;
        if (asset is ItemTacticalAsset) return false;
        if (asset is ItemGripAsset) return false;
        if (asset is ItemBarrelAsset) return false;
        return true;
    }

    /// <summary>
    /// Convenience overload — looks up the asset by ID and delegates to
    /// <see cref="IsSafeForCustomState(ItemAsset)"/>.
    /// </summary>
    public static bool IsSafeForCustomState(ushort assetId)
    {
        var asset = Assets.find(EAssetType.ITEM, assetId) as ItemAsset;
        return IsSafeForCustomState(asset);
    }
}
