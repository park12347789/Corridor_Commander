using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerCamera;

namespace CorridorCommander.PlayerControl
{
    public enum PlayerInputContext
    {
        Gameplay,
        CommandRadial,
        ItemRadial,
        BuildMenu,
        BuildPreview,
        InstalledObjectActionMenu,
        Shop,
        RewardChoice,
        WavePopup,
        ThrowableAim
    }

    public sealed class PlayerCentralInputController : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private PlayerLocomotionController locomotionController;
        [SerializeField] private PlayerFacingController facingController;
        [SerializeField] private PlayerProjectileLauncher weaponLauncher;
        [SerializeField] private PlayerWeaponRuntime weaponRuntime;
        [SerializeField] private PlayerWeaponInventory weaponInventory;
        [SerializeField] private MouseLookThirdPersonCamera thirdPersonCamera;
        [SerializeField] private PauseMenuPresenter pauseMenuPresenter;

        [Header("Cursor")]
        [SerializeField] private bool lockCursorOnPlay = true;

        [Header("Long Press")]
        [SerializeField] private float commandRadialHoldTime = 0.35f;

        [Header("Context")]
        [SerializeField] private PlayerInputContext currentContext = PlayerInputContext.Gameplay;

        [Header("Debug")]
        [SerializeField] private bool logInputEvents = true;

        private float qHoldTimer;
        private bool hasTemporaryCursorOverride;

        public PlayerInputContext CurrentContext => currentContext;
        public bool IsGameplayContext => currentContext == PlayerInputContext.Gameplay;
        public bool BlocksGameplayInput => currentContext != PlayerInputContext.Gameplay;

        public event Action<PlayerInputContext> ContextChanged;

        public event Action CommandCategoryCycleRequested;
        public event Action CommandRadialOpenRequested;
        public event Action CommandRadialCloseRequested;
        public event Action CommandRadialConfirmRequested;

        public event Action ItemRadialOpenRequested;
        public event Action ItemRadialCloseRequested;
        public event Action ItemRadialConfirmRequested;

        public event Action<int> CommandSlotRequested;
        public event Action<int> CommandSlotPressed;
        public event Action<int> CommandSlotReleased;
        public event Action<int> SquadMemberSlotRequested;
        public event Action<int> SquadSelectionStepRequested;
        public event Action ThrowableAimConfirmRequested;
        public event Action ThrowableAimCancelRequested;
        public event Action InteractRequested;

        public event Action<int> BuildMenuSlotRequested;
        public event Action BuildMenuBackRequested;

        public event Action<float> BuildPreviewRotateRequested;
        public event Action BuildPreviewConfirmRequested;
        public event Action BuildPreviewCancelRequested;

        public event Action<int> InstalledObjectActionRequested;
        public event Action InstalledObjectActionMenuCloseRequested;

        public event Action<int> ShopNumberRequested;
        public event Action ShopBackRequested;

        public event Action<int> RewardChoiceRequested;
        public event Action RewardChoiceCloseRequested;

        public event Action WaveReadyPopupRequested;
        public event Action WaveStartConfirmed;
        public event Action WaveStartCanceled;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (lockCursorOnPlay && Application.isPlaying)
            {
                LockCursor();
            }

