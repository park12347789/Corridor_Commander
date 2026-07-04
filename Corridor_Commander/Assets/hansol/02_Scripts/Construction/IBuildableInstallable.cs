namespace CorridorCommander
{
    public interface IBuildableInstallable
    {
        BuildableKind Kind { get; }
        void OnInstalled(BuildContext context);
    }
}
