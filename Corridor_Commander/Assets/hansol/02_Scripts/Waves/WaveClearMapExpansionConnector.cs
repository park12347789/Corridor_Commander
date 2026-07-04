using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class WaveClearMapExpansionConnector : MonoBehaviour
    {
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private MapExpansionDoorOpener[] doorsByWaveIndex;
        [SerializeField] private bool openOnlyClosedDoors = true;

        private void Awake()
        {
            ResolveWaveDirector();
        }

        private void OnEnable()
        {
            ResolveWaveDirector();
            if (waveDirector != null)
            {
                waveDirector.WaveCleared -= HandleWaveCleared;
                waveDirector.WaveCleared += HandleWaveCleared;
            }
        }

        private void OnDisable()
        {
            if (waveDirector != null)
            {
                waveDirector.WaveCleared -= HandleWaveCleared;
            }
        }

        private void HandleWaveCleared(int waveIndex, EnemyWaveDefinition wave)
        {
            if (doorsByWaveIndex == null || waveIndex < 0 || waveIndex >= doorsByWaveIndex.Length)
            {
                return;
            }

            MapExpansionDoorOpener door = doorsByWaveIndex[waveIndex];
            if (door == null)
            {
                return;
            }

            if (!openOnlyClosedDoors || !door.IsOpen)
            {
                door.Open();
            }
        }

        private void ResolveWaveDirector()
        {
            if (waveDirector == null)
            {
                waveDirector = FindFirstObjectByType<WaveDirector>(FindObjectsInactive.Include);
            }
        }
    }
}
