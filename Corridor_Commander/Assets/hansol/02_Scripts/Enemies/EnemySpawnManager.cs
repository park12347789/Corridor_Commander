using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawnManager : MonoBehaviour
    {
        [SerializeField] private EnemySpawner[] initialActiveSpawners;
        [SerializeField] private EnemySpawner[] initialInactiveSpawners;
        [SerializeField] private SpawnGroupBinding[] spawnGroups;
        [SerializeField] private DoorSpawnRule[] doorSpawnRules;
        [SerializeField] private bool applyInitialStateOnAwake = true;

        public static EnemySpawnManager Instance { get; private set; }

        public event Action<string> RegionOpened;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple EnemySpawnManager instances found.", this);
            }

            Instance = this;

            if (applyInitialStateOnAwake)
            {
                ApplyInitialState();
            }
        }

        private void OnEnable()
        {
            ResetDoorRuleRuntimeState();
            SubscribeDoorRules();
        }

        private void Start()
        {
            ApplyAlreadyOpenDoorRules();
        }

        private void OnDisable()
        {
            UnsubscribeDoorRules();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public IReadOnlyList<EnemySpawner> GetActiveSpawners(EnemySpawnGroupSO spawnGroup, string spawnerNameContains = null)
        {
            List<EnemySpawner> spawners = new List<EnemySpawner>();
            SpawnGroupBinding binding = FindBinding(spawnGroup);
            if (binding != null)
            {
                binding.CollectActiveSpawners(spawners);
            }

            if (binding == null)
            {
                CollectManagedActiveSpawners(spawners);
            }

            FilterByName(spawners, spawnerNameContains);
            return spawners;
        }

        public void SetManagedSpawnerAutomationEnabled(bool enabled)
        {
            List<EnemySpawner> spawners = new List<EnemySpawner>();
            CollectManagedSpawners(spawners);

            for (int i = 0; i < spawners.Count; i++)
            {
                EnemySpawner spawner = spawners[i];
                if (spawner == null)
                {
                    continue;
                }

                spawner.SetUpdateLoopEnabled(enabled);

                Behaviour behaviourAgent = spawner.GetComponent("Unity.Behavior.BehaviorGraphAgent") as Behaviour;
                if (behaviourAgent != null)
                {
                    behaviourAgent.enabled = enabled;
                }
            }
        }

        public bool TryApplyDoorRule(MapExpansionDoorOpener door)
        {
            if (doorSpawnRules == null)
            {
                return false;
            }

            bool appliedAny = false;
            for (int i = 0; i < doorSpawnRules.Length; i++)
            {
                DoorSpawnRule rule = doorSpawnRules[i];
                if (rule != null && rule.TryApply(door))
                {
                    appliedAny = true;
                    if (!string.IsNullOrWhiteSpace(rule.RegionId))
                    {
                        RegionOpened?.Invoke(rule.RegionId);
                    }
                }
            }

            return appliedAny;
        }

        private void ApplyInitialState()
        {
            SetSpawnersActive(initialInactiveSpawners, false);
            SetSpawnersActive(initialActiveSpawners, true);
        }

        private void SubscribeDoorRules()
        {
            if (doorSpawnRules == null)
            {
                return;
            }

            for (int i = 0; i < doorSpawnRules.Length; i++)
            {
                doorSpawnRules[i]?.Subscribe(HandleDoorOpened);
            }
        }

        private void UnsubscribeDoorRules()
        {
            if (doorSpawnRules == null)
            {
                return;
            }

            for (int i = 0; i < doorSpawnRules.Length; i++)
            {
                doorSpawnRules[i]?.Unsubscribe(HandleDoorOpened);
            }
        }

        private void ResetDoorRuleRuntimeState()
        {
            if (doorSpawnRules == null)
            {
                return;
            }

            for (int i = 0; i < doorSpawnRules.Length; i++)
            {
                doorSpawnRules[i]?.ResetRuntimeState();
            }
        }

        private void ApplyAlreadyOpenDoorRules()
        {
            if (doorSpawnRules == null)
            {
                return;
            }

            for (int i = 0; i < doorSpawnRules.Length; i++)
            {
                DoorSpawnRule rule = doorSpawnRules[i];
                if (rule != null && rule.TryApplyIfDoorIsOpen() && !string.IsNullOrWhiteSpace(rule.RegionId))
                {
                    RegionOpened?.Invoke(rule.RegionId);
                }
            }
        }

        private void HandleDoorOpened(MapExpansionDoorOpener door)
        {
            TryApplyDoorRule(door);
        }

        private SpawnGroupBinding FindBinding(EnemySpawnGroupSO spawnGroup)
        {
            if (spawnGroup == null || spawnGroups == null)
            {
                return null;
            }

            for (int i = 0; i < spawnGroups.Length; i++)
            {
                SpawnGroupBinding binding = spawnGroups[i];
                if (binding != null && binding.SpawnGroup == spawnGroup)
                {
                    return binding;
                }
            }

            return null;
        }

        private void CollectManagedSpawners(List<EnemySpawner> spawners)
        {
            CollectUnique(spawners, initialActiveSpawners);
            CollectUnique(spawners, initialInactiveSpawners);

            if (spawnGroups != null)
            {
                for (int i = 0; i < spawnGroups.Length; i++)
                {
                    spawnGroups[i]?.CollectSpawners(spawners);
                }
            }

            if (doorSpawnRules != null)
            {
                for (int i = 0; i < doorSpawnRules.Length; i++)
                {
                    doorSpawnRules[i]?.CollectSpawners(spawners);
                }
            }
        }

        private void CollectManagedActiveSpawners(List<EnemySpawner> spawners)
        {
            List<EnemySpawner> managedSpawners = new List<EnemySpawner>();
            CollectManagedSpawners(managedSpawners);
            for (int i = 0; i < managedSpawners.Count; i++)
            {
                EnemySpawner spawner = managedSpawners[i];
                if (spawner != null && spawner.gameObject.activeInHierarchy && !spawners.Contains(spawner))
                {
                    spawners.Add(spawner);
                }
            }
        }

        private static void FilterByName(List<EnemySpawner> spawners, string spawnerNameContains)
        {
            if (spawners == null || string.IsNullOrWhiteSpace(spawnerNameContains))
            {
                return;
            }

            for (int i = spawners.Count - 1; i >= 0; i--)
            {
                EnemySpawner spawner = spawners[i];
                if (spawner == null || !spawner.name.Contains(spawnerNameContains, StringComparison.OrdinalIgnoreCase))
                {
                    spawners.RemoveAt(i);
                }
            }
        }

        private static void SetSpawnersActive(EnemySpawner[] spawners, bool active)
        {
            if (spawners == null)
            {
                return;
            }

            for (int i = 0; i < spawners.Length; i++)
            {
                EnemySpawner spawner = spawners[i];
                if (spawner != null)
                {
                    spawner.gameObject.SetActive(active);
                }
            }
        }

        private static void CollectUnique(List<EnemySpawner> result, EnemySpawner[] spawners)
        {
            if (spawners == null)
            {
                return;
            }

            for (int i = 0; i < spawners.Length; i++)
            {
                EnemySpawner spawner = spawners[i];
                if (spawner != null && !result.Contains(spawner))
                {
                    result.Add(spawner);
                }
            }
        }

        [Serializable]
        public sealed class SpawnGroupBinding
        {
            [SerializeField] private EnemySpawnGroupSO spawnGroup;
            [SerializeField] private EnemySpawner[] spawners;

            public EnemySpawnGroupSO SpawnGroup => spawnGroup;

            public void CollectActiveSpawners(List<EnemySpawner> result)
            {
                if (spawners == null)
                {
                    return;
                }

                for (int i = 0; i < spawners.Length; i++)
                {
                    EnemySpawner spawner = spawners[i];
                    if (spawner != null && spawner.gameObject.activeInHierarchy)
                    {
                        result.Add(spawner);
                    }
                }
            }

            public void CollectSpawners(List<EnemySpawner> result)
            {
                CollectUnique(result, spawners);
            }
        }

        [Serializable]
        public sealed class DoorSpawnRule
        {
            [SerializeField] private MapExpansionDoorOpener door;
            [SerializeField] private string regionId;
            [SerializeField] private EnemySpawner[] enableSpawners;
            [SerializeField] private EnemySpawner[] disableSpawners;
            [SerializeField] private bool applyOnlyOnce = true;

            [NonSerialized] private bool isApplied;

            public string RegionId => regionId;

            public void ResetRuntimeState()
            {
                isApplied = false;
            }

            public void Subscribe(Action<MapExpansionDoorOpener> openedHandler)
            {
                if (door != null)
                {
                    door.Opened += openedHandler;
                }
            }

            public void Unsubscribe(Action<MapExpansionDoorOpener> openedHandler)
            {
                if (door != null)
                {
                    door.Opened -= openedHandler;
                }
            }

            public bool TryApplyIfDoorIsOpen()
            {
                return door != null && door.IsOpen && TryApply(door);
            }

            public bool TryApply(MapExpansionDoorOpener openedDoor)
            {
                if (door == null || openedDoor != door)
                {
                    return false;
                }

                if (applyOnlyOnce && isApplied)
                {
                    return false;
                }

                SetSpawnersActive(disableSpawners, false);
                SetSpawnersActive(enableSpawners, true);
                isApplied = true;
                return true;
            }

            public void CollectSpawners(List<EnemySpawner> result)
            {
                CollectUnique(result, enableSpawners);
                CollectUnique(result, disableSpawners);
            }
        }
    }
}
