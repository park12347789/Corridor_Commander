using System.Collections.Generic;
using CorridorCommander.PlayerControl;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlacementPoint))]
    public sealed class PlacementPointInteraction : MonoBehaviour, IInteractionPromptSource, IInteractionPromptScreenPosition
    {
        private const string PromptMessage = "E  Build Menu";

        [SerializeField] private float interactionRange = 2.6f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private GameObject interactionPromptRoot;
        [SerializeField] private Text interactionPromptText;
        [SerializeField] private Vector3 promptWorldOffset = new Vector3(0f, 1.35f, 0f);
        [SerializeField] private PlacementBuildMenuPresenter buildMenuPresenter;
        [SerializeField] private PlacementPreviewController previewController;
        [SerializeField] private GameObject buildPanelRoot;
        [SerializeField] private Text buildPanelText;
        [SerializeField] private int promptFontSize = 26;
        [SerializeField] private Vector2 minimumPromptSize = new Vector2(360f, 72f);
        [SerializeField] private GameObject activeHighlightRoot;
        [SerializeField] private Image activeHighlightImage;
        [SerializeField] private Color activeHighlightColor = new Color(0.15f, 0.92f, 1f, 0.62f);
        [SerializeField] private Color menuHighlightColor = new Color(1f, 0.78f, 0.18f, 0.78f);
        [SerializeField] private bool useFixedScreenPromptPosition = true;
        [SerializeField] private Vector2 fixedPromptViewportAnchor = new Vector2(0.5f, 0.22f);
        [SerializeField] private Vector2 fixedPromptScreenOffset = Vector2.zero;

        private PlacementPoint placementPoint;
        private GameObject currentPlayer;
        private bool isPanelOpen;
        private bool isPromptVisible;

        public bool IsPromptVisible => isPromptVisible;
        public string PromptText => PromptMessage;
        public Vector3 PromptWorldPosition => transform.position + promptWorldOffset;
        public float PromptDistanceSqr => currentPlayer != null
            ? Vector3.SqrMagnitude(currentPlayer.transform.position - transform.position)
            : float.MaxValue;
        public int PromptPriority => 0;

        public bool TryGetPromptScreenPosition(Vector2 screenSize, Vector2 promptSize, out Vector2 screenPosition)
        {
            if (!useFixedScreenPromptPosition)
            {
                screenPosition = default;
                return false;
            }

            screenPosition = new Vector2(
                screenSize.x * fixedPromptViewportAnchor.x,
                screenSize.y * fixedPromptViewportAnchor.y) + fixedPromptScreenOffset;
            return true;
        }

        private void OnEnable()
        {
            ResolvePlacementPoint();
            ResolvePresenter();
            ResolvePreviewController();
            ResolveActiveHighlight();
            InteractionPromptPresenter.Register(this);
        }

        private void OnDisable()
        {
            InteractionPromptPresenter.Unregister(this);
            isPromptVisible = false;
        }

        private void Awake()
        {
            ResolvePlacementPoint();
            ResolvePresenter();
            ResolvePreviewController();
            ResolveActiveHighlight();
            SetPromptActive(false);
            SetLegacyPanelActive(false);
            SetActiveHighlight(false, false);
        }

        private void OnDestroy()
        {
            InteractionPromptPresenter.Unregister(this);
            buildMenuPresenter?.Hide(this);
            UiInputCoordinator.EndContextIfActive(this);
        }

        private void Update()
        {
            currentPlayer = FindClosestPlayer();

            if (previewController != null && previewController.IsPreviewing(placementPoint))
            {
                previewController.Tick();
                RefreshUi();
                return;
            }

            if (KeyboardInputMessenger.WasInteractPressed())
            {
                TogglePanel();
            }

            RefreshUi();
        }

        private void TogglePanel()
        {
            if (!CanInteract()
                || !InteractionPromptPresenter.IsBestVisibleSource(this)
                || !UiInputCoordinator.Instance.TryConsumeInteract(this))
            {
                ClosePanel();
                return;
            }

            isPanelOpen = !isPanelOpen;
            if (isPanelOpen)
            {
                OpenPanel();
            }
            else
            {
                ClosePanel();
            }
        }

        public void TryBuildFromMenu(BuildableKind kind)
        {
            if (!CanInteract() || !CanBuildFromMenu(kind))
            {
                ClosePanel();
                return;
            }

            if (placementPoint.RequiresPreviewRotation(kind))
            {
                ResolvePreviewController();
                ClosePanel();
                if (previewController != null)
                {
                    previewController.Begin(placementPoint, kind, currentPlayer);
                }

                return;
            }

            GameObject builtObject = placementPoint.Build(kind, currentPlayer);
            if (builtObject != null)
            {
                ClosePanel();
            }
        }

        public void TryBuildFromMenu(BuildableDefinitionSO definition)
        {
            if (!CanInteract() || !CanBuildFromMenu(definition))
            {
                ClosePanel();
                return;
            }

            if (placementPoint.RequiresPreviewRotation(definition))
            {
                ResolvePreviewController();
                ClosePanel();
                if (previewController != null)
                {
                    previewController.Begin(placementPoint, definition, currentPlayer);
                }

                return;
            }

            if (!TrySpendBuildCost(definition, currentPlayer))
            {
                return;
            }

            GameObject builtObject = placementPoint.Build(definition, currentPlayer);
            if (builtObject != null)
            {
                ClosePanel();
            }
        }

        public bool CanBuildFromMenu(BuildableKind kind)
        {
            return CanInteract()
                && SupportTruckShopGlobalUnlocks.CanBuild(kind)
                && placementPoint != null
                && placementPoint.CanBuild(kind);
        }

        public bool CanBuildFromMenu(BuildableDefinitionSO definition)
        {
            return CanInteract()
                && SupportTruckShopGlobalUnlocks.CanBuild(definition)
                && placementPoint != null
                && placementPoint.CanBuild(definition);
        }

        public bool RequiresPreviewFromMenu(BuildableKind kind)
        {
            return placementPoint != null && placementPoint.RequiresPreviewRotation(kind);
        }

        public bool RequiresPreviewFromMenu(BuildableDefinitionSO definition)
        {
            return placementPoint != null && placementPoint.RequiresPreviewRotation(definition);
        }

        public void GetBuildableDefinitionsFromMenu(BuildableCategory category, List<BuildableDefinitionSO> results)
        {
            placementPoint?.GetBuildableDefinitions(category, results);
        }

        private bool TrySpendBuildCost(BuildableDefinitionSO definition, GameObject player)
        {
            int price = definition != null ? Mathf.Max(0, definition.Price) : 0;
            if (price <= 0)
            {
                return true;
            }

            PlayerCurrencyWallet wallet = ResolveCurrencyWallet(player);
            if (wallet == null)
            {
                Debug.LogWarning("[PlacementPointInteraction] PlayerCurrencyWallet is not connected for build cost.", this);
                return false;
            }

            if (!wallet.TrySpendMoney(price))
            {
                Debug.Log($"[PlacementPointInteraction] Not enough money to build {definition.DisplayName}. Need {price}, Current {wallet.CurrentMoney}.", this);
                return false;
            }

            Debug.Log($"[PlacementPointInteraction] Build cost paid: {definition.DisplayName} -{price}.", this);
            return true;
        }

        private static PlayerCurrencyWallet ResolveCurrencyWallet(GameObject player)
        {
            if (player == null)
            {
                return null;
            }

            PlayerCurrencyWallet wallet = player.GetComponentInParent<PlayerCurrencyWallet>();
            if (wallet != null)
            {
                return wallet;
            }

            return player.GetComponentInChildren<PlayerCurrencyWallet>(true);
        }

        public void NotifyMenuClosed(PlacementBuildMenuPresenter presenter)
        {
            if (presenter != null && buildMenuPresenter == presenter)
            {
                isPanelOpen = false;
                UiInputCoordinator.EndContextIfActive(this);
            }
        }

        private GameObject FindClosestPlayer()
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
            GameObject closestPlayer = null;
            float closestDistance = interactionRange * interactionRange;

            foreach (GameObject player in players)
            {
                float distance = Vector3.SqrMagnitude(player.transform.position - transform.position);
                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }

            return closestPlayer;
        }

        private bool CanInteract()
        {
            ResolvePlacementPoint();

            return currentPlayer != null
                && placementPoint != null
                && !placementPoint.IsOccupied
                && UiInputCoordinator.Instance.CanUseWorldInteraction(this)
                && !IsAnyPreviewActive()
                && IsClosestAvailablePointForPlayer();
        }

        private bool IsClosestAvailablePointForPlayer()
        {
            if (currentPlayer == null)
            {
                return false;
            }

            PlacementPoint[] points = FindObjectsByType<PlacementPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            float currentDistance = Vector3.SqrMagnitude(currentPlayer.transform.position - transform.position);

            foreach (PlacementPoint point in points)
            {
                if (point == null || point == placementPoint || point.IsOccupied)
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(currentPlayer.transform.position - point.transform.position);
                if (distance + 0.001f < currentDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private void RefreshUi()
        {
            bool isPreviewing = IsAnyPreviewActive();
            bool isPreviewingThisPoint = previewController != null && previewController.IsPreviewing(placementPoint);
            bool canInteract = CanInteract();
            if (!canInteract && !isPreviewingThisPoint)
            {
                ClosePanel();
            }

            isPromptVisible = canInteract && !isPanelOpen && !isPreviewing;
            SetPromptActive(false);
            SetLegacyPanelActive(false);
            SetActiveHighlight(canInteract || isPanelOpen || isPreviewingThisPoint, isPanelOpen || isPreviewingThisPoint);

            if (interactionPromptText != null)
            {
                interactionPromptText.text = string.Empty;
                ApplyPromptStyle(interactionPromptText, ResolveSize(promptFontSize, 42));
            }

            ApplyMinimumSize(interactionPromptRoot, ResolveSize(minimumPromptSize, new Vector2(440f, 82f)));
        }

        private bool IsAnyPreviewActive()
        {
            return previewController != null && previewController.IsActive;
        }

        private void SetPromptActive(bool active)
        {
            if (interactionPromptRoot != null)
            {
                interactionPromptRoot.SetActive(active);
            }
        }

        private void SetActiveHighlight(bool active, bool strong)
        {
            ResolveActiveHighlight();
            if (activeHighlightRoot != null)
            {
                activeHighlightRoot.SetActive(active);
                ApplyActiveHighlightColors(strong);
            }
        }

        private void ApplyActiveHighlightColors(bool strong)
        {
            Color fillColor = strong ? menuHighlightColor : activeHighlightColor;
            Color lineColor = fillColor;
            lineColor.a = Mathf.Max(lineColor.a, 0.88f);
            bool hasRootFillImage = activeHighlightImage != null
                && activeHighlightRoot != null
                && activeHighlightImage.gameObject == activeHighlightRoot;

            if (hasRootFillImage)
            {
                Color transparentFill = fillColor;
                transparentFill.a = 0f;
                activeHighlightImage.color = transparentFill;
            }

            if (activeHighlightRoot == null)
            {
                return;
            }

            Image[] images = activeHighlightRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null || hasRootFillImage && images[i] == activeHighlightImage)
                {
                    continue;
                }

                images[i].color = lineColor;
            }

            Outline[] outlines = activeHighlightRoot.GetComponentsInChildren<Outline>(true);
            for (int i = 0; i < outlines.Length; i++)
            {
                if (outlines[i] == null)
                {
                    continue;
                }

                outlines[i].effectColor = lineColor;
                outlines[i].effectDistance = strong ? new Vector2(5f, -5f) : new Vector2(4f, -4f);
            }
        }

        private void OpenPanel()
        {
            ResolvePresenter();
                if (buildMenuPresenter == null)
                {
                    isPanelOpen = false;
                return;
            }

            if (!UiInputCoordinator.Instance.TryBeginContext(this, UiInputContext.PlacementBuildMenu, true))
            {
                isPanelOpen = false;
                return;
            }

            buildMenuPresenter?.Show(this);
        }

        private void ClosePanel()
        {
            isPanelOpen = false;
            buildMenuPresenter?.Hide(this);
            UiInputCoordinator.Instance.EndContext(this);
        }

        private void ResolvePresenter()
        {
            if (buildMenuPresenter == null)
            {
                buildMenuPresenter = FindFirstObjectByType<PlacementBuildMenuPresenter>(FindObjectsInactive.Include);
            }

            if (buildMenuPresenter == null)
            {
                Debug.LogWarning("[PlacementPointInteraction] PlacementBuildMenuPresenter is not assigned.", this);
            }
        }

        private void ResolvePreviewController()
        {
            if (previewController == null)
            {
                previewController = FindFirstObjectByType<PlacementPreviewController>(FindObjectsInactive.Include);
            }

            if (previewController == null)
            {
                Debug.LogWarning("[PlacementPointInteraction] PlacementPreviewController is not assigned.", this);
            }
        }

        private void ResolvePlacementPoint()
        {
            if (placementPoint == null)
            {
                placementPoint = GetComponent<PlacementPoint>();
            }
        }

        private void ResolveActiveHighlight()
        {
            if (activeHighlightRoot == null)
            {
                Transform found = FindChildRecursive(transform, "PlacementPointActiveHighlight");
                if (found != null)
                {
                    activeHighlightRoot = found.gameObject;
                }
            }

            if (activeHighlightImage == null && activeHighlightRoot != null)
            {
                activeHighlightImage = activeHighlightRoot.GetComponent<Image>();
            }
        }

        private void SetLegacyPanelActive(bool active)
        {
            if (buildPanelRoot != null)
            {
                buildPanelRoot.SetActive(active);
            }
        }

        private static void ApplyPromptStyle(Text text, int fontSize)
        {
            text.fontStyle = FontStyle.Bold;
            text.fontSize = Mathf.Max(text.fontSize, fontSize);
            text.resizeTextForBestFit = false;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static int ResolveSize(int configuredValue, int fallback)
        {
            return configuredValue > 0 ? configuredValue : fallback;
        }

        private static Vector2 ResolveSize(Vector2 configuredValue, Vector2 fallback)
        {
            return configuredValue.x > 0f && configuredValue.y > 0f ? configuredValue : fallback;
        }

        private static void ApplyMinimumSize(GameObject target, Vector2 minimumSize)
        {
            if (target == null)
            {
                return;
            }

            RectTransform rectTransform = target.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                Mathf.Max(rectTransform.rect.width, minimumSize.x));
            rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(rectTransform.rect.height, minimumSize.y));
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
