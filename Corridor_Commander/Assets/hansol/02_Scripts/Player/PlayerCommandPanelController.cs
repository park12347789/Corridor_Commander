using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerItems;
using CorridorCommander.PlayerUI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerCommandPanelController : MonoBehaviour
    {
        [System.Serializable]
        private sealed class SquadCommandSlot
        {
            [SerializeField] private string displayName;
            [SerializeField] private PlayerSquadCommandType commandType;
            [SerializeField] private Sprite icon;

            public string DisplayName => displayName;
            public PlayerSquadCommandType CommandType => commandType;
            public Sprite Icon => icon;

            public SquadCommandSlot(string configuredDisplayName, PlayerSquadCommandType configuredCommandType)
            {
                displayName = configuredDisplayName;
                commandType = configuredCommandType;
            }
        }

        [Header("Input")]
        [SerializeField] private PlayerCentralInputController inputController;

        [Header("UI Presenters")]
        [SerializeField] private PlayerCommandHotbarPresenter commandPresenter;
        [SerializeField] private PlayerCommandRadialPresenter commandRadialPresenter;
        [SerializeField] private PlayerItemRadialPresenter itemRadialPresenter;

        [Header("Weapon System")]
        [SerializeField] private PlayerWeaponInventory weaponInventory;

        [Header("Item System")]
        [SerializeField] private PlayerItemInventory itemInventory;
        [SerializeField] private PlayerItemUseController itemUseController;
        [SerializeField] private PlayerThrowableItemController throwableItemController;

        [Header("Squad Commands")]
        [SerializeField] private PlayerSquadCommandController squadCommandController;

        [Header("Squad Command Slots")]
        [SerializeField]
        private SquadCommandSlot[] squadCommandSlots =
        {
            new SquadCommandSlot("Hold", PlayerSquadCommandType.HoldPosition),
            new SquadCommandSlot("Return", PlayerSquadCommandType.ReturnToPlayer),
            new SquadCommandSlot("Charge", PlayerSquadCommandType.Charge),
            new SquadCommandSlot("Select All", PlayerSquadCommandType.SelectAll)
        };

        [Header("Options")]
        [SerializeField] private bool showPanelOnStart = true;

        private PlayerCommandCategory currentCategory = PlayerCommandCategory.Weapons;
        private readonly List<string> slotLabels = new List<string>(PlayerCommandHotbarPresenter.MaxSlotCount);
        private readonly List<Sprite> slotIcons = new List<Sprite>(PlayerCommandHotbarPresenter.MaxSlotCount);
        private int activeThrowableSlotIndex = -1;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (showPanelOnStart)
            {
                RefreshCommandPanel("Ready: tap Q to cycle / hold Q to select");
            }
        }

        private void OnEnable()
        {
            ResolveReferences();
            InstalledSkillRegistry.Instance.Changed += HandleSkillRegistryChanged;

            if (inputController != null)
            {
                inputController.CommandCategoryCycleRequested += HandleCommandCategoryCycleRequested;
                inputController.CommandSlotRequested += HandleCommandSlotRequested;
                inputController.CommandSlotPressed += HandleCommandSlotPressed;
                inputController.CommandSlotReleased += HandleCommandSlotReleased;
                inputController.SquadMemberSlotRequested += HandleSquadMemberSlotRequested;
                inputController.SquadSelectionStepRequested += HandleSquadSelectionStepRequested;
                inputController.CommandRadialOpenRequested += HandleCommandRadialOpenRequested;
                inputController.CommandRadialConfirmRequested += HandleCommandRadialConfirmRequested;
                inputController.CommandRadialCloseRequested += HandleCommandRadialCloseRequested;
                inputController.ItemRadialOpenRequested += HandleItemRadialOpenRequested;
                inputController.ItemRadialConfirmRequested += HandleItemRadialConfirmRequested;
                inputController.ItemRadialCloseRequested += HandleItemRadialCloseRequested;
            }

            if (weaponInventory != null)
            {
                weaponInventory.WeaponListChanged += HandleWeaponInventoryChanged;
                weaponInventory.CurrentWeaponChanged += HandleCurrentWeaponChanged;
            }

            if (itemInventory != null)
            {
                itemInventory.ItemListChanged += HandleItemInventoryChanged;
            }
        }

        private void OnDisable()
        {
            InstalledSkillRegistry registry = InstalledSkillRegistry.Current;
            if (registry != null)
            {
                registry.Changed -= HandleSkillRegistryChanged;
            }

            if (inputController != null)
            {
                inputController.CommandCategoryCycleRequested -= HandleCommandCategoryCycleRequested;
                inputController.CommandSlotRequested -= HandleCommandSlotRequested;
                inputController.CommandSlotPressed -= HandleCommandSlotPressed;
                inputController.CommandSlotReleased -= HandleCommandSlotReleased;
                inputController.SquadMemberSlotRequested -= HandleSquadMemberSlotRequested;
                inputController.SquadSelectionStepRequested -= HandleSquadSelectionStepRequested;
                inputController.CommandRadialOpenRequested -= HandleCommandRadialOpenRequested;
                inputController.CommandRadialConfirmRequested -= HandleCommandRadialConfirmRequested;
                inputController.CommandRadialCloseRequested -= HandleCommandRadialCloseRequested;
                inputController.ItemRadialOpenRequested -= HandleItemRadialOpenRequested;
                inputController.ItemRadialConfirmRequested -= HandleItemRadialConfirmRequested;
                inputController.ItemRadialCloseRequested -= HandleItemRadialCloseRequested;
            }

            if (weaponInventory != null)
            {
                weaponInventory.WeaponListChanged -= HandleWeaponInventoryChanged;
                weaponInventory.CurrentWeaponChanged -= HandleCurrentWeaponChanged;
            }

            if (itemInventory != null)
            {
                itemInventory.ItemListChanged -= HandleItemInventoryChanged;
            }

            commandRadialPresenter?.Hide();
            itemRadialPresenter?.Hide();
            throwableItemController?.CancelAim();
        }

        private void Update()
        {
            UpdateOpenRadialSelection();
        }

        private void HandleWeaponInventoryChanged()
        {
            RefreshCommandPanel("Weapon list updated");
        }

        private void HandleCurrentWeaponChanged(WeaponRuntimeState currentWeaponState)
        {
            if (currentWeaponState == null || currentWeaponState.WeaponDefinition == null)
            {
                return;
            }

            RefreshCommandPanel("Equipped: " + currentWeaponState.WeaponDefinition.displayName);
        }

        private void HandleItemInventoryChanged()
        {
            RefreshCommandPanel("Item list updated");
        }

        private void HandleSkillRegistryChanged()
        {
            if (currentCategory == PlayerCommandCategory.TurretSkills)
            {
                RefreshCommandPanel("Skill list updated");
            }
        }

        private void HandleCommandCategoryCycleRequested()
        {
            CycleCommandCategory();
        }

        private void HandleCommandSlotRequested(int slotNumber)
        {
            int slotIndex = slotNumber - 1;

            if (slotIndex == activeThrowableSlotIndex)
            {
                RefreshCommandPanel("Aiming throwable");
                return;
            }

            string status = UseCommandSlot(slotIndex);
            RefreshCommandPanel(status);
        }

        private void HandleCommandSlotPressed(int slotNumber)
        {
            int slotIndex = slotNumber - 1;

            if (!TryBeginThrowableSlot(slotIndex, out string status))
            {
                return;
            }

            RefreshCommandPanel(status);
        }

        private void HandleCommandSlotReleased(int slotNumber)
        {
            int slotIndex = slotNumber - 1;

            if (slotIndex != activeThrowableSlotIndex)
            {
                return;
            }

            activeThrowableSlotIndex = -1;

            if (throwableItemController == null)
            {
                RefreshCommandPanel("No throwable controller");
                return;
            }

            throwableItemController.ConfirmThrow(out string status);
            RefreshCommandPanel(status);
        }

        private void HandleSquadMemberSlotRequested(int slotNumber)
        {
            ResolveReferences();
            if (squadCommandController == null)
            {
                RefreshCommandPanel("No squad command controller");
                return;
            }

            squadCommandController.SelectMemberSlot(slotNumber, out string statusMessage);
            RefreshCommandPanel(statusMessage);
        }

        private void HandleSquadSelectionStepRequested(int direction)
        {
            ResolveReferences();
            if (squadCommandController == null)
            {
                RefreshCommandPanel("No squad command controller");
                return;
            }

            squadCommandController.SelectAdjacentMember(direction, out string statusMessage);
            RefreshCommandPanel(statusMessage);
        }

        private void HandleCommandRadialOpenRequested()
        {
            CancelActiveThrowableAim();
            ResolveReferences();

            if (commandRadialPresenter == null)
            {
                return;
            }

            commandRadialPresenter.Show(currentCategory);
            UpdateCommandRadialSelection();
        }

        private void HandleCommandRadialConfirmRequested()
        {
            if (commandRadialPresenter == null)
            {
                return;
            }

            currentCategory = commandRadialPresenter.SelectedCategory;
            commandRadialPresenter.Hide();
            RefreshCommandPanel("Selected set: " + ResolveCategoryName(currentCategory));
        }

        private void HandleCommandRadialCloseRequested()
        {
            commandRadialPresenter?.Hide();
        }

        private void HandleItemRadialOpenRequested()
        {
            CancelActiveThrowableAim();
            ResolveReferences();

            if (itemRadialPresenter == null)
            {
                return;
            }

            if (itemInventory == null)
            {
                RefreshCommandPanel("No item inventory");
                return;
            }

            itemRadialPresenter.Show(itemInventory.Items);
            UpdateItemRadialSelection();
        }

        private void HandleItemRadialConfirmRequested()
        {
            if (itemRadialPresenter == null)
            {
                return;
            }

            PlayerItemRuntimeEntry selectedItem = itemRadialPresenter.GetSelectedItem();
            itemRadialPresenter.Hide();

            string status = UseItem(selectedItem, "Selected item");
            RefreshCommandPanel(status);
        }

        private void HandleItemRadialCloseRequested()
        {
            itemRadialPresenter?.Hide();
        }

        private void CycleCommandCategory()
        {
            currentCategory = currentCategory switch
            {
                PlayerCommandCategory.Weapons => PlayerCommandCategory.TurretSkills,
                PlayerCommandCategory.TurretSkills => PlayerCommandCategory.SquadCommands,
                _ => PlayerCommandCategory.Weapons
            };

            commandRadialPresenter?.SetActiveCategory(currentCategory);
            RefreshCommandPanel("Panel: " + ResolveCategoryName(currentCategory));
        }

        private string UseCommandSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PlayerCommandHotbarPresenter.MaxSlotCount)
            {
                return "Invalid slot";
            }

            return currentCategory switch
            {
                PlayerCommandCategory.Weapons => UseWeaponCategorySlot(slotIndex),
                PlayerCommandCategory.TurretSkills => UseTurretSkillSlot(slotIndex),
                PlayerCommandCategory.SquadCommands => UseSquadCommandSlot(slotIndex),
                _ => "Unknown command"
            };
        }

        private string UseWeaponCategorySlot(int slotIndex)
        {
            int weaponSlotCount = GetWeaponSlotCount();

            if (slotIndex < weaponSlotCount)
            {
                return EquipWeaponSlot(slotIndex);
            }

            int itemIndex = slotIndex - weaponSlotCount;
            PlayerItemRuntimeEntry item = GetItemEntryAt(itemIndex);
            return UseItem(item, "Slot item");
        }

        private string EquipWeaponSlot(int slotIndex)
        {
            if (weaponInventory == null)
            {
                return "No weapon inventory";
            }

            WeaponRuntimeState weaponState = weaponInventory.GetWeaponStateAt(slotIndex);
            if (weaponState == null || weaponState.WeaponDefinition == null)
            {
                return "Empty weapon slot";
            }

            bool equipped = weaponInventory.EquipWeaponAt(slotIndex);
            return equipped
                ? "Equipped: " + weaponState.WeaponDefinition.displayName
                : "Equip failed: " + weaponState.WeaponDefinition.displayName;
        }

        private string UseTurretSkillSlot(int slotIndex)
        {
            int slotNumber = slotIndex + 1;
            InstalledSkillRegistry registry = InstalledSkillRegistry.Instance;
            SkillDefinitionSO skill = registry.GetSlotSkill(slotNumber);
            if (skill == null || registry.GetSlotTotalCount(slotNumber) <= 0)
            {
                return "Empty turret skill slot: " + slotNumber.ToString();
            }

            if (!TryCreateSkillUseContext(out SkillUseContext context))
            {
                return "No skill target";
            }

            bool used = registry.TryUseSlot(slotNumber, context);
            return used
                ? "Skill used: " + skill.DisplayName
                : "Skill not ready: " + skill.DisplayName;
        }

        private string UseSquadCommandSlot(int slotIndex)
        {
            if (squadCommandSlots == null || slotIndex < 0 || slotIndex >= squadCommandSlots.Length)
            {
                return "Empty squad command slot";
            }

            SquadCommandSlot slot = squadCommandSlots[slotIndex];
            if (slot == null)
            {
                return "Empty squad command slot";
            }

            ResolveReferences();

            if (squadCommandController == null)
            {
                return "No squad command controller";
            }

            if (slot.CommandType == PlayerSquadCommandType.SelectAll)
            {
                squadCommandController.SelectAll(out string selectAllStatus);
                return selectAllStatus;
            }

            squadCommandController.TryIssueCommand(
                slot.CommandType,
                slot.DisplayName,
                out string statusMessage);

            return statusMessage;
        }

        private string UseItem(PlayerItemRuntimeEntry item, string sourceLabel)
        {
            ResolveReferences();

            if (item == null || item.ItemDefinition == null)
            {
                return sourceLabel + ": no usable item";
            }

            if (itemUseController == null)
            {
                return sourceLabel + ": no item use controller";
            }

            itemUseController.TryUseItem(
                item,
                gameObject,
                out string statusMessage);

            return sourceLabel + ": " + statusMessage;
        }

        private bool TryBeginThrowableSlot(int slotIndex, out string status)
        {
            status = string.Empty;
            ResolveReferences();

            if (currentCategory != PlayerCommandCategory.Weapons)
            {
                return false;
            }

            PlayerItemRuntimeEntry item = GetItemForWeaponCategorySlot(slotIndex);

            if (item == null || item.ItemDefinition == null)
            {
                return false;
            }

            if (throwableItemController == null || !throwableItemController.CanAimItem(item))
            {
                return false;
            }

            if (!throwableItemController.BeginAim(item, gameObject, out status))
            {
                return false;
            }

            activeThrowableSlotIndex = slotIndex;
            return true;
        }

        private PlayerItemRuntimeEntry GetItemForWeaponCategorySlot(int slotIndex)
        {
            int weaponSlotCount = GetWeaponSlotCount();

            if (slotIndex < weaponSlotCount)
            {
                return null;
            }

            return GetItemEntryAt(slotIndex - weaponSlotCount);
        }

        private void CancelActiveThrowableAim()
        {
            if (activeThrowableSlotIndex < 0)
            {
                return;
            }

            activeThrowableSlotIndex = -1;
            throwableItemController?.CancelAim();
        }

        private void RefreshCommandPanel(string status)
        {
            ResolveReferences();

            if (commandPresenter == null)
            {
                return;
            }

            BuildSlotLabels();

            commandPresenter.Show(
                ResolveHotbarTitle(currentCategory),
                slotLabels,
                status,
                slotIcons: slotIcons);
        }

        private void BuildSlotLabels()
        {
            slotLabels.Clear();
            slotIcons.Clear();

            switch (currentCategory)
            {
                case PlayerCommandCategory.Weapons:
                    AddWeaponCategoryLabels();
                    break;
                case PlayerCommandCategory.TurretSkills:
                    AddTurretSkillLabels();
                    break;
                case PlayerCommandCategory.SquadCommands:
                    AddSquadCommandLabels();
                    break;
            }

            while (slotLabels.Count < PlayerCommandHotbarPresenter.MaxSlotCount)
            {
                slotLabels.Add(string.Empty);
                slotIcons.Add(null);
            }
        }

        private void AddWeaponCategoryLabels()
        {
            int weaponSlotCount = GetWeaponSlotCount();

            for (int i = 0; i < weaponSlotCount && slotLabels.Count < PlayerCommandHotbarPresenter.MaxSlotCount; i++)
            {
                WeaponRuntimeState weaponState = weaponInventory != null
                    ? weaponInventory.GetWeaponStateAt(i)
                    : null;

                string label = weaponState != null && weaponState.WeaponDefinition != null
                    ? weaponState.WeaponDefinition.displayName
                    : string.Empty;

                slotLabels.Add(label);
                slotIcons.Add(weaponState != null && weaponState.WeaponDefinition != null
                    ? weaponState.WeaponDefinition.icon
                    : null);
            }

            int itemIndex = 0;
            while (slotLabels.Count < PlayerCommandHotbarPresenter.MaxSlotCount)
            {
                PlayerItemRuntimeEntry item = GetItemEntryAt(itemIndex);
                if (item == null)
                {
                    break;
                }

                slotLabels.Add(CreateItemSlotLabel(item));
                slotIcons.Add(item != null && item.ItemDefinition != null
                    ? item.ItemDefinition.icon
                    : null);
                itemIndex++;
            }
        }

        private void AddTurretSkillLabels()
        {
            for (int i = 0; i < PlayerCommandHotbarPresenter.MaxSlotCount; i++)
            {
                int slotNumber = i + 1;
                InstalledSkillRegistry registry = InstalledSkillRegistry.Instance;
                SkillDefinitionSO skill = registry.GetSlotSkill(slotNumber);
                int totalCount = registry.GetSlotTotalCount(slotNumber);
                if (skill == null || totalCount <= 0)
                {
                    slotLabels.Add(string.Empty);
                    slotIcons.Add(null);
                    continue;
                }

                int readyCount = registry.GetSlotReadyCount(slotNumber);
                slotLabels.Add(skill.DisplayName + "\n" + readyCount.ToString() + "/" + totalCount.ToString());
                slotIcons.Add(skill.Icon);
            }
        }

        private bool TryCreateSkillUseContext(out SkillUseContext context)
        {
            PlayerAimSkillTargetProvider provider =
                FindFirstObjectByType<PlayerAimSkillTargetProvider>(FindObjectsInactive.Exclude);
            if (provider == null)
            {
                provider = FindFirstObjectByType<PlayerAimSkillTargetProvider>(FindObjectsInactive.Include);
            }

            if (provider == null)
            {
                context = default;
                return false;
            }

            return provider.TryCreateContext(provider.gameObject, out context);
        }

        private void AddSquadCommandLabels()
        {
            for (int i = 0; i < PlayerCommandHotbarPresenter.MaxSlotCount; i++)
            {
                if (squadCommandSlots != null && i < squadCommandSlots.Length && squadCommandSlots[i] != null)
                {
                    slotLabels.Add(squadCommandSlots[i].DisplayName);
                    slotIcons.Add(squadCommandSlots[i].Icon);
                }
                else
                {
                    slotLabels.Add(string.Empty);
                    slotIcons.Add(null);
                }
            }
        }

        private int GetWeaponSlotCount()
        {
            return weaponInventory != null
                ? Mathf.Min(weaponInventory.WeaponCount, PlayerCommandHotbarPresenter.MaxSlotCount)
                : 0;
        }

        private PlayerItemRuntimeEntry GetItemEntryAt(int itemIndex)
        {
            if (itemInventory == null || itemInventory.Items == null || itemIndex < 0)
            {
                return null;
            }

            int visibleIndex = 0;
            IReadOnlyList<PlayerItemRuntimeEntry> items = itemInventory.Items;

            for (int i = 0; i < items.Count; i++)
            {
                PlayerItemRuntimeEntry item = items[i];

                if (item == null || !item.IsAvailable)
                {
                    continue;
                }

                if (visibleIndex == itemIndex)
                {
                    return item;
                }

                visibleIndex++;
            }

            return null;
        }

        private static string CreateItemSlotLabel(PlayerItemRuntimeEntry item)
        {
            if (item == null || item.ItemDefinition == null)
            {
                return string.Empty;
            }

            return item.ItemDefinition.displayName + "\nx" + item.Count.ToString();
        }

        private void UpdateOpenRadialSelection()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || inputController == null)
            {
                return;
            }

            if (inputController.CurrentContext == PlayerInputContext.CommandRadial)
            {
                UpdateCommandRadialSelection();
                return;
            }

            if (inputController.CurrentContext == PlayerInputContext.ItemRadial)
            {
                UpdateItemRadialSelection();
            }
        }

        private void UpdateCommandRadialSelection()
        {
            if (commandRadialPresenter == null || Mouse.current == null)
            {
                return;
            }

            commandRadialPresenter.UpdateSelection(Mouse.current.position.ReadValue());
        }

        private void UpdateItemRadialSelection()
        {
            if (itemRadialPresenter == null)
            {
                return;
            }

            if (KeyboardInputMessenger.HasKeyboard)
            {
                for (int i = 1; i <= PlayerItemRadialPresenter.MaxItemCount; i++)
                {
                    if (KeyboardInputMessenger.WasMenuSlotPressed(i))
                    {
                        itemRadialPresenter.SelectIndex(i - 1);
                        return;
                    }
                }
            }

            if (Mouse.current != null)
            {
                itemRadialPresenter.UpdateSelection(Mouse.current.position.ReadValue());
            }
        }

        private void ResolveReferences()
        {
            if (inputController == null)
            {
                inputController = FindFirstObjectByType<PlayerCentralInputController>(FindObjectsInactive.Include);
            }

            if (weaponInventory == null)
            {
                weaponInventory = FindFirstObjectByType<PlayerWeaponInventory>(FindObjectsInactive.Include);
            }

            if (itemInventory == null)
            {
                itemInventory = FindFirstObjectByType<PlayerItemInventory>(FindObjectsInactive.Include);
            }

            if (itemUseController == null)
            {
                itemUseController = FindFirstObjectByType<PlayerItemUseController>(FindObjectsInactive.Include);
            }

            if (throwableItemController == null)
            {
                throwableItemController = FindFirstObjectByType<PlayerThrowableItemController>(FindObjectsInactive.Include);
            }

            if (squadCommandController == null)
            {
                squadCommandController = GetComponentInParent<PlayerSquadCommandController>();
            }

            if (squadCommandController == null)
            {
                squadCommandController = FindFirstObjectByType<PlayerSquadCommandController>(FindObjectsInactive.Include);
            }

            if (squadCommandController == null)
            {
                squadCommandController = gameObject.AddComponent<PlayerSquadCommandController>();
            }

            Transform canvas = ResolveMainCanvasTransform();

            if (commandPresenter == null)
            {
                commandPresenter = FindFirstObjectByType<PlayerCommandHotbarPresenter>(FindObjectsInactive.Include);
            }

            if (commandPresenter == null)
            {
                GameObject presenterObject = new GameObject(nameof(PlayerCommandHotbarPresenter), typeof(RectTransform));
                presenterObject.transform.SetParent(canvas, false);
                StretchToParent(presenterObject.GetComponent<RectTransform>());
                commandPresenter = presenterObject.AddComponent<PlayerCommandHotbarPresenter>();
                commandPresenter.UseDedicatedGeneratedUiMode();
            }

            if (commandRadialPresenter == null)
            {
                commandRadialPresenter = FindFirstObjectByType<PlayerCommandRadialPresenter>(FindObjectsInactive.Include);
            }

            if (commandRadialPresenter == null)
            {
                Debug.LogWarning("[PlayerCommandPanelController] PlayerCommandRadialPresenter가 연결되지 않았습니다.", this);
            }

            if (itemRadialPresenter == null)
            {
                itemRadialPresenter = FindFirstObjectByType<PlayerItemRadialPresenter>(FindObjectsInactive.Include);
            }

            if (itemRadialPresenter == null)
            {
                GameObject presenterObject = new GameObject(nameof(PlayerItemRadialPresenter), typeof(RectTransform));
                presenterObject.transform.SetParent(canvas, false);
                StretchToParent(presenterObject.GetComponent<RectTransform>());
                itemRadialPresenter = presenterObject.AddComponent<PlayerItemRadialPresenter>();
            }
        }

        private static string ResolveCategoryName(PlayerCommandCategory category)
        {
            return category switch
            {
                PlayerCommandCategory.Weapons => "Weapons",
                PlayerCommandCategory.TurretSkills => "Turret Skills",
                PlayerCommandCategory.SquadCommands => "Squad Commands",
                _ => "Commands"
            };
        }

        private static string ResolveHotbarTitle(PlayerCommandCategory category)
        {
            return category switch
            {
                PlayerCommandCategory.Weapons => "Q 1/3 - Weapons",
                PlayerCommandCategory.TurretSkills => "Q 2/3 - Turret Skills",
                PlayerCommandCategory.SquadCommands => "Q 3/3 - Squad Commands",
                _ => "Command Panel"
            };
        }

        private static Transform ResolveMainCanvasTransform()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Canvas fallback = null;

            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null || canvases[i].renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                if (canvases[i].name == "MainCanvas")
                {
                    return canvases[i].transform;
                }

                fallback ??= canvases[i];
            }

            if (fallback != null)
            {
                return fallback.transform;
            }

            GameObject canvasObject = new GameObject("MainCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas.transform;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
