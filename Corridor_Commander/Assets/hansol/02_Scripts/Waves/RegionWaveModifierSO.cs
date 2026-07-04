using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Waves/Region Wave Modifier",
        fileName = "RegionWaveModifier")]
    public sealed class RegionWaveModifierSO : ScriptableObject
    {
        [SerializeField] private string regionId = "region";
        [SerializeField, Min(0)] private int minWaveOffsetAfterOpen = 1;
        [SerializeField] private RegionWaveApplyMode applyMode = RegionWaveApplyMode.AllFutureWaves;
        [SerializeField] private List<WaveSpawnPhase> extraPhases = new List<WaveSpawnPhase>();

        public string RegionId => regionId;
        public int MinWaveOffsetAfterOpen => minWaveOffsetAfterOpen;
        public RegionWaveApplyMode ApplyMode => applyMode;
        public IReadOnlyList<WaveSpawnPhase> ExtraPhases => extraPhases;

        public bool AppliesTo(StageProgressState progressState, int waveIndex)
        {
            if (progressState == null || string.IsNullOrWhiteSpace(regionId))
            {
                return false;
            }

            if (!progressState.TryGetRegionOpenedWave(regionId, out int openedWaveIndex))
            {
                return false;
            }

            int firstAllowedWave = openedWaveIndex + minWaveOffsetAfterOpen;
            if (waveIndex < firstAllowedWave)
            {
                return false;
            }

            return applyMode != RegionWaveApplyMode.NextWaveOnly || waveIndex == firstAllowedWave;
        }
    }

    public enum RegionWaveApplyMode
    {
        NextWaveOnly,
        AllFutureWaves
    }
}
