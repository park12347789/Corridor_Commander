using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Waves/Boss Schedule",
        fileName = "BossSchedule")]
    public sealed class BossScheduleSO : ScriptableObject
    {
        [SerializeField, Min(1)] private int everyNWave = 5;
        [SerializeField, Min(0)] private int firstBossWaveIndex = 4;
        [SerializeField, Min(0f)] private float phaseDelay = 4f;
        [SerializeField] private EnemySpawnGroupSO spawnGroup;
        [SerializeField, Min(0)] private int spawnCount = 1;
        [SerializeField, Min(0)] private int additionalCountPerPeriod;
        [SerializeField, Min(0f)] private float spawnInterval = 0f;
        [SerializeField] private List<EnemySpawnEntry> bossEnemies = new List<EnemySpawnEntry>();

        public int EveryNWave => everyNWave;
        public int FirstBossWaveIndex => firstBossWaveIndex;
        public float PhaseDelay => phaseDelay;
        public EnemySpawnGroupSO SpawnGroup => spawnGroup;
        public int SpawnCount => spawnCount;
        public int AdditionalCountPerPeriod => additionalCountPerPeriod;
        public float SpawnInterval => spawnInterval;
        public IReadOnlyList<EnemySpawnEntry> BossEnemies => bossEnemies;

        public bool ShouldAddBoss(int waveIndex)
        {
            if (waveIndex < firstBossWaveIndex || everyNWave <= 0)
            {
                return false;
            }

            return (waveIndex - firstBossWaveIndex) % everyNWave == 0;
        }

        public int GetSpawnCount(int waveIndex)
        {
            if (!ShouldAddBoss(waveIndex))
            {
                return 0;
            }

            int period = ((waveIndex - firstBossWaveIndex) / everyNWave) + 1;
            return Mathf.Max(0, spawnCount) + Mathf.Max(0, additionalCountPerPeriod) * period;
        }
    }
}
