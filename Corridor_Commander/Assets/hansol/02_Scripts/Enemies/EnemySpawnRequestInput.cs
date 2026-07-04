using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawnRequestInput : MonoBehaviour
    {
        [SerializeField] private Button spawnButton;
        [SerializeField] private Text statusText;
        [SerializeField] private EnemySpawnManager spawnManager;
        [SerializeField] private EnemySpawnGroupSO spawnGroup;
        [SerializeField] private bool enableKeyboardShortcut;
        [SerializeField] private Key keyboardShortcut = Key.O;

        private void Awake()
        {
            if (spawnButton != null)
            {
                spawnButton.onClick.AddListener(SpawnFromLastActivatedPoint);
            }

            RefreshStatus();
        }

        private void OnDestroy()
        {
            if (spawnButton != null)
            {
                spawnButton.onClick.RemoveListener(SpawnFromLastActivatedPoint);
            }
        }

        private void Update()
        {
            if (enableKeyboardShortcut && KeyboardInputMessenger.WasShortcutPressed(keyboardShortcut))
            {
                SpawnFromLastActivatedPoint();
            }

            RefreshStatus();
        }

        public void SpawnFromLastActivatedPoint()
        {
            EnemySpawner spawner = FindSpawnTarget();
            if (spawner == null)
            {
                return;
            }

            spawner.TrySpawnOne(out _);
            RefreshStatus();
        }

        private EnemySpawner FindSpawnTarget()
        {
            ResolveSpawnManager();
            if (spawnManager == null || spawnGroup == null)
            {
                return null;
            }

            IReadOnlyList<EnemySpawner> spawners = spawnManager.GetActiveSpawners(spawnGroup);
            for (int i = 0; i < spawners.Count; i++)
            {
                EnemySpawner spawner = spawners[i];
                if (spawner != null && spawner.HasSpawnCapacity)
                {
                    return spawner;
                }
            }

            return null;
        }

        private void ResolveSpawnManager()
        {
            if (spawnManager == null)
            {
                spawnManager = EnemySpawnManager.Instance;
            }

            if (spawnManager == null)
            {
                spawnManager = FindFirstObjectByType<EnemySpawnManager>(FindObjectsInactive.Include);
            }
        }

        private void RefreshStatus()
        {
            if (statusText == null)
            {
                return;
            }

            EnemySpawner spawner = FindSpawnTarget();
            string inputLabel = enableKeyboardShortcut
                ? $"{keyboardShortcut}  적 스폰"
                : "수동 적 스폰";

            statusText.text = spawner != null
                ? $"{inputLabel}\n{spawner.name}"
                : $"{inputLabel}\n활성 스폰포인트 없음";
        }

    }
}
