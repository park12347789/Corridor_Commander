namespace CorridorCommander
{
    public readonly struct InstalledAimInfo
    {
        public InstalledAimInfo(
            string title,
            string levelText,
            string statText,
            string healthText,
            float healthFillAmount,
            bool hasRange,
            float range)
        {
            Title = title;
            LevelText = levelText;
            StatText = statText;
            HealthText = healthText;
            HealthFillAmount = healthFillAmount;
            HasRange = hasRange;
            Range = range;
        }

        public string Title { get; }
        public string LevelText { get; }
        public string StatText { get; }
        public string HealthText { get; }
        public float HealthFillAmount { get; }
        public bool HasRange { get; }
        public float Range { get; }
    }
}
