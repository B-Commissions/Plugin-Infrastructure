namespace BlueBeard.Zones.Flags;

public interface IFlagHandler
{
    string FlagName { get; }
    void Subscribe();
    void Unsubscribe();
}
