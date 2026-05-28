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

        private PlacementPoint currentPlacementPoint;
        private bool isPanelOpen;

        public BuildableKind SelectedKind => selectedKind;
        public PlacementPoint CurrentPlacementPoint => currentPlacementPoint;
        public bool IsPanelOpen => isPanelOpen;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            currentPlacementPoint = FindClosestPlacementPoint();

            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame)
                {
                    TogglePanel();
                }

                if (isPanelOpen && keyboard.digit1Key.wasPressedThisFrame)
                {
                    selectedKind = BuildableKind.Turret;
                    TryBuildCurrent(BuildableKind.Turret);
                }

                if (isPanelOpen && keyboard.digit2Key.wasPressedThisFrame)
                {
                    selectedKind = BuildableKind.Barricade;
                    TryBuildCurrent(BuildableKind.Barricade);
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

            isPanelOpen = true;
            RefreshUi();
            return true;
        }

        public void ClosePanel()
        {
            isPanelOpen = false;
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

            TryOpenPanel();
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

            if (interactionPromptRoot != null)
            {
                interactionPromptRoot.SetActive(hasPoint && !isPanelOpen);
            }

            if (interactionPromptText != null)
            {
                interactionPromptText.text = hasPoint ? "E  건설 메뉴" : string.Empty;
            }

            if (buildPanelRoot != null)
            {
                buildPanelRoot.SetActive(hasPoint && isPanelOpen);
            }

            if (buildPanelText != null)
            {
                buildPanelText.text = hasPoint
                    ? "건설 선택\n\n[1] 포탑\n사거리 안의 적을 자동으로 공격\n\n[2] 바리케이드\n적 진로를 막고 체력으로 버팀\n\n[E] 닫기"
                    : string.Empty;
            }
        }
    }
}
