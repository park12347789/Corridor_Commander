using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform goal;
        [SerializeField] private int spawnCount = 5;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private float initialDelay = 0.5f;
        [SerializeField] private bool runUpdateLoop = true;

        private int spawnedCount;
        private float nextSpawnTime;

        public int SpawnedCount => spawnedCount;

        private void OnEnable()
        {
            spawnedCount = 0;
            nextSpawnTime = Time.time + initialDelay;
        }

        private void Update()
        {
            if (runUpdateLoop)
            {
                TickSpawner();
            }
        }

        public void TickSpawner()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                return;
            }

            if (spawnedCount >= spawnCount || Time.time < nextSpawnTime)
            {
                return;
            }

            SpawnOne();
            nextSpawnTime = Time.time + spawnInterval;
        }

        public void SetUpdateLoopEnabled(bool enabled)
        {
            runUpdateLoop = enabled;
        }

        public GameObject SpawnOne()
        {
            if (enemyPrefab == null || spawnPoint == null || goal == null)
            {
                return null;
            }

            GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            enemy.name = $"Enemy_Basic_Spawned_{spawnedCount + 1:00}";
            if (enemy.TryGetComponent(out EnemyMovementController movementController))
            {
                movementController.SetTarget(goal);
            }

            spawnedCount++;
            return enemy;
        }
    }
}
