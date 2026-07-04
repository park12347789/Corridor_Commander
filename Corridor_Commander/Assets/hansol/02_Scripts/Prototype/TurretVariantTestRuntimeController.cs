using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TurretVariantTestRuntimeController : MonoBehaviour
    {
        [SerializeField] private bool spawnRuntimeEnemies = true;
        [SerializeField] private int desiredEnemyCount = 6;
        [SerializeField] private float spawnInterval = 1.5f;
        [SerializeField] private float enemyHitPoints = 80f;

        private readonly List<Health> spawnedEnemies = new List<Health>();
        private Transform runtimeEnemyRoot;
        private Transform enemyGoal;
        private Material runtimeEnemyMaterial;
        private float nextSpawnTime;

        private void Awake()
        {
            InstalledSkillRegistry unused = InstalledSkillRegistry.Instance;

            if (spawnRuntimeEnemies)
            {
                EnsureRuntimeEnemyAnchors();
                nextSpawnTime = Time.time;
            }
        }

        private void Start()
        {
            ApplyMortarSlotOrder();
        }

        private void Update()
        {
            if (!spawnRuntimeEnemies)
            {
                return;
            }

            PruneSpawnedEnemies();
            if (spawnedEnemies.Count >= Mathf.Max(0, desiredEnemyCount) || Time.time < nextSpawnTime)
            {
                return;
            }

            SpawnRuntimeEnemy(spawnedEnemies.Count);
            nextSpawnTime = Time.time + Mathf.Max(0.1f, spawnInterval);
        }

        private static void ApplyMortarSlotOrder()
        {
            MortarSkillRole[] mortarRoles = FindObjectsByType<MortarSkillRole>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (mortarRoles == null || mortarRoles.Length == 0)
            {
                return;
            }

            List<SkillDefinitionSO> orderedSkills = new List<SkillDefinitionSO>();
            AddSkillByAssetName(mortarRoles, "Skill_Mortar", orderedSkills);
            AddSkillByAssetName(mortarRoles, "Skill_Mortar_Rapid", orderedSkills);
            AddSkillByAssetName(mortarRoles, "Skill_Mortar_Heavy", orderedSkills);
            InstalledSkillRegistry.Instance.SetSkillOrder(orderedSkills);
        }

        private static void AddSkillByAssetName(
            MortarSkillRole[] mortarRoles,
            string assetName,
            List<SkillDefinitionSO> orderedSkills)
        {
            for (int i = 0; i < mortarRoles.Length; i++)
            {
                SkillDefinitionSO skill = mortarRoles[i] != null ? mortarRoles[i].SkillDefinition : null;
                if (skill == null || skill.name != assetName || orderedSkills.Contains(skill))
                {
                    continue;
                }

                orderedSkills.Add(skill);
                return;
            }
        }

        private void EnsureRuntimeEnemyAnchors()
        {
            GameObject enemyRootObject = GameObject.Find("RuntimeEnemies");
            if (enemyRootObject == null)
            {
                enemyRootObject = new GameObject("RuntimeEnemies");
            }

            runtimeEnemyRoot = enemyRootObject.transform;

            GameObject goalObject = GameObject.Find("RuntimeEnemyGoal");
            if (goalObject == null)
            {
                goalObject = new GameObject("RuntimeEnemyGoal");
                goalObject.transform.SetParent(runtimeEnemyRoot, false);
                goalObject.transform.position = new Vector3(0f, 0.05f, -5.5f);
            }

            enemyGoal = goalObject.transform;
        }

        private void SpawnRuntimeEnemy(int slotIndex)
        {
            Vector3 spawnPosition = new Vector3(-10f + (slotIndex % 6) * 4f, 0.9f, 10f);
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "RuntimeEnemy_" + (Time.frameCount % 10000).ToString("0000");
            enemy.transform.SetParent(runtimeEnemyRoot, false);
            enemy.transform.position = spawnPosition;
            enemy.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
            enemy.layer = ResolveEnemyLayer();

            Renderer renderer = enemy.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = ResolveRuntimeEnemyMaterial();
            }

            Health health = enemy.AddComponent<Health>();
            health.Configure(enemyHitPoints, true);
            enemy.AddComponent<StatusEffectReceiver>();
            enemy.AddComponent<CharacterController>();
            enemy.AddComponent<DirectCharacterMovementMotor>();

            EnemyMovementController movement = enemy.AddComponent<EnemyMovementController>();
            movement.SetTarget(enemyGoal);
            spawnedEnemies.Add(health);
        }

        private void PruneSpawnedEnemies()
        {
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] == null || !spawnedEnemies[i].IsAlive)
                {
                    spawnedEnemies.RemoveAt(i);
                }
            }
        }

        private Material ResolveRuntimeEnemyMaterial()
        {
            if (runtimeEnemyMaterial != null)
            {
                return runtimeEnemyMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            runtimeEnemyMaterial = new Material(shader)
            {
                color = new Color(0.75f, 0.08f, 0.08f, 1f)
            };
            return runtimeEnemyMaterial;
        }

        private static int ResolveEnemyLayer()
        {
            int layer = LayerMask.NameToLayer("Enemy");
            return layer >= 0 ? layer : 7;
        }
    }
}
