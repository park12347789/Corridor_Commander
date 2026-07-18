using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TreasureRewardMenuPresenter : MonoBehaviour
    {
        public const int MaxChoiceCount = 3;

        private static readonly Color[] CardColors =
        {
            new Color(0.08f, 0.17f, 0.23f, 0.98f),
            new Color(0.10f, 0.15f, 0.28f, 0.98f),
            new Color(0.14f, 0.13f, 0.24f, 0.98f)
        };

        private static readonly Color RewardTextColor = new Color(0.96f, 0.98f, 1f, 1f);
        private static readonly Color RewardMutedTextColor = new Color(0.70f, 0.83f, 0.92f, 1f);
        private static readonly Color RewardAmountColor = new Color(1f, 0.90f, 0.42f, 1f);
        private static readonly Color RewardSelectionColor = new Color(0.12f, 0.92f, 1f, 0.28f);
        private static readonly Color ClaimEnabledColor = new Color(1f, 0.67f, 0.12f, 1f);
        private static readonly Color ClaimDisabledColor = new Color(0.35f, 0.35f, 0.35f, 0.92f);
        private const string RewardHint = "1-3 \uC120\uD0DD / Enter\u00B7\uB354\uBE14\uD074\uB9AD \uD68D\uB4DD / ESC \uB2EB\uAE30";
        private const string MandatoryRewardHint = "보상 선택 필수 / 1-3 선택 / Enter·더블클릭 획득";
        private const string ClaimText = "\uD68D\uB4DD";
        private const float RewardIconSize = 96f;
        private const float ArtifactRewardIconSize = 112f;
        private const float RewardIconY = 48f;

        private static TreasureRewardMenuPresenter instance;
        private static bool missingPresenterWarned;

        [Header("Prompt")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private TMP_Text promptTmpText;

        [Header("Reward Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private DotweenUiPanelTransition panelTransition;
        [SerializeField] private Text titleText;
        [SerializeField] private TMP_Text titleTmpText;
        [SerializeField] private Button[] choiceButtons = new Button[MaxChoiceCount];
        [SerializeField] private Text[] choiceTexts = new Text[MaxChoiceCount];
        [SerializeField] private TMP_Text[] choiceTmpTexts = new TMP_Text[MaxChoiceCount];
        [SerializeField] private Image[] choiceIconImages = new Image[MaxChoiceCount];
        [SerializeField] private Text[] choiceAmountTexts = new Text[MaxChoiceCount];
        [SerializeField] private TMP_Text[] choiceAmountTmpTexts = new TMP_Text[MaxChoiceCount];
        [SerializeField] private TMP_Text[] choiceExplanationTmpTexts = new TMP_Text[MaxChoiceCount];
        [SerializeField] private Image[] choiceSelectionImages = new Image[MaxChoiceCount];
        [SerializeField] private Button claimButton;
        [SerializeField] private Text claimButtonText;
        [SerializeField] private TMP_Text claimButtonTmpText;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject artifactDescriptionRoot;
        [SerializeField] private Text artifactDescriptionText;
        [SerializeField] private TMP_Text artifactDescriptionTmpText;
        [SerializeField] private Text hintText;
        [SerializeField] private TMP_Text hintTmpText;
        [SerializeField] private float doubleClickWindow = 0.35f;

        [Header("Status")]
        [SerializeField] private GameObject statusRoot;
        [SerializeField] private Text statusText;
        [SerializeField] private TMP_Text statusTmpText;
        [SerializeField] private float statusDuration = 2f;

        private readonly List<TreasureRewardEntry> visibleRewards = new List<TreasureRewardEntry>(MaxChoiceCount);
        private UnityEngine.Object activeOwner;
        private UnityEngine.Object promptOwner;
        private Action<int> selectionChanged;
        private Action<int> claimRequested;
        private int selectedIndex = -1;
        private int lastClickedIndex = -1;
        private float lastClickAt = -999f;
        private float hideStatusAt;
        private readonly Button[] boundChoiceButtons = new Button[MaxChoiceCount];
        private readonly UnityAction[] choiceClickHandlers = new UnityAction[MaxChoiceCount];
        private readonly Transform[] rewardCardRoots = new Transform[MaxChoiceCount];
        private Button boundClaimButton;
        private Button boundCloseButton;

        public static TreasureRewardMenuPresenter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<TreasureRewardMenuPresenter>(FindObjectsInactive.Include);
                }

                WarnIfMissingPresenter();

                return instance;
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
            missingPresenterWarned = false;
            EnsureArrayLengths();
            ResolveMissingUiReferences();
            WarnIfMissingReferences();
            BindButtons();
            SetPromptActive(false);
            SetPanelActive(false, true);
            SetStatusActive(false);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (statusRoot != null && statusRoot.activeSelf && Time.unscaledTime >= hideStatusAt)
            {
                SetStatusActive(false);
            }

            if (activeOwner == null || panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }

            for (int i = 0; i < MaxChoiceCount; i++)
            {
                if (KeyboardInputMessenger.WasMenuSlotPressed(i + 1)
                    && UiInputCoordinator.Instance.TryConsumeMenuSlot(activeOwner, i + 1))
                {
                    SelectReward(i);

                    return;
                }
            }

            if (KeyboardInputMessenger.WasContextConfirmPressed()
                && UiInputCoordinator.Instance.TryConsumeContextInput(activeOwner))
            {
                ClaimSelectedReward();
                return;
            }

            if (KeyboardInputMessenger.WasCancelPressed()
                && UiInputCoordinator.Instance.TryConsumeCancel(activeOwner))
            {
                HandleBackRequested();
            }
        }

        public bool IsShowingFor(UnityEngine.Object owner)
        {
            return owner != null && activeOwner == owner && panelRoot != null && panelRoot.activeSelf;
        }

        public bool HasRewardPanel => panelRoot != null;

        public void ShowPrompt(TreasureChest chest, bool visible, string message)
        {
            ShowPrompt((UnityEngine.Object)chest, visible, message);
        }

        public void ShowPrompt(UnityEngine.Object owner, bool visible, string message)
        {
            if (owner == null || activeOwner != null)
            {
                return;
            }

            if (!visible)
            {
                if (promptOwner == owner)
                {
                    promptOwner = null;
                    SetPromptActive(false);
                }

                return;
            }

            promptOwner = owner;
            if (promptText != null)
            {
                promptText.text = message;
            }
            SetText(promptTmpText, message);

            SetPromptActive(true);
        }

        public void ShowRewards(
            TreasureChest chest,
            IReadOnlyList<TreasureRewardEntry> rewards,
            Action<int> onSelectionChanged,
            Action<int> onClaimRequested)
        {
            ShowRewards((UnityEngine.Object)chest, rewards, onSelectionChanged, onClaimRequested);
        }

        public void ShowRewards(
            UnityEngine.Object owner,
            IReadOnlyList<TreasureRewardEntry> rewards,
            Action<int> onSelectionChanged,
            Action<int> onClaimRequested)
        {
            if (owner == null || rewards == null || rewards.Count == 0)
            {
                return;
            }

            activeOwner = owner;
            selectionChanged = onSelectionChanged;
            claimRequested = onClaimRequested;
            selectedIndex = -1;
            lastClickedIndex = -1;
            promptOwner = null;

            visibleRewards.Clear();
            for (int i = 0; i < rewards.Count && visibleRewards.Count < MaxChoiceCount; i++)
            {
                if (rewards[i] != null)
                {
                    visibleRewards.Add(rewards[i]);
                }
            }

            EnsureArrayLengths();
            BindButtons();
            SetPromptActive(false);
            SetStatusActive(false);

            string title = BuildRewardTitleText(owner);
            if (titleText != null)
            {
                titleText.text = title;
            }
            SetText(titleTmpText, title);

            for (int i = 0; i < MaxChoiceCount; i++)
            {
                ApplyRewardCard(i, i < visibleRewards.Count ? visibleRewards[i] : null);
            }

            if (claimButtonText != null)
            {
                claimButtonText.text = "획득";
            }
            SetText(claimButtonTmpText, "획득");

            SetCleanRewardStaticTexts();
            SetRewardHintText(owner);
            ConfigureClosePolicy(owner);
            SetPanelActive(true);
            SelectReward(0);
            SelectCurrentButton();
        }

        public void SelectReward(int index)
        {
            if (index < 0 || index >= visibleRewards.Count)
            {
                return;
            }

            selectedIndex = index;
            selectionChanged?.Invoke(index);
            RefreshSelectionVisuals();
            RefreshArtifactDescription();
        }

        public void ClaimSelectedReward()
        {
            if (selectedIndex < 0 || selectedIndex >= visibleRewards.Count)
            {
                return;
            }

            claimRequested?.Invoke(selectedIndex);
        }

        public void Hide(UnityEngine.Object owner)
        {
            if (activeOwner == owner)
            {
                activeOwner = null;
                selectionChanged = null;
                claimRequested = null;
                selectedIndex = -1;
                SetArtifactDescriptionActive(false);
                SetPanelActive(false);
                UiInputCoordinator.EndContextIfActive(owner);
            }

            if (promptOwner == owner)
            {
                promptOwner = null;
                SetPromptActive(false);
            }
        }

        public void ShowSelected(UnityEngine.Object owner, string message)
        {
            if (activeOwner == owner)
            {
                activeOwner = null;
                selectionChanged = null;
                claimRequested = null;
                selectedIndex = -1;
                SetArtifactDescriptionActive(false);
                SetPanelActive(false);
                UiInputCoordinator.EndContextIfActive(owner);
            }

            ShowStatus(message);
        }

        public void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            SetText(statusTmpText, message);

            hideStatusAt = Time.unscaledTime + Mathf.Max(0.1f, statusDuration);
            SetStatusActive(true);
        }

        private void HandleCardClicked(int index)
        {
            if (index < 0 || index >= visibleRewards.Count)
            {
                return;
            }

            float now = Time.unscaledTime;
            bool isDoubleClick = selectedIndex == index
                && lastClickedIndex == index
                && now - lastClickAt <= Mathf.Max(0.05f, doubleClickWindow);

            SelectReward(index);
            lastClickedIndex = index;
            lastClickAt = now;

            if (isDoubleClick)
            {
                ClaimSelectedReward();
            }
        }

        private void BindButtons()
        {
            EnsureArrayLengths();
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int index = i;
                if (choiceClickHandlers[i] == null)
                {
                    choiceClickHandlers[i] = () => HandleCardClicked(index);
                }

                if (boundChoiceButtons[i] == choiceButtons[i])
                {
                    continue;
                }

                if (boundChoiceButtons[i] != null)
                {
                    boundChoiceButtons[i].onClick.RemoveListener(choiceClickHandlers[i]);
                }

                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].onClick.AddListener(choiceClickHandlers[i]);
                }

                boundChoiceButtons[i] = choiceButtons[i];
            }

            if (boundClaimButton != claimButton)
            {
                if (boundClaimButton != null)
                {
                    boundClaimButton.onClick.RemoveListener(ClaimSelectedReward);
                }

                if (claimButton != null)
                {
                    claimButton.onClick.AddListener(ClaimSelectedReward);
                }

                boundClaimButton = claimButton;
            }

            if (boundCloseButton != closeButton)
            {
                if (boundCloseButton != null)
                {
                    boundCloseButton.onClick.RemoveListener(HandleBackRequested);
                }

                if (closeButton != null)
                {
                    closeButton.onClick.AddListener(HandleBackRequested);
                }

                boundCloseButton = closeButton;
            }
        }

        private void HandleBackRequested()
        {
            if (activeOwner is WaveRewardController)
            {
                ShowStatus("보상을 선택해야 다음 웨이브가 진행됩니다.");
                return;
            }

            if (activeOwner != null)
            {
                Hide(activeOwner);
            }
        }

        private void SetRewardHintText(UnityEngine.Object owner)
        {
            string hint = owner is WaveRewardController ? MandatoryRewardHint : RewardHint;
            if (hintText != null)
            {
                hintText.text = hint;
            }
            SetText(hintTmpText, hint);
        }

        private void ConfigureClosePolicy(UnityEngine.Object owner)
        {
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(!(owner is WaveRewardController));
            }
        }

        private void SetCleanRewardStaticTexts()
        {
            if (claimButtonText != null)
            {
                claimButtonText.text = ClaimText;
            }

            SetText(claimButtonTmpText, ClaimText);
        }

        private void EnsureArrayLengths()
        {
            EnsureLength(ref choiceButtons);
            EnsureLength(ref choiceTexts);
            EnsureLength(ref choiceTmpTexts);
            EnsureLength(ref choiceIconImages);
            EnsureLength(ref choiceAmountTexts);
            EnsureLength(ref choiceAmountTmpTexts);
            EnsureLength(ref choiceExplanationTmpTexts);
            EnsureLength(ref choiceSelectionImages);
        }

        private static void EnsureLength<T>(ref T[] array)
        {
            if (array != null && array.Length == MaxChoiceCount)
            {
                return;
            }

            T[] resized = new T[MaxChoiceCount];
            if (array != null)
            {
                Array.Copy(array, resized, Mathf.Min(array.Length, resized.Length));
            }

            array = resized;
        }

        private void ApplyRewardCard(int index, TreasureRewardEntry reward)
        {
            bool hasReward = reward != null;
            TMP_Text numberText = ResolveNumberText(index);
            if (numberText != null)
            {
                numberText.SetText("{0}", index + 1);
                numberText.gameObject.SetActive(hasReward);
            }

            if (choiceButtons[index] != null)
            {
                ConfigureChoiceButtonHitArea(choiceButtons[index]);
                choiceButtons[index].gameObject.SetActive(hasReward);
                choiceButtons[index].interactable = hasReward;
                Image background = choiceButtons[index].targetGraphic as Image;
                if (background != null && background.transform == choiceButtons[index].transform)
                {
                    background.color = CardColors[Mathf.Clamp(index, 0, CardColors.Length - 1)];
                }

                ConfigureRewardCardFrame(choiceButtons[index].transform, index);
            }

            if (choiceTexts[index] != null)
            {
                choiceTexts[index].text = BuildNameText(index, reward);
            }
            SetText(choiceTmpTexts, index, BuildNameText(index, reward));

            if (choiceAmountTexts[index] != null)
            {
                choiceAmountTexts[index].text = hasReward ? BuildAmountText(reward) : string.Empty;
            }
            SetText(choiceAmountTmpTexts, index, hasReward ? BuildAmountText(reward) : string.Empty);

            string explanation = hasReward ? BuildCardDescriptionText(reward) : string.Empty;
            SetText(choiceExplanationTmpTexts, index, explanation);
            SetActive(choiceExplanationTmpTexts, index, hasReward && !string.IsNullOrWhiteSpace(explanation));

            if (choiceIconImages[index] != null)
            {
                Sprite rewardIcon = hasReward ? ResolveRewardIcon(reward) : null;
                choiceIconImages[index].sprite = rewardIcon;
                choiceIconImages[index].color = hasReward && rewardIcon == null
                    ? GetFallbackIconColor(reward.GrantType)
                    : Color.white;
                choiceIconImages[index].enabled = hasReward;
                choiceIconImages[index].preserveAspect = true;
                ConfigureRewardIcon(choiceIconImages[index], reward);
            }

            CleanupRewardCardText(index, numberText);
        }

        private void CleanupRewardCardText(int index, TMP_Text numberText)
        {
            Transform cardRoot = index >= 0 && index < rewardCardRoots.Length ? rewardCardRoots[index] : null;
            if (cardRoot == null)
            {
                return;
            }

            TMP_Text nameText = GetText(choiceTmpTexts, index);
            TMP_Text amountText = GetText(choiceAmountTmpTexts, index);
            TMP_Text descriptionText = GetText(choiceExplanationTmpTexts, index);
            ConfigureCardText(nameText, TextAlignmentOptions.Center, TextWrappingModes.Normal, 15f, 20f, RewardCardTextRole.Name);
            ConfigureCardText(amountText, TextAlignmentOptions.Center, TextWrappingModes.NoWrap, 15f, 20f, RewardCardTextRole.Amount);
            ConfigureCardText(descriptionText, TextAlignmentOptions.Center, TextWrappingModes.Normal, 11f, 14f, RewardCardTextRole.Description);
            SetRewardTextColor(nameText, RewardTextColor);
            SetRewardTextColor(amountText, RewardAmountColor);
            SetRewardTextColor(descriptionText, RewardMutedTextColor);

            TMP_Text[] texts = cardRoot.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null
                    || text == nameText
                    || text == amountText
                    || text == descriptionText
                    || text == numberText)
                {
                    continue;
                }

                text.text = string.Empty;
                text.gameObject.SetActive(false);
            }
        }

        private static TMP_Text GetText(TMP_Text[] texts, int index)
        {
            return texts != null && index >= 0 && index < texts.Length ? texts[index] : null;
        }

        private enum RewardCardTextRole
        {
            Name,
            Amount,
            Description
        }

        private static void ConfigureCardText(
            TMP_Text text,
            TextAlignmentOptions alignment,
            TextWrappingModes wrapping,
            float minSize,
            float maxSize,
            RewardCardTextRole role)
        {
            if (text == null)
            {
                return;
            }

            text.alignment = alignment;
            text.textWrappingMode = wrapping;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;

            RectTransform rectTransform = text.rectTransform;
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            if (role == RewardCardTextRole.Amount)
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 150f);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30f);
                rectTransform.anchoredPosition = new Vector2(0f, -22f);
            }
            else if (role == RewardCardTextRole.Name)
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 152f);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40f);
                rectTransform.anchoredPosition = new Vector2(0f, 104f);
            }
            else
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 260f);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 58f);
                rectTransform.anchoredPosition = new Vector2(0f, -112f);
            }
        }

        private static void SetRewardTextColor(TMP_Text text, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.color = color;
        }

        private static void ConfigureRewardCardFrame(Transform cardRoot, int index)
        {
            if (cardRoot == null)
            {
                return;
            }

            Color baseColor = CardColors[Mathf.Clamp(index, 0, CardColors.Length - 1)];
            Image[] images = cardRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image.raycastTarget && image.transform == cardRoot)
                {
                    continue;
                }

                string imageName = image.name;
                if (imageName.Contains("Icon")
                    || imageName.Contains("Selected")
                    || imageName.Contains("Selection"))
                {
                    continue;
                }

                if (imageName.Contains("Frame") || imageName.Contains("Card") || imageName.Contains("Bg") || imageName.Contains("Back"))
                {
                    image.color = new Color(
                        Mathf.Clamp01(baseColor.r + 0.04f),
                        Mathf.Clamp01(baseColor.g + 0.05f),
                        Mathf.Clamp01(baseColor.b + 0.06f),
                        Mathf.Max(baseColor.a, 0.94f));
                }
            }
        }

        private static void ConfigureRewardIcon(Image icon, TreasureRewardEntry reward)
        {
            if (icon == null)
            {
                return;
            }

            RectTransform rectTransform = icon.rectTransform;
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            float size = reward != null && reward.GrantType == TreasureRewardGrantType.Artifact
                ? ArtifactRewardIconSize
                : RewardIconSize;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            rectTransform.anchoredPosition = new Vector2(0f, RewardIconY);
        }

        private void RefreshSelectionVisuals()
        {
            for (int i = 0; i < MaxChoiceCount; i++)
            {
                bool selected = i == selectedIndex;
                if (choiceSelectionImages != null && i < choiceSelectionImages.Length && choiceSelectionImages[i] != null)
                {
                    choiceSelectionImages[i].enabled = selected;
                    choiceSelectionImages[i].gameObject.SetActive(selected);
                    choiceSelectionImages[i].color = RewardSelectionColor;
                }
            }

            if (claimButton != null)
            {
                bool canClaim = selectedIndex >= 0 && selectedIndex < visibleRewards.Count;
                claimButton.interactable = canClaim;
                Image claimImage = claimButton.targetGraphic as Image;
                if (claimImage != null)
                {
                    claimImage.color = canClaim ? ClaimEnabledColor : ClaimDisabledColor;
                }
            }
        }

        private void SetPromptActive(bool active)
        {
            if (promptRoot != null)
            {
                promptRoot.SetActive(active);
            }
        }

        private void SetPanelActive(bool active, bool immediate = false)
        {
            if (panelTransition != null)
            {
                if (active)
                {
                    panelTransition.Show();
                }
                else if (immediate)
                {
                    panelTransition.HideImmediate();
                }
                else
                {
                    panelTransition.Hide();
                }
            }
            else if (panelRoot != null)
            {
                if (active)
                {
                    EnsureVisibleScale(panelRoot.transform);
                }

                panelRoot.SetActive(active);
            }

            if (active)
            {
                PopupDimOverlayController.RequestShow(this, panelRoot != null ? panelRoot.transform : transform);
            }
            else
            {
                PopupDimOverlayController.Release(this);
                SetArtifactDescriptionActive(false);
            }
        }

        private void SelectCurrentButton()
        {
            if (selectedIndex < 0 || selectedIndex >= visibleRewards.Count || choiceButtons == null || selectedIndex >= choiceButtons.Length)
            {
                return;
            }

            Button selectedButton = choiceButtons[selectedIndex];
            if (selectedButton == null || !selectedButton.gameObject.activeInHierarchy)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(selectedButton.gameObject);
            }
        }

        private static void ConfigureChoiceButtonHitArea(Button button)
        {
            if (button == null)
            {
                return;
            }

            Image rootImage = button.GetComponent<Image>();
            if (rootImage == null)
            {
                rootImage = button.gameObject.AddComponent<Image>();
                rootImage.color = new Color(1f, 1f, 1f, 0f);
            }

            rootImage.raycastTarget = true;
        }

        private static void EnsureVisibleScale(Transform root)
        {
            Transform current = root;
            while (current != null)
            {
                if (current.localScale == Vector3.zero)
                {
                    Debug.LogError("[TreasureRewardMenuPresenter] UI transform has zero scale: " + current.name + ".", current);
                    current.localScale = Vector3.one;
                }

                if (current.GetComponent<Canvas>() != null)
                {
                    break;
                }

                current = current.parent;
            }
        }

        private void SetStatusActive(bool active)
        {
            if (statusRoot != null)
            {
                statusRoot.SetActive(active);
            }
        }

        private string BuildNameText(int index, TreasureRewardEntry reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            string displayName = ResolveRewardDisplayName(reward);
            bool hasAmountText = HasText(choiceAmountTexts, index) || HasText(choiceAmountTmpTexts, index);
            return hasAmountText ? displayName : $"{displayName} x{reward.Amount}";
        }

        private static string BuildAmountText(TreasureRewardEntry reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            int amount = Mathf.Max(1, reward.Amount);
            switch (reward.GrantType)
            {
                case TreasureRewardGrantType.Money:
                case TreasureRewardGrantType.KillProgress:
                case TreasureRewardGrantType.StatPoint:
                    return "+" + amount;
                case TreasureRewardGrantType.Artifact:
                    return amount > 1 ? "x" + amount : string.Empty;
                default:
                    return "x" + amount;
            }
        }

        private static string ResolveRewardDisplayName(TreasureRewardEntry reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            if (reward.GrantType == TreasureRewardGrantType.Artifact && reward.ArtifactDefinition != null)
            {
                return reward.ArtifactDefinition.DisplayName;
            }

            return reward.DisplayName;
        }

        private static Sprite ResolveRewardIcon(TreasureRewardEntry reward)
        {
            if (reward == null)
            {
                return null;
            }

            if (reward.Icon != null)
            {
                return reward.Icon;
            }

            return reward.GrantType == TreasureRewardGrantType.Artifact && reward.ArtifactDefinition != null
                ? reward.ArtifactDefinition.Icon
                : null;
        }

        private static string BuildRewardTitleText(UnityEngine.Object owner)
        {
            return BuildCleanTitleText(owner);
        }

        private static string BuildTitleText(UnityEngine.Object owner)
        {
            return BuildRewardTitleText(owner);
        }

        private void RefreshArtifactDescription()
        {
            if (selectedIndex < 0 || selectedIndex >= visibleRewards.Count)
            {
                SetArtifactDescriptionActive(false);
                return;
            }

            string description = BuildArtifactDescriptionText(visibleRewards[selectedIndex]);
            if (string.IsNullOrWhiteSpace(description))
            {
                SetArtifactDescriptionActive(false);
                return;
            }

            if (artifactDescriptionText != null)
            {
                artifactDescriptionText.text = description;
            }
            SetText(artifactDescriptionTmpText, description);
            SetArtifactDescriptionActive(true);
        }

        private void SetArtifactDescriptionActive(bool active)
        {
            if (artifactDescriptionRoot != null)
            {
                artifactDescriptionRoot.SetActive(active);
            }
        }

        private static string BuildArtifactDescriptionText(TreasureRewardEntry reward)
        {
            if (reward == null
                || reward.GrantType != TreasureRewardGrantType.Artifact
                || reward.ArtifactDefinition == null)
            {
                return string.Empty;
            }

            ArtifactDefinitionSO artifact = reward.ArtifactDefinition;
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(artifact.Description))
            {
                builder.Append(artifact.Description.Trim());
            }

            string effectSummary = BuildArtifactEffectSummary(artifact, reward.Amount, "\n");
            if (!string.IsNullOrWhiteSpace(effectSummary))
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append("\uD6A8\uACFC");
                builder.AppendLine();
                builder.Append(effectSummary);
            }

            return builder.ToString();
        }

        private static string BuildCardDescriptionText(TreasureRewardEntry reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            if (reward.GrantType == TreasureRewardGrantType.Artifact)
            {
                return reward.ArtifactDefinition != null
                    ? BuildArtifactEffectSummary(reward.ArtifactDefinition, reward.Amount, "   ")
                    : string.Empty;
            }

            if (reward.GrantType == TreasureRewardGrantType.Money)
            {
                return "\uC790\uAE08 +" + Mathf.Max(1, reward.Amount);
            }

            if (reward.GrantType == TreasureRewardGrantType.KillProgress)
            {
                return "\uACBD\uD5D8\uCE58 +" + Mathf.Max(1, reward.Amount);
            }

            if (reward.GrantType == TreasureRewardGrantType.StatPoint)
            {
                return "\uC2A4\uD0EF \uD3EC\uC778\uD2B8 +" + Mathf.Max(1, reward.Amount);
            }

            if (reward.GrantType == TreasureRewardGrantType.Item
                && reward.ItemDefinition != null
                && !string.IsNullOrWhiteSpace(reward.ItemDefinition.description))
            {
                return reward.ItemDefinition.description.Trim();
            }

            return string.Empty;
        }

        private static string BuildArtifactEffectSummary(ArtifactDefinitionSO artifact, int amount, string separator)
        {
            if (artifact == null || artifact.Modifiers == null)
            {
                return string.Empty;
            }

            int stackCount = Mathf.Max(1, amount);
            IReadOnlyList<ArtifactStatModifier> modifiers = artifact.Modifiers;
            System.Text.StringBuilder builder = new System.Text.StringBuilder(96);
            for (int i = 0; i < modifiers.Count; i++)
            {
                ArtifactStatModifier modifier = modifiers[i];
                if (modifier == null)
                {
                    continue;
                }

                int percent = CalculateDisplayPercent(modifier.Stat, Mathf.Pow(modifier.Multiplier, stackCount));
                if (percent == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(separator);
                }

                builder.Append(GetRewardTargetLabel(modifier.Target));
                builder.Append(' ');
                builder.Append(GetRewardStatLabel(modifier.Stat));
                builder.Append(' ');
                AppendPercent(builder, percent);
            }

            return builder.ToString();
        }

        private static int CalculateDisplayPercent(ArtifactStat stat, float multiplier)
        {
            bool lowerIsBetter = stat == ArtifactStat.AttackInterval || stat == ArtifactStat.Cooldown;
            float percentValue = lowerIsBetter
                ? (1f / Mathf.Max(0.01f, multiplier) - 1f) * 100f
                : (multiplier - 1f) * 100f;
            return Mathf.RoundToInt(percentValue);
        }

        private static void AppendPercent(System.Text.StringBuilder builder, int percent)
        {
            builder.Append(percent >= 0 ? "+" : string.Empty);
            builder.Append(percent);
            builder.Append('%');
        }

        private static string GetRewardTargetLabel(ArtifactTarget target)
        {
            switch (target)
            {
                case ArtifactTarget.Turret:
                    return "\uD3EC\uD0D1";
                case ArtifactTarget.Mortar:
                    return "\uBC15\uACA9\uD3EC";
                case ArtifactTarget.Squad:
                    return "\uC2A4\uCFFC\uB4DC";
                case ArtifactTarget.Player:
                    return "\uD50C\uB808\uC774\uC5B4";
                default:
                    return target.ToString();
            }
        }

        private static string GetRewardStatLabel(ArtifactStat stat)
        {
            switch (stat)
            {
                case ArtifactStat.Damage:
                    return "\uACF5\uACA9";
                case ArtifactStat.AttackInterval:
                    return "\uC5F0\uC0AC";
                case ArtifactStat.Range:
                    return "\uC0AC\uAC70\uB9AC";
                case ArtifactStat.Cooldown:
                    return "\uCFFC\uAC10";
                case ArtifactStat.Health:
                    return "\uCCB4\uB825";
                case ArtifactStat.MoveSpeed:
                    return "\uC18D\uB3C4";
                default:
                    return stat.ToString();
            }
        }

        private static string GetTargetLabel(ArtifactTarget target)
        {
            switch (target)
            {
                case ArtifactTarget.Turret:
                    return "터렛";
                case ArtifactTarget.Mortar:
                    return "박격포";
                case ArtifactTarget.Squad:
                    return "분대";
                case ArtifactTarget.Player:
                    return "플레이어";
                default:
                    return target.ToString();
            }
        }

        private static string GetStatLabel(ArtifactStat stat)
        {
            switch (stat)
            {
                case ArtifactStat.Damage:
                    return "피해";
                case ArtifactStat.AttackInterval:
                    return "공격 간격";
                case ArtifactStat.Range:
                    return "사거리";
                case ArtifactStat.Cooldown:
                    return "재사용 대기";
                case ArtifactStat.Health:
                    return "체력";
                case ArtifactStat.MoveSpeed:
                    return "이동속도";
                default:
                    return stat.ToString();
            }
        }

        private static Color GetFallbackIconColor(TreasureRewardGrantType grantType)
        {
            switch (grantType)
            {
                case TreasureRewardGrantType.Money:
                    return new Color(1f, 0.74f, 0.18f, 0.9f);
                case TreasureRewardGrantType.KillProgress:
                    return new Color(0.28f, 0.74f, 1f, 0.9f);
                case TreasureRewardGrantType.StatPoint:
                    return new Color(0.54f, 0.95f, 0.48f, 0.9f);
                case TreasureRewardGrantType.Item:
                    return new Color(0.9f, 0.76f, 1f, 0.9f);
                case TreasureRewardGrantType.Artifact:
                    return new Color(0.64f, 0.46f, 1f, 0.92f);
                default:
                    return new Color(0.86f, 0.9f, 1f, 0.7f);
            }
        }

        private TMP_Text ResolveNumberText(int index)
        {
            if (choiceButtons == null || index < 0 || index >= choiceButtons.Length || choiceButtons[index] == null)
            {
                return null;
            }

            Transform cardRoot = choiceButtons[index].transform;
            Transform visualRoot = FindChildRecursive(cardRoot, "CardFrame02");
            if (visualRoot == null)
            {
                visualRoot = cardRoot;
            }

            TMP_Text numberText = FindExistingNumberText(visualRoot, index);
            if (numberText == null)
            {
                numberText = FindTmpText(visualRoot, "Text_Num", "NumberText", "SlotNumber");
            }

            if (numberText != null)
            {
                ConfigureNumberText(numberText);
                DisableDuplicateNumberTexts(visualRoot, numberText, index);
                return numberText;
            }

            GameObject numberObject = new GameObject("Text_Num", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            numberObject.transform.SetParent(visualRoot, false);

            numberText = numberObject.GetComponent<TMP_Text>();
            ConfigureNumberText(numberText);
            DisableDuplicateNumberTexts(visualRoot, numberText, index);
            return numberText;
        }

        private static TMP_Text FindExistingNumberText(Transform root, int index)
        {
            if (root == null)
            {
                return null;
            }

            string expected = (index + 1).ToString();
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || IsAmountTextName(text.name))
                {
                    continue;
                }

                if (string.Equals(text.text.Trim(), expected, StringComparison.Ordinal))
                {
                    return text;
                }
            }

            return null;
        }

        private static void DisableDuplicateNumberTexts(Transform root, TMP_Text selectedText, int index)
        {
            if (root == null || selectedText == null)
            {
                return;
            }

            string expected = (index + 1).ToString();
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text == selectedText || IsAmountTextName(text.name))
                {
                    continue;
                }

                bool duplicateByName = text.name == "Text_Num"
                    || text.name == "NumberText"
                    || text.name == "SlotNumber";
                bool duplicateByValue = string.Equals(text.text.Trim(), expected, StringComparison.Ordinal);
                if (duplicateByName || duplicateByValue)
                {
                    text.gameObject.SetActive(false);
                }
            }
        }

        private static bool IsAmountTextName(string name)
        {
            return name == "RewardAmountText"
                || name == "Text_Amount"
                || name == "AmountText"
                || name == "Text_Count";
        }

        private static void ConfigureNumberText(TMP_Text numberText)
        {
            if (numberText == null)
            {
                return;
            }

            RectTransform rectTransform = numberText.transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.anchoredPosition = new Vector2(16f, -14f);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 54f);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 54f);
            }

            LayoutElement layoutElement = numberText.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = numberText.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.ignoreLayout = true;
            numberText.alignment = TextAlignmentOptions.Center;
            numberText.fontSize = 30f;
            numberText.fontStyle = FontStyles.Bold;
            numberText.color = Color.white;
            numberText.textWrappingMode = TextWrappingModes.NoWrap;
            numberText.raycastTarget = false;
            numberText.transform.SetAsLastSibling();
        }

        private static bool HasText(Text[] texts, int index)
        {
            return texts != null && index >= 0 && index < texts.Length && texts[index] != null;
        }

        private static bool HasText(TMP_Text[] texts, int index)
        {
            return texts != null && index >= 0 && index < texts.Length && texts[index] != null;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetText(TMP_Text[] texts, int index, string value)
        {
            if (texts != null && index >= 0 && index < texts.Length && texts[index] != null)
            {
                texts[index].text = value;
            }
        }

        private static void SetActive(TMP_Text[] texts, int index, bool active)
        {
            if (texts != null && index >= 0 && index < texts.Length && texts[index] != null)
            {
                texts[index].gameObject.SetActive(active);
            }
        }

        private void ResolveMissingUiReferences()
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform promptRootTransform = promptRoot != null ? promptRoot.transform : transform;
            Transform statusRootTransform = statusRoot != null ? statusRoot.transform : transform;

            promptTmpText = promptTmpText != null ? promptTmpText : FindTmpText(promptRootTransform, "PromptText", "Text (TMP)");
            titleTmpText = titleTmpText != null ? titleTmpText : FindTmpText(panelRoot.transform, "TitleText", "Text_Title", "Text (TMP)");
            hintTmpText = hintTmpText != null ? hintTmpText : FindTmpText(panelRoot.transform, "HintText", "Text_Hint");
            statusTmpText = statusTmpText != null ? statusTmpText : FindTmpText(statusRootTransform, "StatusText", "Text_Status");
            artifactDescriptionRoot = artifactDescriptionRoot != null ? artifactDescriptionRoot : FindGameObject(panelRoot.transform, "ArtifactDescriptionRoot", "ArtifactDescriptionPanel", "RewardDescriptionRoot");
            Transform artifactDescriptionTransform = artifactDescriptionRoot != null ? artifactDescriptionRoot.transform : panelRoot.transform;
            artifactDescriptionTmpText = artifactDescriptionTmpText != null ? artifactDescriptionTmpText : FindTmpText(artifactDescriptionTransform, "ArtifactDescriptionText", "RewardDescriptionText", "DescriptionText", "Text_Description");

            Transform[] cardRoots = FindRewardCardRoots(panelRoot.transform);
            for (int i = 0; i < cardRoots.Length && i < MaxChoiceCount; i++)
            {
                Transform cardRoot = cardRoots[i];
                rewardCardRoots[i] = cardRoot;
                choiceButtons[i] = choiceButtons[i] != null ? choiceButtons[i] : cardRoot.GetComponent<Button>();
                choiceIconImages[i] = choiceIconImages[i] != null ? choiceIconImages[i] : FindImage(cardRoot, "RewardIcon", "ItemIcon", "Icon");
                choiceTmpTexts[i] = choiceTmpTexts[i] != null ? choiceTmpTexts[i] : FindTmpText(cardRoot, "RewardNameText", "Text_Name", "Text_name", "Text (TMP)");
                choiceAmountTmpTexts[i] = choiceAmountTmpTexts[i] != null ? choiceAmountTmpTexts[i] : FindTmpText(cardRoot, "RewardAmountText", "Text_Amount", "AmountText", "Text_Count");
                choiceExplanationTmpTexts[i] = choiceExplanationTmpTexts[i] != null ? choiceExplanationTmpTexts[i] : FindTmpText(cardRoot, "RewardEffectText", "explanation", " explanation", "Explanation", "ArtifactDescriptionText");
                choiceSelectionImages[i] = choiceSelectionImages[i] != null ? choiceSelectionImages[i] : FindImage(cardRoot, "SelectedOutline", "Selection", "Selected");
            }

            claimButton = claimButton != null ? claimButton : FindButton(panelRoot.transform, "ClaimButton", "Button_Claim", "Claim");
            if (claimButton != null)
            {
                claimButtonTmpText = claimButtonTmpText != null ? claimButtonTmpText : FindTmpText(claimButton.transform, "ClaimButtonText", "Text (TMP)");
            }

            closeButton = closeButton != null ? closeButton : FindButton(panelRoot.transform, "Button_Back", "BackButton", "Button_Exit", "CloseButton", "ExitButton", "Close", "Back");
        }

        private static Transform[] FindRewardCardRoots(Transform root)
        {
            List<Transform> cards = new List<Transform>(MaxChoiceCount);
            CollectRewardCardRoots(root, cards);
            cards.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return cards.ToArray();
        }

        private static void CollectRewardCardRoots(Transform root, List<Transform> results)
        {
            if (root == null || results.Count >= MaxChoiceCount)
            {
                return;
            }

            if (root.name.StartsWith("RewardCard_", StringComparison.Ordinal))
            {
                results.Add(root);
                return;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                CollectRewardCardRoots(root.GetChild(i), results);
            }
        }

        private static Button FindButton(Transform root, params string[] names)
        {
            Transform found = FindFirstNamedChild(root, names);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static Image FindImage(Transform root, params string[] names)
        {
            Transform found = FindFirstNamedChild(root, names);
            return found != null ? found.GetComponent<Image>() : null;
        }

        private static GameObject FindGameObject(Transform root, params string[] names)
        {
            Transform found = FindFirstNamedChild(root, names);
            return found != null ? found.gameObject : null;
        }

        private static TMP_Text FindTmpText(Transform root, params string[] names)
        {
            Transform found = FindFirstNamedChild(root, names);
            return found != null ? found.GetComponent<TMP_Text>() : null;
        }

        private static Transform FindFirstNamedChild(Transform root, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Transform found = FindChildRecursive(root, names[i]);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
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

        private void WarnIfMissingReferences()
        {
            if (panelRoot == null)
            {
                Debug.LogWarning("[TreasureRewardMenuPresenter] Reward Panel Root is not assigned.", this);
            }

            // Treasure prompts are presented by InteractionPromptPresenter. This presenter owns only the reward panel/status.
        }

        private static string BuildCleanTitleText(UnityEngine.Object owner)
        {
            return owner is WaveRewardController ? "보스 웨이브 보상" : "보물 선택";
        }

        private static string GetCleanTargetLabel(ArtifactTarget target)
        {
            switch (target)
            {
                case ArtifactTarget.Turret:
                    return "터렛";
                case ArtifactTarget.Mortar:
                    return "박격포";
                case ArtifactTarget.Squad:
                    return "분대";
                case ArtifactTarget.Player:
                    return "플레이어";
                default:
                    return target.ToString();
            }
        }

        private static string GetCleanStatLabel(ArtifactStat stat)
        {
            switch (stat)
            {
                case ArtifactStat.Damage:
                    return "피해";
                case ArtifactStat.AttackInterval:
                    return "공격 간격";
                case ArtifactStat.Range:
                    return "사거리";
                case ArtifactStat.Cooldown:
                    return "재사용 대기";
                case ArtifactStat.Health:
                    return "체력";
                case ArtifactStat.MoveSpeed:
                    return "이동속도";
                default:
                    return stat.ToString();
            }
        }

        private static void WarnIfMissingPresenter()
        {
            if (instance == null && !missingPresenterWarned)
            {
                Debug.LogWarning("[TreasureRewardMenuPresenter] No presenter exists in the active scene.");
                missingPresenterWarned = true;
            }
        }
    }
}