            UpdateGlobalHotkeyBlockState();
        }

        private void OnDisable()
        {
            TEMP_CommandInputState.CentralInputContextBlocksHotkeys = false;
            UnlockCursor();
            ClearAllDeliveredInputs();
        }

        private void Update()
        {
            Keyboard keyboard = CorridorCommander.KeyboardInputMessenger.CurrentKeyboard;
            Mouse mouse = Mouse.current;

            if (keyboard == null)
            {
                ClearAllDeliveredInputs();
                return;
            }

            if (ShouldBlockForExternalUiContext())
            {
                SendGameplayBlockedInputs();
                qHoldTimer = 0f;
                return;
            }

            switch (currentContext)
            {
                case PlayerInputContext.Gameplay:
                    HandleGameplayContext(keyboard, mouse);
                    break;

                case PlayerInputContext.CommandRadial:
                    HandleCommandRadialContext(keyboard, mouse);
                    break;

                case PlayerInputContext.ItemRadial:
                    HandleItemRadialContext(keyboard, mouse);
                    break;

                case PlayerInputContext.BuildMenu:
                    HandleBuildMenuContext(keyboard);
                    break;

                case PlayerInputContext.BuildPreview:
                    HandleBuildPreviewContext(keyboard, mouse);
                    break;

                case PlayerInputContext.ThrowableAim:
                    HandleThrowableAimContext(keyboard, mouse);
                    break;

                case PlayerInputContext.InstalledObjectActionMenu:
                    HandleInstalledObjectActionMenuContext(keyboard);
                    break;

                case PlayerInputContext.Shop:
                    HandleShopContext(keyboard);
                    break;

                case PlayerInputContext.RewardChoice:
                    HandleRewardChoiceContext(keyboard);
                    break;

                case PlayerInputContext.WavePopup:
                    HandleWavePopupContext(keyboard);
                    break;

                default:
                    SetContext(PlayerInputContext.Gameplay);
                    break;
            }
        }

        public void SetContext(PlayerInputContext nextContext)
        {
            if (currentContext == nextContext)
            {
                return;
            }

            PlayerInputContext previousContext = currentContext;
            currentContext = nextContext;

            qHoldTimer = 0f;
            UpdateGlobalHotkeyBlockState();

            if (currentContext != PlayerInputContext.Gameplay)
            {
                SendGameplayBlockedInputs();
            }

            HandleContextExit(previousContext);
            HandleContextEnter(currentContext);

            if (logInputEvents)
            {
                Debug.Log($"[PlayerCentralInputController] Context: {previousContext} -> {currentContext}");
            }

            ContextChanged?.Invoke(currentContext);
        }

        private void UpdateGlobalHotkeyBlockState()
        {
            TEMP_CommandInputState.CentralInputContextBlocksHotkeys = currentContext != PlayerInputContext.Gameplay;
        }

        public void ReturnToGameplay()
        {
            SetContext(PlayerInputContext.Gameplay);
        }

        public void EnterBuildMenu()
        {
            SetContext(PlayerInputContext.BuildMenu);
        }

        public void EnterBuildPreview()
        {
            SetContext(PlayerInputContext.BuildPreview);
        }

        public void EnterThrowableAim()
        {
            SetContext(PlayerInputContext.ThrowableAim);
        }

        public void EnterInstalledObjectActionMenu()
        {
            SetContext(PlayerInputContext.InstalledObjectActionMenu);
        }

        public void EnterShop()
        {
            SetContext(PlayerInputContext.Shop);
        }

        public void EnterRewardChoice()
        {
            SetContext(PlayerInputContext.RewardChoice);
        }

        public void EnterWavePopup()
        {
            SetContext(PlayerInputContext.WavePopup);
        }

        private bool ShouldBlockForExternalUiContext()
        {
            return currentContext == PlayerInputContext.Gameplay
                && UiInputCoordinator.BlocksGameplayInput;
        }

        private void HandleContextEnter(PlayerInputContext context)
        {
            switch (context)
            {
                case PlayerInputContext.CommandRadial:
                    BeginTemporaryCursorOverride();

                    if (logInputEvents)
                    {
                        Debug.Log("[PlayerCentralInputController] Command Radial Open Requested");
                    }

                    CommandRadialOpenRequested?.Invoke();
                    break;

                case PlayerInputContext.ItemRadial:
                    BeginTemporaryCursorOverride();

                    if (logInputEvents)
                    {
                        Debug.Log("[PlayerCentralInputController] Item Radial Open Requested");
                    }

                    ItemRadialOpenRequested?.Invoke();
                    break;
            }
        }

        private void HandleContextExit(PlayerInputContext context)
        {
            switch (context)
            {
                case PlayerInputContext.CommandRadial:
                    CommandRadialCloseRequested?.Invoke();
                    EndTemporaryCursorOverride();
                    break;

                case PlayerInputContext.ItemRadial:
                    ItemRadialCloseRequested?.Invoke();
                    EndTemporaryCursorOverride();
                    break;
            }
        }

        private void HandleGameplayContext(Keyboard keyboard, Mouse mouse)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (TryOpenPauseMenu())
                {
                    return;
                }

                UnlockCursor();
                return;
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (!IsCursorInGameplayPolicy())
                {
                    LockCursor();
                    return;
                }
            }

            HandleGameplayRadialOpenInputs(keyboard);

            if (currentContext != PlayerInputContext.Gameplay)
            {
                return;
            }

            HandleGameplayMovementAndCombat(keyboard, mouse);
            HandleSquadMemberSelection(keyboard);
            HandleSquadSelectionStep(keyboard);
            HandleGameplayNumberSlots(keyboard);
            HandleGameplayWeaponCycle(keyboard);
            HandleGameplayInteraction(keyboard);
            HandleGameplayWavePopup(keyboard);
        }

        private void HandleGameplayRadialOpenInputs(Keyboard keyboard)
        {
            HandleQInput(keyboard);
        }

        private void HandleQInput(Keyboard keyboard)
        {
            if (keyboard.qKey.wasPressedThisFrame)
            {
                qHoldTimer = 0f;

                if (logInputEvents)
                {
                    Debug.Log("[PlayerCentralInputController] Q Pressed");
                }
            }

            if (keyboard.qKey.isPressed)
            {
                qHoldTimer += Time.unscaledDeltaTime;

                if (qHoldTimer >= commandRadialHoldTime)
                {
                    SetContext(PlayerInputContext.CommandRadial);
                    return;
                }
            }

            if (keyboard.qKey.wasReleasedThisFrame)
            {
                if (qHoldTimer < commandRadialHoldTime)
                {
                    if (logInputEvents)
                    {
                        Debug.Log("[PlayerCentralInputController] Q Short Press - Cycle Command Category");
                    }

                    CommandCategoryCycleRequested?.Invoke();
                }

                qHoldTimer = 0f;
            }
        }

        private void HandleGameplayMovementAndCombat(Keyboard keyboard, Mouse mouse)
        {
            Vector2 moveInput = ReadMoveInput(keyboard);
            bool runHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            bool jumpPressed = keyboard.spaceKey.wasPressedThisFrame;

            locomotionController?.SetMoveInput(moveInput);
            locomotionController?.SetRunHeld(runHeld);

            if (jumpPressed)
            {
                locomotionController?.RequestJump();
            }

            bool aimHeld = mouse != null && mouse.rightButton.isPressed;
            bool fireHeld = mouse != null && mouse.leftButton.isPressed;
            bool firePressed = mouse != null && mouse.leftButton.wasPressedThisFrame;

            facingController?.SetAimHeld(aimHeld);
            facingController?.SetFireHeld(fireHeld);

            weaponLauncher?.SetFireHeld(fireHeld);

            if (firePressed)
            {
                weaponLauncher?.RequestFirePressed();
            }

            HandleGameplayCameraZoom(mouse);

            if (CorridorCommander.KeyboardInputMessenger.WasReloadPressed())
            {
                weaponRuntime?.RequestReload();
            }
        }

        private void HandleGameplayCameraZoom(Mouse mouse)
        {
            if (mouse == null || thirdPersonCamera == null)
            {
                return;
            }

            Vector2 scroll = mouse.scroll.ReadValue();

            if (Mathf.Abs(scroll.y) <= 0.01f)
            {
                return;
            }

            thirdPersonCamera.AdjustZoom(scroll.y);
        }

        private void HandleSquadMemberSelection(Keyboard keyboard)
        {
            Key[] selectionKeys = { Key.F1, Key.F2, Key.F3, Key.F4, Key.F5 };
            for (int i = 0; i < selectionKeys.Length; i++)
            {
                if (keyboard[selectionKeys[i]].wasPressedThisFrame)
                {
                    SquadMemberSlotRequested?.Invoke(i + 1);
                    return;
                }
            }
        }

        private void HandleSquadSelectionStep(Keyboard keyboard)
        {
            if (keyboard.pageUpKey.wasPressedThisFrame)
            {
                SquadSelectionStepRequested?.Invoke(-1);
                return;
            }

            if (keyboard.pageDownKey.wasPressedThisFrame)
            {
                SquadSelectionStepRequested?.Invoke(1);
            }
        }

        private void HandleGameplayNumberSlots(Keyboard keyboard)
        {
            for (int i = 1; i <= 9; i++)
            {
                if (CorridorCommander.KeyboardInputMessenger.WasMenuSlotPressed(i))
                {
                    if (logInputEvents)
                    {
                        Debug.Log($"[PlayerCentralInputController] Command Slot Requested: {i}");
                    }

                    CommandSlotPressed?.Invoke(i);
                    CommandSlotRequested?.Invoke(i);
                    return;
                }

                if (WasNumberReleased(keyboard, i))
                {
                    if (logInputEvents)
                    {
                        Debug.Log($"[PlayerCentralInputController] Command Slot Released: {i}");
                    }

                    CommandSlotReleased?.Invoke(i);
                    return;
                }
            }
        }

        private void HandleGameplayInteraction(Keyboard keyboard)
        {
            if (CorridorCommander.KeyboardInputMessenger.WasInteractPressed())
            {
                InteractRequested?.Invoke();
            }
        }

        private void HandleGameplayWeaponCycle(Keyboard keyboard)
        {
            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (logInputEvents)
                {
                    Debug.Log("[PlayerCentralInputController] Weapon Cycle Requested");
                }

                EquipNextWeapon();
            }
        }

        private void HandleGameplayWavePopup(Keyboard keyboard)
        {
            if (CorridorCommander.KeyboardInputMessenger.WasWavePopupPressed()
                && WaveReadyPopupRequested != null
                && !CorridorCommander.WaveDirector.ShouldSuppressWavePopupHotkey())
            {
                WaveReadyPopupRequested?.Invoke();
                SetContext(PlayerInputContext.WavePopup);
            }
        }

        private void HandleCommandRadialContext(Keyboard keyboard, Mouse mouse)
        {
            SendGameplayBlockedInputs();

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetContext(PlayerInputContext.Gameplay);
                return;
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (logInputEvents)
                {
                    Debug.Log("[PlayerCentralInputController] Command Radial Confirmed By LMB");
                }

                CommandRadialConfirmRequested?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
                return;
            }

            if (keyboard.qKey.wasReleasedThisFrame)
            {
                if (logInputEvents)
                {
                    Debug.Log("[PlayerCentralInputController] Command Radial Confirmed By Q Release");
                }

                CommandRadialConfirmRequested?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
            }
        }

        private void HandleItemRadialContext(Keyboard keyboard, Mouse mouse)
        {
            SendGameplayBlockedInputs();

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetContext(PlayerInputContext.Gameplay);
                return;
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (logInputEvents)
                {
                    Debug.Log("[PlayerCentralInputController] Item Radial Confirmed By LMB");
                }

                ItemRadialConfirmRequested?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
                return;
            }

        }

        private void HandleBuildMenuContext(Keyboard keyboard)
        {
            SendGameplayBlockedInputs();

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                BuildMenuBackRequested?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
                return;
            }

            for (int i = 1; i <= 3; i++)
            {
                if (CorridorCommander.KeyboardInputMessenger.WasMenuSlotPressed(i))
                {
                    TEMP_CommandInputState.MarkCentralInputConsumed();
                    BuildMenuSlotRequested?.Invoke(i);
                    return;
                }
            }
        }

        private void HandleBuildPreviewContext(Keyboard keyboard, Mouse mouse)
        {
            SendGameplayBlockedInputs();

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                BuildPreviewCancelRequested?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
                return;
            }

            if (CorridorCommander.KeyboardInputMessenger.WasReloadPressed())
            {
                BuildPreviewRotateRequested?.Invoke(90f);
                return;
            }

            if (mouse != null)
            {
                Vector2 scroll = mouse.scroll.ReadValue();

                if (Mathf.Abs(scroll.y) > 0.01f)
                {
                    BuildPreviewRotateRequested?.Invoke(scroll.y > 0f ? 15f : -15f);
                    return;
                }

                if (mouse.rightButton.wasPressedThisFrame)
                {
                    BuildPreviewConfirmRequested?.Invoke();
                    SetContext(PlayerInputContext.Gameplay);
                    return;
                }
            }

            if (CorridorCommander.KeyboardInputMessenger.WasInteractPressed())
            {
                BuildPreviewConfirmRequested?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
            }
        }

        private void HandleThrowableAimContext(Keyboard keyboard, Mouse mouse)
        {
            SendGameplayBlockedInputs();

            if (keyboard.escapeKey.wasPressedThisFrame
                || (mouse != null && mouse.rightButton.wasPressedThisFrame))
            {
                ThrowableAimCancelRequested?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
                return;
            }

            for (int i = 1; i <= 9; i++)
            {
                if (WasNumberReleased(keyboard, i))
                {
                    ThrowableAimConfirmRequested?.Invoke();
                    SetContext(PlayerInputContext.Gameplay);
                    return;
                }
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                ThrowableAimConfirmRequested?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
            }
        }

        private void HandleInstalledObjectActionMenuContext(Keyboard keyboard)
        {
            SendGameplayBlockedInputs();

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                InstalledObjectActionMenuCloseRequested?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
                return;
            }

            for (int i = 1; i <= 3; i++)
            {
                if (CorridorCommander.KeyboardInputMessenger.WasMenuSlotPressed(i))
                {
                    TEMP_CommandInputState.MarkCentralInputConsumed();
                    InstalledObjectActionRequested?.Invoke(i);
                    return;
                }
            }
        }

        private void HandleShopContext(Keyboard keyboard)
        {
            SendGameplayBlockedInputs();

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ShopBackRequested?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
                return;
            }

            for (int i = 1; i <= 5; i++)
            {
                if (CorridorCommander.KeyboardInputMessenger.WasMenuSlotPressed(i))
                {
                    TEMP_CommandInputState.MarkCentralInputConsumed();
                    ShopNumberRequested?.Invoke(i);
                    return;
                }
            }
        }

        private void HandleRewardChoiceContext(Keyboard keyboard)
        {
            SendGameplayBlockedInputs();

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                RewardChoiceCloseRequested?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
                return;
            }

            for (int i = 1; i <= 3; i++)
            {
                if (CorridorCommander.KeyboardInputMessenger.WasMenuSlotPressed(i))
                {
                    TEMP_CommandInputState.MarkCentralInputConsumed();
                    RewardChoiceRequested?.Invoke(i);
                    SetContext(PlayerInputContext.Gameplay);
                    return;
                }
            }
        }

        private void HandleWavePopupContext(Keyboard keyboard)
        {
            SendGameplayBlockedInputs();

            if (CorridorCommander.KeyboardInputMessenger.WasWaveConfirmPressed())
            {
                TEMP_CommandInputState.MarkCentralInputConsumed();
                WaveStartConfirmed?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
                return;
            }

            if (CorridorCommander.KeyboardInputMessenger.WasWaveCancelPressed())
            {
                TEMP_CommandInputState.MarkCentralInputConsumed();
                WaveStartCanceled?.Invoke();
                SetContext(PlayerInputContext.Gameplay);
            }
        }

        private Vector2 ReadMoveInput(Keyboard keyboard)
        {
            Vector2 input = Vector2.zero;

            if (keyboard.wKey.isPressed)
            {
                input.y += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                input.x += 1f;
            }

            if (keyboard.aKey.isPressed)
            {
                input.x -= 1f;
            }

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            return input;
        }

        private void ResolveReferences()
        {
            if (weaponInventory == null)
            {
                weaponInventory = GetComponent<PlayerWeaponInventory>();
            }

            if (weaponInventory == null)
            {
                weaponInventory = GetComponentInParent<PlayerWeaponInventory>();
            }

            if (weaponInventory == null)
            {
                weaponInventory = GetComponentInChildren<PlayerWeaponInventory>(true);
            }

            if (weaponInventory == null)
            {
                weaponInventory = FindFirstObjectByType<PlayerWeaponInventory>(FindObjectsInactive.Include);
            }

            if (thirdPersonCamera == null)
            {
                thirdPersonCamera = FindFirstObjectByType<MouseLookThirdPersonCamera>(FindObjectsInactive.Include);
            }

            if (pauseMenuPresenter == null)
            {
                pauseMenuPresenter = FindFirstObjectByType<PauseMenuPresenter>(FindObjectsInactive.Include);
            }
        }

        private bool TryOpenPauseMenu()
        {
            ResolveReferences();

            if (pauseMenuPresenter == null)
            {
                return false;
            }

            pauseMenuPresenter.OpenPause();
            return pauseMenuPresenter.IsPaused;
        }

        private bool EquipNextWeapon()
        {
            ResolveReferences();

            if (weaponInventory == null || weaponInventory.WeaponCount <= 1)
            {
                return false;
            }

            return weaponInventory.EquipNextWeapon();
        }

        private bool WasNumberPressed(Keyboard keyboard, int number)
        {
            switch (number)
            {
                case 1:
                    return keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
                case 2:
                    return keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
                case 3:
                    return keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame;
                case 4:
                    return keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame;
                case 5:
                    return keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame;
                case 6:
                    return keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame;
                case 7:
                    return keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame;
                case 8:
                    return keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame;
                case 9:
                    return keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame;
                default:
                    return false;
            }
        }

        private bool WasNumberReleased(Keyboard keyboard, int number)
        {
            switch (number)
            {
                case 1:
                    return keyboard.digit1Key.wasReleasedThisFrame || keyboard.numpad1Key.wasReleasedThisFrame;
                case 2:
                    return keyboard.digit2Key.wasReleasedThisFrame || keyboard.numpad2Key.wasReleasedThisFrame;
                case 3:
                    return keyboard.digit3Key.wasReleasedThisFrame || keyboard.numpad3Key.wasReleasedThisFrame;
                case 4:
                    return keyboard.digit4Key.wasReleasedThisFrame || keyboard.numpad4Key.wasReleasedThisFrame;
                case 5:
                    return keyboard.digit5Key.wasReleasedThisFrame || keyboard.numpad5Key.wasReleasedThisFrame;
                case 6:
                    return keyboard.digit6Key.wasReleasedThisFrame || keyboard.numpad6Key.wasReleasedThisFrame;
                case 7:
                    return keyboard.digit7Key.wasReleasedThisFrame || keyboard.numpad7Key.wasReleasedThisFrame;
                case 8:
                    return keyboard.digit8Key.wasReleasedThisFrame || keyboard.numpad8Key.wasReleasedThisFrame;
                case 9:
                    return keyboard.digit9Key.wasReleasedThisFrame || keyboard.numpad9Key.wasReleasedThisFrame;
                default:
                    return false;
            }
        }

        private void SendGameplayBlockedInputs()
        {
            locomotionController?.SetMoveInput(Vector2.zero);
            locomotionController?.SetRunHeld(false);

            facingController?.SetAimHeld(false);
            facingController?.SetFireHeld(false);

            weaponLauncher?.ClearFireInput();
        }

        private void ClearAllDeliveredInputs()
        {
            locomotionController?.ClearMoveInput();
            facingController?.ClearCombatInput();
            weaponLauncher?.ClearFireInput();
        }

        private void LockCursor()
        {
            CorridorCommander.GameplayOptionsController.ApplyGameplayCursorPolicy();
        }

        private void UnlockCursor()
        {
            CorridorCommander.GameplayOptionsController.ApplyPointerCursorPolicy();
        }

        private void BeginTemporaryCursorOverride()
        {
            if (!hasTemporaryCursorOverride)
            {
                hasTemporaryCursorOverride = true;
            }

            UnlockCursor();
        }

        private void EndTemporaryCursorOverride()
        {
            if (!hasTemporaryCursorOverride)
            {
                return;
            }

            CorridorCommander.GameplayOptionsController.ApplyGameplayCursorPolicy();
            hasTemporaryCursorOverride = false;
        }

        private static bool IsCursorInGameplayPolicy()
        {
            switch (CorridorCommander.GameplayOptionsController.CurrentDisplayCursorMode)
            {
                case CorridorCommander.DisplayCursorMode.WindowFree:
                    return Cursor.lockState == CursorLockMode.None && Cursor.visible;

                case CorridorCommander.DisplayCursorMode.WindowConfined:
                    return Cursor.lockState == CursorLockMode.Confined && Cursor.visible;

                default:
                    return Cursor.lockState == CursorLockMode.Locked && !Cursor.visible;
            }
        }
    }
}
