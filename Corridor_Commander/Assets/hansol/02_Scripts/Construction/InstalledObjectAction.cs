namespace CorridorCommander
{
    public readonly struct InstalledObjectAction
    {
        public InstalledObjectAction(string label, bool isEnabled, bool closeAfterExecute)
            : this(label, isEnabled, closeAfterExecute, string.Empty, false)
        {
        }

        public InstalledObjectAction(
            string label,
            bool isEnabled,
            bool closeAfterExecute,
            string infoLabel,
            bool showCurrencyIcon)
            : this(
                label,
                isEnabled,
                closeAfterExecute,
                infoLabel,
                showCurrencyIcon,
                false,
                false,
                0,
                0,
                0f)
        {
        }

        public InstalledObjectAction(
            string label,
            bool isEnabled,
            bool closeAfterExecute,
            string infoLabel,
            bool showCurrencyIcon,
            bool showUpgradeStars,
            bool showHealthBar,
            int currentValue,
            int maxValue,
            float fillAmount)
            : this(
                label,
                isEnabled,
                closeAfterExecute,
                infoLabel,
                showCurrencyIcon,
                string.Empty,
                showUpgradeStars,
                showHealthBar,
                currentValue,
                maxValue,
                fillAmount)
        {
        }

        public InstalledObjectAction(
            string label,
            bool isEnabled,
            bool closeAfterExecute,
            string infoLabel,
            bool showCurrencyIcon,
            string costLabel,
            bool showUpgradeStars,
            bool showHealthBar,
            int currentValue,
            int maxValue,
            float fillAmount)
        {
            Label = label;
            IsEnabled = isEnabled;
            CloseAfterExecute = closeAfterExecute;
            InfoLabel = infoLabel;
            ShowCurrencyIcon = showCurrencyIcon;
            CostLabel = costLabel;
            ShowUpgradeStars = showUpgradeStars;
            ShowHealthBar = showHealthBar;
            CurrentValue = currentValue;
            MaxValue = maxValue;
            FillAmount = fillAmount;
        }

        public string Label { get; }
        public bool IsEnabled { get; }
        public bool CloseAfterExecute { get; }
        public string InfoLabel { get; }
        public bool ShowCurrencyIcon { get; }
        public string CostLabel { get; }
        public bool ShowUpgradeStars { get; }
        public bool ShowHealthBar { get; }
        public int CurrentValue { get; }
        public int MaxValue { get; }
        public float FillAmount { get; }
    }
}
