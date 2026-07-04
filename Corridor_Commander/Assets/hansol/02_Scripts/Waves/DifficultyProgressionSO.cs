using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Waves/Difficulty Progression",
        fileName = "DifficultyProgression")]
    public sealed class DifficultyProgressionSO : ScriptableObject
    {
        [SerializeField] private AnimationCurve healthMultiplierByWave = AnimationCurve.Linear(0f, 1f, 10f, 2f);
        [SerializeField] private AnimationCurve spawnCountMultiplierByWave = AnimationCurve.Linear(0f, 1f, 10f, 1.5f);
        [SerializeField] private AnimationCurve spawnIntervalMultiplierByWave = AnimationCurve.Linear(0f, 1f, 10f, 0.75f);

        public float GetHealthMultiplier(int waveIndex)
        {
            return EvaluateMultiplier(healthMultiplierByWave, waveIndex);
        }

        public float GetSpawnCountMultiplier(int waveIndex)
        {
            return EvaluateMultiplier(spawnCountMultiplierByWave, waveIndex);
        }

        public float GetSpawnIntervalMultiplier(int waveIndex)
        {
            return EvaluateMultiplier(spawnIntervalMultiplierByWave, waveIndex);
        }

        private static float EvaluateMultiplier(AnimationCurve curve, int waveIndex)
        {
            if (curve == null || curve.length == 0)
            {
                return 1f;
            }

            return Mathf.Max(0.01f, curve.Evaluate(Mathf.Max(0, waveIndex)));
        }
    }
}
