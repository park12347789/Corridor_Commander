using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SupportTruckShop))]
    public sealed class SupportTruckShopInteraction : MonoBehaviour, IInteractionPromptSource
    {
        private const string PromptMessage = "E  Support Shop";

        [SerializeField] private float interactionRange = 3.2f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private GameObject interactionPromptRoot;
        [SerializeField] private Text interactionPromptText;
        [SerializeField] private Vector3 promptWorldOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Vector2 minimumPromptSize = new Vector2(520f, 82f);
        [SerializeField] private int promptFontSize = 42;
        [SerializeField] private int promptPriority = 10;
        [SerializeField] private SupportTruckShopPresenter shopPresenter;
        [SerializeField] private Collider[] interactionColliders;

        private SupportTruckShop shop;
        private Transform currentPlayer;
        private bool isPanelOpen;
        private bool isPromptVisible;
        private bool missingInteractionColliderWarned;

        public bool IsPromptVisible => isPromptVisible;
        public string PromptText => PromptMessage;
        public Vector3 PromptWorldPosition => transform.position + promptWorldOffset;
        public float PromptDistanceSqr => currentPlayer != null
            ? GetInteractionDistanceSqr(currentPlayer.position)
            : float.MaxValue;
        public int PromptPriority => promptPriority;

        private void OnEnable()
        {
            InteractionPromptPresenter.Register(this);
        }

        private void OnDisable()
        {
            InteractionPromptPresenter.Unregister(this);
            isPromptVisible = false;
        }

        private void Awake()
        {
            shop = GetComponent<SupportTruckShop>();
            ResolveInteractionColliders();
            SetPromptActive(false);
            RefreshPrompt();
        }

        private void OnDestroy()
        {
            InteractionPromptPresenter.Unregister(this);
            SetPromptActive(false);
            shopPresenter?.ShowPrompt(this, false, string.Empty);
            shopPresenter?.Hide(this);
            UiInputCoordinator.EndContextIfActive(this);
        }

        private void Update()
        {
            currentPlayer = FindClosestPlayer();

            if (KeyboardInputMessenger.WasInteractPressed())
            {
                TogglePanel();
            }

            RefreshPrompt();
        }

        public void NotifyMenuClosed(SupportTruckShopPresenter presenter)
        {
            if (presenter != null && presenter == shopPresenter)
            {
                isPanelOpen = false;
                UiInputCoordinator.EndContextIfActive(this);
            }
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

        private void OpenPanel()
        {
            ResolvePresenter();
            if (shopPresenter == null)
            {
                isPanelOpen = false;
                return;
            }

            if (!UiInputCoordinator.Instance.TryBeginContext(this, UiInputContext.SupportTruckShop, true))
            {
                isPanelOpen = false;
                return;
            }

            shop.SetDefaultFollowTarget(currentPlayer);
            shopPresenter.Show(this, shop, currentPlayer);
        }

        private void ClosePanel()
        {
            isPanelOpen = false;
            shopPresenter?.Hide(this);
            UiInputCoordinator.Instance.EndContext(this);
        }

        private void RefreshPrompt()
        {
            bool canInteract = CanInteract();
            if (!canInteract)
            {
                ClosePanel();
            }

            bool showPrompt = canInteract && !isPanelOpen;
            isPromptVisible = showPrompt;
            if (interactionPromptText != null)
            {
                interactionPromptText.text = string.Empty;
                ApplyPromptStyle(interactionPromptText, ResolveSize(promptFontSize, 42));
            }

            ApplyMinimumSize(interactionPromptRoot, ResolveSize(minimumPromptSize, new Vector2(520f, 82f)));
            SetPromptActive(false);
        }

        private Transform FindClosestPlayer()
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
            Transform closestPlayer = null;
            float closestDistance = interactionRange * interactionRange;

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null)
                {
                    continue;
                }

                float distance = GetInteractionDistanceSqr(players[i].transform.position);
                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = players[i].transform;
                }
            }

            return closestPlayer;
        }

        private bool CanInteract()
        {
            return currentPlayer != null
                && shop != null
                && IsPlayerWithinInteractionRange(currentPlayer)
                && UiInputCoordinator.Instance.CanUseWorldInteraction(this)
                && IsClosestShopForPlayer();
        }

        private bool IsClosestShopForPlayer()
        {
            SupportTruckShopInteraction[] shops = FindObjectsByType<SupportTruckShopInteraction>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            float currentDistance = GetInteractionDistanceSqr(currentPlayer.position);

            for (int i = 0; i < shops.Length; i++)
            {
                SupportTruckShopInteraction other = shops[i];
                if (other == null || other == this)
                {
                    continue;
                }

                float distance = other.GetInteractionDistanceSqr(currentPlayer.position);
                if (distance + 0.001f < currentDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPlayerWithinInteractionRange(Transform player)
        {
            if (player == null)
            {
                return false;
            }

            float safeRange = Mathf.Max(0f, interactionRange);
            return GetInteractionDistanceSqr(player.position) <= safeRange * safeRange;
        }

        private float GetInteractionDistanceSqr(Vector3 worldPosition)
        {
            ResolveInteractionColliders();

            if (interactionColliders == null || interactionColliders.Length == 0)
            {
                WarnMissingInteractionColliders();
                return float.MaxValue;
            }

            float closestDistance = float.MaxValue;
            for (int i = 0; i < interactionColliders.Length; i++)
            {
                Collider interactionCollider = interactionColliders[i];
                if (interactionCollider == null || !interactionCollider.enabled)
                {
                    continue;
                }

                Vector3 closestPoint = interactionCollider.ClosestPoint(worldPosition);
                float distance = Vector3.SqrMagnitude(worldPosition - closestPoint);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }

            if (closestDistance == float.MaxValue)
            {
                WarnMissingInteractionColliders();
            }

            return closestDistance;
        }

        private void ResolveInteractionColliders()
        {
            if (HasUsableInteractionCollider())
            {
                return;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            int usableCount = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];
                if (candidate != null && candidate.enabled && !candidate.isTrigger)
                {
                    usableCount++;
                }
            }

            interactionColliders = new Collider[usableCount];
            int writeIndex = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];
                if (candidate != null && candidate.enabled && !candidate.isTrigger)
                {
                    interactionColliders[writeIndex] = candidate;
                    writeIndex++;
                }
            }
        }

        private bool HasUsableInteractionCollider()
        {
            if (interactionColliders == null)
            {
                return false;
            }

            for (int i = 0; i < interactionColliders.Length; i++)
            {
                if (interactionColliders[i] != null && interactionColliders[i].enabled && !interactionColliders[i].isTrigger)
                {
                    return true;
                }
            }

            return false;
        }

        private void WarnMissingInteractionColliders()
        {
            if (missingInteractionColliderWarned)
            {
                return;
            }

            Debug.LogWarning("[SupportTruckShopInteraction] Interaction collider is not assigned.", this);
            missingInteractionColliderWarned = true;
        }

        private void ResolvePresenter()
        {
            if (shopPresenter == null)
            {
                shopPresenter = SupportTruckShopPresenter.Instance;
            }
        }

        private void SetPromptActive(bool active)
        {
            if (interactionPromptRoot != null)
            {
                interactionPromptRoot.SetActive(active);
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
    }
}
