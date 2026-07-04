namespace CorridorCommander
{
    public interface IInstalledUpgradeLevelProvider
    {
        int CurrentUpgradeLevel { get; }
        int MaxUpgradeLevel { get; }
        int VisibleUpgradeStars { get; }
    }
}
