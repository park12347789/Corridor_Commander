using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerSquadRoster : MonoBehaviour
    {
        public const int MaxMemberCount = 5;

        [SerializeField, Min(0.1f)] private float discoveryInterval = 0.5f;
        [SerializeField, Min(0f)] private float corpseRemovalDelay = 1.5f;
        [SerializeField] private bool logRosterChanges;
        [SerializeField] private bool logMemberDamage = true;

        private readonly AlliedSquadMemberFollower[] members = new AlliedSquadMemberFollower[MaxMemberCount];
        private readonly Health[] memberHealth = new Health[MaxMemberCount];
        private float nextDiscoveryTime;
        private int selectedSlotIndex = -1;
        private bool isAllSelected;

        public static PlayerSquadRoster Instance { get; private set; }
        public int SelectedSlotIndex => selectedSlotIndex;
        public bool IsAllSelected => isAllSelected;
        public int MemberCount { get; private set; }
        public bool IsFull => MemberCount >= MaxMemberCount;

        public event Action RosterChanged;
        public event Action SelectionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            DiscoverMembers();
        }

        private void OnDestroy()
        {
            ClearSubscriptions();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextDiscoveryTime)
            {
                return;
            }

            nextDiscoveryTime = Time.unscaledTime + Mathf.Max(0.1f, discoveryInterval);
            RemoveInvalidMembers();
            DiscoverMembers();
        }

        public AlliedSquadMemberFollower GetMemberAt(int slotIndex)
        {
            return IsValidSlot(slotIndex) ? members[slotIndex] : null;
        }

        public Health GetHealthAt(int slotIndex)
        {
            return IsValidSlot(slotIndex) ? memberHealth[slotIndex] : null;
        }

        public bool SelectSlot(int slotIndex)
        {
            if (!IsValidSlot(slotIndex) || members[slotIndex] == null)
            {
                return false;
            }

            selectedSlotIndex = slotIndex;
            isAllSelected = false;
            SelectionChanged?.Invoke();
            return true;
        }

        public bool SelectAll()
        {
            if (MemberCount <= 0)
            {
                return false;
            }

            selectedSlotIndex = -1;
            isAllSelected = true;
            SelectionChanged?.Invoke();
            return true;
        }

        public bool SelectAdjacent(int direction)
        {
            if (MemberCount <= 0 || direction == 0)
            {
                return false;
            }

            int step = direction < 0 ? -1 : 1;
            int startIndex = selectedSlotIndex;
            if (isAllSelected || !IsValidSlot(startIndex) || members[startIndex] == null)
            {
                startIndex = step > 0 ? -1 : MaxMemberCount;
            }

            for (int offset = 1; offset <= MaxMemberCount; offset++)
            {
                int candidateIndex = (startIndex + step * offset + MaxMemberCount * 2) % MaxMemberCount;
                if (members[candidateIndex] == null)
                {
                    continue;
                }

                selectedSlotIndex = candidateIndex;
                isAllSelected = false;
                SelectionChanged?.Invoke();
                return true;
            }

            return false;
        }

        public bool IsSlotSelected(int slotIndex)
        {
            return IsValidSlot(slotIndex)
                && members[slotIndex] != null
                && (isAllSelected || selectedSlotIndex == slotIndex);
        }

        public void FillCommandTargets(List<AlliedSquadMemberFollower> results)
        {
            results.Clear();

            if (isAllSelected)
            {
                for (int i = 0; i < members.Length; i++)
                {
                    if (members[i] != null)
                    {
                        results.Add(members[i]);
                    }
                }

                return;
            }

            AlliedSquadMemberFollower selectedMember = GetMemberAt(selectedSlotIndex);
            if (selectedMember != null)
            {
                results.Add(selectedMember);
            }
        }

        public bool TryRegisterMember(AlliedSquadMemberFollower member)
        {
            RemoveInvalidMembers();

            if (member == null || !member.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (Contains(member))
            {
                return true;
            }

            int emptySlot = FindEmptySlot();
            if (emptySlot < 0)
            {
                return false;
            }

            RegisterMember(emptySlot, member);

            if (selectedSlotIndex < 0 && !isAllSelected)
            {
                selectedSlotIndex = emptySlot;
                SelectionChanged?.Invoke();
            }

            RosterChanged?.Invoke();
            return true;
        }

        private void DiscoverMembers()
        {
            AlliedSquadMemberFollower[] discovered = FindObjectsByType<AlliedSquadMemberFollower>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            Array.Sort(discovered, CompareByInstanceId);
            bool changed = false;

            for (int i = 0; i < discovered.Length; i++)
            {
                AlliedSquadMemberFollower member = discovered[i];
                if (member == null || Contains(member))
                {
                    continue;
                }

                int emptySlot = FindEmptySlot();
                if (emptySlot < 0)
                {
                    break;
                }

                RegisterMember(emptySlot, member);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            if (selectedSlotIndex < 0 && !isAllSelected)
            {
                selectedSlotIndex = FindFirstOccupiedSlot();
                SelectionChanged?.Invoke();
            }

            RosterChanged?.Invoke();
        }

        private void RegisterMember(int slotIndex, AlliedSquadMemberFollower member)
        {
            members[slotIndex] = member;
            memberHealth[slotIndex] = ResolveHealth(member);
            MemberCount++;

            if (memberHealth[slotIndex] != null)
            {
                memberHealth[slotIndex].Damaged -= HandleMemberDamaged;
                memberHealth[slotIndex].Damaged += HandleMemberDamaged;
                memberHealth[slotIndex].Died -= HandleMemberDied;
                memberHealth[slotIndex].Died += HandleMemberDied;
            }

            member.Configure(ResolvePlayerTarget(), slotIndex);
            Log($"Registered {member.name} in F{slotIndex + 1}");
        }

        private void RemoveInvalidMembers()
        {
            bool changed = false;
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] == null || !members[i].gameObject.activeInHierarchy)
                {
                    changed |= RemoveAt(i);
                }
            }

            if (changed)
            {
                RosterChanged?.Invoke();
            }
        }

        private void HandleMemberDied(Health deadHealth)
        {
            for (int i = 0; i < memberHealth.Length; i++)
            {
                if (memberHealth[i] != deadHealth)
                {
                    continue;
                }

                GameObject corpse = members[i] != null ? members[i].gameObject : null;
                RemoveAt(i);
                RosterChanged?.Invoke();

                if (corpse != null)
                {
                    Destroy(corpse, Mathf.Max(0f, corpseRemovalDelay));
                }

                return;
            }
        }

        private void HandleMemberDamaged(Health damagedHealth, float damageAmount)
        {
            RosterChanged?.Invoke();

            if (!logMemberDamage)
            {
                return;
            }

            for (int i = 0; i < memberHealth.Length; i++)
            {
                if (memberHealth[i] != damagedHealth)
                {
                    continue;
                }

                string memberName = members[i] != null ? members[i].name : damagedHealth.name;
                Debug.Log(
                    $"[PlayerSquadRoster] Squad F{i + 1} damaged: {memberName} -{damageAmount:0.##} HP {damagedHealth.CurrentHitPoints:0.##}/{damagedHealth.MaxHitPoints:0.##}",
                    damagedHealth);
                return;
            }
        }

        private bool RemoveAt(int slotIndex)
        {
            if (!IsValidSlot(slotIndex) || members[slotIndex] == null)
            {
                return false;
            }

            if (memberHealth[slotIndex] != null)
            {
                memberHealth[slotIndex].Damaged -= HandleMemberDamaged;
                memberHealth[slotIndex].Died -= HandleMemberDied;
            }

            Log($"Removed {members[slotIndex].name} from F{slotIndex + 1}");
            members[slotIndex] = null;
            memberHealth[slotIndex] = null;
            MemberCount = Mathf.Max(0, MemberCount - 1);

            if (!isAllSelected && selectedSlotIndex == slotIndex)
            {
                selectedSlotIndex = FindFirstOccupiedSlot();
                SelectionChanged?.Invoke();
            }

            return true;
        }

        private void ClearSubscriptions()
        {
            for (int i = 0; i < memberHealth.Length; i++)
            {
                if (memberHealth[i] != null)
                {
                    memberHealth[i].Damaged -= HandleMemberDamaged;
                    memberHealth[i].Died -= HandleMemberDied;
                }
            }
        }

        private Transform ResolvePlayerTarget()
        {
            PlayerLocomotionController locomotion = GetComponentInChildren<PlayerLocomotionController>(true);
            return locomotion != null ? locomotion.transform : transform;
        }

        private static Health ResolveHealth(AlliedSquadMemberFollower member)
        {
            if (member == null)
            {
                return null;
            }

            Health health = member.GetComponent<Health>();
            if (health != null)
            {
                return health;
            }

            health = member.GetComponentInChildren<Health>(true);
            if (health != null)
            {
                return health;
            }

            health = member.GetComponentInParent<Health>();
            if (health != null)
            {
                return health;
            }

            Transform root = member.transform.root;
            return root != null ? root.GetComponentInChildren<Health>(true) : null;
        }

        private bool Contains(AlliedSquadMemberFollower member)
        {
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] == member)
                {
                    return true;
                }
            }

            return false;
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindFirstOccupiedSlot()
        {
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] != null)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MaxMemberCount;
        }

        private static int CompareByInstanceId(AlliedSquadMemberFollower left, AlliedSquadMemberFollower right)
        {
            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return left.GetInstanceID().CompareTo(right.GetInstanceID());
        }

        private void Log(string message)
        {
            if (logRosterChanges)
            {
                Debug.Log("[PlayerSquadRoster] " + message, this);
            }
        }
    }
}
