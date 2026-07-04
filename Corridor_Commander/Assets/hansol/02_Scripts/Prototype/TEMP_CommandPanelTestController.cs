using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TEMP_CommandPanelTestController : MonoBehaviour, ISupportTruckItemReceiver
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private TEMP_CommandPanelPresenter commandPresenter;
        [SerializeField] private TEMP_CommandRadialPanelPresenter commandRadialPresenter;
        [SerializeField] private TEMP_ItemRadialPanelPresenter itemRadialPresenter;
        [SerializeField] private LayerMask aimLayers = ~0;
        [SerializeField] private float aimDistance = 80f;
        [SerializeField] private float qHoldThreshold = 0.35f;
        [SerializeField] private float panelAutoHideDelay = 0f;
        [SerializeField] private float grenadeMarkerDuration = 1.25f;
        [SerializeField] private TEMP_CommandPanelEntry[] weaponEntries =
        {
            new TEMP_CommandPanelEntry("temp_rifle", "임시 돌격소총", "임시 총 슬롯 1", TEMP_CommandActionType.EquipWeapon),
            new TEMP_CommandPanelEntry("temp_shotgun", "임시 산탄총", "임시 총 슬롯 2", TEMP_CommandActionType.EquipWeapon)
        };
        [SerializeField] private TEMP_CommandPanelEntry[] squadCommandEntries =
        {
            new TEMP_CommandPanelEntry("squad_hold", "위치사수", "분대원이 현재 위치를 지킴", TEMP_CommandActionType.IssueSquadCommand, TEMP_SquadCommandType.HoldPosition),
            new TEMP_CommandPanelEntry("squad_return", "복귀", "플레이어 위치로 복귀", TEMP_CommandActionType.IssueSquadCommand, TEMP_SquadCommandType.ReturnToPlayer),
            new TEMP_CommandPanelEntry("squad_charge", "돌격", "조준점으로 전진", TEMP_CommandActionType.IssueSquadCommand, TEMP_SquadCommandType.Charge)
        };
        [SerializeField] private TEMP_UsableItemEntry[] itemEntries =
        {
            new TEMP_UsableItemEntry("temp_medkit", "회복킷", "총 세트 임시 회복템", TEMP_ItemUseType.Heal, 1, 35f, 0f),
            new TEMP_UsableItemEntry("temp_grenade", "수류탄", "총 세트 임시 수류탄", TEMP_ItemUseType.Grenade, 1, 45f, 4f)
        };

        private TEMP_CommandPanelCategory currentCategory;
        private int equippedWeaponIndex;
        private bool qIsHeld;
        private bool commandRadialOpen;
        private bool commandRadialInvokedDuringHold;
        private bool radialOpen;
        private float qPressedAt;
        private readonly List<string> slotLabels = new List<string>(TEMP_CommandPanelPresenter.MaxSlotCount);

        private void Awake()
        {
            EnsureRuntimeEntries();
            ResolveReferences();
        }

        private void EnsureRuntimeEntries()
        {
            weaponEntries = new[]
            {
                new TEMP_CommandPanelEntry("temp_rifle", "임시 돌격소총", "총 세트 임시 슬롯 1", TEMP_CommandActionType.EquipWeapon),
                new TEMP_CommandPanelEntry("temp_shotgun", "임시 산탄총", "총 세트 임시 슬롯 2", TEMP_CommandActionType.EquipWeapon)
            };

            squadCommandEntries = new[]
            {
                new TEMP_CommandPanelEntry("squad_hold", "위치사수", "분대원이 현재 위치를 지킴", TEMP_CommandActionType.IssueSquadCommand, TEMP_SquadCommandType.HoldPosition),
                new TEMP_CommandPanelEntry("squad_return", "복귀", "플레이어 위치로 복귀", TEMP_CommandActionType.IssueSquadCommand, TEMP_SquadCommandType.ReturnToPlayer),
                new TEMP_CommandPanelEntry("squad_charge", "돌격", "조준점으로 전진", TEMP_CommandActionType.IssueSquadCommand, TEMP_SquadCommandType.Charge)
            };

            itemEntries = new[]
            {
                new TEMP_UsableItemEntry("temp_medkit", "회복킷", "총 세트 임시 회복템", TEMP_ItemUseType.Heal, 1, 35f, 0f),
                new TEMP_UsableItemEntry("temp_grenade", "수류탄", "총 세트 임시 수류탄", TEMP_ItemUseType.Grenade, 1, 45f, 4f)
            };
        }

        private void Start()
        {
            RefreshCommandPanel("준비: Q 짧게 세트 변경 / 길게 선택");
        }

        private void OnDisable()
        {
            CloseCommandRadialPanel();
            CloseRadialPanel();
            TEMP_CommandInputState.CommandPanelOpen = false;
            TEMP_CommandInputState.PointerPanelOpen = false;
        }

        private void Update()
        {
            Keyboard keyboard = KeyboardInputMessenger.CurrentKeyboard;
            if (keyboard == null)
            {
                return;
            }

            TEMP_CommandInputState.CommandPanelOpen = true;
            if (!UiInputCoordinator.Instance.CanUseCommandHotkeys(this))
            {
                qIsHeld = false;
                return;
            }

            UpdateCommandInput(keyboard, Mouse.current);

            if (!radialOpen && !commandRadialOpen && TEMP_CommandInputState.CommandPanelOpen)
            {
                for (int i = 0; i < TEMP_CommandPanelPresenter.MaxSlotCount; i++)
                {
                    if (KeyboardInputMessenger.WasMenuSlotPressed(i + 1)
                        && UiInputCoordinator.Instance.TryConsumeCommandSlot(this, i + 1))
                    {
                        UseCommandSlot(i);
                    }
                }
            }

        }

        public bool TryReceiveSupportTruckItem(
            SupportTruckShopItemGrant itemGrant,
            int amount,
            out string statusMessage)
        {
            int grantAmount = Mathf.Max(1, amount);
            switch (itemGrant)
            {
                case SupportTruckShopItemGrant.TemporaryGun:
                    return TryReceiveTemporaryGun(out statusMessage);
                case SupportTruckShopItemGrant.Heal:
                    return TryAddItemCharges(TEMP_ItemUseType.Heal, grantAmount, out statusMessage);
                case SupportTruckShopItemGrant.Grenade:
                    return TryAddItemCharges(TEMP_ItemUseType.Grenade, grantAmount, out statusMessage);
                default:
                    statusMessage = "지원 아이템 데이터 없음";
                    return false;
            }
        }

        private void CycleCommandPanel()
        {
            TEMP_CommandPanelCategory nextCategory = currentCategory switch
            {
                TEMP_CommandPanelCategory.Weapons => TEMP_CommandPanelCategory.TurretSkills,
                TEMP_CommandPanelCategory.TurretSkills => TEMP_CommandPanelCategory.SquadCommands,
                _ => TEMP_CommandPanelCategory.Weapons
            };

            SelectCommandCategory(nextCategory, "패널 변경");
        }

        private void SelectCommandCategory(TEMP_CommandPanelCategory category, string sourceLabel)
        {
            currentCategory = category;
            commandRadialPresenter?.SetActiveCategory(currentCategory);
            RefreshCommandPanel(sourceLabel + ": " + ResolveCategoryName(currentCategory));
        }

        private void UseCommandSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= TEMP_CommandPanelPresenter.MaxSlotCount)
            {
                return;
            }

            string status = currentCategory switch
            {
                TEMP_CommandPanelCategory.Weapons => UseWeaponSetSlot(slotIndex),
                TEMP_CommandPanelCategory.TurretSkills => UseRegisteredTurretSkill(slotIndex),
                TEMP_CommandPanelCategory.SquadCommands => UseSquadCommandSlot(slotIndex),
                _ => "알 수 없는 명령"
            };

            RefreshCommandPanel(status);
        }

        private string UseWeaponSetSlot(int slotIndex)
        {
            int weaponSlotCount = GetWeaponSlotCount();
            if (slotIndex < weaponSlotCount)
            {
                TEMP_CommandPanelEntry entry = weaponEntries != null && slotIndex < weaponEntries.Length ? weaponEntries[slotIndex] : null;
                return EquipWeapon(slotIndex, entry);
            }

            int itemIndex = slotIndex - weaponSlotCount;
            TEMP_UsableItemEntry item = GetItemEntryAt(itemIndex);
            if (item == null)
            {
                return "빈 슬롯";
            }

            return UseItemFromCommandSlot(item);
        }

        private string EquipWeapon(int slotIndex, TEMP_CommandPanelEntry entry)
        {
            if (entry == null)
            {
                return "빈 무기 슬롯";
            }

            equippedWeaponIndex = slotIndex;
            return "무기 장착: " + entry.DisplayName;
        }

        private string UseTurretSkill(int slotIndex)
        {
            return "빈 포탑 슬롯: " + (slotIndex + 1).ToString();
        }

        private string UseSquadCommandSlot(int slotIndex)
        {
            if (squadCommandEntries == null || slotIndex >= squadCommandEntries.Length || squadCommandEntries[slotIndex] == null)
            {
                return "빈 분대명령 슬롯";
            }

            return IssueSquadCommand(squadCommandEntries[slotIndex]);
        }

        private void UpdateCommandInput(Keyboard keyboard, Mouse mouse)
        {
            if (radialOpen)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                qIsHeld = true;
                qPressedAt = Time.unscaledTime;
                commandRadialInvokedDuringHold = false;
            }

            if (!qIsHeld)
            {
                return;
            }

            float heldTime = Time.unscaledTime - qPressedAt;
            bool reachedHoldThreshold = heldTime >= qHoldThreshold;

            if (!commandRadialOpen && keyboard.qKey.isPressed && reachedHoldThreshold)
            {
                OpenCommandRadialPanel(mouse);
            }

            if (commandRadialOpen)
            {
                UpdateCommandRadialSelection(mouse);

                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    TEMP_CommandPanelCategory selectedCategory = commandRadialPresenter != null
                        ? commandRadialPresenter.SelectedCategory
                        : currentCategory;
                    SelectCommandCategory(selectedCategory, "세트 호출");
                    commandRadialInvokedDuringHold = true;
                }
            }

            if (!keyboard.qKey.wasReleasedThisFrame)
            {
                return;
            }

            if (commandRadialOpen || reachedHoldThreshold)
            {
                if (!commandRadialOpen)
                {
                    OpenCommandRadialPanel(mouse);
                }

                UpdateCommandRadialSelection(mouse);
                TEMP_CommandPanelCategory selectedCategory = commandRadialPresenter != null
                    ? commandRadialPresenter.SelectedCategory
                    : currentCategory;

                CloseCommandRadialPanel();
                if (!commandRadialInvokedDuringHold)
                {
                    SelectCommandCategory(selectedCategory, "세트 호출");
                }
            }
            else
            {
                CycleCommandPanel();
            }

            qIsHeld = false;
            commandRadialInvokedDuringHold = false;
        }

        private void OpenCommandRadialPanel(Mouse mouse)
        {
            ResolveReferences();
            if (!UiInputCoordinator.Instance.TryBeginPointerContext(this, UiInputContext.CommandRadial))
            {
                qIsHeld = false;
                return;
            }

            TEMP_CommandInputState.PointerPanelOpen = true;
            commandRadialOpen = true;
            commandRadialPresenter.Show(currentCategory);
            UpdateCommandRadialSelection(mouse);
        }

        private void CloseCommandRadialPanel()
        {
            if (!commandRadialOpen)
            {
                return;
            }

            commandRadialOpen = false;
            commandRadialPresenter?.Hide();
            TEMP_CommandInputState.PointerPanelOpen = false;
            UiInputCoordinator.Instance.EndContext(this);
        }

        private void UpdateCommandRadialSelection(Mouse mouse)
        {
            if (commandRadialPresenter == null || mouse == null)
            {
                return;
            }

            commandRadialPresenter.UpdateSelection(mouse.position.ReadValue());
        }

        private string IssueSquadCommand(TEMP_CommandPanelEntry entry)
        {
            AlliedSquadMemberFollower[] members = FindObjectsByType<AlliedSquadMemberFollower>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            if (members.Length == 0)
            {
                return "분대원 없음: 지원 트럭에서 먼저 고용";
            }

            Vector3 aimPoint = ResolveAimPoint();
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] == null)
                {
                    continue;
                }

                switch (entry.SquadCommandType)
                {
                    case TEMP_SquadCommandType.HoldPosition:
                        members[i].SetHoldPosition();
                        break;
                    case TEMP_SquadCommandType.ReturnToPlayer:
                        members[i].ReturnToPlayer(transform, i);
                        break;
                    case TEMP_SquadCommandType.Charge:
                        members[i].SetChargeTarget(aimPoint, i);
                        break;
                }
            }

            return "분대 명령: " + entry.DisplayName + " x" + members.Length;
        }

        private void UpdateItemInput(Keyboard keyboard, Mouse mouse)
        {
            if (radialOpen && mouse != null)
            {
                itemRadialPresenter.UpdateSelection(mouse.position.ReadValue());
            }
        }

        private void OpenRadialPanel()
        {
            ResolveReferences();
            if (!UiInputCoordinator.Instance.TryBeginPointerContext(this, UiInputContext.ItemRadial))
            {
                return;
            }

            TEMP_CommandInputState.PointerPanelOpen = true;
            radialOpen = true;
            itemRadialPresenter.Show(itemEntries);
        }

        private void CloseRadialPanel()
        {
            if (!radialOpen)
            {
                return;
            }

            radialOpen = false;
            TEMP_CommandInputState.PointerPanelOpen = false;
            UiInputCoordinator.Instance.EndContext(this);
            itemRadialPresenter?.Hide();
        }

        private void UseItem(TEMP_UsableItemEntry item, string sourceLabel)
        {
            if (item == null)
            {
                RefreshCommandPanel(sourceLabel + ": 사용 가능 아이템 없음");
                return;
            }

            if (!item.TryConsume())
            {
                RefreshCommandPanel(sourceLabel + ": " + item.DisplayName + " 없음");
                return;
            }

            string status = item.UseType switch
            {
                TEMP_ItemUseType.Heal => UseHeal(item),
                TEMP_ItemUseType.Grenade => UseGrenade(item),
                _ => "아이템 사용: " + item.DisplayName
            };

            RefreshCommandPanel(sourceLabel + ": " + status);
        }

        private bool TryReceiveTemporaryGun(out string statusMessage)
        {
            if (weaponEntries == null || weaponEntries.Length == 0 || weaponEntries[0] == null)
            {
                statusMessage = "임시 총 지급 실패: 무기 슬롯 없음";
                return false;
            }

            equippedWeaponIndex = 0;
            statusMessage = "상점 획득: " + weaponEntries[0].DisplayName;
            RefreshCommandPanel(statusMessage);
            return true;
        }

        private bool TryAddItemCharges(
            TEMP_ItemUseType useType,
            int amount,
            out string statusMessage)
        {
            TEMP_UsableItemEntry item = FindItemByUseType(useType);
            if (item == null)
            {
                statusMessage = "상점 아이템 지급 실패: " + ResolveItemUseName(useType);
                return false;
            }

            int grantAmount = Mathf.Max(1, amount);
            item.AddCharges(grantAmount);
            statusMessage = "상점 획득: " + item.DisplayName + " x" + grantAmount.ToString();
            RefreshCommandPanel(statusMessage);
            return true;
        }

        private TEMP_UsableItemEntry FindItemByUseType(TEMP_ItemUseType useType)
        {
            if (itemEntries == null)
            {
                return null;
            }

            for (int i = 0; i < itemEntries.Length; i++)
            {
                if (itemEntries[i] != null && itemEntries[i].UseType == useType)
                {
                    return itemEntries[i];
                }
            }

            return null;
        }

        private static string ResolveItemUseName(TEMP_ItemUseType useType)
        {
            return useType switch
            {
                TEMP_ItemUseType.Heal => "회복킷",
                TEMP_ItemUseType.Grenade => "수류탄",
                _ => "아이템"
            };
        }

        private string UseItemFromCommandSlot(TEMP_UsableItemEntry item)
        {
            if (item == null)
            {
                return "사용 가능 아이템 없음";
            }

            if (!item.TryConsume())
            {
                return item.DisplayName + " 없음";
            }

            return item.UseType switch
            {
                TEMP_ItemUseType.Heal => UseHeal(item),
                TEMP_ItemUseType.Grenade => UseGrenade(item),
                _ => "아이템 사용: " + item.DisplayName
            };
        }

        private string UseHeal(TEMP_UsableItemEntry item)
        {
            Health health = GetComponent<Health>();
            if (health == null)
            {
                health = gameObject.AddComponent<Health>();
            }

            health.Restore(item.Value);
            return item.DisplayName + " +" + item.Value.ToString("0");
        }

        private string UseGrenade(TEMP_UsableItemEntry item)
        {
            Vector3 targetPoint = ResolveAimPoint();
            Collider[] colliders = Physics.OverlapSphere(
                targetPoint,
                item.Radius,
                aimLayers,
                QueryTriggerInteraction.Ignore);

            int hitCount = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                IDamageable damageable = colliders[i] != null
                    ? colliders[i].GetComponentInParent<IDamageable>()
                    : null;

                if (damageable == null || colliders[i].transform.IsChildOf(transform))
                {
                    continue;
                }

                damageable.TakeDamage(new DamageInfo(item.Value, gameObject, colliders[i].ClosestPoint(targetPoint)));
                hitCount++;
            }

            SpawnMarker(targetPoint, item.Radius, new Color(1f, 0.45f, 0.05f, 0.45f), "TEMP_GrenadeMarker", grenadeMarkerDuration);
            return item.DisplayName + " 피해대상 " + hitCount;
        }

        private TEMP_UsableItemEntry GetQuickItem()
        {
            if (itemEntries == null)
            {
                return null;
            }

            for (int i = 0; i < itemEntries.Length; i++)
            {
                if (itemEntries[i] != null && itemEntries[i].UseType == TEMP_ItemUseType.Heal && itemEntries[i].IsAvailable)
                {
                    return itemEntries[i];
                }
            }

            for (int i = 0; i < itemEntries.Length; i++)
            {
                if (itemEntries[i] != null && itemEntries[i].IsAvailable)
                {
                    return itemEntries[i];
                }
            }

            return null;
        }

        private void RefreshCommandPanel(string status)
        {
            ResolveReferences();
            TEMP_CommandInputState.CommandPanelOpen = true;
            BuildSlotLabels();
            commandPresenter.Show(currentCategory, slotLabels, status, panelAutoHideDelay);
        }

        private void BuildSlotLabels()
        {
            slotLabels.Clear();
            switch (currentCategory)
            {
                case TEMP_CommandPanelCategory.Weapons:
                    AddWeaponSetLabels();
                    break;
                case TEMP_CommandPanelCategory.TurretSkills:
                    AddRegisteredTurretSkillLabels();
                    break;
                case TEMP_CommandPanelCategory.SquadCommands:
                    AddSquadCommandLabels();
                    break;
            }

            while (slotLabels.Count < TEMP_CommandPanelPresenter.MaxSlotCount)
            {
                slotLabels.Add(string.Empty);
            }
        }

        private void AddWeaponSetLabels()
        {
            int weaponSlotCount = GetWeaponSlotCount();
            for (int i = 0; i < weaponSlotCount && slotLabels.Count < TEMP_CommandPanelPresenter.MaxSlotCount; i++)
            {
                slotLabels.Add(CreateWeaponSlotLabel(i));
            }

            int itemIndex = 0;
            while (slotLabels.Count < TEMP_CommandPanelPresenter.MaxSlotCount)
            {
                TEMP_UsableItemEntry item = GetItemEntryAt(itemIndex);
                if (item == null)
                {
                    break;
                }

                slotLabels.Add(CreateItemSlotLabel(item));
                itemIndex++;
            }
        }

        private void AddTurretSkillLabels()
        {
            for (int i = 0; i < TEMP_CommandPanelPresenter.MaxSlotCount; i++)
            {
                slotLabels.Add(string.Empty);
            }
        }

        private string UseRegisteredTurretSkill(int slotIndex)
        {
            int slotNumber = slotIndex + 1;
            InstalledSkillRegistry registry = InstalledSkillRegistry.Instance;
            SkillDefinitionSO skill = registry.GetSlotSkill(slotNumber);
            if (skill == null || registry.GetSlotTotalCount(slotNumber) <= 0)
            {
                return "Empty skill slot: " + slotNumber.ToString();
            }

            if (!TryCreateSkillUseContext(out SkillUseContext context))
            {
                return "Skill target provider missing";
            }

            bool used = registry.TryUseSlot(slotNumber, context);
            return used
                ? "Skill used: " + skill.DisplayName
                : "Skill not ready: " + skill.DisplayName;
        }

        private void AddRegisteredTurretSkillLabels()
        {
            InstalledSkillRegistry registry = InstalledSkillRegistry.Instance;
            for (int i = 0; i < TEMP_CommandPanelPresenter.MaxSlotCount; i++)
            {
                int slotNumber = i + 1;
                SkillDefinitionSO skill = registry.GetSlotSkill(slotNumber);
                int totalCount = registry.GetSlotTotalCount(slotNumber);
                if (skill == null || totalCount <= 0)
                {
                    slotLabels.Add(string.Empty);
                    continue;
                }

                int readyCount = registry.GetSlotReadyCount(slotNumber);
                slotLabels.Add(skill.DisplayName + "\n" + readyCount.ToString() + "/" + totalCount.ToString());
            }
        }

        private bool TryCreateSkillUseContext(out SkillUseContext context)
        {
            PlayerAimSkillTargetProvider provider =
                FindFirstObjectByType<PlayerAimSkillTargetProvider>(FindObjectsInactive.Include);
            if (provider == null)
            {
                context = default;
                return false;
            }

            return provider.TryCreateContext(gameObject, out context);
        }

        private void AddSquadCommandLabels()
        {
            for (int i = 0; i < TEMP_CommandPanelPresenter.MaxSlotCount; i++)
            {
                TEMP_CommandPanelEntry entry = squadCommandEntries != null && i < squadCommandEntries.Length
                    ? squadCommandEntries[i]
                    : null;
                slotLabels.Add(entry != null ? entry.DisplayName : string.Empty);
            }
        }

        private int GetWeaponSlotCount()
        {
            return weaponEntries != null
                ? Mathf.Min(weaponEntries.Length, Mathf.Max(0, TEMP_CommandPanelPresenter.MaxSlotCount - GetItemEntryCount()))
                : 0;
        }

        private string CreateWeaponSlotLabel(int slotIndex)
        {
            TEMP_CommandPanelEntry entry = weaponEntries != null && slotIndex < weaponEntries.Length
                ? weaponEntries[slotIndex]
                : null;
            string prefix = equippedWeaponIndex == slotIndex ? "* " : string.Empty;
            return entry != null ? prefix + entry.DisplayName : string.Empty;
        }

        private string CreateItemSlotLabel(TEMP_UsableItemEntry item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            return item.DisplayName + "\nx" + item.Charges.ToString();
        }

        private int GetItemEntryCount()
        {
            if (itemEntries == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < itemEntries.Length; i++)
            {
                if (itemEntries[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private TEMP_UsableItemEntry GetItemEntryAt(int itemIndex)
        {
            if (itemEntries == null || itemIndex < 0)
            {
                return null;
            }

            int visibleIndex = 0;
            for (int i = 0; i < itemEntries.Length; i++)
            {
                if (itemEntries[i] == null)
                {
                    continue;
                }

                if (visibleIndex == itemIndex)
                {
                    return itemEntries[i];
                }

                visibleIndex++;
            }

            return null;
        }

        private Vector3 ResolveAimPoint()
        {
            Camera resolvedCamera = ResolveCamera();
            if (resolvedCamera == null)
            {
                return transform.position + transform.forward * Mathf.Max(1f, aimDistance);
            }

            Ray ray = resolvedCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimLayers, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return ray.origin + ray.direction * aimDistance;
        }

        private Camera ResolveCamera()
        {
            if (aimCamera != null)
            {
                return aimCamera;
            }

            PlayerAimSkillTargetProvider provider = GetComponent<PlayerAimSkillTargetProvider>();
            if (provider != null && provider.TryCreateContext(gameObject, out SkillUseContext context))
            {
                aimCamera = context.AimCamera;
            }

            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            return aimCamera;
        }

        private void ResolveReferences()
        {
            if (commandPresenter == null)
            {
                Debug.LogWarning("[TEMP_CommandPanelTestController] TEMP_CommandPanelPresenter is not assigned.", this);
            }

            if (itemRadialPresenter == null)
            {
                Debug.LogWarning("[TEMP_CommandPanelTestController] TEMP_ItemRadialPanelPresenter is not assigned.", this);
            }

            if (commandRadialPresenter == null)
            {
                Debug.LogWarning("[TEMP_CommandPanelTestController] TEMP_CommandRadialPanelPresenter is not assigned.", this);
            }

            ResolveCamera();
        }

        private static string ResolveCategoryName(TEMP_CommandPanelCategory category)
        {
            return category switch
            {
                TEMP_CommandPanelCategory.Weapons => "총",
                TEMP_CommandPanelCategory.TurretSkills => "포탑",
                TEMP_CommandPanelCategory.SquadCommands => "분대명령",
                _ => "명령"
            };
        }

        private static void SpawnMarker(Vector3 position, float radius, Color color, string name, float duration)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            marker.transform.position = position + Vector3.up * 0.15f;
            marker.transform.localScale = Vector3.one * Mathf.Max(0.25f, radius * 2f);

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            Destroy(marker, Mathf.Max(0.1f, duration));
        }
    }
}
