using CorridorCommander;
using CorridorCommander.PlayerControl;
using UnityEngine;

namespace CorridorCommander.PlayerUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class CrosshairVisibilityController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private PlayerCentralInputController inputController;
        [SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;

        private void Awake()
        {
            ResolveReferences();
            ApplyVisibility(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshVisibility();
        }

        private void Update()
        {
            RefreshVisibility();
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void RefreshVisibility()
        {
            bool shouldShow = IsGameplayInputReady()
                && !PopupDimOverlayController.HasActivePopup
                && IsCursorInGameplayPolicy();

            ApplyVisibility(shouldShow);
        }

        private bool IsGameplayInputReady()
        {
            return !UiInputCoordinator.HasActiveContext
                && !UiInputCoordinator.BlocksGameplayInput
                && (inputController == null
                    || (inputController.IsGameplayContext && !inputController.BlocksGameplayInput));
        }

        private static bool IsCursorInGameplayPolicy()
        {
            switch (GameplayOptionsController.CurrentDisplayCursorMode)
            {
                case DisplayCursorMode.WindowFree:
                    return Cursor.lockState == CursorLockMode.None && Cursor.visible;

                case DisplayCursorMode.WindowConfined:
                    return Cursor.lockState == CursorLockMode.Confined && Cursor.visible;

                default:
                    return Cursor.lockState == CursorLockMode.Locked && !Cursor.visible;
            }
        }

        private void ApplyVisibility(bool shouldShow)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = shouldShow ? visibleAlpha : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                TryGetComponent(out canvasGroup);
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (inputController == null)
            {
                inputController = FindFirstObjectByType<PlayerCentralInputController>(FindObjectsInactive.Include);
            }
        }
    }
}
