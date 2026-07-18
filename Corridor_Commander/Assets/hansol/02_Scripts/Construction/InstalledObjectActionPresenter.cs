using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class InstalledObjectActionPresenter : MonoBehaviour
    {
        private const int MaxActionCount = 3;

        private static InstalledObjectActionPresenter instance;
        private static bool missingPresenterWarned;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private DotweenUiPanelTransition panelTransition;
        [SerializeField] private Text titleText;
        [SerializeField] private Button[] actionButtons = new Button[MaxActionCount];
        [SerializeField] private Text[] actionTexts = new Text[MaxActionCount];
        [SerializeField] private Text detailText;
        [SerializeField] private Text hintText;
        [SerializeField] private Text statusText;
        [SerializeField] private TMP_Text titleTmpText;
        [SerializeField] private TMP_Text[] actionTmpTexts = new TMP_Text[MaxActionCount];
        [SerializeField] private TMP_Text[] actionInfoTmpTexts = new TMP_Text[MaxActionCount];
        [SerializeField] private TMP_Text[] actionCostTmpTexts = new TMP_Text[MaxActionCount];
        [SerializeField] private TMP_Text detailTmpText;
        [SerializeField] private TMP_Text hintTmpText;
        [SerializeField] private TMP_Text statusTmpText;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject popupFrameRoot;
        [SerializeField] private GameObject[] actionCurrencyIconRoots = new GameObject[MaxActionCount];
        [SerializeField] private GameObject[] actionCostIconRoots = new GameObject[MaxActionCount];
        [SerializeField] private Transform[] actionUpgradeStarRoots = new Transform[MaxActionCount];
        [SerializeField] private GameObject[] actionHealthBarRoots = new GameObject[MaxActionCount];
        [SerializeField] private Image[] actionHealthFillImages = new Image[MaxActionCount];
        [SerializeField] private Slider[] actionHealthSliders = new Slider[MaxActionCount];
        [SerializeField] private TMP_Text[] actionHealthTmpTexts = new TMP_Text[MaxActionCount];
        [SerializeField] private Image[] actionIconImages = new Image[MaxActionCount];
        [SerializeField] private Sprite[] actionIcons = new Sprite[MaxActionCount];

        private readonly List<InstalledObjectAction> actions = new List<InstalledObjectAction>(MaxActionCount);
        private InstalledObjectInteraction currentInteraction;
        private IInstalledObjectActionProvider activeProvider;
        private Transform activePlayer;
        private bool actionListenersBound;
        private bool closeButtonListenerBound;

        public static InstalledObjectActionPresenter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<InstalledObjectActionPresenter>(FindObjectsInactive.Include);
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
            ResolveMissingUiReferences();
            BindButtons();
            if (panelTransition != null)
            {
                panelTransition.HideImmediate();
                SetSupplementalRootsActive(false);
                PopupDimOverlayController.Release(this);
            }
            else
            {
                SetPanelActive(false);
            }
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
            if (activeProvider == null || panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }

            if (KeyboardInputMessenger.WasMenuSlotPressed(1)
                && UiInputCoordinator.Instance.TryConsumeMenuSlot(currentInteraction, 1))
            {
                SubmitAction(0);
            }

            if (KeyboardInputMessenger.WasMenuSlotPressed(2)
                && UiInputCoordinator.Instance.TryConsumeMenuSlot(currentInteraction, 2))
            {
                SubmitAction(1);
            }

            if (KeyboardInputMessenger.WasMenuSlotPressed(3)
                && UiInputCoordinator.Instance.TryConsumeMenuSlot(currentInteraction, 3))
            {
                SubmitAction(2);
            }

            if (KeyboardInputMessenger.WasCancelPressed()
                && UiInputCoordinator.Instance.TryConsumeCancel(currentInteraction))
            {
                HandleBackRequested();
            }
        }

        public void Show(
            InstalledObjectInteraction interaction,
            IInstalledObjectActionProvider provider,
            Transform player)
        {
            if (interaction == null || provider == null)
            {
                return;
            }

            currentInteraction = interaction;
            activeProvider = provider;
            activePlayer = player;
            ResolveMissingUiReferences();
            BindButtons();
            SetPanelActive(true);
            ClearStatus();
            RefreshActions();
        }

        public void Hide(InstalledObjectInteraction interaction)
        {
            if (currentInteraction != null && currentInteraction != interaction)
            {
                return;
            }

            Hide();
        }

        public void Hide()
        {
            currentInteraction?.NotifyMenuClosed(this);
            currentInteraction = null;
            activeProvider = null;
            activePlayer = null;
            actions.Clear();
            SetPanelActive(false);
            ClearStatus();
        }

        private void HandleBackRequested()
        {
            Hide();
        }

        private void SubmitAction(int actionIndex)
        {
            if (activeProvider == null)
            {
                Hide();
                return;
            }

            RefreshActionList();
            if (actionIndex < 0 || actionIndex >= actions.Count || !actions[actionIndex].IsEnabled)
            {
                return;
            }

            bool closeAfterAction = actions[actionIndex].CloseAfterExecute;
            if (!activeProvider.ExecuteAction(actionIndex, activePlayer, out string statusMessage))
            {
                closeAfterAction = false;
            }

            ShowStatus(statusMessage);
            if (closeAfterAction)
            {
                Hide();
                return;
            }

            RefreshActions();
        }

        private void RefreshActions()
        {
            RefreshActionList();

            if (titleText != null)
            {
                titleText.text = activeProvider != null ? activeProvider.Title : string.Empty;
            }
            SetText(titleTmpText, activeProvider != null ? activeProvider.Title : string.Empty);

            if (detailText != null)
            {
                detailText.text = activeProvider != null ? activeProvider.GetSummary() : string.Empty;
            }
            SetText(detailTmpText, activeProvider != null ? activeProvider.GetSummary() : string.Empty);

            for (int i = 0; i < MaxActionCount; i++)
            {
                bool hasAction = i < actions.Count;
                string label = hasAction ? $"[{i + 1}] {actions[i].Label}" : string.Empty;
                SetAction(i, hasAction, hasAction && actions[i].IsEnabled, label, hasAction ? actions[i] : default);
            }

            if (hintText != null)
            {
                hintText.text = "1-3 \uC2E4\uD589 / ESC\u00B7\uB4A4\uB85C \uB2EB\uAE30";
            }
            SetText(hintTmpText, "1-3 \uC2E4\uD589 / ESC\u00B7\uB4A4\uB85C \uB2EB\uAE30");
        }

        private void RefreshActionList()
        {
            actions.Clear();
            activeProvider?.CollectActions(actions);

            while (actions.Count > MaxActionCount)
            {
                actions.RemoveAt(actions.Count - 1);
            }
        }

        private void SetAction(
            int index,
            bool visible,
            bool interactable,
            string label,
            InstalledObjectAction action)
        {
            if (actionButtons != null && index >= 0 && index < actionButtons.Length && actionButtons[index] != null)
            {
                actionButtons[index].gameObject.SetActive(visible);
                actionButtons[index].interactable = interactable;
            }

            if (actionTexts != null && index >= 0 && index < actionTexts.Length && actionTexts[index] != null)
            {
                actionTexts[index].text = label;
            }

            if (actionTmpTexts != null && index >= 0 && index < actionTmpTexts.Length && actionTmpTexts[index] != null)
            {
                actionTmpTexts[index].text = label;
            }

            if (actionInfoTmpTexts != null && index >= 0 && index < actionInfoTmpTexts.Length && actionInfoTmpTexts[index] != null)
            {
                actionInfoTmpTexts[index].text = visible ? action.InfoLabel : string.Empty;
                actionInfoTmpTexts[index].gameObject.SetActive(visible && !string.IsNullOrWhiteSpace(action.InfoLabel));
            }

            SetOptionalRoot(actionCurrencyIconRoots, index, visible && action.ShowCurrencyIcon);
            SetCost(index, visible ? action.CostLabel : string.Empty);
            SetOptionalRoot(actionHealthBarRoots, index, visible && action.ShowHealthBar);
            SetHealthText(index, action.ShowHealthBar ? action.InfoLabel : string.Empty);
            SetHealthFill(index, action.ShowHealthBar ? action.FillAmount : 0f);
            SetUpgradeStars(index, visible && action.ShowUpgradeStars, action.CurrentValue, action.MaxValue);
            SetActionIcon(index, visible);
        }

        private void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            SetText(statusTmpText, message);
        }

        private void ClearStatus()
        {
            if (statusText != null)
            {
                statusText.text = string.Empty;
            }
            SetText(statusTmpText, string.Empty);
        }

        private void BindButtons()
        {
            if (!actionListenersBound && actionButtons != null)
            {
                for (int i = 0; i < actionButtons.Length; i++)
                {
                    int actionIndex = i;
                    if (actionButtons[i] != null)
                    {
                        actionButtons[i].onClick.AddListener(() => SubmitAction(actionIndex));
                    }
                }

                actionListenersBound = true;
            }

            if (!closeButtonListenerBound && closeButton != null)
            {
                closeButton.onClick.AddListener(HandleBackRequested);
                closeButtonListenerBound = true;
            }
        }

        private void SetPanelActive(bool active)
        {
            if (active && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (active)
            {
                SetSupplementalRootsActive(true);
                if (panelTransition != null)
                {
                    panelTransition.Show();
                }
                else if (panelRoot != null)
                {
                    panelRoot.SetActive(true);
                }
            }
            else if (panelTransition != null)
            {
                SetSupplementalRootsActive(false);
                panelTransition.Hide();
            }
            else
            {
                if (panelRoot != null)
                {
                    panelRoot.SetActive(false);
                }

                SetSupplementalRootsActive(false);
            }

            if (active)
            {
                PopupDimOverlayController.RequestShow(this, panelRoot != null ? panelRoot.transform : transform);
            }
            else
            {
                PopupDimOverlayController.Release(this);
            }
        }

        private void SetSupplementalRootsActive(bool active)
        {
            if (popupFrameRoot != null && popupFrameRoot != panelRoot)
            {
                popupFrameRoot.SetActive(active);
            }

            if (closeButton != null && closeButton.gameObject != panelRoot)
            {
                closeButton.gameObject.SetActive(active);
            }
        }

        private void ResolveMissingUiReferences()
        {
            popupFrameRoot ??= FindChildRecursive(transform, "Background_Common")?.gameObject
                ?? FindChildRecursive(transform, "InstalledObjectPopupFrame")?.gameObject;
            closeButton ??= FindCloseButton(transform);

            if (panelRoot == null)
            {
                panelRoot = FindChildRecursive(transform, "MenuPanel")?.gameObject
                    ?? FindChildRecursive(transform, "InstalledObjectPanel")?.gameObject;

                if (panelRoot == null)
                {
                    Debug.LogWarning("[InstalledObjectActionPresenter] Panel Root is not assigned.", this);
                    return;
                }
            }

            titleTmpText ??= FindTmpText(panelRoot.transform, "TitleText", "Text_Title");
            detailTmpText ??= FindTmpText(panelRoot.transform, "DetailText", "Text_Detail", "explanation");
            hintTmpText ??= FindTmpText(panelRoot.transform, "HintText", "Text_Hint");
            statusTmpText ??= FindTmpText(panelRoot.transform, "StatusText", "Text_Status");

            if (actionButtons == null || actionButtons.Length != MaxActionCount)
            {
                actionButtons = new Button[MaxActionCount];
            }

            if (actionTexts == null || actionTexts.Length != MaxActionCount)
            {
                actionTexts = new Text[MaxActionCount];
            }

            if (actionTmpTexts == null || actionTmpTexts.Length != MaxActionCount)
            {
                actionTmpTexts = new TMP_Text[MaxActionCount];
            }

            EnsureAuxiliaryArrays();

            Transform actionRoot = FindActiveSelfChildRecursive(panelRoot.transform, "CardFrame02-Group")
                ?? FindActiveSelfChildRecursive(panelRoot.transform, "CardFrame03-Group")
                ?? FindChildRecursive(panelRoot.transform, "CardFrame02-Group")
                ?? FindChildRecursive(panelRoot.transform, "CardFrame03-Group");
            if (actionRoot == null)
            {
                actionRoot = panelRoot.transform;
            }

            int slotIndex = 0;
            for (int i = 0; i < actionRoot.childCount && slotIndex < MaxActionCount; i++)
            {
                Transform child = actionRoot.GetChild(i);
                Button button = child.GetComponent<Button>();
                TMP_Text label = FindTmpText(child, "Text_Name", "Text_name", "Text");
                Text legacyLabel = FindLegacyText(child, "Text_Name", "Text_name", "Text");

                if (button == null && label == null && legacyLabel == null)
                {
                    continue;
                }

                if (NeedsSlotRebind(actionButtons[slotIndex], child))
                {
                    actionButtons[slotIndex] = button;
                }

                if (NeedsSlotRebind(actionTmpTexts[slotIndex], child))
                {
                    actionTmpTexts[slotIndex] = label;
                }

                if (NeedsSlotRebind(actionTexts[slotIndex], child))
                {
                    actionTexts[slotIndex] = legacyLabel;
                }

                if (NeedsSlotRebind(actionInfoTmpTexts[slotIndex], child))
                {
                    actionInfoTmpTexts[slotIndex] = FindTmpText(child, "InfoText", "ActionInfoText", "ValueText");
                }

                if (NeedsSlotRebind(actionCostTmpTexts[slotIndex], child))
                {
                    actionCostTmpTexts[slotIndex] = FindTmpText(child, "CostText", "prices_text", "PriceText");
                }

                if (NeedsSlotRebind(actionCurrencyIconRoots[slotIndex], child))
                {
                    actionCurrencyIconRoots[slotIndex] = FindChildRecursive(child, "CurrencyIcon")?.gameObject
                        ?? FindChildRecursive(child, "GoldIcon")?.gameObject;
                }

                if (NeedsSlotRebind(actionCostIconRoots[slotIndex], child))
                {
                    actionCostIconRoots[slotIndex] = FindChildRecursive(child, "CostIcon")?.gameObject;
                }

                if (NeedsSlotRebind(actionUpgradeStarRoots[slotIndex], child))
                {
                    actionUpgradeStarRoots[slotIndex] = FindChildRecursive(child, "UpgradeStars");
                }

                if (NeedsSlotRebind(actionIconImages[slotIndex], child))
                {
                    actionIconImages[slotIndex] = FindActionIconImage(child);
                }

                Transform healthBar = FindChildRecursive(child, "HealthBar");
                if (healthBar != null)
                {
                    if (actionHealthBarRoots[slotIndex] == null
                        || actionHealthBarRoots[slotIndex].transform != healthBar)
                    {
                        actionHealthBarRoots[slotIndex] = healthBar.gameObject;
                    }

                    if (actionHealthFillImages[slotIndex] == null
                        || !actionHealthFillImages[slotIndex].transform.IsChildOf(healthBar))
                    {
                        actionHealthFillImages[slotIndex] = FindImage(healthBar, "HealthFill", "Fill", "FillImage");
                    }

                    if (actionHealthSliders[slotIndex] == null
                        || !actionHealthSliders[slotIndex].transform.IsChildOf(healthBar))
                    {
                        actionHealthSliders[slotIndex] = healthBar.GetComponentInChildren<Slider>(true);
                    }

                    if (actionHealthTmpTexts[slotIndex] == null
                        || !actionHealthTmpTexts[slotIndex].transform.IsChildOf(healthBar))
                    {
                        actionHealthTmpTexts[slotIndex] = FindTmpText(healthBar, "Text (TMP)", "HealthText", "Text");
                    }
                }

                TMP_Text numberText = FindTmpText(child, "Text_Num");
                if (numberText != null)
                {
                    numberText.text = (slotIndex + 1).ToString();
                }

                slotIndex++;
            }
        }

        private void EnsureAuxiliaryArrays()
        {
            if (actionInfoTmpTexts == null || actionInfoTmpTexts.Length != MaxActionCount)
            {
                actionInfoTmpTexts = new TMP_Text[MaxActionCount];
            }

            if (actionCurrencyIconRoots == null || actionCurrencyIconRoots.Length != MaxActionCount)
            {
                actionCurrencyIconRoots = new GameObject[MaxActionCount];
            }

            if (actionCostTmpTexts == null || actionCostTmpTexts.Length != MaxActionCount)
            {
                actionCostTmpTexts = new TMP_Text[MaxActionCount];
            }

            if (actionCostIconRoots == null || actionCostIconRoots.Length != MaxActionCount)
            {
                actionCostIconRoots = new GameObject[MaxActionCount];
            }

            if (actionUpgradeStarRoots == null || actionUpgradeStarRoots.Length != MaxActionCount)
            {
                actionUpgradeStarRoots = new Transform[MaxActionCount];
            }

            if (actionHealthBarRoots == null || actionHealthBarRoots.Length != MaxActionCount)
            {
                actionHealthBarRoots = new GameObject[MaxActionCount];
            }

            if (actionHealthFillImages == null || actionHealthFillImages.Length != MaxActionCount)
            {
                actionHealthFillImages = new Image[MaxActionCount];
            }

            if (actionHealthSliders == null || actionHealthSliders.Length != MaxActionCount)
            {
                actionHealthSliders = new Slider[MaxActionCount];
            }

            if (actionHealthTmpTexts == null || actionHealthTmpTexts.Length != MaxActionCount)
            {
                actionHealthTmpTexts = new TMP_Text[MaxActionCount];
            }

            if (actionIconImages == null || actionIconImages.Length != MaxActionCount)
            {
                actionIconImages = new Image[MaxActionCount];
            }

            if (actionIcons == null || actionIcons.Length != MaxActionCount)
            {
                actionIcons = new Sprite[MaxActionCount];
            }
        }

        private static void SetOptionalRoot(GameObject[] roots, int index, bool active)
        {
            if (roots != null && index >= 0 && index < roots.Length && roots[index] != null)
            {
                roots[index].SetActive(active);
            }
        }

        private void SetHealthFill(int index, float fillAmount)
        {
            if (index < 0)
            {
                return;
            }

            float clampedFill = Mathf.Clamp01(fillAmount);

            if (actionHealthFillImages != null
                && index < actionHealthFillImages.Length
                && actionHealthFillImages[index] != null)
            {
                actionHealthFillImages[index].fillAmount = clampedFill;
            }

            if (actionHealthSliders != null
                && index < actionHealthSliders.Length
                && actionHealthSliders[index] != null)
            {
                actionHealthSliders[index].SetValueWithoutNotify(clampedFill);
            }
        }

        private void SetCost(int index, string value)
        {
            bool hasCost = !string.IsNullOrWhiteSpace(value);

            if (actionCostTmpTexts != null
                && index >= 0
                && index < actionCostTmpTexts.Length
                && actionCostTmpTexts[index] != null)
            {
                actionCostTmpTexts[index].text = value;
                actionCostTmpTexts[index].gameObject.SetActive(hasCost);
            }

            SetOptionalRoot(actionCostIconRoots, index, hasCost);
        }

        private void SetHealthText(int index, string value)
        {
            if (actionHealthTmpTexts != null
                && index >= 0
                && index < actionHealthTmpTexts.Length
                && actionHealthTmpTexts[index] != null)
            {
                actionHealthTmpTexts[index].text = value;
            }
        }

        private void SetUpgradeStars(int index, bool visible, int currentValue, int maxValue)
        {
            if (actionUpgradeStarRoots == null
                || index < 0
                || index >= actionUpgradeStarRoots.Length
                || actionUpgradeStarRoots[index] == null)
            {
                return;
            }

            actionUpgradeStarRoots[index].gameObject.SetActive(visible);

            Image[] stars = actionUpgradeStarRoots[index].GetComponentsInChildren<Image>(true);
            int clampedCurrent = Mathf.Clamp(currentValue, 0, Mathf.Max(0, maxValue));
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null)
                {
                    continue;
                }

                stars[i].gameObject.SetActive(visible && i < clampedCurrent);
            }
        }

        private void SetActionIcon(int index, bool visible)
        {
            if (actionIconImages == null
                || index < 0
                || index >= actionIconImages.Length
                || actionIconImages[index] == null)
            {
                return;
            }

            Sprite icon = actionIcons != null && index < actionIcons.Length ? actionIcons[index] : null;
            actionIconImages[index].sprite = icon;
            actionIconImages[index].enabled = visible && icon != null;
            actionIconImages[index].preserveAspect = true;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static TMP_Text FindTmpText(Transform root, params string[] names)
        {
            Transform found = FindFirstNamedChild(root, names);
            return found != null ? found.GetComponent<TMP_Text>() : null;
        }

        private static Text FindLegacyText(Transform root, params string[] names)
        {
            Transform found = FindFirstNamedChild(root, names);
            return found != null ? found.GetComponent<Text>() : null;
        }

        private static Image FindImage(Transform root, params string[] names)
        {
            Transform found = FindFirstNamedChild(root, names);
            return found != null ? found.GetComponent<Image>() : null;
        }

        private static Image FindActionIconImage(Transform slotRoot)
        {
            Image namedIcon = FindImage(
                slotRoot,
                "ItemIcon",
                "Icon_GemPack_1",
                "Icon_CoinPack_1",
                "Icon");
            if (namedIcon != null && !IsAuxiliaryIconName(namedIcon.name))
            {
                return namedIcon;
            }

            Image[] images = slotRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || IsAuxiliaryIconName(image.name))
                {
                    continue;
                }

                if (image.name.Contains("Icon"))
                {
                    return image;
                }
            }

            return null;
        }

        private static bool IsAuxiliaryIconName(string value)
        {
            return value == "CurrencyIcon"
                || value == "CostIcon"
                || value == "GoldIcon";
        }

        private static Button FindButton(Transform root, params string[] names)
        {
            Transform found = FindFirstNamedChild(root, names);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static Button FindCloseButton(Transform root)
        {
            Button namedButton = FindButton(root, "Button_Back", "BackButton", "Exit", "EXit", "EXIT", "CloseButton", "Button_Exit", "ExitButton");
            if (namedButton != null)
            {
                return namedButton;
            }

            if (root == null)
            {
                return null;
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                if (IsCloseLabel(button.name))
                {
                    return button;
                }

                TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
                if (text != null && IsCloseLabel(text.text))
                {
                    return button;
                }

                Text legacyText = button.GetComponentInChildren<Text>(true);
                if (legacyText != null && IsCloseLabel(legacyText.text))
                {
                    return button;
                }
            }

            return null;
        }

        private static bool IsCloseLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value.Trim();
            return normalized.Equals("Exit", System.StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Close", System.StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Back", System.StringComparison.OrdinalIgnoreCase)
                || normalized == "\uB4A4\uB85C"
                || normalized == "\uB2EB\uAE30";
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

        private static bool NeedsSlotRebind(Component current, Transform slotRoot)
        {
            return current == null || slotRoot == null || !current.transform.IsChildOf(slotRoot);
        }

        private static bool NeedsSlotRebind(GameObject current, Transform slotRoot)
        {
            return current == null || slotRoot == null || !current.transform.IsChildOf(slotRoot);
        }

        private static bool NeedsSlotRebind(Transform current, Transform slotRoot)
        {
            return current == null || slotRoot == null || !current.IsChildOf(slotRoot);
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

        private static Transform FindActiveSelfChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            if (root.name == childName && root.gameObject.activeSelf)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindActiveSelfChildRecursive(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void WarnIfMissingPresenter()
        {
            if (instance == null && !missingPresenterWarned)
            {
                Debug.LogWarning("[InstalledObjectActionPresenter] No presenter exists in the active scene.");
                missingPresenterWarned = true;
            }
        }
    }
}
