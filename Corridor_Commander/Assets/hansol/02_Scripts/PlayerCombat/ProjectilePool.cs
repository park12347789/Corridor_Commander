using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CorridorCommander.PlayerCombat
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        [System.Serializable]
        private sealed class PoolEntry
        {
            public ProjectileDefinitionSO projectileDefinition;
        }

        [SerializeField] private PoolEntry[] poolEntries;
        [SerializeField] private Transform poolRoot;

        private readonly Dictionary<ProjectileDefinitionSO, Queue<Projectile>> pools = new();
        private readonly HashSet<Projectile> pooledProjectiles = new();

        private void Awake()
        {
            ValidatePoolRoot();
            PrewarmPools();
        }

        private bool ValidatePoolRoot()
        {
            if (IsValidRuntimeTransform(poolRoot))
            {
                return true;
            }

            if (poolRoot != null)
            {
                Debug.LogError("[ProjectilePool] Pool Root references a prefab asset or invalid transform.", this);
            }
            else
            {
                Debug.LogError("[ProjectilePool] Pool Root is not assigned.", this);
            }

            return false;
        }

        private static bool IsValidRuntimeTransform(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            if (!target.gameObject.scene.IsValid())
            {
                return false;
            }

#if UNITY_EDITOR
            if (EditorUtility.IsPersistent(target))
            {
                return false;
            }

            if (EditorUtility.IsPersistent(target.gameObject))
            {
                return false;
            }
#endif

            return true;
        }

        private void PrewarmPools()
        {
            if (poolEntries == null)
            {
                return;
            }

            for (int i = 0; i < poolEntries.Length; i++)
            {
                PoolEntry entry = poolEntries[i];

                if (entry == null || entry.projectileDefinition == null)
                {
                    continue;
                }

                ProjectileDefinitionSO definition = entry.projectileDefinition;

                if (definition.projectilePrefab == null)
                {
                    Debug.LogWarning($"[ProjectilePool] {definition.name}에 Projectile Prefab이 연결되지 않았습니다.");
                    continue;
                }

                GetOrCreatePool(definition);

                int size = Mathf.Max(0, definition.initialPoolSize);

                for (int j = 0; j < size; j++)
                {
                    Projectile projectile = CreateProjectile(definition);

                    if (projectile == null)
                    {
                        continue;
                    }

                    ReturnToPool(definition, projectile);
                }
            }
        }

        public Projectile Get(
            ProjectileDefinitionSO definition,
            Vector3 position,
            Quaternion rotation)
        {
            if (definition == null)
            {
                Debug.LogWarning("[ProjectilePool] ProjectileDefinition이 없습니다.");
                return null;
            }

            if (definition.projectilePrefab == null)
            {
                Debug.LogWarning($"[ProjectilePool] {definition.name}에 Projectile Prefab이 연결되지 않았습니다.");
                return null;
            }

            if (!ValidatePoolRoot())
            {
                return null;
            }

            Queue<Projectile> pool = GetOrCreatePool(definition);

            Projectile projectile = pool.Count > 0
                ? pool.Dequeue()
                : CreateProjectile(definition);

            if (projectile == null)
            {
                return null;
            }

            Transform projectileTransform = projectile.transform;
            pooledProjectiles.Remove(projectile);
            projectileTransform.SetParent(null, true);
            projectileTransform.SetPositionAndRotation(position, rotation);

            projectile.gameObject.SetActive(true);

            return projectile;
        }

        public void Release(Projectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            ProjectileDefinitionSO definition = projectile.Definition;

            if (definition == null)
            {
                Debug.LogWarning("[ProjectilePool] 반환된 Projectile에 Definition이 없습니다. Destroy 처리합니다.");
                Destroy(projectile.gameObject);
                return;
            }

            ReturnToPool(definition, projectile);
        }

        private Queue<Projectile> GetOrCreatePool(ProjectileDefinitionSO definition)
        {
            if (!pools.TryGetValue(definition, out Queue<Projectile> pool))
            {
                pool = new Queue<Projectile>();
                pools.Add(definition, pool);
            }

            return pool;
        }

        private Projectile CreateProjectile(ProjectileDefinitionSO definition)
        {
            if (definition == null || definition.projectilePrefab == null)
            {
                return null;
            }

            GameObject projectileObject = Instantiate(definition.projectilePrefab);
            projectileObject.name = definition.projectilePrefab.name;

            if (IsValidRuntimeTransform(poolRoot))
            {
                projectileObject.transform.SetParent(poolRoot, false);
            }

            if (!projectileObject.TryGetComponent(out Projectile projectile))
            {
                Debug.LogWarning($"[ProjectilePool] {definition.projectilePrefab.name}에 Projectile 컴포넌트가 없습니다.");
                Destroy(projectileObject);
                return null;
            }

            projectileObject.SetActive(false);
            return projectile;
        }

        private void ReturnToPool(
            ProjectileDefinitionSO definition,
            Projectile projectile)
        {
            if (definition == null || projectile == null)
            {
                return;
            }

            if (!ValidatePoolRoot())
            {
                Destroy(projectile.gameObject);
                return;
            }

            Queue<Projectile> pool = GetOrCreatePool(definition);

            if (!pooledProjectiles.Add(projectile))
            {
                return;
            }

            projectile.gameObject.SetActive(false);

            if (IsValidRuntimeTransform(poolRoot))
            {
                projectile.transform.SetParent(poolRoot, false);
            }

            pool.Enqueue(projectile);
        }
    }
}
