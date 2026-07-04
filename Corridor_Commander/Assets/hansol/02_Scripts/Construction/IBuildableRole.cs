namespace CorridorCommander
{
    public interface IBuildableRole
    {
        void Initialize(BuildableObject owner, BuildContext context);
        void Dispose();
    }
}
