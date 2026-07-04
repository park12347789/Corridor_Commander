namespace CorridorCommander
{
    public static class TEMP_CommandInputState
    {
        public static bool CommandPanelOpen { get; set; }
        public static bool PointerPanelOpen { get; set; }
        public static bool CentralInputContextBlocksHotkeys { get; set; }
        private static int centralInputConsumedFrame = -1;

        public static void MarkCentralInputConsumed()
        {
            centralInputConsumedFrame = UnityEngine.Time.frameCount;
        }

        public static bool BlocksHotkeys =>
            CommandPanelOpen
            || PointerPanelOpen
            || CentralInputContextBlocksHotkeys
            || centralInputConsumedFrame == UnityEngine.Time.frameCount
            || UiInputCoordinator.BlocksHotkeys;

        public static bool BlocksGameplayInput =>
            PointerPanelOpen || UiInputCoordinator.BlocksGameplayInput;

        public static bool BlocksLookInput =>
            PointerPanelOpen || !UiInputCoordinator.CanLook;

        public static bool PointerInputActive =>
            PointerPanelOpen || UiInputCoordinator.PointerModeActive;
    }
}
