using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    public sealed class WaveSpawnPlan
    {
        private readonly List<WaveSpawnPhasePlan> phases = new List<WaveSpawnPhasePlan>();

        public IReadOnlyList<WaveSpawnPhasePlan> Phases => phases;

        public void AddPhase(WaveSpawnPhasePlan phase)
        {
            if (phase != null && phase.Rules.Count > 0)
            {
                phases.Add(phase);
            }
        }
    }

    public sealed class WaveSpawnPhasePlan
    {
        private readonly List<WaveSpawnRulePlan> rules = new List<WaveSpawnRulePlan>();

        public WaveSpawnPhasePlan(float delay, string announcementText)
        {
            Delay = Mathf.Max(0f, delay);
            AnnouncementText = announcementText;
        }

        public float Delay { get; }
        public string AnnouncementText { get; }
        public List<WaveSpawnRulePlan> Rules => rules;
    }

    public sealed class WaveSpawnRulePlan
    {
        private readonly List<EnemySpawnEntry> enemyEntries = new List<EnemySpawnEntry>();

        public WaveSpawnRulePlan(
            EnemySpawnGroupSO spawnGroup,
            string spawnerNameContains,
            int spawnCount,
            float spawnInterval,
            float healthMultiplier,
            IReadOnlyList<EnemySpawnEntry> entries)
        {
            SpawnGroup = spawnGroup;
            SpawnerNameContains = spawnerNameContains;
            SpawnCount = Mathf.Max(0, spawnCount);
            SpawnInterval = Mathf.Max(0f, spawnInterval);
            HealthMultiplier = Mathf.Max(0.01f, healthMultiplier);

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null && entries[i].Enemy != null && entries[i].Weight > 0f)
                    {
                        enemyEntries.Add(entries[i]);
                    }
                }
            }
        }

        public EnemySpawnGroupSO SpawnGroup { get; }
        public string SpawnerNameContains { get; }
        public int SpawnCount { get; }
        public float SpawnInterval { get; }
        public float HealthMultiplier { get; }
        public IReadOnlyList<EnemySpawnEntry> EnemyEntries => enemyEntries;

        public EnemyDefinitionSO PickEnemy()
        {
            if (enemyEntries.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < enemyEntries.Count; i++)
            {
                totalWeight += enemyEntries[i].Weight;
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.Range(0f, totalWeight);
            for (int i = 0; i < enemyEntries.Count; i++)
            {
                roll -= enemyEntries[i].Weight;
                if (roll <= 0f)
                {
                    return enemyEntries[i].Enemy;
                }
            }

            return enemyEntries[enemyEntries.Count - 1].Enemy;
        }
    }
}
