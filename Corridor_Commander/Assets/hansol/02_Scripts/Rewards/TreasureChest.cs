using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using CorridorCommander.PlayerControl;
using CorridorCommander.PlayerItems;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TreasureChest : MonoBehaviour, IInteractionPromptSource
    {
        private const string PromptMessage = "E  Treasure";

        [SerializeField] private TreasureChestRewardTable rewardTable;
        [SerializeField] private int roomIndex;
        [SerializeField] private float interactionRange = 2.4f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private GameObject closedVisualRoot;
        [SerializeField] private GameObject openedVisualRoot;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private Vector3 promptWorldOffset = new Vector3(0f, 1.05f, 0f);
        [SerializeField] private Vector2 minimumPromptSize = new Vector2(440f, 82f);
        [SerializeField] private int promptFontSize = 42;
        [SerializeField] private GameObject choicePanelRoot;
        [SerializeField] private Button[] choiceButtons = new Button[3];
        [SerializeField] private Text[] choiceTexts = new Text[3];
        [SerializeField] private Text selectedRewardText;
        [SerializeField] private TreasureRewardMenuPresenter rewardMenuPresenter;
        [SerializeField] private bool consumeOnce = true;

        private readonly List<TreasureRewardEntry> offeredRewards = new List<TreasureRewardEntry>(3);
        private GameObject currentPlayer;
        private bool isOpened;
        private bool isPromptVisible;
        private int selectedRewardIndex = -1;

        public bool IsOpened => isOpened;
        public bool IsPromptVisible => isPromptVisible;
        public string PromptText => PromptMessage;
        public Vector3 PromptWorldPosition => transform.position + promptWorldOffset;
        public float PromptDistanceSqr => currentPlayer != null
            ? Vector3.SqrMagnitude(currentPlayer.transform.position - transform.position)
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

        public void ConfigureRewards(TreasureChestRewardTable configuredRewardTable, int configuredRoomIndex)
        {
            if (configuredRewardTable != null)
            {
                rewardTable = configuredRewardTable;
            }

            roomIndex = Mathf.Max(0, configuredRoomIndex);
        }

        private void Awake()
        {
            ResolvePresenter();
            DisableLegacyChoiceUi();
            RefreshVisuals();
            SetPromptActive(false);
            SetChoicePanelActive(false);
        }

        private void OnDestroy()
        {
            InteractionPromptPresenter.Unregister(this);
            rewardMenuPresenter?.Hide(this);
            SetPromptActive(false);
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

        public bool ShowRewardChoices()
        {
            if (!CanInteract())
            {
                return false;
            }

            if (rewardTable == null)
            {
                return false;
            }

            ArtifactInventory artifactInventory = RewardGrantService.Current != null
                ? RewardGrantService.Current.ArtifactInventory
                : FindFirstObjectByType<ArtifactInventory>(FindObjectsInactive.Include);
            rewardTable.GetAvailableRewards(
                ResolveRewardOfferSeed(),
                TreasureRewardMenuPresenter.MaxChoiceCount,
                artifactInventory,
                offeredRewards);
            if (offeredRewards.Count == 0)
            {
                return false;
            }

            if (!UiInputCoordinator.Instance.TryBeginPausedContext(this, UiInputContext.TreasureRewardMenu, true))
            {
                return false;
            }

            selectedRewardIndex = -1;
            RefreshChoiceTexts();
            return true;
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

            if (IsChoicePanelOpen())
            {
                ClosePanel();
            }
            else
            {
                ShowRewardChoices();
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

        private void RefreshPrompt()
        {
            bool canInteract = CanInteract();
            bool isPanelOpen = IsChoicePanelOpen();
            if (!canInteract && isPanelOpen)
            {
                ClosePanel();
                isPanelOpen = false;
            }

            bool showPrompt = canInteract && !isPanelOpen;
            isPromptVisible = showPrompt;
            SetPromptActive(false);

            if (promptText != null)
            {
                promptText.text = string.Empty;
                ApplyPromptStyle(promptText, ResolveSize(promptFontSize, 42));
            }

            ApplyMinimumSize(promptRoot, ResolveSize(minimumPromptSize, new Vector2(440f, 82f)));
        }

        private void RefreshVisuals()
        {
            if (closedVisualRoot != null)
            {
                closedVisualRoot.SetActive(!isOpened);
            }

            if (openedVisualRoot != null)
            {
                openedVisualRoot.SetActive(isOpened);
            }
        }

        private void RefreshChoiceTexts()
        {
            int choiceTextCount = choiceTexts != null ? choiceTexts.Length : 0;
            for (int i = 0; i < choiceTextCount; i++)
            {
                bool hasReward = i < offeredRewards.Count;
                if (choiceTexts[i] != null)
                {
                    choiceTexts[i].text = hasReward
                        ? $"[{i + 1}] {offeredRewards[i].DisplayName} x{offeredRewards[i].Amount}"
                        : string.Empty;
                }

                if (choiceButtons != null && i < choiceButtons.Length && choiceButtons[i] != null)
                {
                    choiceButtons[i].gameObject.SetActive(hasReward);
                }
            }

            ResolvePresenter();
            rewardMenuPresenter?.ShowRewards(this, offeredRewards, SelectRewardChoice, ClaimReward);
        }

        private void SelectRewardChoice(int index)
        {
            if (index < 0 || index >= offeredRewards.Count)
            {
                return;
            }

            selectedRewardIndex = index;
            if (selectedRewardText != null)
            {
                TreasureRewardEntry reward = offeredRewards[index];
                selectedRewardText.text = $"{reward.DisplayName} x{reward.Amount}";
            }
        }

        private void SelectAndClaimRewardChoice(int index)
        {
            if (index < 0 || index >= offeredRewards.Count)
            {
                return;
            }

            rewardMenuPresenter?.SelectReward(index);
            ClaimReward(index);
        }

        private void ClaimSelectedReward()
        {
            ClaimReward(selectedRewardIndex);
        }

        private void ClaimReward(int index)
        {
            if (index < 0 || index >= offeredRewards.Count)
            {
                return;
            }

            TreasureRewardEntry reward = offeredRewards[index];
            if (!TryGrantReward(reward, out string grantMessage))
            {
                Debug.LogWarning($"[TreasureChest] Reward grant failed: {grantMessage}", this);
                rewardMenuPresenter?.ShowSelected(this, grantMessage);
                return;
            }

            isOpened = true;
            selectedRewardIndex = -1;
            SetChoicePanelActive(false);
            RefreshVisuals();

            string selectedMessage = grantMessage;
            if (selectedRewardText != null)
            {
                selectedRewardText.text = string.Empty;
            }

            rewardMenuPresenter?.ShowSelected(this, selectedMessage);

            Debug.Log($"Treasure reward selected: room={roomIndex}, reward={reward.DisplayName} x{reward.Amount}", this);
        }

        private bool TryGrantReward(TreasureRewardEntry reward, out string message)
        {
            RewardGrantService service = RewardGrantService.Current;
            if (service == null)
            {
                message = "Reward service missing";
                Debug.LogError("[TreasureChest] RewardGrantService is missing in the active scene.", this);
                return false;
            }

            return service.TryGrant(reward, out message);
        }

        private int ResolveRewardOfferSeed()
        {
            Vector3 position = transform.position;
            unchecked
            {
                int seed = roomIndex * 397;
                seed = seed * 31 + Mathf.RoundToInt(position.x * 10f);
                seed = seed * 31 + Mathf.RoundToInt(position.y * 10f);
                seed = seed * 31 + Mathf.RoundToInt(position.z * 10f);
                return seed;
            }
        }

        private PlayerCurrencyWallet ResolveCurrencyWallet()
        {
            PlayerCurrencyWallet wallet = ResolveFromCurrentPlayer<PlayerCurrencyWallet>();
            return wallet != null ? wallet : FindFirstObjectByType<PlayerCurrencyWallet>(FindObjectsInactive.Exclude);
        }

        private PlayerLevelProgression ResolveLevelProgression()
        {
            PlayerLevelProgression progression = ResolveFromCurrentPlayer<PlayerLevelProgression>();
            return progression != null ? progression : FindFirstObjectByType<PlayerLevelProgression>(FindObjectsInactive.Exclude);
        }

        private PlayerItemInventory ResolveItemInventory()
        {
            PlayerItemInventory inventory = ResolveFromCurrentPlayer<PlayerItemInventory>();
            return inventory != null ? inventory : FindFirstObjectByType<PlayerItemInventory>(FindObjectsInactive.Exclude);
        }

        private T ResolveFromCurrentPlayer<T>() where T : Component
        {
            if (currentPlayer == null)
            {
                return null;
            }

            T component = currentPlayer.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            component = currentPlayer.GetComponentInParent<T>();
            if (component != null)
            {
                return component;
            }

            return currentPlayer.GetComponentInChildren<T>(true);
        }

        private bool IsChoicePanelOpen()
        {
            return rewardMenuPresenter != null && rewardMenuPresenter.IsShowingFor(this);
        }

        private bool CanInteract()
        {
            return currentPlayer != null
                && (!isOpened || !consumeOnce)
                && UiInputCoordinator.Instance.CanUseWorldInteraction(this)
                && IsClosestAvailableChestForPlayer();
        }

        private bool IsClosestAvailableChestForPlayer()
        {
            if (currentPlayer == null)
            {
                return false;
            }

            TreasureChest[] chests = FindObjectsByType<TreasureChest>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            float currentDistance = Vector3.SqrMagnitude(currentPlayer.transform.position - transform.position);
            for (int i = 0; i < chests.Length; i++)
            {
                TreasureChest chest = chests[i];
                if (chest == null || chest == this || chest.isOpened && chest.consumeOnce)
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(currentPlayer.transform.position - chest.transform.position);
                if (distance + 0.001f < currentDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private void SetChoicePanelActive(bool active)
        {
            if (choicePanelRoot != null)
            {
                choicePanelRoot.SetActive(false);
            }

            if (!active)
            {
                ClosePanel();
            }
        }

        private void ClosePanel()
        {
            selectedRewardIndex = -1;
            rewardMenuPresenter?.Hide(this);
            UiInputCoordinator.Instance.EndContext(this);
        }

        private void SetPromptActive(bool active)
        {
            if (promptRoot != null)
            {
                promptRoot.SetActive(active);
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

        private void ResolvePresenter()
        {
            if (rewardMenuPresenter == null)
            {
                rewardMenuPresenter = TreasureRewardMenuPresenter.Instance;
            }
        }

        private void DisableLegacyChoiceUi()
        {
            if (selectedRewardText != null)
            {
                selectedRewardText.text = string.Empty;
            }

            if (choicePanelRoot != null)
            {
                choicePanelRoot.SetActive(false);
            }
        }
    }
}
