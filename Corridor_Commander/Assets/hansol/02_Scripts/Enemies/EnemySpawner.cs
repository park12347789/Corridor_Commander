using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform goal;
        [SerializeField] private EnemyRoute route;
        [SerializeField] private int spawnCount = 5;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private float initialDelay = 0.5f;
        [SerializeField] private float spawnHeightOffset = 0f;
        [SerializeField] private bool runUpdateLoop = true;

        private int spawnedCount;
        private float nextSpawnTime;
        private bool missingConfigurationWarningLogged;

        public int SpawnedCount => spawnedCount;
        public int SpawnLimit => spawnCount;
        public bool HasSpawnCapacity => spawnedCount < spawnCount;
        public Transform SpawnPoint => spawnPoint;
        public Transform Goal => goal;
        public EnemyRoute Route => route;

        private void OnEnable()
        {
            ResetRuntimeState();
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

            TrySpawnOne(out _);
            nextSpawnTime = Time.time + spawnInterval;
        }

        public void SetUpdateLoopEnabled(bool enabled)
        {
            runUpdateLoop = enabled;
        }

        public void ConfigureEnemyPrefab(GameObject configuredEnemyPrefab)
        {
            if (configuredEnemyPrefab != null)
            {
                enemyPrefab = configuredEnemyPrefab;
            }
        }

        public void ResetRuntimeState()
        {
            spawnedCount = 0;
            nextSpawnTime = Time.time + initialDelay;
        }

        public GameObject SpawnOne()
        {
            TrySpawnOne(out GameObject enemy);
            return enemy;
        }

        public bool TrySpawnOne(out GameObject enemy)
        {
            return TrySpawnOne(null, 1f, out enemy);
        }

        public bool TrySpawnOne(EnemyDefinitionSO enemyDefinition, float healthMultiplier, out GameObject enemy)
        {
            EnemySpawnRequest request = CreateSpawnRequest(enemyDefinition, healthMultiplier);
            if (!CanSpawn(request))
            {
                enemy = null;
                return false;
            }

            enemy = InstantiateEnemy(request);
            ApplyEnemyDefinition(enemy, request.EnemyDefinition, request.HealthMultiplier);
            ConfigureMovement(enemy, request);

            spawnedCount++;
            return true;
        }

        private EnemySpawnRequest CreateSpawnRequest(EnemyDefinitionSO enemyDefinition, float healthMultiplier)
        {
            return new EnemySpawnRequest(
                ResolveEnemyPrefab(enemyDefinition),
                enemyDefinition,
                Mathf.Max(0.01f, healthMultiplier),
                spawnPoint,
                ResolveGoal(),
                route);
        }

        private Transform ResolveGoal()
        {
            return goal != null ? goal : GameManager.Instance?.MainTarget;
        }

        private bool CanSpawn(EnemySpawnRequest request)
        {
            if (HasSpawnCapacity
                && request.EnemyPrefab != null
                && request.SpawnPoint != null
                && request.Goal != null)
            {
                return true;
            }

            LogMissingConfiguration(request);
            return false;
        }

        private void LogMissingConfiguration(EnemySpawnRequest request)
        {
            if (missingConfigurationWarningLogged)
            {
                return;
            }

            if (request.EnemyPrefab == null)
            {
                Debug.LogWarning("[EnemySpawner] Enemy prefab is missing.", this);
            }

            if (request.SpawnPoint == null)
            {
                Debug.LogWarning("[EnemySpawner] Spawn point is missing.", this);
            }

            if (request.Goal == null)
            {
                Debug.LogWarning("[EnemySpawner] Goal is missing and GameManager.MainTarget fallback was not available.", this);
            }

            missingConfigurationWarningLogged = true;
        }

        private GameObject InstantiateEnemy(EnemySpawnRequest request)
        {
            Vector3 spawnPosition = request.SpawnPoint.position + Vector3.up * spawnHeightOffset;
            GameObject enemy = Instantiate(request.EnemyPrefab, spawnPosition, request.SpawnPoint.rotation);
            enemy.name = $"{request.EnemyPrefab.name}_Spawned_{spawnedCount + 1:00}";
            return enemy;
        }

        private static void ConfigureMovement(GameObject enemy, EnemySpawnRequest request)
        {
            if (enemy == null || !enemy.TryGetComponent(out EnemyMovementController movementController))
            {
                return;
            }

            if (request.Route != null)
            {
                movementController.SetRoute(
                    request.Route.Waypoints,
                    request.Route.IncludeFinalTarget ? request.Goal : null);
            }
            else
            {
                movementController.SetTarget(request.Goal);
            }
        }

        private GameObject ResolveEnemyPrefab(EnemyDefinitionSO enemyDefinition)
        {
            return enemyDefinition != null && enemyDefinition.Prefab != null
                ? enemyDefinition.Prefab
                : enemyPrefab;
        }

        private static void ApplyEnemyDefinition(
            GameObject enemy,
            EnemyDefinitionSO enemyDefinition,
            float healthMultiplier)
        {
            if (enemy == null)
            {
                return;
            }

            float resolvedHealthMultiplier = healthMultiplier;
            if (enemyDefinition != null)
            {
                resolvedHealthMultiplier *= enemyDefinition.HealthMultiplier;
            }

            if (!Mathf.Approximately(resolvedHealthMultiplier, 1f)
                && enemy.TryGetComponent(out Health health))
            {
                health.ScaleMaxHitPoints(resolvedHealthMultiplier);
            }

            float moveSpeedMultiplier = enemyDefinition != null ? enemyDefinition.MoveSpeedMultiplier : 1f;
            if (!Mathf.Approximately(moveSpeedMultiplier, 1f))
            {
                MonoBehaviour[] behaviours = enemy.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IMoveSpeedMultiplierReceiver receiver)
                    {
                        receiver.SetMoveSpeedMultiplier(moveSpeedMultiplier);
                    }
                }
            }

            float visualScaleMultiplier = enemyDefinition != null ? enemyDefinition.VisualScaleMultiplier : 1f;
            if (!Mathf.Approximately(visualScaleMultiplier, 1f))
            {
                enemy.transform.localScale *= Mathf.Max(0.01f, visualScaleMultiplier);
            }
        }

        private readonly struct EnemySpawnRequest
        {
            public EnemySpawnRequest(
                GameObject enemyPrefab,
                EnemyDefinitionSO enemyDefinition,
                float healthMultiplier,
                Transform spawnPoint,
                Transform goal,
                EnemyRoute route)
            {
                EnemyPrefab = enemyPrefab;
                EnemyDefinition = enemyDefinition;
                HealthMultiplier = healthMultiplier;
                SpawnPoint = spawnPoint;
                Goal = goal;
                Route = route;
            }

            public GameObject EnemyPrefab { get; }
            public EnemyDefinitionSO EnemyDefinition { get; }
            public float HealthMultiplier { get; }
            public Transform SpawnPoint { get; }
            public Transform Goal { get; }
            public EnemyRoute Route { get; }
        }

        private void OnValidate()
        {
            if (route == null)
            {
                route = GetComponent<EnemyRoute>();
            }
        }
    }
}
