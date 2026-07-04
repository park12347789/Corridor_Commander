namespace CorridorCommander
{
    public interface IInstalledAimInfoProvider
    {
        bool TryGetAimInfo(out InstalledAimInfo info);
    }
}
