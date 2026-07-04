using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Enemies/Enemy Definition",
        fileName = "EnemyDefinition")]
    public sealed class EnemyDefinitionSO : ScriptableObject
    {
        [SerializeField] private string enemyId = "enemy";
        [SerializeField] private string displayName = "Enemy";
        [SerializeField] private EnemyRank rank;
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0)] private int unlockWaveIndex;
        [SerializeField, Min(0f)] private float baseWeight = 1f;
        [SerializeField, Min(0.01f)] private float healthMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float moveSpeedMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float visualScaleMultiplier = 1f;
        [SerializeField, Min(0)] private int rewardValue = 1;

        public string EnemyId => enemyId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public EnemyRank Rank => rank;
        public GameObject Prefab => prefab;
        public int UnlockWaveIndex => unlockWaveIndex;
        public float BaseWeight => baseWeight;
        public float HealthMultiplier => healthMultiplier;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public float VisualScaleMultiplier => visualScaleMultiplier;
        public int RewardValue => rewardValue;

        public bool IsUnlocked(int waveIndex)
        {
            return waveIndex >= unlockWaveIndex;
        }
    }
}
