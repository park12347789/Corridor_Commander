namespace CorridorCommander
{
    public interface IInstalledRangeIndicator
    {
        void SetRange(float range);
        void ShowCachedRange();
        void HideRange();
    }
}
