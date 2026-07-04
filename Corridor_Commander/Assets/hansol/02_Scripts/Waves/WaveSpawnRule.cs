using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [Serializable]
    public sealed class WaveSpawnRule
    {
        [SerializeField] private EnemySpawnGroupSO spawnGroup;
        [SerializeField, HideInInspector] private string spawnerNameContains = "Enemy_SpawnPoint_RED";
        [SerializeField] [Min(0)] private int spawnCount = 3;
        [SerializeField] [Min(0f)] private float spawnInterval = 0.75f;
        [SerializeField] private List<EnemySpawnEntry> enemyEntries = new List<EnemySpawnEntry>();

        public EnemySpawnGroupSO SpawnGroup => spawnGroup;
        public int SpawnCount => spawnCount;
        public float SpawnInterval => spawnInterval;
        public IReadOnlyList<EnemySpawnEntry> EnemyEntries => enemyEntries;
        public string LegacySpawnerNameContains => spawnerNameContains;
    }
}
