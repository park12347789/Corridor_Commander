using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TEMP_GhostOperatorBuildInput : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2.6f;
        [SerializeField] private LayerMask placementLayers = ~0;
        [SerializeField] private BuildableKind selectedKind = BuildableKind.Turret;
        [SerializeField] private GameObject interactionPromptRoot;
        [SerializeField] private Text interactionPromptText;
        [SerializeField] private GameObject buildPanelRoot;
        [SerializeField] private Text buildPanelText;
        [SerializeField] private PlacementPreviewController previewController;

        private PlacementPoint currentPlacementPoint;
        private bool isPanelOpen;

        public BuildableKind SelectedKind => selectedKind;
        public PlacementPoint CurrentPlacementPoint => currentPlacementPoint;
        public bool IsPanelOpen => isPanelOpen;

        private void OnDestroy()
        {
            UiInputCoordinator.EndContextIfActive(this);
        }

        private void Update()
        {
            Keyboard keyboard = KeyboardInputMessenger.CurrentKeyboard;
            currentPlacementPoint = FindClosestPlacementPoint();

            if (previewController != null && previewController.IsActive)
            {
                previewController.Tick();
                RefreshUi();
                return;
            }

            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame)
                {
                    TogglePanel();
                }

                if (isPanelOpen
                    && keyboard.digit1Key.wasPressedThisFrame
                    && UiInputCoordinator.Instance.TryConsumeMenuSlot(this, 1))
                {
                    selectedKind = BuildableKind.Turret;
                    TryBuildCurrent(BuildableKind.Turret);
                }

                if (isPanelOpen
                    && keyboard.digit2Key.wasPressedThisFrame
                    && UiInputCoordinator.Instance.TryConsumeMenuSlot(this, 2))
                {
                    selectedKind = BuildableKind.Barricade;
                    TryBuildCurrent(BuildableKind.Barricade);
                }

                if (isPanelOpen
                    && keyboard.digit3Key.wasPressedThisFrame
                    && UiInputCoordinator.Instance.TryConsumeMenuSlot(this, 3))
                {
                    selectedKind = BuildableKind.Mortar;
                    TryBuildCurrent(BuildableKind.Mortar);
                }
            }

            RefreshUi();
        }

        public GameObject TryBuildAt(PlacementPoint placementPoint, BuildableKind kind)
        {
            if (placementPoint == null)
            {
                return null;
            }

            if (placementPoint.RequiresPreviewRotation(kind))
            {
                BeginPreview(placementPoint, kind);
                return null;
            }

            return placementPoint.Build(kind, gameObject);
        }

        public bool TryOpenPanel()
        {
            if (currentPlacementPoint == null || currentPlacementPoint.IsOccupied)
            {
                isPanelOpen = false;
                RefreshUi();
                return false;
            }

            if (!UiInputCoordinator.Instance.TryBeginContext(this, UiInputContext.LegacyBuildMenu, true))
            {
                isPanelOpen = false;
                RefreshUi();
                return false;
            }

            isPanelOpen = true;
            RefreshUi();
            return true;
        }

        public void ClosePanel()
        {
            isPanelOpen = false;
            UiInputCoordinator.Instance.EndContext(this);
            RefreshUi();
        }

        public GameObject TryBuildCurrent(BuildableKind kind)
        {
            GameObject builtObject = TryBuildAt(currentPlacementPoint, kind);
            if (builtObject != null)
            {
                ClosePanel();
            }

            return builtObject;
        }

        private void TogglePanel()
        {
            if (isPanelOpen)
            {
                ClosePanel();
                return;
            }

            if (currentPlacementPoint == null || currentPlacementPoint.IsOccupied)
            {
                TryOpenPanel();
                return;
            }

            if (!UiInputCoordinator.Instance.TryConsumeInteract(this))
            {
                return;
            }

            TryOpenPanel();
        }

        private bool BeginPreview(PlacementPoint placementPoint, BuildableKind kind)
        {
            ResolvePreviewController();
            ClosePanel();
            if (previewController == null || !previewController.Begin(placementPoint, kind, gameObject))
            {
                return false;
            }

            return true;
        }

        private void ResolvePreviewController()
        {
            if (previewController == null)
            {
                Debug.LogWarning("[TEMP_GhostOperatorBuildInput] PlacementPreviewController is not assigned.", this);
            }
        }

        private PlacementPoint FindClosestPlacementPoint()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactionRange, placementLayers, QueryTriggerInteraction.Collide);
            PlacementPoint closestPoint = null;
            float closestDistance = float.MaxValue;

            foreach (Collider hit in hits)
            {
                PlacementPoint placementPoint = hit.GetComponentInParent<PlacementPoint>();
                if (placementPoint == null || placementPoint.IsOccupied)
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(placementPoint.transform.position - transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = placementPoint;
                }
            }

            if (closestPoint == null)
            {
                isPanelOpen = false;
            }

            return closestPoint;
        }

        private void RefreshUi()
        {
            bool hasPoint = currentPlacementPoint != null && !currentPlacementPoint.IsOccupied;
            bool isPreviewing = previewController != null && previewController.IsActive;
            bool canUseInteraction = UiInputCoordinator.Instance.CanUseWorldInteraction(this);

            if (interactionPromptRoot != null)
            {
                interactionPromptRoot.SetActive(hasPoint && canUseInteraction && !isPanelOpen && !isPreviewing);
            }

            if (interactionPromptText != null)
            {
                interactionPromptText.text = hasPoint && canUseInteraction && !isPreviewing ? "E  건설 메뉴" : string.Empty;
            }

            if (buildPanelRoot != null)
            {
                buildPanelRoot.SetActive(hasPoint && isPanelOpen && !isPreviewing);
            }

            if (buildPanelText != null)
            {
                buildPanelText.text = hasPoint && !isPreviewing
                    ? "\uAC74\uC124 \uC120\uD0DD\n\n[1] \uD3EC\uD0D1\n\uC0AC\uAC70\uB9AC \uC548\uC758 \uC801\uC744 \uC790\uB3D9\uC73C\uB85C \uACF5\uACA9\n\n[2] \uBC29\uBCBD\nR/\uD720 \uD68C\uC804, E/\uC6B0\uD074\uB9AD \uC124\uCE58\n\n[3] \uBC15\uACA9\uD3EC\n\uC870\uC900 \uC704\uCE58 \uD3EC\uACA9 \uC2A4\uD0AC \uC81C\uACF5\n\n[E] \uB2EB\uAE30"
                    : string.Empty;
            }
        }
    }
}
