namespace BlueBeard.Zones.Flags;

public static class ZoneFlag
{
    // Damage
    public const string NoDamage = "noDamage";
    public const string NoPlayerDamage = "noPlayerDamage";
    public const string NoVehicleDamage = "noVehicleDamage";
    public const string NoTireDamage = "noTireDamage";
    public const string NoAnimalDamage = "noAnimalDamage";
    public const string NoZombieDamage = "noZombieDamage";

    // Access
    public const string NoEnter = "noEnter";
    public const string NoLeave = "noLeave";
    public const string NoVehicleCarjack = "noVehicleCarjack";
    public const string NoPvP = "noPvP";

    // Build / Items
    public const string NoBuild = "noBuild";
    public const string NoItemEquip = "noItemEquip";
    public const string NoLockpick = "noLockpick";

    // Environment
    public const string NoZombie = "noZombie";
    public const string NoVehicleSiphoning = "noVehicleSiphoning";
    public const string InfiniteGenerator = "infiniteGenerator";

    // Notifications
    public const string EnterMessage = "enterMessage";
    public const string LeaveMessage = "leaveMessage";

    // Effects
    public const string EnterAddEffect = "enterAddEffect";
    public const string LeaveAddEffect = "leaveAddEffect";
    public const string EnterRemoveEffect = "enterRemoveEffect";
    public const string LeaveRemoveEffect = "leaveRemoveEffect";

    // Groups
    public const string EnterAddGroup = "enterAddGroup";
    public const string EnterRemoveGroup = "enterRemoveGroup";
    public const string LeaveAddGroup = "leaveAddGroup";
    public const string LeaveRemoveGroup = "leaveRemoveGroup";
}
