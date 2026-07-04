using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Waves/Periodic Wave Modifier",
        fileName = "PeriodicWaveModifier")]
    public sealed class PeriodicWaveModifierSO : ScriptableObject
    {
        [SerializeField, Min(1)] private int everyNWave = 3;
        [SerializeField, Min(0)] private int firstWaveIndex = 2;
        [SerializeField, Min(0)] private int extraCountPerPeriod = 1;
        [SerializeField] private List<WaveSpawnPhase> extraPhases = new List<WaveSpawnPhase>();

        public int EveryNWave => everyNWave;
        public int FirstWaveIndex => firstWaveIndex;
        public int ExtraCountPerPeriod => extraCountPerPeriod;
        public IReadOnlyList<WaveSpawnPhase> ExtraPhases => extraPhases;

        public bool AppliesTo(int waveIndex)
        {
            if (waveIndex < firstWaveIndex || everyNWave <= 0)
            {
                return false;
            }

            return (waveIndex - firstWaveIndex) % everyNWave == 0;
        }

        public int GetPeriodCountBonus(int waveIndex)
        {
            if (!AppliesTo(waveIndex) || extraCountPerPeriod <= 0)
            {
                return 0;
            }

            return (((waveIndex - firstWaveIndex) / everyNWave) + 1) * extraCountPerPeriod;
        }
    }
}
