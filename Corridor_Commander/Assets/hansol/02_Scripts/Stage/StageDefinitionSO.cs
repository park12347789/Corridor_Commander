using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Stage/Stage Definition",
        fileName = "StageDefinition")]
    public sealed class StageDefinitionSO : ScriptableObject
    {
        [SerializeField] private string stageId = "stage_01";
        [SerializeField] private string displayName = "Stage 01";
        [SerializeField] private EnemyWaveDefinition[] waves;
        [SerializeField] private TreasureChestRewardTable rewardTable;
        [SerializeField] private SupportTruckShopCatalogSO supportTruckCatalog;
        [SerializeField] private BuildableDefinitionSO[] buildableDefinitions;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private EnemyCatalogSO enemyCatalog;
        [SerializeField] private DifficultyProgressionSO difficultyProgression;
        [SerializeField] private RegionWaveModifierSO[] regionWaveModifiers;
        [SerializeField] private PeriodicWaveModifierSO[] periodicWaveModifiers;
        [SerializeField] private BossScheduleSO bossSchedule;

        public string StageId => stageId;
        public string DisplayName => displayName;
        public IReadOnlyList<EnemyWaveDefinition> Waves => waves;
        public TreasureChestRewardTable RewardTable => rewardTable;
        public SupportTruckShopCatalogSO SupportTruckCatalog => supportTruckCatalog;
        public IReadOnlyList<BuildableDefinitionSO> BuildableDefinitions => buildableDefinitions;
        public GameObject EnemyPrefab => enemyPrefab;
        public EnemyCatalogSO EnemyCatalog => enemyCatalog;
        public DifficultyProgressionSO DifficultyProgression => difficultyProgression;
        public IReadOnlyList<RegionWaveModifierSO> RegionWaveModifiers => regionWaveModifiers;
        public IReadOnlyList<PeriodicWaveModifierSO> PeriodicWaveModifiers => periodicWaveModifiers;
        public BossScheduleSO BossSchedule => bossSchedule;
    }
}
