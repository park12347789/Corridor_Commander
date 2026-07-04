using UnityEngine;
using UnityEngine.InputSystem;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class UiInputCoordinator : MonoBehaviour
    {
        private static UiInputCoordinator instance;
        private static bool missingInstanceWarned;

        private Object activeOwner;
        private UiInputContext activeContext;
        private bool pointerModeActive;
        private CursorLockMode cursorLockBeforePointer;
        private bool cursorVisibleBeforePointer;
        private int interactFrame = -1;
        private Object interactOwner;
        private int menuInputFrame = -1;
        private Object menuInputOwner;
        private int commandInputFrame = -1;
        private Object commandInputOwner;
        private int consumedInputFrame = -1;
        private bool gameplayPauseActive;
        private float timeScaleBeforeGameplayPause = 1f;

        public static UiInputCoordinator Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<UiInputCoordinator>(FindObjectsInactive.Include);
                }

                if (instance == null)
                {
                    WarnIfMissingInstance();
                }

                return instance;
            }
        }

        public static UiInputContext CurrentContext => Instance != null ? Instance.activeContext : UiInputContext.None;
        public static bool HasActiveContext => Instance != null && Instance.activeContext != UiInputContext.None;
        public static bool PointerModeActive => Instance != null && Instance.pointerModeActive;
        public static bool HasConsumedInputThisFrame => Instance != null && Instance.consumedInputFrame == Time.frameCount;
        public static bool BlocksHotkeys => HasActiveContext || HasConsumedInputThisFrame;
        public static bool BlocksGameplayInput => HasActiveContext || HasConsumedInputThisFrame;
        public static bool CanLook => !HasActiveContext;

        public static void EndContextIfActive(Object owner)
        {
            if (instance != null)
            {
                instance.EndContext(owner);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            missingInstanceWarned = false;
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            ClearContext();
            instance = null;
        }

        private static void WarnIfMissingInstance()
        {
            if (missingInstanceWarned)
            {
                return;
            }

            Debug.LogWarning("[UiInputCoordinator] No coordinator exists in the active scene.");
            missingInstanceWarned = true;
        }

        public bool CanUseWorldInteraction(Object owner)
        {
            return owner != null && (!HasActiveContext || activeOwner == owner);
        }

        public bool CanUseCommandHotkeys(Object owner)
        {
            return owner != null && (!HasActiveContext || activeOwner == owner);
        }

        public bool TryConsumeInteract(Object owner)
        {
            if (!CanUseWorldInteraction(owner))
            {
                return false;
            }

            return TryConsumeFrame(owner, ref interactFrame, ref interactOwner);
        }

        public bool TryConsumeMenuSlot(Object owner, int slotNumber)
        {
            if (slotNumber <= 0 || activeOwner != owner)
            {
                return false;
            }

            return TryConsumeFrame(owner, ref menuInputFrame, ref menuInputOwner);
        }

        public bool TryConsumeCancel(Object owner)
        {
            if (activeOwner != owner)
            {
                return false;
            }

            return TryConsumeFrame(owner, ref menuInputFrame, ref menuInputOwner);
        }

        public bool TryConsumeContextInput(Object owner)
        {
            if (activeOwner != owner)
            {
                return false;
            }

            return TryConsumeFrame(owner, ref menuInputFrame, ref menuInputOwner);
        }

        public bool TryConsumeCommandSlot(Object owner, int slotNumber)
        {
            if (slotNumber <= 0 || !CanUseCommandHotkeys(owner))
            {
                return false;
            }

            return TryConsumeFrame(owner, ref commandInputFrame, ref commandInputOwner);
        }

        public bool TryBeginContext(Object owner, UiInputContext context, bool enablePointerMode = false)
        {
            return TryBeginContext(owner, context, enablePointerMode, false);
        }

        public bool TryBeginPausedContext(Object owner, UiInputContext context, bool enablePointerMode = false)
        {
            return TryBeginContext(owner, context, enablePointerMode, true);
        }

        private bool TryBeginContext(Object owner, UiInputContext context, bool enablePointerMode, bool pauseGameplay)
        {
            if (owner == null || context == UiInputContext.None)
            {
                return false;
            }

            if (HasActiveContext)
            {
                if (activeOwner != owner || activeContext != context)
                {
                    return false;
                }

                if (enablePointerMode)
                {
                    BeginPointerMode();
                }

                if (pauseGameplay)
                {
                    BeginGameplayPause();
                }

                return true;
            }

            activeOwner = owner;
            activeContext = context;

            if (enablePointerMode)
            {
                BeginPointerMode();
            }

            if (pauseGameplay)
            {
                BeginGameplayPause();
            }

            return true;
        }

        public bool TryBeginPointerContext(Object owner, UiInputContext context)
        {
            return TryBeginContext(owner, context, true);
        }

        public void EndContext(Object owner)
        {
            if (owner == null || activeOwner != owner)
            {
                return;
            }

            ClearContext();
        }

        private void BeginPointerMode()
        {
            if (pointerModeActive)
            {
                return;
            }

            cursorLockBeforePointer = Cursor.lockState;
            cursorVisibleBeforePointer = Cursor.visible;
            pointerModeActive = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ClearContext()
        {
            if (pointerModeActive)
            {
                if (activeContext == UiInputContext.PauseMenu)
                {
                    GameplayOptionsController.ApplyGameplayCursorPolicy();
                }
                else
                {
                    Cursor.lockState = cursorLockBeforePointer;
                    Cursor.visible = cursorVisibleBeforePointer;
                }
            }

            activeOwner = null;
            activeContext = UiInputContext.None;
            pointerModeActive = false;
            EndGameplayPause();
        }

        private void BeginGameplayPause()
        {
            if (gameplayPauseActive)
            {
                return;
            }

            timeScaleBeforeGameplayPause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            gameplayPauseActive = true;
        }

        private void EndGameplayPause()
        {
            if (!gameplayPauseActive)
            {
                return;
            }

            Time.timeScale = timeScaleBeforeGameplayPause;
            gameplayPauseActive = false;
        }

        private bool TryConsumeFrame(Object owner, ref int frame, ref Object frameOwner)
        {
            if (owner == null)
            {
                return false;
            }

            if (frame == Time.frameCount && frameOwner != owner)
            {
                return false;
            }

            frame = Time.frameCount;
            frameOwner = owner;
            consumedInputFrame = Time.frameCount;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance = null;
        }
    }

    public static class KeyboardInputMessenger
    {
        public static Keyboard CurrentKeyboard => Keyboard.current;
        public static bool HasKeyboard => CurrentKeyboard != null;
        public static bool WasInteractPressed() => CurrentKeyboard != null && CurrentKeyboard.eKey.wasPressedThisFrame;
        public static bool WasCancelPressed() => CurrentKeyboard != null && CurrentKeyboard.escapeKey.wasPressedThisFrame;
        public static bool WasReloadPressed() => CurrentKeyboard != null && CurrentKeyboard.rKey.wasPressedThisFrame;
        public static bool WasWavePopupPressed() => CurrentKeyboard != null && CurrentKeyboard.pKey.wasPressedThisFrame;
        public static bool WasPagePreviousPressed() => CurrentKeyboard != null && CurrentKeyboard.aKey.wasPressedThisFrame;
        public static bool WasPageNextPressed() => CurrentKeyboard != null && CurrentKeyboard.dKey.wasPressedThisFrame;

        public static bool WasMenuSlotPressed(int slotNumber)
        {
            Keyboard keyboard = CurrentKeyboard;
            if (keyboard == null)
            {
                return false;
            }

            return slotNumber switch
            {
                1 => keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame,
                2 => keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame,
                3 => keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame,
                4 => keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame,
                5 => keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame,
                6 => keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame,
                7 => keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame,
                8 => keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame,
                9 => keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame,
                _ => false
            };
        }

        public static bool WasContextConfirmPressed()
        {
            Keyboard keyboard = CurrentKeyboard;
            return keyboard != null
                && (keyboard.enterKey.wasPressedThisFrame
                    || keyboard.numpadEnterKey.wasPressedThisFrame
                    || keyboard.spaceKey.wasPressedThisFrame);
        }

        public static bool WasWaveConfirmPressed()
        {
            Keyboard keyboard = CurrentKeyboard;
            return keyboard != null
                && (keyboard.enterKey.wasPressedThisFrame
                    || keyboard.numpadEnterKey.wasPressedThisFrame
                    || keyboard.yKey.wasPressedThisFrame
                    || keyboard.eKey.wasPressedThisFrame
                    || WasMenuSlotPressed(1));
        }

        public static bool WasWaveCancelPressed()
        {
            Keyboard keyboard = CurrentKeyboard;
            return keyboard != null
                && (keyboard.escapeKey.wasPressedThisFrame
                    || keyboard.nKey.wasPressedThisFrame
                    || WasMenuSlotPressed(2));
        }

        public static bool WasShortcutPressed(Key key)
        {
            Keyboard keyboard = CurrentKeyboard;
            if (keyboard == null || key == Key.None)
            {
                return false;
            }

            UnityEngine.InputSystem.Controls.KeyControl keyControl = keyboard[key];
            return keyControl != null && keyControl.wasPressedThisFrame;
        }
    }
}
