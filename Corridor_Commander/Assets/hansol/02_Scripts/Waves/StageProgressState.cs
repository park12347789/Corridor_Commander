using System.Collections.Generic;

namespace CorridorCommander
{
    public enum StageSectorState
    {
        Locked,
        Breached,
        Contested,
        Secured
    }

    public sealed class StageProgressState
    {
        private readonly Dictionary<string, int> openedRegions = new Dictionary<string, int>();
        private readonly Dictionary<string, StageSectorState> sectorStates = new Dictionary<string, StageSectorState>();

        public int CurrentWaveIndex { get; private set; }

        public void SetCurrentWaveIndex(int waveIndex)
        {
            CurrentWaveIndex = waveIndex;
        }

        public void MarkRegionOpened(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId))
            {
                return;
            }

            if (!openedRegions.ContainsKey(regionId))
            {
                openedRegions.Add(regionId, CurrentWaveIndex);
            }

            SetSectorState(regionId, StageSectorState.Breached);
        }

        public bool TryGetRegionOpenedWave(string regionId, out int waveIndex)
        {
            if (string.IsNullOrWhiteSpace(regionId))
            {
                waveIndex = -1;
                return false;
            }

            return openedRegions.TryGetValue(regionId, out waveIndex);
        }

        public StageSectorState GetSectorState(string sectorId)
        {
            if (string.IsNullOrWhiteSpace(sectorId))
            {
                return StageSectorState.Locked;
            }

            return sectorStates.TryGetValue(sectorId, out StageSectorState state)
                ? state
                : StageSectorState.Locked;
        }

        public void MarkSectorContested(string sectorId)
        {
            SetSectorState(sectorId, StageSectorState.Contested);
        }

        public void MarkSectorSecured(string sectorId)
        {
            SetSectorState(sectorId, StageSectorState.Secured);
        }

        private void SetSectorState(string sectorId, StageSectorState state)
        {
            if (string.IsNullOrWhiteSpace(sectorId))
            {
                return;
            }

            sectorStates[sectorId] = state;
        }
    }
}
