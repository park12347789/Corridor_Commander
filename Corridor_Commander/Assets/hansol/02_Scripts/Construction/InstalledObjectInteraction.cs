using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class InstalledObjectInteraction : MonoBehaviour, IInteractionPromptSource
    {
        [SerializeField] private float interactionRange = 2.8f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private GameObject interactionPromptRoot;
        [SerializeField] private Text interactionPromptText;
        [SerializeField] private Vector3 promptWorldOffset = new Vector3(0f, 1.8f, 0f);
        [SerializeField] private Vector2 minimumPromptSize = new Vector2(440f, 82f);
        [SerializeField] private int promptFontSize = 42;
        [SerializeField] private MonoBehaviour actionProviderBehaviour;
        [SerializeField] private InstalledObjectActionPresenter presenter;

        private IInstalledObjectActionProvider actionProvider;
        private Transform currentPlayer;
        private bool isPanelOpen;
        private bool isPromptVisible;
        private string promptTextValue = string.Empty;

        public bool IsPromptVisible => isPromptVisible;
        public string PromptText => promptTextValue;
        public Vector3 PromptWorldPosition => transform.position + promptWorldOffset;
        public float PromptDistanceSqr => currentPlayer != null
            ? Vector3.SqrMagnitude(currentPlayer.position - transform.position)
            : float.MaxValue;
        public int PromptPriority => 0;

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
            ResolveActionProvider();
            SetPromptActive(false);
            RefreshPrompt();
        }

        private void OnDestroy()
        {
            InteractionPromptPresenter.Unregister(this);
            SetPromptActive(false);
            presenter?.Hide(this);
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

        public void NotifyMenuClosed(InstalledObjectActionPresenter closedPresenter)
        {
            if (closedPresenter != null && closedPresenter == presenter)
            {
                isPanelOpen = false;
                UiInputCoordinator.EndContextIfActive(this);
            }
        }

        public bool TransferOpenPanelTo(InstalledObjectInteraction replacement, Transform player)
        {
            if (!isPanelOpen || replacement == null)
            {
                return false;
            }

            InstalledObjectActionPresenter currentPresenter = presenter;
            isPanelOpen = false;
            UiInputCoordinator.EndContextIfActive(this);
            return replacement.OpenPanelFromReplacement(currentPresenter, player);
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
            ResolveActionProvider();
            if (presenter == null || actionProvider == null)
            {
                isPanelOpen = false;
                return;
            }

            if (!UiInputCoordinator.Instance.TryBeginContext(this, UiInputContext.InstalledObjectMenu, true))
            {
                isPanelOpen = false;
                return;
            }

            presenter.Show(this, actionProvider, currentPlayer);
        }

        private bool OpenPanelFromReplacement(InstalledObjectActionPresenter sourcePresenter, Transform player)
        {
            currentPlayer = player != null ? player : FindClosestPlayer();
            presenter = sourcePresenter != null ? sourcePresenter : InstalledObjectActionPresenter.Instance;
            ResolveActionProvider();

            if (presenter == null || actionProvider == null)
            {
                isPanelOpen = false;
                return false;
            }

            if (!UiInputCoordinator.Instance.TryBeginContext(this, UiInputContext.InstalledObjectMenu, true))
            {
                isPanelOpen = false;
                return false;
            }

            isPanelOpen = true;
            presenter.Show(this, actionProvider, currentPlayer);
            return true;
        }

        private void ClosePanel()
        {
            isPanelOpen = false;
            presenter?.Hide(this);
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
            promptTextValue = showPrompt && actionProvider != null
                ? "E  " + actionProvider.Prompt
                : string.Empty;
            SetPromptActive(false);

            if (interactionPromptText != null)
            {
                interactionPromptText.text = string.Empty;
                ApplyPromptStyle(interactionPromptText, ResolveSize(promptFontSize, 42));
            }

            ApplyMinimumSize(interactionPromptRoot, ResolveSize(minimumPromptSize, new Vector2(440f, 82f)));
        }

        private void SetPromptActive(bool active)
        {
            if (interactionPromptRoot != null)
            {
                interactionPromptRoot.SetActive(active);
            }
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

                float distance = Vector3.SqrMagnitude(players[i].transform.position - transform.position);
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
            ResolveActionProvider();
            return currentPlayer != null
                && actionProvider != null
                && UiInputCoordinator.Instance.CanUseWorldInteraction(this)
                && IsClosestInstalledObjectForPlayer();
        }

        private bool IsClosestInstalledObjectForPlayer()
        {
            InstalledObjectInteraction[] interactions = FindObjectsByType<InstalledObjectInteraction>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            float currentDistance = Vector3.SqrMagnitude(currentPlayer.position - transform.position);

            for (int i = 0; i < interactions.Length; i++)
            {
                InstalledObjectInteraction other = interactions[i];
                if (other == null || other == this)
                {
                    continue;
                }

                if (!other.HasActionProvider())
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(currentPlayer.position - other.transform.position);
                if (distance + 0.001f < currentDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasActionProvider()
        {
            ResolveActionProvider();
            return actionProvider != null;
        }

        private void ResolveActionProvider()
        {
            if (actionProvider != null)
            {
                return;
            }

            if (actionProviderBehaviour is IInstalledObjectActionProvider provider)
            {
                actionProvider = provider;
                return;
            }

            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInstalledObjectActionProvider foundProvider)
                {
                    actionProviderBehaviour = behaviours[i];
                    actionProvider = foundProvider;
                    return;
                }
            }
        }

        private void ResolvePresenter()
        {
            if (presenter == null)
            {
                presenter = InstalledObjectActionPresenter.Instance;
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
