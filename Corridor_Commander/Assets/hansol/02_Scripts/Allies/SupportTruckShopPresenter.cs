using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerControl;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class SupportTruckShopPresenter : MonoBehaviour
    {
        public const int MaxVisibleOfferCount = 5;

        private enum MenuMode
        {
            CategorySelection,
            OfferSelection,
            StatUpgradeSelection
        }

        private sealed class CategoryView
        {
            public GameObject Root;
            public Button Button;
            public TMP_Text NameText;
            public TMP_Text NumberText;
        }

        private sealed class ListFrameView
        {
            public GameObject Root;
            public Button Button;
            public TMP_Text NameText;
            public TMP_Text NumberText;
            public TMP_Text PriceText;
            public TMP_Text ExplanationText;
            public Image IconImage;
            public CanvasGroup CanvasGroup;
        }

        private static readonly SupportTruckShopCategory[] CategorySlots =
        {
            SupportTruckShopCategory.Items,
            SupportTruckShopCategory.Squad,
            SupportTruckShopCategory.Upgrades
        };

        private static readonly string[] CategoryLabels =
        {
            "Items",
            "Squad",
            "Upgrades",
            "Stats"
        };

        private const string PreviousPageKeyLabel = "A";
        private const string NextPageKeyLabel = "D";
        private const string PreviousPageCaption = "PREV";
        private const string NextPageCaption = "NEXT";

        private static SupportTruckShopPresenter instance;
        private static bool missingPresenterWarned;

        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private DotweenUiPanelTransition panelTransition;
        [SerializeField] private Text titleText;
        [SerializeField] private Text currencyText;
        [SerializeField] private Button[] choiceButtons = new Button[MaxVisibleOfferCount];
        [SerializeField] private Text[] choiceTexts = new Text[MaxVisibleOfferCount];
        [SerializeField] private Button closeButton;
        [SerializeField] private Button previousPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Text pageText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text hintText;
        [SerializeField] private Text statusText;
        [SerializeField] private PlayerCurrencyWallet playerCurrencyWallet;
        [SerializeField] private PlayerWeaponInventory playerWeaponInventory;
        [SerializeField] private PlayerLevelProgression playerLevelProgression;
        [SerializeField] private PlayerStatModifier playerStatModifier;
        [SerializeField] private PlayerStatUpgradeController playerStatUpgradeController;
        [SerializeField] private int debugAvailableCurrency = 500;

        [Header("Stat Upgrade Icons")]
        [SerializeField] private Sprite healthUpgradeIcon;
        [SerializeField] private Sprite damageUpgradeIcon;
        [SerializeField] private Sprite moveSpeedUpgradeIcon;
        [SerializeField] private Sprite staminaUpgradeIcon;

        [Header("New Panel Auto Binding")]
        [SerializeField] private string newPanelName = "new";
        [SerializeField] private string categoryPanelName = "MenuPanel";

        private SupportTruckShopInteraction currentInteraction;
        private SupportTruckShop activeShop;
        private SupportTruckShopOfferListSO activeList;
        private MenuMode mode;
        private bool listenersBound;
        private bool walletEventsBound;
        private bool statEventsBound;
        private Transform newRoot;
        private GameObject newBackgroundRoot;
        private GameObject categoryPanelRoot;
        private GameObject categoryGroupRoot;
        private GameObject pageControlsRoot;
        private readonly CategoryView[] categoryViews = new CategoryView[MaxVisibleOfferCount - 1];
        private readonly Transform[] listRoots = new Transform[MaxVisibleOfferCount - 1];
        private readonly Transform[] listContentRoots = new Transform[MaxVisibleOfferCount - 1];
        private readonly GameObject[] listDecorationRoots = new GameObject[MaxVisibleOfferCount - 1];
        private readonly GameObject[] listTemplates = new GameObject[MaxVisibleOfferCount - 1];
        private readonly List<GameObject>[] listFrameSets =
        {
            new List<GameObject>(),
            new List<GameObject>(),
            new List<GameObject>(),
            new List<GameObject>()
        };
        private TMP_Text newTitleText;
        private TMP_Text newCurrencyText;
        private TMP_Text newHintText;
        private TMP_Text newPageText;
        private int currentOfferPage;
        private bool newPanelBound;

        public event Action<SupportTruckShopOfferEntry> OfferPurchased;

        public static SupportTruckShopPresenter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SupportTruckShopPresenter>(FindObjectsInactive.Include);
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
            BindNewPanelIfNeeded();
            BindButtons();
            ResolveCurrencyWallet();
            ResolveWeaponInventory();
            ResolveStatUpgradeReferences();
            SetPromptActive(false);
            SetPanelActive(false, true);
        }

        private void OnEnable()
        {
            ResolveCurrencyWallet();
            ResolveWeaponInventory();
            ResolveStatUpgradeReferences();
            BindWalletEvents();
            BindStatEvents();
        }

        private void OnDisable()
        {
            UnbindWalletEvents();
            UnbindStatEvents();
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
            if (activeShop == null || panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }

            if (mode == MenuMode.OfferSelection)
            {
                RefreshOfferPageControls();

                if (KeyboardInputMessenger.WasPagePreviousPressed()
                    && UiInputCoordinator.Instance.TryConsumeContextInput(currentInteraction))
                {
                    GoToPreviousOfferPage();
                }

                if (KeyboardInputMessenger.WasPageNextPressed()
                    && UiInputCoordinator.Instance.TryConsumeContextInput(currentInteraction))
                {
                    GoToNextOfferPage();
                }
            }

            for (int i = 0; i < MaxVisibleOfferCount; i++)
            {
                if (KeyboardInputMessenger.WasMenuSlotPressed(i + 1)
                    && UiInputCoordinator.Instance.TryConsumeMenuSlot(currentInteraction, i + 1))
                {
                    SubmitSlot(i);
                }
            }

            if (KeyboardInputMessenger.WasCancelPressed()
                && UiInputCoordinator.Instance.TryConsumeCancel(currentInteraction))
            {
                HandleBackRequested();
            }
        }

        public void ShowPrompt(SupportTruckShopInteraction interaction, bool visible, string message)
        {
            if (interaction == null || activeShop != null)
            {
                return;
            }

            if (promptText != null)
            {
                promptText.text = visible ? message : string.Empty;
            }

            SetPromptActive(visible);
        }

        public void Show(SupportTruckShopInteraction interaction, SupportTruckShop shop, Transform player)
        {
            if (interaction == null || shop == null)
            {
                return;
            }

            currentInteraction = interaction;
            activeShop = shop;
            BindNewPanelIfNeeded();
            ResolveCurrencyWallet();
            ResolveStatUpgradeReferences();

            if (player != null)
            {
                activeShop.SetDefaultFollowTarget(player);

                if (playerCurrencyWallet == null)
                {
                    SetCurrencyWallet(player.GetComponentInParent<PlayerCurrencyWallet>());
                }

                if (playerCurrencyWallet == null)
                {
                    SetCurrencyWallet(player.GetComponentInChildren<PlayerCurrencyWallet>(true));
                }

                ResolveStatUpgradeReferences(player);
            }

            SetPromptActive(false);
            SetPanelActive(true);
            ShowCategories();
        }

        public void ShowStatUpgrades(SupportTruckShopInteraction interaction, SupportTruckShop shop, Transform player)
        {
            if (interaction == null || shop == null)
            {
                return;
            }

            currentInteraction = interaction;
            activeShop = shop;
            BindNewPanelIfNeeded();
            ResolveCurrencyWallet();
            ResolveStatUpgradeReferences();

            if (player != null)
            {
                activeShop.SetDefaultFollowTarget(player);
                ResolveStatUpgradeReferences(player);
            }

            SetPromptActive(false);
            SetPanelActive(true);
            ShowStatUpgradeMenu();
        }

        public void Hide(SupportTruckShopInteraction interaction)
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
            activeShop = null;
            activeList = null;
            currentOfferPage = 0;
            SetPanelActive(false);
            ClearStatus();
        }

        private void ShowCategories()
        {
            mode = MenuMode.CategorySelection;
            activeList = null;
            currentOfferPage = 0;

            if (titleText != null)
            {
                titleText.text = "Support Truck Shop";
            }

            RefreshCurrency();
            SetChoice(0, true, "[1] Items");
            SetChoice(1, true, "[2] Squad");
            SetChoice(2, true, "[3] Upgrades");
            SetChoice(3, true, "[4] Stats");

            for (int i = 4; i < MaxVisibleOfferCount; i++)
            {
                SetChoice(i, false, string.Empty);
            }

            if (descriptionText != null)
            {
                descriptionText.text = "Select a shop category";
            }

            if (hintText != null)
            {
                hintText.text = "1-4 선택 / ESC·뒤로 닫기";
            }

            RefreshOfferPageControls();
            RefreshNewCategorySelection();
        }

        private void ShowStatUpgradeMenu()
        {
            mode = MenuMode.StatUpgradeSelection;
            activeList = null;
            currentOfferPage = 0;
            ResolveStatUpgradeReferences();

            if (titleText != null)
            {
                titleText.text = "Stat Upgrades";
            }

            RefreshCurrency();
            RefreshStatUpgradeChoices();
            RefreshNewStatUpgradeSelection();

            if (descriptionText != null)
            {
                int points = playerLevelProgression != null ? playerLevelProgression.CurrentStatPoints : 0;
                descriptionText.text = "Spend stat points to improve the player / Points: " + points;
            }

            if (hintText != null)
            {
                hintText.text = "1-4 강화 / ESC·뒤로 목록";
            }

            RefreshNewStatUpgradeSelection();
            RefreshOfferPageControls();
        }

        private void RefreshStatUpgradeChoices()
        {
            int healthLevel = playerStatModifier != null ? playerStatModifier.HealthUpgradeLevel : 0;
            int damageLevel = playerStatModifier != null ? playerStatModifier.DamageUpgradeLevel : 0;
            int moveSpeedLevel = playerStatModifier != null ? playerStatModifier.MoveSpeedUpgradeLevel : 0;
            int staminaLevel = playerStatModifier != null ? playerStatModifier.StaminaUpgradeLevel : 0;

            SetChoice(0, true, $"[1] Health Upgrade Lv {healthLevel}");
            SetChoice(1, true, $"[2] Damage Upgrade Lv {damageLevel}");
            SetChoice(2, true, $"[3] Move Speed Upgrade Lv {moveSpeedLevel}");
            SetChoice(3, true, $"[4] Stamina Upgrade Lv {staminaLevel}");

            for (int i = 4; i < MaxVisibleOfferCount; i++)
            {
                SetChoice(i, false, string.Empty);
            }
        }

        private void ShowOfferList(SupportTruckShopCategory category)
        {
            mode = MenuMode.OfferSelection;
            activeList = activeShop != null ? activeShop.GetOfferList(category) : null;
            currentOfferPage = 0;

            string title = activeList != null ? activeList.DisplayName : "Empty List";
            if (titleText != null)
            {
                titleText.text = title;
            }

            RefreshCurrency();
            RefreshOffers();
            RefreshOfferPageControls();

            if (hintText != null)
            {
                hintText.text = "1-5 선택 / ESC·뒤로 목록";
            }
        }

        private void RefreshOfferHintText()
        {
            if (hintText != null)
            {
                hintText.text = BuildOfferHintText();
            }
        }

        private void HandleBackRequested()
        {
            if (mode == MenuMode.OfferSelection || mode == MenuMode.StatUpgradeSelection)
            {
                ShowCategories();
            }
            else
            {
                Hide();
            }
        }

        private void RefreshOffers()
        {
            if (mode == MenuMode.StatUpgradeSelection)
            {
                RefreshStatUpgradeChoices();
                return;
            }

            IReadOnlyList<SupportTruckShopOfferEntry> offers = activeList != null
                ? activeList.Offers
                : null;
            ClampCurrentOfferPage(offers);
            int offerStartIndex = GetOfferPageStartIndex();

            for (int i = 0; i < MaxVisibleOfferCount; i++)
            {
                int offerIndex = offerStartIndex + i;
                SupportTruckShopOfferEntry offer = offers != null && offerIndex < offers.Count ? offers[offerIndex] : null;
                if (offer == null)
                {
                    SetChoice(i, false, string.Empty);
                    continue;
                }

                bool interactable = CanPurchaseOffer(offer);
                SetChoice(i, true, $"[{i + 1}] {offer.DisplayName}  {ResolveOfferPriceText(offer)}", interactable);
            }

            if (descriptionText != null)
            {
                descriptionText.text = ResolveDescriptionText(offers);
            }

            if (hintText != null)
            {
                hintText.text = BuildOfferHintText();
            }

            RefreshOfferPageControls();
            RefreshNewOfferSelection();
        }

        private string ResolveDescriptionText(IReadOnlyList<SupportTruckShopOfferEntry> offers)
        {
            if (offers == null || offers.Count == 0)
            {
                return "No offers";
            }

            int firstVisibleIndex = Mathf.Clamp(GetOfferPageStartIndex(), 0, offers.Count - 1);
            SupportTruckShopOfferEntry firstOffer = offers[firstVisibleIndex];
            return firstOffer != null ? firstOffer.Description : string.Empty;
        }

        private int GetOfferPageStartIndex()
        {
            return Mathf.Max(0, currentOfferPage) * MaxVisibleOfferCount;
        }

        private int GetPagedOfferIndex(int visibleSlotIndex)
        {
            return GetOfferPageStartIndex() + visibleSlotIndex;
        }

        private int GetOfferPageCount(IReadOnlyList<SupportTruckShopOfferEntry> offers)
        {
            if (offers == null || offers.Count == 0)
            {
                return 1;
            }

            return Mathf.Max(1, Mathf.CeilToInt(offers.Count / (float)MaxVisibleOfferCount));
        }

        private void ClampCurrentOfferPage(IReadOnlyList<SupportTruckShopOfferEntry> offers)
        {
            int pageCount = GetOfferPageCount(offers);
            currentOfferPage = Mathf.Clamp(currentOfferPage, 0, pageCount - 1);
        }

        private bool HasMultipleOfferPages()
        {
            IReadOnlyList<SupportTruckShopOfferEntry> offers = activeList != null ? activeList.Offers : null;
            return GetOfferPageCount(offers) > 1;
        }

        private string BuildOfferPageLabel()
        {
            IReadOnlyList<SupportTruckShopOfferEntry> offers = activeList != null ? activeList.Offers : null;
            int pageCount = GetOfferPageCount(offers);
            return $"Page {currentOfferPage + 1}/{pageCount}";
        }

        private string BuildOfferHintText()
        {
            return HasMultipleOfferPages()
                ? $"1-5 Select / A Prev / D Next / ESC Back / {BuildOfferPageLabel()}"
                : "1-5 Select / ESC Back";
        }

        private void GoToPreviousOfferPage()
        {
            if (mode != MenuMode.OfferSelection || activeList == null || currentOfferPage <= 0)
            {
                return;
            }

            currentOfferPage--;
            RefreshOffers();
        }

        private void GoToNextOfferPage()
        {
            IReadOnlyList<SupportTruckShopOfferEntry> offers = activeList != null ? activeList.Offers : null;
            int pageCount = GetOfferPageCount(offers);
            if (mode != MenuMode.OfferSelection || activeList == null || currentOfferPage >= pageCount - 1)
            {
                return;
            }

            currentOfferPage++;
            RefreshOffers();
        }

        private void RefreshOfferPageControls()
        {
            EnsureGeneratedPageControls();

            IReadOnlyList<SupportTruckShopOfferEntry> offers = activeList != null ? activeList.Offers : null;
            ClampCurrentOfferPage(offers);

            bool showPaging = mode == MenuMode.OfferSelection && GetOfferPageCount(offers) > 1;
            bool canGoPrevious = showPaging && currentOfferPage > 0;
            bool canGoNext = showPaging && currentOfferPage < GetOfferPageCount(offers) - 1;
            string pageLabel = showPaging ? BuildOfferPageLabel() : string.Empty;

            SetActive(pageControlsRoot, showPaging);
            ApplyPageButton(previousPageButton, showPaging, canGoPrevious);
            ApplyPageButton(nextPageButton, showPaging, canGoNext);

            if (pageText != null)
            {
                pageText.gameObject.SetActive(false);
                pageText.text = string.Empty;
            }

            if (newPageText != null)
            {
                newPageText.gameObject.SetActive(false);
                newPageText.SetText(string.Empty);
            }

            if (hintText != null && mode == MenuMode.OfferSelection)
            {
                hintText.text = BuildOfferHintText();
            }
        }

        private static void ApplyPageButton(Button button, bool visible, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(visible);
            button.interactable = interactable;
        }

        private void SubmitSlot(int slotIndex)
        {
            if (activeShop == null)
            {
                Hide();
                return;
            }

            if (mode == MenuMode.CategorySelection)
            {
                if (slotIndex >= 0 && slotIndex < CategorySlots.Length)
                {
                    ShowOfferList(CategorySlots[slotIndex]);
                }
                else if (slotIndex == 3)
                {
                    ShowStatUpgradeMenu();
                }

                return;
            }

            if (mode == MenuMode.StatUpgradeSelection)
            {
                SubmitStatUpgradeSlot(slotIndex);
                return;
            }

            int offerIndex = GetPagedOfferIndex(slotIndex);
            SupportTruckShopOfferEntry offer = activeList != null ? activeList.GetOffer(offerIndex) : null;
            if (offer == null)
            {
                return;
            }

            if (!CanPurchaseOffer(offer))
            {
                ShowStatus(ResolveCannotPurchaseStatus(offer));
                RefreshCurrency();
                RefreshOffers();
                return;
            }

            int availableCurrency = GetAvailableCurrency();

            if (playerCurrencyWallet != null && !playerCurrencyWallet.CanSpend(offer.Cost))
            {
                ShowStatus("Not enough money");
                RefreshCurrency();
                RefreshOffers();
                return;
            }

            int remainingCurrency;
            GameObject spawnedObject;
            string statusMessage;
            bool purchased = activeShop.TryPurchaseOffer(
                offer,
                availableCurrency,
                out remainingCurrency,
                out spawnedObject,
                out statusMessage);

            if (purchased)
            {
                if (playerCurrencyWallet != null)
                {
                    playerCurrencyWallet.TrySpendMoney(offer.Cost);
                }
                else
                {
                    debugAvailableCurrency = remainingCurrency;
                }

                OfferPurchased?.Invoke(offer);
            }

            ShowStatus(statusMessage);
            RefreshCurrency();
            RefreshOffers();
        }

        private void SubmitStatUpgradeSlot(int slotIndex)
        {
            ResolveStatUpgradeReferences();

            if (playerStatUpgradeController == null)
            {
                ShowStatus("No stat upgrade controller found");
                return;
            }

            bool upgraded;
            string upgradeName;

            switch (slotIndex)
            {
                case 0:
                    upgraded = playerStatUpgradeController.TryUpgradeHealth();
                    upgradeName = "Health";
                    break;

                case 1:
                    upgraded = playerStatUpgradeController.TryUpgradeDamage();
                    upgradeName = "Damage";
                    break;

                case 2:
                    upgraded = playerStatUpgradeController.TryUpgradeMoveSpeed();
                    upgradeName = "Move Speed";
                    break;

                case 3:
                    upgraded = playerStatUpgradeController.TryUpgradeStamina();
                    upgradeName = "Stamina";
                    break;

                default:
                    return;
            }

            ShowStatus(upgraded
                ? upgradeName + " upgraded"
                : "Not enough stat points");

            RefreshCurrency();
            RefreshStatUpgradeChoices();

            if (descriptionText != null)
            {
                int points = playerLevelProgression != null ? playerLevelProgression.CurrentStatPoints : 0;
                descriptionText.text = "Spend stat points to improve the player / Points: " + points;
            }
        }

        private void SetChoice(int slotIndex, bool visible, string label, bool interactable = true)
        {
            if (choiceButtons != null && slotIndex >= 0 && slotIndex < choiceButtons.Length && choiceButtons[slotIndex] != null)
            {
                choiceButtons[slotIndex].gameObject.SetActive(visible);
                choiceButtons[slotIndex].interactable = visible && interactable;
            }

            if (choiceTexts != null && slotIndex >= 0 && slotIndex < choiceTexts.Length && choiceTexts[slotIndex] != null)
            {
                choiceTexts[slotIndex].text = label;
            }
        }

        private void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void ClearStatus()
        {
            if (statusText != null)
            {
                statusText.text = string.Empty;
            }
        }

        private void RefreshCurrency()
        {
            if (currencyText != null)
            {
                currencyText.text = playerCurrencyWallet != null
                    ? "Money: " + playerCurrencyWallet.CurrentMoney
                    : "Debug Money: " + debugAvailableCurrency;
            }

            if (newCurrencyText != null)
            {
                newCurrencyText.SetText("Money : {0}", GetAvailableCurrency());
            }
        }

        private void ResolveWeaponInventory()
        {
            if (playerWeaponInventory == null)
            {
                playerWeaponInventory = FindFirstObjectByType<PlayerWeaponInventory>(FindObjectsInactive.Include);
            }
        }

        private void HandleMoneyChanged(int money)
        {
            RefreshCurrency();
            if (mode == MenuMode.OfferSelection || mode == MenuMode.StatUpgradeSelection)
            {
                RefreshOffers();
            }
        }

        private void HandleStatPointsChanged(int statPoints)
        {
            if (mode == MenuMode.StatUpgradeSelection)
            {
                RefreshStatUpgradeChoices();
                RefreshNewStatUpgradeSelection();

                if (descriptionText != null)
                {
                    descriptionText.text = "Spend stat points to improve the player / Points: " + statPoints;
                }
            }
        }

        private void HandleStatsChanged()
        {
            if (mode == MenuMode.StatUpgradeSelection)
            {
                RefreshStatUpgradeChoices();
                RefreshNewStatUpgradeSelection();
            }
        }

        private int GetAvailableCurrency()
        {
            return playerCurrencyWallet != null
                ? playerCurrencyWallet.CurrentMoney
                : debugAvailableCurrency;
        }

        private void ResolveCurrencyWallet()
        {
            if (playerCurrencyWallet != null)
            {
                return;
            }

            SetCurrencyWallet(FindFirstObjectByType<PlayerCurrencyWallet>(FindObjectsInactive.Include));
        }

        private void ResolveStatUpgradeReferences()
        {
            if (playerLevelProgression == null)
            {
                playerLevelProgression = FindFirstObjectByType<PlayerLevelProgression>(FindObjectsInactive.Include);
            }

            if (playerStatModifier == null)
            {
                playerStatModifier = FindFirstObjectByType<PlayerStatModifier>(FindObjectsInactive.Include);
            }

            if (playerStatUpgradeController == null)
            {
                playerStatUpgradeController = FindFirstObjectByType<PlayerStatUpgradeController>(FindObjectsInactive.Include);
            }
        }

        private void ResolveStatUpgradeReferences(Transform player)
        {
            if (player == null)
            {
                return;
            }

            if (playerLevelProgression == null)
            {
                playerLevelProgression = player.GetComponentInParent<PlayerLevelProgression>();
            }

            if (playerLevelProgression == null)
            {
                playerLevelProgression = player.GetComponentInChildren<PlayerLevelProgression>(true);
            }

            if (playerStatModifier == null)
            {
                playerStatModifier = player.GetComponentInParent<PlayerStatModifier>();
            }

            if (playerStatModifier == null)
            {
                playerStatModifier = player.GetComponentInChildren<PlayerStatModifier>(true);
            }

            if (playerStatUpgradeController == null)
            {
                playerStatUpgradeController = player.GetComponentInParent<PlayerStatUpgradeController>();
            }

            if (playerStatUpgradeController == null)
            {
                playerStatUpgradeController = player.GetComponentInChildren<PlayerStatUpgradeController>(true);
            }

            BindStatEvents();
        }

        private void SetCurrencyWallet(PlayerCurrencyWallet wallet)
        {
            if (playerCurrencyWallet == wallet)
            {
                return;
            }

            UnbindWalletEvents();
            playerCurrencyWallet = wallet;
            BindWalletEvents();
        }

        private void BindWalletEvents()
        {
            if (walletEventsBound || playerCurrencyWallet == null || !isActiveAndEnabled)
            {
                return;
            }

            playerCurrencyWallet.MoneyChanged += HandleMoneyChanged;
            walletEventsBound = true;
        }

        private void UnbindWalletEvents()
        {
            if (!walletEventsBound || playerCurrencyWallet == null)
            {
                walletEventsBound = false;
                return;
            }

            playerCurrencyWallet.MoneyChanged -= HandleMoneyChanged;
            walletEventsBound = false;
        }

        private void BindStatEvents()
        {
            if (statEventsBound || !isActiveAndEnabled)
            {
                return;
            }

            if (playerLevelProgression != null)
            {
                playerLevelProgression.StatPointsChanged += HandleStatPointsChanged;
            }

            if (playerStatModifier != null)
            {
                playerStatModifier.StatsChanged += HandleStatsChanged;
            }

            statEventsBound = playerLevelProgression != null || playerStatModifier != null;
        }

        private void UnbindStatEvents()
        {
            if (!statEventsBound)
            {
                return;
            }

            if (playerLevelProgression != null)
            {
                playerLevelProgression.StatPointsChanged -= HandleStatPointsChanged;
            }

            if (playerStatModifier != null)
            {
                playerStatModifier.StatsChanged -= HandleStatsChanged;
            }

            statEventsBound = false;
        }

        private void BindButtons()
        {
            if (listenersBound || choiceButtons == null)
            {
                return;
            }

            closeButton ??= FindCloseButton(transform);
            previousPageButton ??= FindButton(transform, "Button_PrevPage", "Button_PreviousPage", "PrevPageButton", "PreviousPageButton", "Button_PagePrev");
            nextPageButton ??= FindButton(transform, "Button_NextPage", "NextPageButton", "Button_PageNext");
            pageText ??= FindLegacyText(transform, "Text_Page", "PageText", "Text_PageNumber");

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int slotIndex = i;
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].onClick.AddListener(() => SubmitSlot(slotIndex));
                }
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleBackRequested);
            }

            BindPageButtons();

            listenersBound = true;
        }

        private void BindPageButtons()
        {
            if (previousPageButton != null)
            {
                previousPageButton.onClick.RemoveListener(GoToPreviousOfferPage);
                previousPageButton.onClick.AddListener(GoToPreviousOfferPage);
            }

            if (nextPageButton != null)
            {
                nextPageButton.onClick.RemoveListener(GoToNextOfferPage);
                nextPageButton.onClick.AddListener(GoToNextOfferPage);
            }
        }

        private void BindNewPanelIfNeeded()
        {
            if (newPanelBound && HasRequiredNewPanelBindings())
            {
                return;
            }

            newRoot = transform.Find(newPanelName);
            if (newRoot == null)
            {
                return;
            }

            newBackgroundRoot = FindChildExact(newRoot, "Background_Common")?.gameObject;
            categoryPanelRoot = FindChildExact(newRoot, categoryPanelName)?.gameObject;
            newTitleText = FindTmpText(newRoot, "Text_Title", "TitleText");
            newCurrencyText = FindTmpText(newRoot, "Text_Money", "MoneyText", "CurrencyText");
            newHintText = FindTmpText(newRoot, "Text_Hint", "HintText");
            newPageText = FindTmpText(newRoot, "Text_Page", "PageText", "Text_PageNumber");

            previousPageButton ??= FindButton(newRoot, "Button_PrevPage", "Button_PreviousPage", "PrevPageButton", "PreviousPageButton", "Button_PagePrev");
            nextPageButton ??= FindButton(newRoot, "Button_NextPage", "NextPageButton", "Button_PageNext");
            EnsureGeneratedPageControls();
            BindPageButtons();

            BindNewCategoryViews();
            BindNewListRoots();
            HideOldNewPanelTemplates();

            newPanelBound = HasRequiredNewPanelBindings();
        }

        private void EnsureGeneratedPageControls()
        {
            if (previousPageButton != null && nextPageButton != null && newPageText != null)
            {
                Transform controlsParent = previousPageButton.transform.parent;
                pageControlsRoot = controlsParent != null ? controlsParent.gameObject : null;
                PruneDuplicatePageControls(controlsParent);
                ApplyGeneratedPageButtonLayout(previousPageButton, new Vector2(-160f, 0f));
                ApplyGeneratedPageButtonLayout(nextPageButton, new Vector2(160f, 0f));
                ApplyGeneratedPageButtonLabel(previousPageButton, PreviousPageKeyLabel, PreviousPageCaption);
                ApplyGeneratedPageButtonLabel(nextPageButton, NextPageKeyLabel, NextPageCaption);
                controlsParent?.SetAsLastSibling();
                return;
            }

            Transform pageParent = newRoot != null
                ? newRoot
                : panelRoot != null
                    ? panelRoot.transform
                    : transform;

            RectTransform controlsRoot = FindChildExact(pageParent, "ShopPageControls") as RectTransform;
            if (controlsRoot == null)
            {
                GameObject controlsObject = new GameObject("ShopPageControls", typeof(RectTransform));
                controlsRoot = controlsObject.GetComponent<RectTransform>();
                controlsRoot.SetParent(pageParent, false);
                controlsRoot.anchorMin = new Vector2(0.5f, 0f);
                controlsRoot.anchorMax = new Vector2(0.5f, 0f);
                controlsRoot.pivot = new Vector2(0.5f, 0.5f);
            }

            controlsRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 460f);
            controlsRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 96f);
            controlsRoot.anchoredPosition = new Vector2(-9f, 290f);
            pageControlsRoot = controlsRoot.gameObject;
            controlsRoot.SetAsLastSibling();
            EnsureTopCanvas(controlsRoot.gameObject);
            PruneDuplicatePageControls(controlsRoot);

            HorizontalLayoutGroup layoutGroup = controlsRoot.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
            {
                Destroy(layoutGroup);
            }

            previousPageButton ??= FindButton(
                controlsRoot,
                "Button_PrevPage",
                "Button_PreviousPage",
                "PrevPageButton",
                "PreviousPageButton",
                "Button_PagePrev");
            nextPageButton ??= FindButton(controlsRoot, "Button_NextPage", "NextPageButton", "Button_PageNext");
            newPageText ??= FindTmpText(controlsRoot, "Text_Page", "PageText", "Text_PageNumber");
            previousPageButton ??= CreateGeneratedPageButton(
                controlsRoot,
                "Button_PrevPage",
                PreviousPageKeyLabel,
                PreviousPageCaption,
                new Vector2(-160f, 0f));
            if (newPageText == null)
            {
                newPageText = CreateGeneratedPageText(controlsRoot);
            }

            nextPageButton ??= CreateGeneratedPageButton(
                controlsRoot,
                "Button_NextPage",
                NextPageKeyLabel,
                NextPageCaption,
                new Vector2(160f, 0f));
            ApplyGeneratedPageButtonLayout(previousPageButton, new Vector2(-160f, 0f));
            ApplyGeneratedPageButtonLayout(nextPageButton, new Vector2(160f, 0f));
            ApplyGeneratedPageButtonLabel(previousPageButton, PreviousPageKeyLabel, PreviousPageCaption);
            ApplyGeneratedPageButtonLabel(nextPageButton, NextPageKeyLabel, NextPageCaption);
        }

        private void PruneDuplicatePageControls(Transform controlsRoot)
        {
            if (controlsRoot == null)
            {
                return;
            }

            Button firstPreviousButton = null;
            Button firstNextButton = null;
            TMP_Text firstPageText = null;
            List<GameObject> duplicates = new List<GameObject>();

            for (int i = 0; i < controlsRoot.childCount; i++)
            {
                Transform child = controlsRoot.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (IsPagePreviousControlName(child.name))
                {
                    Button button = child.GetComponent<Button>();
                    if (firstPreviousButton == null && button != null)
                    {
                        firstPreviousButton = button;
                    }
                    else
                    {
                        duplicates.Add(child.gameObject);
                    }
                }
                else if (IsPageNextControlName(child.name))
                {
                    Button button = child.GetComponent<Button>();
                    if (firstNextButton == null && button != null)
                    {
                        firstNextButton = button;
                    }
                    else
                    {
                        duplicates.Add(child.gameObject);
                    }
                }
                else if (IsPageTextControlName(child.name))
                {
                    TMP_Text text = child.GetComponent<TMP_Text>();
                    if (firstPageText == null && text != null)
                    {
                        firstPageText = text;
                    }
                    else
                    {
                        duplicates.Add(child.gameObject);
                    }
                }
            }

            previousPageButton = firstPreviousButton ?? previousPageButton;
            nextPageButton = firstNextButton ?? nextPageButton;
            newPageText = firstPageText ?? newPageText;

            for (int i = 0; i < duplicates.Count; i++)
            {
                GameObject duplicate = duplicates[i];
                if (duplicate == null)
                {
                    continue;
                }

                duplicate.SetActive(false);
                Destroy(duplicate);
            }
        }

        private static bool IsPagePreviousControlName(string value)
        {
            return value == "Button_PrevPage"
                || value == "Button_PreviousPage"
                || value == "PrevPageButton"
                || value == "PreviousPageButton"
                || value == "Button_PagePrev";
        }

        private static bool IsPageNextControlName(string value)
        {
            return value == "Button_NextPage"
                || value == "NextPageButton"
                || value == "Button_PageNext";
        }

        private static bool IsPageTextControlName(string value)
        {
            return value == "Text_Page"
                || value == "PageText"
                || value == "Text_PageNumber";
        }

        private static void EnsureTopCanvas(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Canvas canvas = target.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = target.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = 30000;

            GraphicRaycaster raycaster = target.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                target.AddComponent<GraphicRaycaster>();
            }
        }

        private static Button CreateGeneratedPageButton(
            Transform parent,
            string objectName,
            string keyLabel,
            string caption,
            Vector2 anchoredPosition)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 112f);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 88f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            ApplyGeneratedPageButtonLabel(button, keyLabel, caption);

            return button;
        }

        private static void ApplyGeneratedPageButtonLayout(Button button, Vector2 anchoredPosition)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 112f);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 88f);
        }

        private static void ApplyGeneratedPageButtonLabel(Button button, string keyLabel, string caption)
        {
            if (button == null)
            {
                return;
            }

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                return;
            }

            text.richText = true;
            text.SetText($"<size=58>{keyLabel}</size>\n<size=18>{caption}</size>");
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.fontSize = 58f;
            text.lineSpacing = -8f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = new Color(1f, 0.92f, 0f, 1f);
            text.raycastTarget = false;
        }

        private static TMP_Text CreateGeneratedPageText(Transform parent)
        {
            GameObject textObject = new GameObject("Text_Page", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 0f);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 126f);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 28f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.SetText("Page 1/1");
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 15f;
            text.color = new Color(1f, 1f, 1f, 0.9f);

            return text;
        }

        private bool HasRequiredNewPanelBindings()
        {
            return categoryPanelRoot != null
                && listRoots[0] != null
                && listRoots[1] != null
                && listRoots[2] != null
                && listRoots[3] != null;
        }

        private void BindNewCategoryViews()
        {
            if (categoryPanelRoot == null)
            {
                return;
            }

            Transform group = FindChildExact(categoryPanelRoot.transform, "CardFrame03-Group");
            if (group == null)
            {
                group = categoryPanelRoot.transform;
            }

            categoryGroupRoot = group.gameObject;

            int slotIndex = 0;
            for (int i = 0; i < group.childCount && slotIndex < categoryViews.Length; i++)
            {
                Transform child = group.GetChild(i);
                Button button = EnsureButton(child.gameObject);
                int capturedIndex = slotIndex;
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => SubmitSlot(capturedIndex));
                }

                categoryViews[slotIndex] = new CategoryView
                {
                    Root = child.gameObject,
                    Button = button,
                    NameText = FindTmpText(child, "Text_name", "Text_Name", "Text"),
                    NumberText = FindTmpText(child, "Text_Num")
                };

                slotIndex++;
            }
        }

        private void BindNewListRoots()
        {
            BindNewListRoot(0, "Set01");
            BindNewListRoot(1, "Set02");
            BindNewListRoot(2, "Set03");
            BindNewListRoot(3, "Set04");

            if (listRoots[3] == null)
            {
                Debug.LogWarning("[SupportTruckShopPresenter] Stat upgrade list root is not assigned.", this);
            }
        }

        private void BindNewListRoot(int index, params string[] names)
        {
            Transform categoryRoot = null;
            for (int i = 0; i < names.Length && categoryRoot == null; i++)
            {
                categoryRoot = FindChildExact(newRoot, names[i]);
            }

            Transform listRoot = categoryRoot != null ? FindChildExact(categoryRoot, "list") : null;
            Transform contentRoot = EnsureScrollableList(listRoot);
            listRoots[index] = listRoot;
            listContentRoots[index] = contentRoot;
            listDecorationRoots[index] = FindListDecorationRoot(categoryRoot, listRoot);
            CollectListFrames(contentRoot, listFrameSets[index]);
            listTemplates[index] = listFrameSets[index].Count > 0 ? listFrameSets[index][0] : null;
        }

        private static GameObject FindListDecorationRoot(Transform categoryRoot, Transform listRoot)
        {
            if (categoryRoot == null)
            {
                return null;
            }

            for (int i = 0; i < categoryRoot.childCount; i++)
            {
                Transform child = categoryRoot.GetChild(i);
                if (child == null || child == listRoot)
                {
                    continue;
                }

                if (child.name.StartsWith("CardFrame", StringComparison.OrdinalIgnoreCase))
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private void RefreshNewCategorySelection()
        {
            if (!newPanelBound)
            {
                return;
            }

            EnsureNewBackgroundVisible();
            ClearGeneratedFrames();
            SetActive(categoryPanelRoot, true);
            SetActive(categoryGroupRoot, true);
            SetNewListActive(-1);
            SetNewText(newTitleText, "Support Truck Shop");
            SetNewText(newHintText, "1-4 선택 / ESC·뒤로 닫기");
            RefreshCurrency();

            for (int i = 0; i < categoryViews.Length; i++)
            {
                CategoryView view = categoryViews[i];
                if (view == null)
                {
                    continue;
                }

                SetActive(view.Root, true);
                if (view.Button != null)
                {
                    view.Button.interactable = true;
                }

                SetNewText(view.NameText, CategoryLabels[i]);
                if (view.NumberText != null)
                {
                    view.NumberText.SetText("{0}", i + 1);
                }
            }
        }

        private void RefreshNewOfferSelection()
        {
            BindNewPanelIfNeeded();

            if (!newPanelBound || mode != MenuMode.OfferSelection)
            {
                return;
            }

            int listIndex = ResolveActiveListIndex();
            EnsureNewBackgroundVisible();
            SetActive(categoryPanelRoot, false);
            SetNewListActive(listIndex);
            SetNewText(newTitleText, activeList != null ? activeList.DisplayName : "Empty List");
            SetNewText(newHintText, "1-5 선택 / ESC·뒤로 목록");

            IReadOnlyList<SupportTruckShopOfferEntry> offers = activeList != null ? activeList.Offers : null;
            ClampCurrentOfferPage(offers);
            int offerStartIndex = GetOfferPageStartIndex();
            int remainingOfferCount = offers != null ? Mathf.Max(0, offers.Count - offerStartIndex) : 0;
            int frameCount = Mathf.Min(MaxVisibleOfferCount, remainingOfferCount);
            RebuildNewFrames(listIndex, frameCount, (frameView, slotIndex) =>
            {
                SupportTruckShopOfferEntry offer = offers[offerStartIndex + slotIndex];
                bool interactable = CanPurchaseOffer(offer);
                string price = ResolveOfferPriceText(offer);
                ApplyNewFrame(
                    frameView,
                    slotIndex,
                    offer.DisplayName,
                    price,
                    offer.Description,
                    offer.Icon,
                    interactable);
            });
            SetNewText(newHintText, BuildOfferHintText());
            RefreshOfferPageControls();
            NormalizeNewListLayout(listIndex);
        }

        private void RefreshNewStatUpgradeSelection()
        {
            BindNewPanelIfNeeded();

            if (!newPanelBound || mode != MenuMode.StatUpgradeSelection)
            {
                return;
            }

            EnsureNewBackgroundVisible();
            SetActive(categoryPanelRoot, false);
            SetNewListActive(3);
            SetNewText(newTitleText, "Stat Upgrades");
            SetNewText(newHintText, "1-4 강화 / ESC·뒤로 목록");

            string[] labels =
            {
                playerStatModifier != null ? $"Health Upgrade Lv {playerStatModifier.HealthUpgradeLevel}" : "Health Upgrade",
                playerStatModifier != null ? $"Damage Upgrade Lv {playerStatModifier.DamageUpgradeLevel}" : "Damage Upgrade",
                playerStatModifier != null ? $"Move Speed Upgrade Lv {playerStatModifier.MoveSpeedUpgradeLevel}" : "Move Speed Upgrade",
                playerStatModifier != null ? $"Stamina Upgrade Lv {playerStatModifier.StaminaUpgradeLevel}" : "Stamina Upgrade"
            };

            int points = playerLevelProgression != null ? playerLevelProgression.CurrentStatPoints : 0;
            RebuildNewFrames(3, labels.Length, (frameView, slotIndex) =>
            {
                bool interactable = playerStatUpgradeController != null && points > 0;
                ApplyNewFrame(
                    frameView,
                    slotIndex,
                    labels[slotIndex],
                    "1 pt",
                    "Points: " + points,
                    ResolveStatUpgradeIcon(slotIndex),
                    interactable);
            });
            NormalizeNewListLayout(3);
        }

        private Sprite ResolveStatUpgradeIcon(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0:
                    return healthUpgradeIcon;
                case 1:
                    return damageUpgradeIcon;
                case 2:
                    return moveSpeedUpgradeIcon;
                case 3:
                    return staminaUpgradeIcon;
                default:
                    return null;
            }
        }

        private void NormalizeNewListLayout(int listIndex)
        {
            // Layout is authored in the scene/prefab.
        }

        private void RebuildNewFrames(int listIndex, int count, System.Action<ListFrameView, int> applyFrame)
        {
            if (listIndex < 0 || listIndex >= listRoots.Length)
            {
                return;
            }

            ClearGeneratedFrames();

            Transform listRoot = listRoots[listIndex];
            if (listRoot == null)
            {
                return;
            }

            List<GameObject> frames = listFrameSets[listIndex];
            if (count > frames.Count)
            {
                Debug.LogWarning(
                    $"[SupportTruckShopPresenter] Not enough authored ListFrame objects for list {listIndex}. Required: {count}, Available: {frames.Count}.",
                    this);
            }

            int activeCount = Mathf.Min(count, frames.Count);
            for (int i = 0; i < activeCount; i++)
            {
                GameObject frame = frames[i];
                frame.SetActive(true);
                applyFrame(BindNewListFrame(frame), i);
            }
        }

        private ListFrameView BindNewListFrame(GameObject frame)
        {
            Transform root = frame != null ? frame.transform : null;
            if (root == null)
            {
                return null;
            }

            return new ListFrameView
            {
                Root = frame,
                Button = EnsureButton(frame),
                NameText = FindTmpText(root, "Text_name", "Text_Name", "Text"),
                NumberText = FindTmpText(root, "Text_Num"),
                PriceText = FindTmpText(root, "prices_text", "PriceText"),
                ExplanationText = FindTmpText(root, "explanation", "DescriptionText"),
                IconImage = FindNamedImage(root, "Icon"),
                CanvasGroup = EnsureCanvasGroup(frame)
            };
        }

        private void ApplyNewFrame(
            ListFrameView frameView,
            int slotIndex,
            string label,
            string price,
            string explanation,
            Sprite icon,
            bool interactable)
        {
            if (frameView == null)
            {
                return;
            }

            SetNewText(frameView.NameText, label);
            SetNewText(frameView.PriceText, price);
            SetNewText(frameView.ExplanationText, explanation);

            if (frameView.IconImage != null)
            {
                frameView.IconImage.sprite = icon;
                frameView.IconImage.color = Color.white;
                frameView.IconImage.enabled = icon != null;
                frameView.IconImage.preserveAspect = true;
                RestoreListIconLayout(frameView.IconImage.rectTransform);
            }

            if (frameView.NumberText != null)
            {
                frameView.NumberText.SetText("{0}", slotIndex + 1);
            }

            if (frameView.Button != null)
            {
                int capturedIndex = slotIndex;
                frameView.Button.onClick.RemoveAllListeners();
                frameView.Button.onClick.AddListener(() => SubmitSlot(capturedIndex));
                frameView.Button.interactable = interactable;
            }

            if (frameView.CanvasGroup != null)
            {
                frameView.CanvasGroup.alpha = interactable ? 1f : 0.42f;
                frameView.CanvasGroup.interactable = interactable;
                frameView.CanvasGroup.blocksRaycasts = interactable;
            }
        }

        private static void RestoreListIconLayout(RectTransform iconRectTransform)
        {
            if (iconRectTransform == null)
            {
                return;
            }

            iconRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconRectTransform.pivot = new Vector2(0.5f, 0.5f);
            iconRectTransform.anchoredPosition = new Vector2(0f, 0.3f);
            iconRectTransform.localScale = Vector3.one;
            iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 160f);
            iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 160f);
        }

        private bool CanPurchaseOffer(SupportTruckShopOfferEntry offer)
        {
            if (offer == null)
            {
                return false;
            }

            if (IsUnlockedOffer(offer) || IsOwnedWeaponOffer(offer))
            {
                return false;
            }

            return playerCurrencyWallet == null || playerCurrencyWallet.CanSpend(offer.Cost);
        }

        private string ResolveOfferPriceText(SupportTruckShopOfferEntry offer)
        {
            return IsUnlockedOffer(offer) || IsOwnedWeaponOffer(offer)
                ? "Owned"
                : "$" + offer.Cost.ToString();
        }

        private string ResolveCannotPurchaseStatus(SupportTruckShopOfferEntry offer)
        {
            if (IsUnlockedOffer(offer) || IsOwnedWeaponOffer(offer))
            {
                return "Already owned";
            }

            return "Not enough money";
        }

        private bool IsOwnedWeaponOffer(SupportTruckShopOfferEntry offer)
        {
            if (!IsWeaponOffer(offer))
            {
                return false;
            }

            ResolveWeaponInventory();
            return playerWeaponInventory != null && playerWeaponInventory.HasWeapon(offer.WeaponDefinition);
        }

        private static bool IsWeaponOffer(SupportTruckShopOfferEntry offer)
        {
            return offer != null
                && offer.Action == SupportTruckShopOfferAction.GrantItem
                && offer.ItemGrant == SupportTruckShopItemGrant.TemporaryGun
                && offer.WeaponDefinition != null;
        }

        private static bool IsUnlockedOffer(SupportTruckShopOfferEntry offer)
        {
            return offer != null
                && offer.Action == SupportTruckShopOfferAction.BuyUpgrade
                && offer.UnlockKey != SupportTruckShopUnlockKey.None
                && SupportTruckShopGlobalUnlocks.IsUnlocked(offer.UnlockKey);
        }

        private int ResolveActiveListIndex()
        {
            if (activeList == null)
            {
                return 0;
            }

            return activeList.Category switch
            {
                SupportTruckShopCategory.Items => 0,
                SupportTruckShopCategory.Squad => 1,
                SupportTruckShopCategory.Upgrades => 2,
                _ => 0
            };
        }

        private void SetNewListActive(int activeIndex)
        {
            for (int i = 0; i < listDecorationRoots.Length; i++)
            {
                SetActive(listDecorationRoots[i], i == activeIndex);
            }

            for (int i = 0; i < listRoots.Length; i++)
            {
                if (listRoots[i] == null || listRoots[i].parent == null)
                {
                    continue;
                }

                GameObject listPanel = listRoots[i].parent.gameObject;
                bool active = false;
                for (int j = 0; j < listRoots.Length; j++)
                {
                    if (j == activeIndex
                        && listRoots[j] != null
                        && listRoots[j].parent == listRoots[i].parent)
                    {
                        active = true;
                        break;
                    }
                }

                listPanel.SetActive(active);
            }
        }

        private void ClearGeneratedFrames()
        {
            for (int i = 0; i < listFrameSets.Length; i++)
            {
                for (int frameIndex = 0; frameIndex < listFrameSets[i].Count; frameIndex++)
                {
                    SetActive(listFrameSets[i][frameIndex], false);
                }
            }
        }

        private void HideOldNewPanelTemplates()
        {
            for (int i = 0; i < listTemplates.Length; i++)
            {
                SetActive(listTemplates[i], false);
            }
        }

        private static Transform EnsureScrollableList(Transform listRoot)
        {
            if (listRoot == null)
            {
                return null;
            }

            ScrollRect scrollRect = listRoot.GetComponent<ScrollRect>();
            if (scrollRect != null && scrollRect.content != null)
            {
                return scrollRect.content;
            }

            Transform content = FindDirectChild(listRoot, "RuntimeListContent");
            if (content == null)
            {
                content = FindDirectChild(listRoot, "Content");
            }

            if (content == null)
            {
                Debug.LogWarning("[SupportTruckShopPresenter] List content is not assigned on " + listRoot.name + ".", listRoot);
            }

            return content;
        }

        private static void CollectListFrames(Transform listRoot, List<GameObject> results)
        {
            results.Clear();
            if (listRoot == null)
            {
                return;
            }

            for (int i = 0; i < listRoot.childCount; i++)
            {
                Transform child = listRoot.GetChild(i);
                if (child.name.Contains("ListFrame"))
                {
                    results.Add(child.gameObject);
                }
            }
        }

        private static Transform FindDirectChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
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

        private static Image FindNamedImage(Transform root, string name)
        {
            Transform found = FindChildExact(root, name);
            return found != null ? found.GetComponent<Image>() : null;
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }

            return canvasGroup;
        }

        private static void SetNewText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.SetText(value);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static Button EnsureButton(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            Button button = target.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning("[SupportTruckShopPresenter] Button is not assigned on " + target.name + ".", target);
            }

            return button;
        }

        private static Button FindCloseButton(Transform root)
        {
            Button namedButton = FindButton(root, "Button_Back", "BackButton", "Button_Exit", "Exit", "EXit", "EXIT", "CloseButton", "ExitButton");
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

                TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
                if (tmpText != null && IsCloseLabel(tmpText.text))
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

        private static Button FindButton(Transform root, params string[] names)
        {
            Transform found = FindFirstNamedChild(root, names);
            return found != null ? found.GetComponent<Button>() : null;
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

        private static Transform FindChildExact(Transform root, string childName)
        {
            return FindChildRecursive(root, childName);
        }

        private void EnsureNewBackgroundVisible()
        {
            if (newBackgroundRoot != null)
            {
                newBackgroundRoot.SetActive(true);
            }
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
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
                    if (newRoot != null)
                    {
                        newRoot.gameObject.SetActive(true);
                    }

                    panelTransition.Show();
                    EnsureNewBackgroundVisible();
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
            else if (newPanelBound)
            {
                if (panelRoot != null)
                {
                    panelRoot.SetActive(active);
                }

                if (newRoot != null)
                {
                    newRoot.gameObject.SetActive(active);
                }

                if (active)
                {
                    EnsureNewBackgroundVisible();
                }
            }
            else if (panelRoot != null)
            {
                panelRoot.SetActive(active);
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

        private static void WarnIfMissingPresenter()
        {
            if (instance == null && !missingPresenterWarned)
            {
                Debug.LogWarning("[SupportTruckShopPresenter] No presenter exists in the active scene.");
                missingPresenterWarned = true;
            }
        }
    }
}
