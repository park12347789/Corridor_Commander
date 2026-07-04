using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(menuName = "Corridor Commander/Waves/Enemy Wave Definition")]
    public sealed class EnemyWaveDefinition : ScriptableObject
    {
        [SerializeField] private string waveId = "Wave_01";
        [SerializeField] private WaveType waveType = WaveType.Normal;
        [SerializeField] [Min(1f)] private float autoStartDelay = 30f;
        [SerializeField] private List<WaveSpawnRule> spawnRules = new List<WaveSpawnRule>();
        [SerializeField] private List<WaveSpawnPhase> spawnPhases = new List<WaveSpawnPhase>();

        public string WaveId => waveId;
        public WaveType WaveType => waveType;
        public float AutoStartDelay => autoStartDelay;
        public IReadOnlyList<WaveSpawnRule> SpawnRules => spawnRules;
        public IReadOnlyList<WaveSpawnPhase> SpawnPhases => spawnPhases;

        public IReadOnlyList<WaveSpawnPhase> GetResolvedPhases()
        {
            return spawnPhases != null && spawnPhases.Count > 0
                ? spawnPhases
                : new[] { new WaveSpawnPhase(0f, string.Empty, spawnRules) };
        }
    }
}
