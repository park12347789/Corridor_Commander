using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class StageLayoutRoot : MonoBehaviour
    {
        [SerializeField] private Transform mainTarget;
        [SerializeField] private EnemySpawner[] enemySpawners;
        [SerializeField] private EnemyRoute[] enemyRoutes;
        [SerializeField] private PlacementPoint[] placementPoints;
        [SerializeField] private MapExpansionDoorOpener[] doors;
        [SerializeField] private MapExpansionActivationTargetGroup[] activationGroups;
        [SerializeField] private TreasureChest[] treasureChests;
        [SerializeField] private SupportTruckShop[] supportTruckShops;

        public Transform MainTarget => mainTarget;
        public EnemySpawner[] EnemySpawners => enemySpawners;
        public EnemyRoute[] EnemyRoutes => enemyRoutes;
        public PlacementPoint[] PlacementPoints => placementPoints;
        public MapExpansionDoorOpener[] Doors => doors;
        public MapExpansionActivationTargetGroup[] ActivationGroups => activationGroups;
        public TreasureChest[] TreasureChests => treasureChests;
        public SupportTruckShop[] SupportTruckShops => supportTruckShops;

        public void CollectChildren()
        {
            enemySpawners = GetComponentsInChildren<EnemySpawner>(true);
            enemyRoutes = GetComponentsInChildren<EnemyRoute>(true);
            placementPoints = GetComponentsInChildren<PlacementPoint>(true);
            doors = GetComponentsInChildren<MapExpansionDoorOpener>(true);
            activationGroups = GetComponentsInChildren<MapExpansionActivationTargetGroup>(true);
            treasureChests = GetComponentsInChildren<TreasureChest>(true);
            supportTruckShops = GetComponentsInChildren<SupportTruckShop>(true);

            if (mainTarget == null)
            {
                EnemyGoalZone goalZone = GetComponentInChildren<EnemyGoalZone>(true);
                if (goalZone != null)
                {
                    mainTarget = goalZone.transform;
                }
            }
        }

        public void ApplyDefinition(StageDefinitionSO definition)
        {
            if (definition == null)
            {
                return;
            }

            ApplyEnemyPrefab(definition.EnemyPrefab);
            ApplyBuildables(definition);
            ApplyRewards(definition);
            ApplySupportTruck(definition);
        }

        private void ApplyEnemyPrefab(GameObject enemyPrefab)
        {
            if (enemyPrefab == null || enemySpawners == null)
            {
                return;
            }

            for (int i = 0; i < enemySpawners.Length; i++)
            {
                enemySpawners[i]?.ConfigureEnemyPrefab(enemyPrefab);
            }
        }

        private void ApplyBuildables(StageDefinitionSO definition)
        {
            if (placementPoints == null || definition.BuildableDefinitions == null)
            {
                return;
            }

            for (int i = 0; i < placementPoints.Length; i++)
            {
                placementPoints[i]?.ConfigureBuildableDefinitions(definition.BuildableDefinitions);
            }
        }

        private void ApplyRewards(StageDefinitionSO definition)
        {
            if (definition.RewardTable == null || treasureChests == null)
            {
                return;
            }

            for (int i = 0; i < treasureChests.Length; i++)
            {
                treasureChests[i]?.ConfigureRewards(definition.RewardTable, i);
            }
        }

        private void ApplySupportTruck(StageDefinitionSO definition)
        {
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                CollectChildren();
            }
        }
    }
}
