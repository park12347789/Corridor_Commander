using System.Collections.Generic;
using CorridorCommander;
using CorridorCommander.PlayerControl;
using CorridorCommander.PlayerItems;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CorridorCommander.EditorTools
{
    public static class StageRoomCorridorSampleBuilder
    {
        private const string ScenePath = "Assets/hansol/01_Scenes/test1/stage_room_corridor_samples.unity";
        private const string StagePrefabFolder = "Assets/hansol/03_Prefabs/Stage";
        private const string StageSettingsFolder = "Assets/hansol/09_Settings/Stage";
        private const string ConstructionSettingsFolder = "Assets/hansol/09_Settings/Construction";
        private const string WaveSettingsFolder = "Assets/hansol/09_Settings/Waves";
        private const string ArtifactSettingsFolder = "Assets/hansol/09_Settings/Artifacts";
        private const string ArtifactIconFolder = ArtifactSettingsFolder + "/Icons";
        private const string WaveRewardTablePath = ArtifactSettingsFolder + "/Wave_BossRewardTable.asset";
        private const string WaveRewardStatPointIconPath = "Assets/hansol/04_Art/UI/Icons/Generated/icon_stat_point.png";
        private const string WaveRewardMoneyIconPath = "Assets/hansol/04_Art/UI/Icons/Generated/icon_reward_credits.png";
        private const string WaveRewardKillDataIconPath = "Assets/hansol/07_UI/Icons/Icon_ExperienceDataCore.png";
        private const string ArtifactTurretPath = ArtifactSettingsFolder + "/Artifact_TurretLens.asset";
        private const string ArtifactMortarPath = ArtifactSettingsFolder + "/Artifact_MortarCore.asset";
        private const string ArtifactSquadPath = ArtifactSettingsFolder + "/Artifact_SquadRelay.asset";
        private const string ArtifactPlayerPath = ArtifactSettingsFolder + "/Artifact_PlayerExoFrame.asset";
        private const string MouseCursorIconPath = "Assets/hansol/04_Art/UI/Icons/Generated/icon_mouse_cursor_sf_casual.png";
        private const string MapHudFramePrefabPath = "Assets/90_ThirdParty/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_Component_Frames/BasicFrame_Round12_Sky.prefab";
        private const string MapHudContentPrefabPath = "Assets/90_ThirdParty/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_Component_Frames/BasicFrame_Round12_Blue.prefab";
        private const string MapHudStatusFramePrefabPath = "Assets/90_ThirdParty/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_Component_Frames/BannerFrame02_Pattern_Sky.prefab";
        private const string MapHudMarkerPath = "Assets/hansol/04_Art/UI/Generated/map_marker_dot.png";
        private const string MapHudMapIconPath = "Assets/hansol/04_Art/UI/Generated/map_icon_imagegen.png";
        private const string MapHudRangeRingPath = "Assets/90_ThirdParty/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites/Components/UI_Etc/Alert_Circle_l_Bg.png";
        private const string MapHudShadowPath = "Assets/90_ThirdParty/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites/Components/Icon_Chest/Frame/BubbleFrame02_BgShadow.png";
        private const string PopupBackgroundPath = "Assets/90_ThirdParty/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites/Components/Popup/Popup02~09_Topber_White_Bg.png";
        private const string StageRuntimePrefabPath = StagePrefabFolder + "/StageRuntime.prefab";
        private const string StageLayoutPrefabPath = StagePrefabFolder + "/StageLayout_RoomCorridorSamples.prefab";
        private const string MainCanvasPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvas.prefab";
        private const string StageDefinitionPath = StageSettingsFolder + "/Stage_RoomCorridorSamples.asset";
        private const string BuildableTurretPath = ConstructionSettingsFolder + "/Buildable_Turret.asset";
        private const string BuildableTurretRapidPath = ConstructionSettingsFolder + "/Buildable_Turret_Rapid.asset";
        private const string BuildableTurretLongRangePath = ConstructionSettingsFolder + "/Buildable_Turret_LongRange.asset";
        private const string BuildableBarricadePath = ConstructionSettingsFolder + "/Buildable_Barricade.asset";
        private const string BuildableSawTrapPath = ConstructionSettingsFolder + "/Buildable_SawTrap.asset";
        private const string BuildableMortarRapidPath = ConstructionSettingsFolder + "/Buildable_Mortar_Rapid.asset";
        private const string BuildableMortarHeavyPath = ConstructionSettingsFolder + "/Buildable_Mortar_Heavy.asset";
        private const string Wave01Path = WaveSettingsFolder + "/StageSample_Wave_01.asset";
        private const string Wave02Path = WaveSettingsFolder + "/StageSample_Wave_02.asset";
        private const string Wave03Path = WaveSettingsFolder + "/StageSample_Wave_03.asset";
        private const string Wave04Path = WaveSettingsFolder + "/StageSample_Wave_04.asset";
        private const string Wave05Path = WaveSettingsFolder + "/StageSample_Wave_05.asset";
        private const string EnemyCatalogPath = "Assets/hansol/09_Settings/Enemies/EnemyCatalog_MainScene.asset";
        private const string DifficultyProgressionPath = WaveSettingsFolder + "/Difficulty_MainScene.asset";
        private const string BossSchedulePath = WaveSettingsFolder + "/BossSchedule_MainScene.asset";
        private const string KoreanFontPath = "Assets/hansol/09_Settings/Font/BMJUA/BMJUA_ttf.ttf";

        private const string EnemyPrefabPath = "Assets/hansol/03_Prefabs/Enemy_Zombie_Basic.prefab";
        private const string EnemyGoalPrefabPath = "Assets/hansol/03_Prefabs/Enemy_Goal_YELLOW.prefab";
        private const string EnemySpawnerPrefabPath = "Assets/hansol/03_Prefabs/Enemy_SpawnPoint_RED.prefab";
        private const string EnemyRoutePointPrefabPath = "Assets/hansol/03_Prefabs/EnemyRoutePoint.prefab";
        private const string PlacementPointPrefabPath = "Assets/hansol/03_Prefabs/PlacementPoint.prefab";
        private const string WallPlacementPointPrefabPath = "Assets/hansol/03_Prefabs/PlacementPoint_WallMount.prefab";
        private const string MapExpansionGatePrefabPath = "Assets/hansol/03_Prefabs/MapExpansionGate.prefab";
        private const string TreasureChestPrefabPath = "Assets/hansol/03_Prefabs/TreasureChest_Basic.prefab";
        private const string SupportTruckPrefabPath = "Assets/hansol/03_Prefabs/TEMP_SupportTruck_Shop.prefab";
        private const string PlayerPrefabPath = "Assets/hansol/03_Prefabs/Player/PlayerSetup.prefab";
        private const string TurretPrefabPath = "Assets/hansol/03_Prefabs/Turret_Basic.prefab";
        private const string TurretRapidPrefabPath = "Assets/hansol/03_Prefabs/Turret_Rapid.prefab";
        private const string TurretLongRangePrefabPath = "Assets/hansol/03_Prefabs/Turret_LongRange.prefab";
        private const string BarricadePrefabPath = "Assets/hansol/03_Prefabs/Barricade_Basic.prefab";
        private const string SawTrapPrefabPath = "Assets/hansol/03_Prefabs/SawTrap_Turret_Yellow.prefab";
        private const string MortarDefinitionPath = "Assets/hansol/09_Settings/Construction/Buildable_Mortar.asset";
        private const string MortarRapidPrefabPath = "Assets/hansol/03_Prefabs/TEMP_Mortar_Rapid.prefab";
        private const string MortarHeavyPrefabPath = "Assets/hansol/03_Prefabs/TEMP_Mortar_Heavy.prefab";
        private const string RoleMortarRapidPath = "Assets/hansol/09_Settings/Skills/Role_Mortar_Rapid.asset";
        private const string RoleMortarHeavyPath = "Assets/hansol/09_Settings/Skills/Role_Mortar_Heavy.asset";
        private const string RewardTablePath = "Assets/hansol/09_Settings/Rewards/Test_TreasureChestRewards.asset";
        private const string SupportTruckCatalogPath = "Assets/hansol/09_Settings/Shops/SupportTruck_Catalog.asset";
        private const string PlatformerMaterialPath = "Assets/90_ThirdParty/KayKit 1/Packs/KayKit - Platformer Pack (for Unity)/Materials/platformer.mat";
        private const string ResourceMaterialPath = "Assets/90_ThirdParty/KayKit 1/Packs/Bits/KayKit - Resource Bits (for Unity)/Materials/resource.mat";
        private const string ResourcePrefabFolder = "Assets/90_ThirdParty/KayKit 1/Packs/Bits/KayKit - Resource Bits (for Unity)/Prefabs";
        private const string PlatformerNeutralPrefabFolder = "Assets/90_ThirdParty/KayKit 1/Packs/KayKit - Platformer Pack (for Unity)/Prefabs/neutral";

        private const float PlanScale = 4f;
        private const float WallHeight = 8f;
        private const float CorridorWallHeight = 7.2f;
        private const float WallThickness = 1.2f;
        private const float DoorOpeningWidth = 16f;

        private sealed class StageZone
        {
            public StageZone(GameObject root, Transform geometry, Transform gameplay, Transform props)
            {
                Root = root;
                Geometry = geometry;
                Gameplay = gameplay;
                Props = props;
            }

            public GameObject Root { get; }
            public Transform Geometry { get; }
            public Transform Gameplay { get; }
            public Transform Props { get; }
        }

        [System.Flags]
        private enum RoomOpenings
        {
            None = 0,
            North = 1,
            South = 2,
            East = 4,
            West = 8,
            All = North | South | East | West
        }

        private readonly struct StageFootprint
        {
            public StageFootprint(string name, float x, float z, float width, float depth, bool isConnector, RoomOpenings openings)
            {
                Name = name;
                X = x;
                Z = z;
                Width = width;
                Depth = depth;
                IsConnector = isConnector;
                Openings = openings;
            }

            public string Name { get; }
            public float X { get; }
            public float Z { get; }
            public float Width { get; }
            public float Depth { get; }
            public bool IsConnector { get; }
            public RoomOpenings Openings { get; }
            public float MinX => X - Width * 0.5f;
            public float MaxX => X + Width * 0.5f;
            public float MinZ => Z - Depth * 0.5f;
            public float MaxZ => Z + Depth * 0.5f;
            public float ConnectorCrossSize => Width >= Depth ? Depth : Width;
        }

        private readonly struct StageConnection
        {
            public StageConnection(string connectorName, string roomName, RoomOpenings roomSide)
            {
                ConnectorName = connectorName;
                RoomName = roomName;
                RoomSide = roomSide;
            }

            public string ConnectorName { get; }
            public string RoomName { get; }
            public RoomOpenings RoomSide { get; }
        }

        [MenuItem("Corridor Commander/Stage/Build Room Corridor Samples")]
        public static void Build()
        {
            BuildInternal(askBeforeReplacingOpenScene: true);
        }

        public static void BuildForAutomation()
        {
            BuildInternal(askBeforeReplacingOpenScene: false);
        }

        [MenuItem("Corridor Commander/Stage/Validate Room Corridor Samples")]
        public static void Validate()
        {
            ValidateInternal(askBeforeOpeningScene: true);
        }

        public static void ValidateForAutomation()
        {
            ValidateInternal(askBeforeOpeningScene: false);
        }

        private static void ValidateInternal(bool askBeforeOpeningScene)
        {
            if (askBeforeOpeningScene && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> failures = new List<string>();

            if (!scene.IsValid())
            {
                failures.Add($"Scene could not be opened: {ScenePath}");
            }

            RequireSceneTransform("StageRuntime", failures);
            RequireSceneTransform("RealtimeMapHud", failures);
            RequireSceneTransform("StageLayout_RoomCorridorSamples", failures);
            RequireSceneTransform("Stage_Zones", failures);
            RequireSceneTransform("Stage1_SupportTruck_StartSupply", failures);
            RequireSceneTransform("Stage1_Player_Start", failures);
            RequireSceneTransform("Stage1_Final_Goal_YELLOW", failures);
            RequireSceneTransform("01D_EntrySupplyPocket", failures);
            RequireSceneTransform("02C_LowerWorkshopPocket", failures);
            RequireSceneTransform("06C_FinalApproachNorthBay", failures);
            RequireSceneTransform("06E_FinalApproachSouthBay", failures);
            ValidateStageOneFootprints(failures);

            StageLayoutRoot layoutRoot = Object.FindFirstObjectByType<StageLayoutRoot>(FindObjectsInactive.Include);
            StageInitializer initializer = Object.FindFirstObjectByType<StageInitializer>(FindObjectsInactive.Include);
            StageRuntime runtime = Object.FindFirstObjectByType<StageRuntime>(FindObjectsInactive.Include);
            StageDefinitionSO stageDefinition = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(StageDefinitionPath);

            if (layoutRoot == null)
            {
                failures.Add("StageLayoutRoot is missing.");
            }
            else
            {
                layoutRoot.CollectChildren();
                RequireLayoutCount("placement points", 24, layoutRoot.PlacementPoints?.Length ?? 0, failures);
                RequireLayoutCount("doors", 7, layoutRoot.Doors?.Length ?? 0, failures);
                RequireLayoutCount("activation groups", 7, layoutRoot.ActivationGroups?.Length ?? 0, failures);
                RequireLayoutCount("treasure chests", 7, layoutRoot.TreasureChests?.Length ?? 0, failures);
                RequireLayoutCount("support truck shops", 1, layoutRoot.SupportTruckShops?.Length ?? 0, failures);
                RequireLayoutCount("enemy spawners", 5, layoutRoot.EnemySpawners?.Length ?? 0, failures);
                RequireLayoutCount("enemy routes", 5, layoutRoot.EnemyRoutes?.Length ?? 0, failures);
            }

            if (initializer == null)
            {
                failures.Add("StageInitializer is missing.");
            }
            else
            {
                SerializedObject initializerSo = new SerializedObject(initializer);
                if (GetObjectReference(initializerSo, "stageDefinition") == null)
                {
                    failures.Add("StageInitializer.stageDefinition is missing.");
                }

                if (GetObjectReference(initializerSo, "runtime") == null)
                {
                    failures.Add("StageInitializer.runtime is missing.");
                }

                if (GetObjectReference(initializerSo, "layoutRoot") == null)
                {
                    failures.Add("StageInitializer.layoutRoot is missing.");
                }
            }

            if (runtime == null)
            {
                failures.Add("StageRuntime is missing.");
            }
            else
            {
                ValidateStageRuntime(runtime, failures);
            }

            ValidateStageDefinition(stageDefinition, failures);
            ValidateZoneRoots(failures);
            ValidateActivationGroups(failures);
            ValidateDoorVisuals(failures);
            ValidateEnemySpawners(failures);
            ValidateRouteWaypointGeometry(failures);
            ValidateMapNavigationLinks(failures);
            ValidateEnemyRouteNavMeshPaths(failures);
            ValidateSupportTruckStartDistance(failures);

            int missingScriptCount = CountMissingScripts();
            if (missingScriptCount > 0)
            {
                failures.Add($"Missing script components found: {missingScriptCount}.");
            }

            int missingPrefabCount = CountMissingPrefabAssets();
            if (missingPrefabCount > 0)
            {
                failures.Add($"Missing prefab asset instances found: {missingPrefabCount}.");
            }

            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("Stage One room corridor sample validation failed:\n" + string.Join("\n", failures));
            }

            Debug.Log("Stage One room corridor sample validation passed. Zones=8, Gates=7, Placements=24, Chests=7, Spawners=5, SupportTruck=1.");
        }

        private static void BuildInternal(bool askBeforeReplacingOpenScene)
        {
            if (askBeforeReplacingOpenScene && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ValidateStageOneFootprintsOrThrow();
            EnsureFolders();

            GameObject enemyPrefab = LoadRequiredAsset<GameObject>(EnemyPrefabPath);
            GameObject goalPrefab = LoadRequiredAsset<GameObject>(EnemyGoalPrefabPath);
            GameObject spawnerPrefab = LoadRequiredAsset<GameObject>(EnemySpawnerPrefabPath);
            GameObject routePointPrefab = LoadRequiredAsset<GameObject>(EnemyRoutePointPrefabPath);
            GameObject placementPointPrefab = LoadRequiredAsset<GameObject>(PlacementPointPrefabPath);
            GameObject wallPlacementPointPrefab = LoadRequiredAsset<GameObject>(WallPlacementPointPrefabPath);
            GameObject mapExpansionGatePrefab = LoadRequiredAsset<GameObject>(MapExpansionGatePrefabPath);
            GameObject treasureChestPrefab = LoadRequiredAsset<GameObject>(TreasureChestPrefabPath);
            GameObject supportTruckPrefab = LoadRequiredAsset<GameObject>(SupportTruckPrefabPath);
            GameObject playerPrefab = LoadRequiredAsset<GameObject>(PlayerPrefabPath);
            GameObject turretPrefab = LoadRequiredAsset<GameObject>(TurretPrefabPath);
            GameObject turretRapidPrefab = LoadRequiredAsset<GameObject>(TurretRapidPrefabPath);
            GameObject turretLongRangePrefab = LoadRequiredAsset<GameObject>(TurretLongRangePrefabPath);
            GameObject barricadePrefab = LoadRequiredAsset<GameObject>(BarricadePrefabPath);
            GameObject sawTrapPrefab = LoadRequiredAsset<GameObject>(SawTrapPrefabPath);
            BuildableDefinitionSO mortarDefinition = LoadRequiredAsset<BuildableDefinitionSO>(MortarDefinitionPath);
            GameObject mortarRapidPrefab = LoadRequiredAsset<GameObject>(MortarRapidPrefabPath);
            GameObject mortarHeavyPrefab = LoadRequiredAsset<GameObject>(MortarHeavyPrefabPath);
            BuildableRoleDefinitionSO mortarRapidRole = LoadRequiredAsset<BuildableRoleDefinitionSO>(RoleMortarRapidPath);
            BuildableRoleDefinitionSO mortarHeavyRole = LoadRequiredAsset<BuildableRoleDefinitionSO>(RoleMortarHeavyPath);
            TreasureChestRewardTable rewardTable = LoadRequiredAsset<TreasureChestRewardTable>(RewardTablePath);
            ArtifactDefinitionSO[] artifactDefinitions = CreateArtifactDefinitions();
            TreasureChestRewardTable waveRewardTable = CreateWaveRewardTable(artifactDefinitions);
            EnemyCatalogSO enemyCatalog = LoadRequiredAsset<EnemyCatalogSO>(EnemyCatalogPath);
            DifficultyProgressionSO difficultyProgression = LoadRequiredAsset<DifficultyProgressionSO>(DifficultyProgressionPath);
            BossScheduleSO bossSchedule = LoadRequiredAsset<BossScheduleSO>(BossSchedulePath);
            SupportTruckShopCatalogSO supportTruckCatalog = LoadRequiredAsset<SupportTruckShopCatalogSO>(SupportTruckCatalogPath);
            if (enemyPrefab == null || goalPrefab == null || spawnerPrefab == null || routePointPrefab == null
                || placementPointPrefab == null || wallPlacementPointPrefab == null || mapExpansionGatePrefab == null
                || treasureChestPrefab == null || supportTruckPrefab == null || playerPrefab == null || turretPrefab == null
                || turretRapidPrefab == null || turretLongRangePrefab == null || barricadePrefab == null
                || sawTrapPrefab == null
                || mortarDefinition == null || mortarRapidPrefab == null || mortarHeavyPrefab == null
                || mortarRapidRole == null || mortarHeavyRole == null || rewardTable == null || waveRewardTable == null
                || enemyCatalog == null || difficultyProgression == null || bossSchedule == null || supportTruckCatalog == null)
            {
                return;
            }

            BuildableDefinitionSO turretDefinition = CreateBuildableDefinition(
                BuildableTurretPath,
                "turret_basic",
                "UFO Turret",
                BuildableKind.Turret,
                BuildableCategory.Offense,
                turretPrefab,
                false);
            BuildableDefinitionSO turretRapidDefinition = CreateBuildableDefinition(
                BuildableTurretRapidPath,
                "turret_rapid",
                "Rapid UFO Turret",
                BuildableKind.Turret,
                BuildableCategory.Offense,
                turretRapidPrefab,
                false);
            BuildableDefinitionSO turretLongRangeDefinition = CreateBuildableDefinition(
                BuildableTurretLongRangePath,
                "turret_long_range",
                "Long Range UFO Turret",
                BuildableKind.Turret,
                BuildableCategory.Offense,
                turretLongRangePrefab,
                false);
            BuildableDefinitionSO barricadeDefinition = CreateBuildableDefinition(
                BuildableBarricadePath,
                "barricade_basic",
                "Barricade",
                BuildableKind.Barricade,
                BuildableCategory.Defense,
                barricadePrefab,
                true);
            BuildableDefinitionSO sawTrapDefinition = CreateBuildableDefinition(
                BuildableSawTrapPath,
                "saw_trap_turret",
                "Saw Trap Turret",
                BuildableKind.Barricade,
                BuildableCategory.Defense,
                sawTrapPrefab,
                true);
            BuildableDefinitionSO mortarRapidDefinition = CreateBuildableDefinition(
                BuildableMortarRapidPath,
                "mortar_rapid",
                "\uAC10\uC18D\uC7A5\uD310",
                BuildableKind.Mortar,
                BuildableCategory.Skill,
                mortarRapidPrefab,
                false,
                mortarRapidRole);
            BuildableDefinitionSO mortarHeavyDefinition = CreateBuildableDefinition(
                BuildableMortarHeavyPath,
                "mortar_heavy",
                "\uAC15\uB825 \uBC15\uACA9\uD3EC\uD0D1",
                BuildableKind.Mortar,
                BuildableCategory.Skill,
                mortarHeavyPrefab,
                false,
                mortarHeavyRole);

            EnemyWaveDefinition wave01 = CreateWaveDefinition(Wave01Path, "StageOne_Wave_01_Entry", "Stage1_Spawn_Entry", 4, 0.55f);
            EnemyWaveDefinition wave02 = CreateWaveDefinition(Wave02Path, "StageOne_Wave_02_LowerDefense", "Stage1_Spawn_LowerDefense", 7, 0.45f);
            EnemyWaveDefinition wave03 = CreateWaveDefinition(Wave03Path, "StageOne_Wave_03_HighGround", "Stage1_Spawn_HighGround", 7, 0.45f);
            EnemyWaveDefinition wave04 = CreateWaveDefinition(Wave04Path, "StageOne_Wave_04_FinalApproach", "Stage1_Spawn_FinalApproach", 9, 0.4f);
            EnemyWaveDefinition wave05 = CreateWaveDefinition(Wave05Path, "StageOne_Wave_05_FinalVault", "Stage1_Spawn_FinalVault", 10, 0.35f);
            StageDefinitionSO stageDefinition = CreateStageDefinition(
                new[] { wave01, wave02, wave03, wave04, wave05 },
                rewardTable,
                supportTruckCatalog,
                new[]
                {
                    turretDefinition,
                    turretRapidDefinition,
                    turretLongRangeDefinition,
                    barricadeDefinition,
                    sawTrapDefinition,
                    mortarDefinition,
                    mortarRapidDefinition,
                    mortarHeavyDefinition
                },
                enemyPrefab,
                enemyCatalog,
                difficultyProgression,
                bossSchedule);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "stage_room_corridor_samples";

            Material floorMaterial = CreateMaterial("Assets/hansol/04_Materials/StageSample_Floor_Concrete.mat", new Color(0.28f, 0.27f, 0.24f));
            Material propMaterial = LoadOptionalAsset<Material>(ResourceMaterialPath);
            Material wallMaterial = CreateMaterial("Assets/hansol/04_Materials/StageSample_Wall_Dark.mat", new Color(0.09f, 0.1f, 0.12f));
            Material colliderMaterial = CreateMaterial("Assets/hansol/04_Materials/StageSample_Collider_Clear.mat", new Color(0.16f, 0.16f, 0.18f, 0.22f));
            Material markerMaterial = CreateMaterial("Assets/hansol/04_Materials/StageSample_Marker_Cyan.mat", new Color(0.0f, 0.8f, 1f));

            CreateCamera();
            CreateLight();

            GameObject runtimeRoot = CreateStageRuntime(waveRewardTable);
            GameObject layoutRootObject = new GameObject("StageLayout_RoomCorridorSamples");
            StageLayoutRoot layoutRoot = layoutRootObject.AddComponent<StageLayoutRoot>();

            GameObject stageZonesRoot = CreateChild("Stage_Zones", layoutRootObject.transform);
            StageZone[] zones = CreateStageZones(stageZonesRoot.transform);
            BuildStageOneLayout(
                zones,
                goalPrefab,
                spawnerPrefab,
                routePointPrefab,
                enemyPrefab,
                placementPointPrefab,
                wallPlacementPointPrefab,
                mapExpansionGatePrefab,
                treasureChestPrefab,
                rewardTable,
                supportTruckPrefab,
                playerPrefab,
                floorMaterial,
                wallMaterial,
                propMaterial,
                colliderMaterial);

            layoutRoot.CollectChildren();
            ConfigureWaveClearDoorConnector(runtimeRoot, layoutRoot);

            StageInitializer initializer = runtimeRoot.AddComponent<StageInitializer>();
            SetObjectField(initializer, "stageDefinition", stageDefinition);
            SetObjectField(initializer, "runtime", runtimeRoot.GetComponent<StageRuntime>());
            SetObjectField(initializer, "layoutRoot", layoutRoot);
            SetBoolField(initializer, "applyOnAwake", true);
            SetBoolField(initializer, "restartWaveDirector", true);
            initializer.ApplyStage();

            BuildStageNavMesh(layoutRootObject, zones);

            PrefabUtility.SaveAsPrefabAsset(runtimeRoot, StageRuntimePrefabPath);
            PrefabUtility.SaveAsPrefabAsset(layoutRootObject, StageLayoutPrefabPath);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Stage room/corridor sample built: {ScenePath}");
        }

        private static StageZone[] CreateStageZones(Transform parent)
        {
            return new[]
            {
                CreateStageZone(parent, "Zone_00_StartSupply"),
                CreateStageZone(parent, "Zone_01_InterwovenEntry"),
                CreateStageZone(parent, "Zone_02_LowerDefense"),
                CreateStageZone(parent, "Zone_03_RewardVault"),
                CreateStageZone(parent, "Zone_04_HighGroundDefense"),
                CreateStageZone(parent, "Zone_05_TreasureOverlook"),
                CreateStageZone(parent, "Zone_06_FinalApproach"),
                CreateStageZone(parent, "Zone_07_FinalVault")
            };
        }

        private static StageZone CreateStageZone(Transform parent, string name)
        {
            GameObject root = CreateChild(name, parent);
            GameObject geometry = CreateChild("Geometry", root.transform);
            GameObject gameplay = CreateChild("Gameplay", root.transform);
            GameObject props = CreateChild("KayKit_Visual_Props", root.transform);
            return new StageZone(root, geometry.transform, gameplay.transform, props.transform);
        }

        private static void BuildStageOneLayout(
            StageZone[] zones,
            GameObject goalPrefab,
            GameObject spawnerPrefab,
            GameObject routePointPrefab,
            GameObject enemyPrefab,
            GameObject placementPointPrefab,
            GameObject wallPlacementPointPrefab,
            GameObject mapExpansionGatePrefab,
            GameObject treasureChestPrefab,
            TreasureChestRewardTable rewardTable,
            GameObject supportTruckPrefab,
            GameObject playerPrefab,
            Material floorMaterial,
            Material wallMaterial,
            Material propMaterial,
            Material colliderMaterial)
        {
            StageZone start = zones[0];
            StageZone entry = zones[1];
            StageZone lowerDefense = zones[2];
            StageZone rewardVault = zones[3];
            StageZone highGround = zones[4];
            StageZone treasureOverlook = zones[5];
            StageZone finalApproach = zones[6];
            StageZone finalVault = zones[7];

            CreateRoom("00_StartSupplyRoom", start.Geometry, MapPosition(0f, 0f, 0f), MapSize(18f, 16f), floorMaterial, wallMaterial, RoomOpenings.East);
            CreateCorridor("01A_MainEntryCorridor", entry.Geometry, MapPosition(15f, 0f, 0f), MapSize(12f, 4f), floorMaterial, wallMaterial);
            CreateRoom("01_EntryForkRoom", entry.Geometry, MapPosition(30f, 0f, 0f), MapSize(18f, 16f), floorMaterial, wallMaterial, RoomOpenings.West | RoomOpenings.East | RoomOpenings.North);
            CreateVerticalCorridor("01C_EntrySideLoopConnector", entry.Geometry, MapPosition(30f, 0f, 11f), MapSize(4f, 6f), floorMaterial, wallMaterial);
            CreateRoom("01D_EntrySupplyPocket", entry.Geometry, MapPosition(30f, 0f, 18f), MapSize(12f, 8f), floorMaterial, wallMaterial, RoomOpenings.South);
            CreateCorridor("02A_EntryToLowerConnector", lowerDefense.Geometry, MapPosition(43f, 0f, 0f), MapSize(8f, 4f), floorMaterial, wallMaterial);
            CreateTieredDefenseRoom("02_LowerDefenseHall", lowerDefense.Geometry, MapPosition(58f, 0f, 0f), floorMaterial, wallMaterial);
            CreateVerticalCorridor("02B_LowerWorkshopConnector", lowerDefense.Geometry, MapPosition(58f, 0f, -12.5f), MapSize(4f, 7f), floorMaterial, wallMaterial);
            CreateRoom("02C_LowerWorkshopPocket", lowerDefense.Geometry, MapPosition(58f, 0f, -20f), MapSize(14f, 8f), floorMaterial, wallMaterial, RoomOpenings.North);
            CreateVerticalCorridor("03A_LowerToRewardConnector", rewardVault.Geometry, MapPosition(58f, 0f, 13.5f), MapSize(4f, 9f), floorMaterial, wallMaterial);
            CreateRoom("03_RewardVault", rewardVault.Geometry, MapPosition(58f, 0f, 24f), MapSize(18f, 12f), floorMaterial, wallMaterial, RoomOpenings.South);
            CreateCorridor("04A_LowerToHighGroundConnector", highGround.Geometry, MapPosition(73f, 0f, 0f), MapSize(8f, 4f), floorMaterial, wallMaterial);
            CreateHighGroundDefenseRoom("04_HighGroundDefense", highGround.Geometry, MapPosition(88f, 0f, 0f), floorMaterial, wallMaterial);
            CreateVerticalCorridor("05A_HighGroundToTreasureConnector", treasureOverlook.Geometry, MapPosition(88f, 0f, -14.5f), MapSize(4f, 11f), floorMaterial, wallMaterial);
            CreateRoom("05_TreasureOverlook", treasureOverlook.Geometry, MapPosition(88f, 0f, -26f), MapSize(18f, 12f), floorMaterial, wallMaterial, RoomOpenings.North);
            CreateCorridor("06A_HighGroundToFinalConnector", finalApproach.Geometry, MapPosition(103f, 0f, 0f), MapSize(8f, 4f), floorMaterial, wallMaterial);
            CreateRoom("06_FinalApproachHall", finalApproach.Geometry, MapPosition(120f, 0f, 0f), MapSize(26f, 12f), floorMaterial, wallMaterial, RoomOpenings.West | RoomOpenings.East | RoomOpenings.North | RoomOpenings.South);
            CreateVerticalCorridor("06B_FinalApproachNorthConnector", finalApproach.Geometry, MapPosition(120f, 0f, 9f), MapSize(4f, 6f), floorMaterial, wallMaterial);
            CreateRoom("06C_FinalApproachNorthBay", finalApproach.Geometry, MapPosition(120f, 0f, 16f), MapSize(12f, 8f), floorMaterial, wallMaterial, RoomOpenings.South);
            CreateVerticalCorridor("06D_FinalApproachSouthConnector", finalApproach.Geometry, MapPosition(120f, 0f, -9f), MapSize(4f, 6f), floorMaterial, wallMaterial);
            CreateRoom("06E_FinalApproachSouthBay", finalApproach.Geometry, MapPosition(120f, 0f, -16f), MapSize(12f, 8f), floorMaterial, wallMaterial, RoomOpenings.North);
            CreateCorridor("07A_FinalApproachToVaultConnector", finalVault.Geometry, MapPosition(136.5f, 0f, 0f), MapSize(7f, 4f), floorMaterial, wallMaterial);
            CreateFinalVaultRoom("07_FinalVault", finalVault.Geometry, MapPosition(150f, 0f, 0f), floorMaterial, wallMaterial);

            CreateStageOneDensityGeometry(start, entry, lowerDefense, rewardVault, highGround, treasureOverlook, finalApproach, finalVault, floorMaterial, wallMaterial);
            CreateStageOneProps(start, entry, lowerDefense, rewardVault, highGround, treasureOverlook, finalApproach, finalVault, colliderMaterial);

            InstantiatePrefab(supportTruckPrefab, "Stage1_SupportTruck_StartSupply", start.Gameplay, MapPosition(-2.3f, 0f, 4.45f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            GameObject player = InstantiatePrefab(playerPrefab, "Stage1_Player_Start", start.Gameplay, MapPosition(-2f, 0.05f, 4.8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            ConfigureStagePlayerRewardServices(player);

            GameObject goal = InstantiatePrefab(goalPrefab, "Stage1_Final_Goal_YELLOW", finalVault.Gameplay, MapPosition(146f, 0.5f, 0f), Quaternion.identity, Vector3.one);

            CreateSpawner(spawnerPrefab, routePointPrefab, enemyPrefab, goal.transform, start.Gameplay, "Stage1_Spawn_Entry_A_RED", MapPosition(-6f, 0.05f, 0f), new[]
            {
                MapPosition(6f, 0.5f, 0f),
                MapPosition(30f, 0.5f, 0f),
                MapPosition(58f, 0.5f, 0f),
                MapPosition(78f, 0.5f, -7f),
                MapPosition(96f, 0.5f, -7f),
                MapPosition(103f, 0.5f, 0f),
                MapPosition(120f, 0.5f, 0f),
                MapPosition(144f, 0.5f, 0f)
            });
            CreateSpawner(spawnerPrefab, routePointPrefab, enemyPrefab, goal.transform, lowerDefense.Gameplay, "Stage1_Spawn_LowerDefense_A_RED", MapPosition(50f, 0.05f, -6f), new[]
            {
                MapPosition(58f, 0.5f, -3f),
                MapPosition(69f, 0.5f, 0f),
                MapPosition(78f, 0.5f, -7f),
                MapPosition(96f, 0.5f, -7f),
                MapPosition(103f, 0.5f, 0f),
                MapPosition(120f, 0.5f, 0f),
                MapPosition(144f, 0.5f, 0f)
            });
            CreateSpawner(spawnerPrefab, routePointPrefab, enemyPrefab, goal.transform, highGround.Gameplay, "Stage1_Spawn_HighGround_A_RED", MapPosition(82f, 0.05f, -7f), new[]
            {
                MapPosition(86f, 0.5f, -7f),
                MapPosition(96f, 0.5f, -7f),
                MapPosition(103f, 0.5f, 0f),
                MapPosition(120f, 0.5f, 0f),
                MapPosition(144f, 0.5f, 0f)
            });
            CreateSpawner(spawnerPrefab, routePointPrefab, enemyPrefab, goal.transform, finalApproach.Gameplay, "Stage1_Spawn_FinalApproach_A_RED", MapPosition(112f, 0.05f, 2f), new[]
            {
                MapPosition(120f, 0.5f, 0f),
                MapPosition(136f, 0.5f, 0f),
                MapPosition(144f, 0.5f, 0f)
            });
            CreateSpawner(spawnerPrefab, routePointPrefab, enemyPrefab, goal.transform, finalVault.Gameplay, "Stage1_Spawn_FinalVault_A_RED", MapPosition(144f, 0.05f, -6f), new[]
            {
                MapPosition(150f, 0.5f, -3f),
                MapPosition(144f, 0.5f, 0f)
            });

            CreateStageOnePlacementPoints(placementPointPrefab, wallPlacementPointPrefab, start, entry, lowerDefense, rewardVault, highGround, treasureOverlook, finalApproach, finalVault);
            CreateStageOneTreasureChests(treasureChestPrefab, rewardTable, entry, lowerDefense, rewardVault, treasureOverlook, finalApproach, finalVault);
            CreateStageOneGates(mapExpansionGatePrefab, start, entry, lowerDefense, rewardVault, highGround, treasureOverlook, finalApproach, finalVault);
            SetInitialZoneVisibility(zones);
        }

        private static void CreateStageOneProps(
            StageZone start,
            StageZone entry,
            StageZone lowerDefense,
            StageZone rewardVault,
            StageZone highGround,
            StageZone treasureOverlook,
            StageZone finalApproach,
            StageZone finalVault,
            Material colliderMaterial)
        {
            PlaceResourceVisual("Fuel_A_Barrels.prefab", "Visual_Start_Fuel_Barrels", start.Props, MapPosition(-6f, 0.2f, 4.5f), Quaternion.identity, Vector3.one * 1.25f);
            PlaceResourceVisual("Food_Crate_Large_Apples.prefab", "Visual_Start_Food_Crate", start.Props, MapPosition(4.5f, 0.2f, -5f), Quaternion.identity, Vector3.one * 1.2f);
            PlaceKayKitVisual(PlatformerNeutralPrefabFolder + "/signage_arrows_right.prefab", "Visual_Entry_Signage_Right", entry.Props, MapPosition(21.5f, 0.2f, -2.2f), Quaternion.identity, Vector3.one * 1.1f);
            PlaceResourceVisual("Food_Crate_Small_Berries.prefab", "Visual_Entry_Pocket_Food", entry.Props, MapPosition(30f, 0.2f, 18f), Quaternion.identity, Vector3.one * 1.1f);
            PlaceResourceVisual("Containers_Crate_Medium_Wood.prefab", "Visual_Entry_Side_Crate_A", entry.Props, MapPosition(26.5f, 0.2f, 17f), Quaternion.identity, Vector3.one * 1.1f);
            PlaceResourceVisual("Containers_Crate_Large.prefab", "Visual_LowerDefense_Crate", lowerDefense.Props, MapPosition(53f, 0.2f, 6f), Quaternion.identity, Vector3.one * 1.25f);
            PlaceResourceVisual("Parts_Pile_Large.prefab", "Visual_LowerDefense_Parts", lowerDefense.Props, MapPosition(63f, 0.2f, -5f), Quaternion.identity, Vector3.one * 1.25f);
            PlaceResourceVisual("Containers_Crate_Medium_Grey.prefab", "Visual_LowerDefense_Workshop_Crate", lowerDefense.Props, MapPosition(55f, 0.2f, -20f), Quaternion.identity, Vector3.one * 1.1f);
            PlaceResourceVisual("Parts_Pile_Medium.prefab", "Visual_LowerDefense_Workshop_Parts", lowerDefense.Props, MapPosition(62f, 0.2f, -20f), Quaternion.identity, Vector3.one * 1.1f);
            PlaceResourceVisual("Gold_Bars_Stack_Medium.prefab", "Visual_RewardVault_GoldBars", rewardVault.Props, MapPosition(54f, 0.2f, 26f), Quaternion.identity, Vector3.one * 1.25f);
            PlaceResourceVisual("Money_Pile_Large.prefab", "Visual_RewardVault_MoneyPile", rewardVault.Props, MapPosition(63f, 0.2f, 22f), Quaternion.identity, Vector3.one * 1.2f);
            PlaceResourceVisual("Gold_Bars_Stack_Small.prefab", "Visual_RewardVault_GoldBars_Small", rewardVault.Props, MapPosition(64f, 0.2f, 28f), Quaternion.identity, Vector3.one * 1.1f);
            PlaceResourceVisual("Money_Pile_Medium.prefab", "Visual_RewardVault_MoneyPile_Medium", rewardVault.Props, MapPosition(52f, 0.2f, 21f), Quaternion.identity, Vector3.one * 1.1f);
            PlaceKayKitVisual(PlatformerNeutralPrefabFolder + "/floor_spikes_4x4x1.prefab", "Visual_HighGround_Spikes", highGround.Props, MapPosition(86f, 0.15f, -3f), Quaternion.identity, Vector3.one * 1.25f);
            PlaceKayKitVisual(PlatformerNeutralPrefabFolder + "/sawblade.prefab", "Visual_HighGround_Sawblade", highGround.Props, MapPosition(95f, 0.35f, 5f), Quaternion.identity, Vector3.one * 1.25f);
            PlaceKayKitVisual(PlatformerNeutralPrefabFolder + "/floor_spikes_2x2x1.prefab", "Visual_HighGround_SmallSpikes_A", highGround.Props, MapPosition(94.5f, 4.55f, -2f), Quaternion.identity, Vector3.one);
            PlaceKayKitVisual(PlatformerNeutralPrefabFolder + "/floor_spikes_2x2x1.prefab", "Visual_HighGround_SmallSpikes_B", highGround.Props, MapPosition(93.5f, 4.55f, 2f), Quaternion.identity, Vector3.one);
            PlaceResourceVisual("Gems_Chest.prefab", "Visual_TreasureOverlook_GemsChest", treasureOverlook.Props, MapPosition(91f, 0.2f, -27f), Quaternion.identity, Vector3.one * 1.25f);
            PlaceResourceVisual("Silver_Bars_Stack_Medium.prefab", "Visual_TreasureOverlook_Silver", treasureOverlook.Props, MapPosition(84f, 0.2f, -24f), Quaternion.identity, Vector3.one * 1.1f);
            PlaceResourceVisual("Gems_Pile_Small.prefab", "Visual_TreasureOverlook_GemsPile", treasureOverlook.Props, MapPosition(83f, 0.2f, -29f), Quaternion.identity, Vector3.one);
            PlaceResourceVisual("Iron_Bars_Stack_Large.prefab", "Visual_FinalApproach_Iron", finalApproach.Props, MapPosition(116f, 0.2f, 3.5f), Quaternion.identity, Vector3.one * 1.2f);
            PlaceResourceVisual("Containers_Crate_Medium_Tan.prefab", "Visual_FinalApproach_NorthBay_Crate", finalApproach.Props, MapPosition(120f, 0.2f, 16f), Quaternion.identity, Vector3.one * 1.1f);
            PlaceResourceVisual("Parts_Pile_Medium.prefab", "Visual_FinalApproach_SouthBay_Parts", finalApproach.Props, MapPosition(120f, 0.2f, -16f), Quaternion.identity, Vector3.one * 1.1f);
            PlaceResourceVisual("Gems_Pile_Large.prefab", "Visual_FinalVault_GemsPile", finalVault.Props, MapPosition(149f, 0.2f, 5f), Quaternion.identity, Vector3.one * 1.25f);
            PlaceResourceVisual("Gold_Bars_Stack_Large.prefab", "Visual_FinalVault_GoldBars_Large", finalVault.Props, MapPosition(153f, 0.2f, -5f), Quaternion.identity, Vector3.one * 1.2f);
            PlaceResourceVisual("Money_Pile_Medium.prefab", "Visual_FinalVault_MoneyPile_Medium", finalVault.Props, MapPosition(144f, 0.2f, -5f), Quaternion.identity, Vector3.one * 1.1f);

            GameObject hazardMarker = CreateBlock("HighGround_NonDamage_HazardMarker", highGround.Geometry, MapPosition(88f, 0.05f, 0f), MapBlockScale(8f, 0.1f, 5f), colliderMaterial);
            hazardMarker.GetComponent<Collider>().isTrigger = true;
        }

        private static void CreateStageOneDensityGeometry(
            StageZone start,
            StageZone entry,
            StageZone lowerDefense,
            StageZone rewardVault,
            StageZone highGround,
            StageZone treasureOverlook,
            StageZone finalApproach,
            StageZone finalVault,
            Material floorMaterial,
            Material wallMaterial)
        {
            CreateLowCover("StartSupply_NorthSupplyCover", start.Geometry, -2.5f, 0.65f, 5.4f, 4f, 1.3f, 0.7f, wallMaterial);
            CreateLowCover("StartSupply_SouthSupplyCover", start.Geometry, 4.5f, 0.65f, -5.5f, 4f, 1.3f, 0.7f, wallMaterial);
            CreateBlock("StartSupply_ServicePillar_North", start.Geometry, MapPosition(5.8f, 1.6f, 5.2f), MapBlockScale(0.7f, 3.2f, 0.7f), wallMaterial);
            CreateBlock("StartSupply_ServicePillar_South", start.Geometry, MapPosition(5.8f, 1.6f, -5.2f), MapBlockScale(0.7f, 3.2f, 0.7f), wallMaterial);

            CreateLowCover("EntryFork_NorthDivider_A", entry.Geometry, 26f, 0.65f, 5.8f, 4f, 1.3f, 0.7f, wallMaterial);
            CreateLowCover("EntryFork_SouthDivider_A", entry.Geometry, 34f, 0.65f, -5.8f, 4f, 1.3f, 0.7f, wallMaterial);
            CreateLowCover("EntrySupplyPocket_BackCover", entry.Geometry, 30f, 0.65f, 20.8f, 5f, 1.3f, 0.7f, wallMaterial);
            CreateBlock("EntrySupplyPocket_Pillar_A", entry.Geometry, MapPosition(26.5f, 1.7f, 17f), MapBlockScale(0.65f, 3.4f, 0.65f), wallMaterial);
            CreateBlock("EntrySupplyPocket_Pillar_B", entry.Geometry, MapPosition(33.5f, 1.7f, 17f), MapBlockScale(0.65f, 3.4f, 0.65f), wallMaterial);

            CreateLowCover("LowerDefense_NorthLowCover_A", lowerDefense.Geometry, 53f, 0.65f, 7.2f, 4.5f, 1.3f, 0.65f, wallMaterial);
            CreateLowCover("LowerDefense_NorthLowCover_B", lowerDefense.Geometry, 63f, 0.65f, 7.2f, 4.5f, 1.3f, 0.65f, wallMaterial);
            CreateLowCover("LowerDefense_SouthLowCover_A", lowerDefense.Geometry, 53f, 0.65f, -7.2f, 4.5f, 1.3f, 0.65f, wallMaterial);
            CreateLowCover("LowerDefense_SouthLowCover_B", lowerDefense.Geometry, 63f, 0.65f, -7.2f, 4.5f, 1.3f, 0.65f, wallMaterial);
            CreateLowCover("LowerWorkshop_BackCover", lowerDefense.Geometry, 58f, 0.65f, -23.2f, 8f, 1.3f, 0.65f, wallMaterial);
            CreateBlock("LowerWorkshop_WorkBench", lowerDefense.Geometry, MapPosition(63f, 0.55f, -18.8f), MapBlockScale(3f, 1.1f, 1f), floorMaterial);

            CreateBlock("RewardVault_InnerPillar_A", rewardVault.Geometry, MapPosition(51.5f, 2.4f, 21f), MapBlockScale(0.8f, 4.8f, 0.8f), wallMaterial);
            CreateBlock("RewardVault_InnerPillar_B", rewardVault.Geometry, MapPosition(65.5f, 2.4f, 28f), MapBlockScale(0.8f, 4.8f, 0.8f), wallMaterial);
            CreateLowCover("RewardVault_GoldDaisFront", rewardVault.Geometry, 58f, 0.55f, 21.2f, 7f, 1.1f, 0.55f, wallMaterial);

            CreateBlock("HighGround_DeckSideCover_North", highGround.Geometry, MapPosition(92f, 4.95f, 6.2f), MapBlockScale(5f, 1f, 0.6f), wallMaterial);
            CreateBlock("HighGround_DeckSideCover_South", highGround.Geometry, MapPosition(92f, 4.95f, -6.2f), MapBlockScale(5f, 1f, 0.6f), wallMaterial);
            CreateBlock("HighGround_LowerPillar_A", highGround.Geometry, MapPosition(82f, 2.2f, 7.2f), MapBlockScale(0.7f, 4.4f, 0.7f), wallMaterial);
            CreateBlock("HighGround_LowerPillar_B", highGround.Geometry, MapPosition(98f, 2.2f, -7.2f), MapBlockScale(0.7f, 4.4f, 0.7f), wallMaterial);

            CreateBlock("TreasureOverlook_InnerPillar_A", treasureOverlook.Geometry, MapPosition(81.5f, 2.4f, -23f), MapBlockScale(0.8f, 4.8f, 0.8f), wallMaterial);
            CreateBlock("TreasureOverlook_InnerPillar_B", treasureOverlook.Geometry, MapPosition(95.5f, 2.4f, -29f), MapBlockScale(0.8f, 4.8f, 0.8f), wallMaterial);
            CreateLowCover("TreasureOverlook_ChestCover", treasureOverlook.Geometry, 88f, 0.55f, -22.5f, 8f, 1.1f, 0.55f, wallMaterial);

            CreateLowCover("FinalApproach_NorthEdgeCover_A", finalApproach.Geometry, 114f, 0.65f, 4.6f, 4.5f, 1.3f, 0.45f, wallMaterial);
            CreateLowCover("FinalApproach_SouthEdgeCover_A", finalApproach.Geometry, 122f, 0.65f, -4.6f, 4.5f, 1.3f, 0.45f, wallMaterial);
            CreateLowCover("FinalApproach_NorthEdgeCover_B", finalApproach.Geometry, 128f, 0.65f, 4.6f, 4.5f, 1.3f, 0.45f, wallMaterial);
            CreateLowCover("FinalApproach_NorthBayCover", finalApproach.Geometry, 120f, 0.65f, 18.8f, 5f, 1.3f, 0.65f, wallMaterial);
            CreateLowCover("FinalApproach_SouthBayCover", finalApproach.Geometry, 120f, 0.65f, -18.8f, 5f, 1.3f, 0.65f, wallMaterial);

            CreateBlock("FinalVault_GuardPillar_North", finalVault.Geometry, MapPosition(150f, 2.8f, 6.4f), MapBlockScale(0.9f, 5.6f, 0.9f), wallMaterial);
            CreateBlock("FinalVault_GuardPillar_South", finalVault.Geometry, MapPosition(150f, 2.8f, -6.4f), MapBlockScale(0.9f, 5.6f, 0.9f), wallMaterial);
            CreateLowCover("FinalVault_NorthGuardCover", finalVault.Geometry, 154f, 0.65f, 5.5f, 4.5f, 1.3f, 0.7f, wallMaterial);
            CreateLowCover("FinalVault_SouthGuardCover", finalVault.Geometry, 154f, 0.65f, -5.5f, 4.5f, 1.3f, 0.7f, wallMaterial);
        }

        private static void CreateStageOnePlacementPoints(
            GameObject placementPrefab,
            GameObject wallPlacementPrefab,
            StageZone start,
            StageZone entry,
            StageZone lowerDefense,
            StageZone rewardVault,
            StageZone highGround,
            StageZone treasureOverlook,
            StageZone finalApproach,
            StageZone finalVault)
        {
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_Start_01", start.Gameplay, 0f, 0.15f, 2.5f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_Entry_Choke_01", entry.Gameplay, 30f, 0.15f, -4.5f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_Entry_Side_02", entry.Gameplay, 34f, 0.15f, 5.5f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_Entry_Pocket_03", entry.Gameplay, 30f, 0.15f, 17.5f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_LowerDefense_Center_01", lowerDefense.Gameplay, 58f, 0.15f, 0f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_LowerDefense_Ramp_02", lowerDefense.Gameplay, 65f, 0.15f, -4f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_LowerDefense_Workshop_03", lowerDefense.Gameplay, 58f, 0.15f, -20f);
            CreateWallPlacement(wallPlacementPrefab, "Stage1_WallPlacement_Lower_North", lowerDefense.Gameplay, 58f, WallHeight * 0.55f, 9.2f, Quaternion.Euler(0f, 180f, 0f));
            CreateWallPlacement(wallPlacementPrefab, "Stage1_WallPlacement_Lower_South", lowerDefense.Gameplay, 58f, WallHeight * 0.55f, -9.2f, Quaternion.identity);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_RewardVault_Guard", rewardVault.Gameplay, 58f, 0.15f, 21.5f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_RewardVault_Back_02", rewardVault.Gameplay, 64f, 0.15f, 28f);
            CreateWallPlacement(wallPlacementPrefab, "Stage1_WallPlacement_RewardVault_East", rewardVault.Gameplay, 67.6f, WallHeight * 0.55f, 24f, Quaternion.Euler(0f, -90f, 0f));
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_HighGround_Ramp_01", highGround.Gameplay, 82f, 0.15f, 0f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_HighGround_Deck_02", highGround.Gameplay, 91f, 4.45f, -4f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_HighGround_Deck_03", highGround.Gameplay, 94f, 4.45f, 4f);
            CreateWallPlacement(wallPlacementPrefab, "Stage1_WallPlacement_HighGround_North", highGround.Gameplay, 88f, WallHeight * 0.55f, 9.2f, Quaternion.Euler(0f, 180f, 0f));
            CreateWallPlacement(wallPlacementPrefab, "Stage1_WallPlacement_HighGround_South", highGround.Gameplay, 88f, WallHeight * 0.55f, -9.2f, Quaternion.identity);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_TreasureOverlook_Reward", treasureOverlook.Gameplay, 88f, 0.15f, -24f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_TreasureOverlook_Back_02", treasureOverlook.Gameplay, 94f, 0.15f, -29f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_FinalApproach_Choke", finalApproach.Gameplay, 120f, 0.15f, 0f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_FinalApproach_NorthBay", finalApproach.Gameplay, 120f, 0.15f, 16f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_FinalApproach_SouthBay", finalApproach.Gameplay, 120f, 0.15f, -16f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_FinalVault_GoalGuard", finalVault.Gameplay, 151f, 0.15f, -4f);
            CreateFloorPlacement(placementPrefab, "Stage1_Placement_FinalVault_GoalGuard_02", finalVault.Gameplay, 151f, 0.15f, 4f);
        }

        private static void CreateStageOneTreasureChests(
            GameObject chestPrefab,
            TreasureChestRewardTable rewardTable,
            StageZone entry,
            StageZone lowerDefense,
            StageZone rewardVault,
            StageZone treasureOverlook,
            StageZone finalApproach,
            StageZone finalVault)
        {
            ConfigureChest(InstantiatePrefab(chestPrefab, "Stage1_TreasureChest_EntryPocket", entry.Gameplay, MapPosition(30f, 0.2f, 20f), Quaternion.identity, Vector3.one), rewardTable, 0);
            ConfigureChest(InstantiatePrefab(chestPrefab, "Stage1_TreasureChest_LowerWorkshop", lowerDefense.Gameplay, MapPosition(54f, 0.2f, -22f), Quaternion.identity, Vector3.one), rewardTable, 1);
            ConfigureChest(InstantiatePrefab(chestPrefab, "Stage1_TreasureChest_RewardVault_A", rewardVault.Gameplay, MapPosition(55f, 0.2f, 28f), Quaternion.identity, Vector3.one), rewardTable, 0);
            ConfigureChest(InstantiatePrefab(chestPrefab, "Stage1_TreasureChest_RewardVault_B", rewardVault.Gameplay, MapPosition(64f, 0.2f, 24f), Quaternion.identity, Vector3.one), rewardTable, 1);
            ConfigureChest(InstantiatePrefab(chestPrefab, "Stage1_TreasureChest_TreasureOverlook", treasureOverlook.Gameplay, MapPosition(88f, 0.2f, -29f), Quaternion.identity, Vector3.one), rewardTable, 2);
            ConfigureChest(InstantiatePrefab(chestPrefab, "Stage1_TreasureChest_FinalApproach_NorthBay", finalApproach.Gameplay, MapPosition(120f, 0.2f, 18.5f), Quaternion.identity, Vector3.one), rewardTable, 3);
            ConfigureChest(InstantiatePrefab(chestPrefab, "Stage1_TreasureChest_FinalVault", finalVault.Gameplay, MapPosition(148f, 0.2f, 5f), Quaternion.identity, Vector3.one), rewardTable, 3);
        }

        private static void CreateStageOneGates(
            GameObject gatePrefab,
            StageZone start,
            StageZone entry,
            StageZone lowerDefense,
            StageZone rewardVault,
            StageZone highGround,
            StageZone treasureOverlook,
            StageZone finalApproach,
            StageZone finalVault)
        {
            CreateActivationGate(gatePrefab, "Stage1_Gate_Start_To_Entry", start.Gameplay, MapPosition(9f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), entry);
            CreateActivationGate(gatePrefab, "Stage1_Gate_Entry_To_LowerDefense", entry.Gameplay, MapPosition(39f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), lowerDefense);
            CreateActivationGate(gatePrefab, "Stage1_Gate_LowerDefense_To_RewardVault", lowerDefense.Gameplay, MapPosition(58f, 0f, 9f), Quaternion.identity, rewardVault);
            CreateActivationGate(gatePrefab, "Stage1_Gate_LowerDefense_To_HighGround", lowerDefense.Gameplay, MapPosition(69f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), highGround);
            CreateActivationGate(gatePrefab, "Stage1_Gate_HighGround_To_TreasureOverlook", highGround.Gameplay, MapPosition(88f, 0f, -9f), Quaternion.identity, treasureOverlook);
            CreateActivationGate(gatePrefab, "Stage1_Gate_HighGround_To_FinalApproach", highGround.Gameplay, MapPosition(99f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), finalApproach);
            CreateActivationGate(gatePrefab, "Stage1_Gate_FinalApproach_To_FinalVault", finalApproach.Gameplay, MapPosition(133f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), finalVault);
        }

        private static void CreateActivationGate(GameObject gatePrefab, string name, Transform parent, Vector3 position, Quaternion rotation, params StageZone[] targetZones)
        {
            GameObject gate = InstantiatePrefab(gatePrefab, name, parent, position, rotation, Vector3.one * 3f);
            MapExpansionActivationTargetGroup activationGroup = gate.GetComponent<MapExpansionActivationTargetGroup>();
            if (activationGroup != null)
            {
                GameObject[] targets = new GameObject[targetZones != null ? targetZones.Length : 0];
                for (int i = 0; targetZones != null && i < targetZones.Length; i++)
                {
                    targets[i] = targetZones[i]?.Root;
                }

                SerializedObject groupSo = new SerializedObject(activationGroup);
                SetObjectArray(groupSo, "activationTargets", targets);
                SetBool(groupSo, "deactivateTargetsOnAwake", true);
                groupSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(activationGroup);
            }

            MapExpansionDoorOpenActivator activator = gate.GetComponent<MapExpansionDoorOpenActivator>();
            MapExpansionDoorOpener opener = gate.GetComponentInChildren<MapExpansionDoorOpener>(true);
            ConfigureOpenedDoorVisualAsNonBlocking(opener);
            if (activator != null)
            {
                SerializedObject activatorSo = new SerializedObject(activator);
                SetObject(activatorSo, "doorOpener", opener);
                SetObject(activatorSo, "activationTargetGroup", activationGroup);
                activatorSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(activator);
            }
        }

        private static void ConfigureOpenedDoorVisualAsNonBlocking(MapExpansionDoorOpener opener)
        {
            if (opener == null)
            {
                return;
            }

            SerializedObject openerSo = new SerializedObject(opener);
            SerializedProperty openedRootProperty = openerSo.FindProperty("openedDoorRoot");
            GameObject openedRoot = openedRootProperty != null ? openedRootProperty.objectReferenceValue as GameObject : null;
            if (openedRoot == null)
            {
                return;
            }

            Collider[] openedColliders = openedRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < openedColliders.Length; i++)
            {
                openedColliders[i].enabled = false;
                EditorUtility.SetDirty(openedColliders[i]);
            }

            EditorUtility.SetDirty(openedRoot);
        }

        private static void SetInitialZoneVisibility(StageZone[] zones)
        {
            for (int i = 0; zones != null && i < zones.Length; i++)
            {
                zones[i].Root.SetActive(i == 0);
            }
        }

        private static void CreateFloorPlacement(GameObject placementPrefab, string name, Transform parent, float x, float y, float z)
        {
            InstantiatePrefab(placementPrefab, name, parent, MapPosition(x, y, z), Quaternion.identity, Vector3.one);
        }

        private static void CreateWallPlacement(GameObject placementPrefab, string name, Transform parent, float x, float y, float z, Quaternion rotation)
        {
            InstantiatePrefab(placementPrefab, name, parent, MapPosition(x, y, z), rotation, Vector3.one);
        }

        private static GameObject CreateLowCover(
            string name,
            Transform parent,
            float x,
            float y,
            float z,
            float width,
            float height,
            float depth,
            Material material)
        {
            return CreateBlock(name, parent, MapPosition(x, y, z), MapBlockScale(width, height, depth), material);
        }

        private static void BuildStageNavMesh(GameObject layoutRootObject, StageZone[] zones)
        {
            if (layoutRootObject == null)
            {
                return;
            }

            bool[] originalActiveStates = new bool[zones != null ? zones.Length : 0];
            for (int i = 0; zones != null && i < zones.Length; i++)
            {
                originalActiveStates[i] = zones[i].Root.activeSelf;
                zones[i].Root.SetActive(true);
            }

            try
            {
                Physics.SyncTransforms();
                NavMeshSurface surface = layoutRootObject.GetComponent<NavMeshSurface>();
                if (surface == null)
                {
                    surface = layoutRootObject.AddComponent<NavMeshSurface>();
                }

                surface.collectObjects = CollectObjects.Children;
                List<Collider> disabledDoorBlockers = SetControlledDoorBlockersEnabled(false);
                try
                {
                    surface.BuildNavMesh();
                    EditorUtility.SetDirty(surface);
                }
                finally
                {
                    RestoreColliders(disabledDoorBlockers);
                }
            }
            finally
            {
                for (int i = 0; zones != null && i < zones.Length && i < originalActiveStates.Length; i++)
                {
                    zones[i].Root.SetActive(originalActiveStates[i]);
                }

                Physics.SyncTransforms();
            }
        }

        private static void PlaceResourceVisual(string prefabName, string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            PlaceKayKitVisual(ResourcePrefabFolder + "/" + prefabName, name, parent, position, rotation, scale);
        }

        private static void CreateTieredDefenseRoom(string name, Transform parent, Vector3 center, Material floorMaterial, Material wallMaterial)
        {
            const float deckTopY = 3.2f;
            const float guardHeight = 2.2f;
            CreateRoom(name, parent, center, MapSize(22f, 18f), floorMaterial, wallMaterial, RoomOpenings.West | RoomOpenings.East | RoomOpenings.North | RoomOpenings.South);
            CreateBlock("RaisedNorthDefenseDeck", parent, center + MapPosition(1f, deckTopY * 0.5f, 5.8f), MapBlockScale(14f, deckTopY, 4.2f), floorMaterial);
            CreateBlock("RaisedSouthDefenseDeck", parent, center + MapPosition(1f, deckTopY * 0.5f, -5.8f), MapBlockScale(14f, deckTopY, 4.2f), floorMaterial);
            GameObject northRamp = CreateBlock("Ramp_To_NorthDefenseDeck", parent, center + MapPosition(-7f, 1.6f, 3.2f), MapBlockScale(8f, 0.35f, 3.2f), floorMaterial);
            northRamp.transform.rotation = Quaternion.Euler(0f, 0f, -6f);
            GameObject southRamp = CreateBlock("Ramp_To_SouthDefenseDeck", parent, center + MapPosition(-7f, 1.6f, -3.2f), MapBlockScale(8f, 0.35f, 3.2f), floorMaterial);
            southRamp.transform.rotation = Quaternion.Euler(0f, 0f, -6f);
            CreateBlock("DeckGuardWall_North", parent, center + MapPosition(1f, deckTopY + guardHeight * 0.5f, 8.5f), MapBlockScale(14f, guardHeight, 0.35f), wallMaterial);
            CreateBlock("DeckGuardWall_South", parent, center + MapPosition(1f, deckTopY + guardHeight * 0.5f, -8.5f), MapBlockScale(14f, guardHeight, 0.35f), wallMaterial);
        }

        private static void CreateHighGroundDefenseRoom(string name, Transform parent, Vector3 center, Material floorMaterial, Material wallMaterial)
        {
            const float deckTopY = 4.1f;
            const float guardHeight = 2.4f;
            CreateRoom(name, parent, center, MapSize(22f, 18f), floorMaterial, wallMaterial, RoomOpenings.West | RoomOpenings.East | RoomOpenings.South);
            CreateBlock("CentralHighGroundDeck", parent, center + MapPosition(2f, deckTopY * 0.5f, 0f), MapBlockScale(14f, deckTopY, 9f), floorMaterial);
            GameObject westRamp = CreateBlock("Ramp_West_To_HighGround", parent, center + MapPosition(-7f, 2.0f, 0f), MapBlockScale(9f, 0.35f, 4.2f), floorMaterial);
            westRamp.transform.rotation = Quaternion.Euler(0f, 0f, -7f);
            GameObject eastRamp = CreateBlock("Ramp_East_From_HighGround", parent, center + MapPosition(9f, 2.0f, 0f), MapBlockScale(9f, 0.35f, 4.2f), floorMaterial);
            eastRamp.transform.rotation = Quaternion.Euler(0f, 0f, 7f);
            CreateBlock("HighDeck_NorthGuardWall", parent, center + MapPosition(2f, deckTopY + guardHeight * 0.5f, 5.4f), MapBlockScale(14f, guardHeight, 0.35f), wallMaterial);
            CreateBlock("HighDeck_SouthGuardWall", parent, center + MapPosition(2f, deckTopY + guardHeight * 0.5f, -5.4f), MapBlockScale(14f, guardHeight, 0.35f), wallMaterial);
        }

        private static void CreateFinalVaultRoom(string name, Transform parent, Vector3 center, Material floorMaterial, Material wallMaterial)
        {
            const float goalPadTopY = 1.8f;
            CreateRoom(name, parent, center, MapSize(20f, 16f), floorMaterial, wallMaterial, RoomOpenings.West);
            CreateBlock("FinalGoalRaisedPad", parent, center + MapPosition(6f, goalPadTopY * 0.5f, 0f), MapBlockScale(8f, goalPadTopY, 7f), floorMaterial);
            GameObject westRamp = CreateBlock("Ramp_West_To_FinalGoalPad", parent, center + MapPosition(1.6f, 0.85f, 0f), MapBlockScale(4.2f, 0.3f, 3.4f), floorMaterial);
            westRamp.transform.rotation = Quaternion.Euler(0f, 0f, -6f);
            CreateBlock("FinalVault_BackWall_Heavy", parent, center + MapPosition(9.8f, 5.5f, 0f), MapBlockScale(0.7f, 11f, 16f), wallMaterial);
            CreateBlock("FinalVault_ResourceDais", parent, center + MapPosition(-2f, 0.35f, 5f), MapBlockScale(5f, 0.5f, 4f), floorMaterial);
        }

        private static void BuildRoomsAndCorridors(
            Transform geometryRoot,
            Transform propsRoot,
            Material floorMaterial,
            Material wallMaterial,
            Material propMaterial,
            Material colliderMaterial)
        {
            CreateRoom("01_StartRoom", geometryRoot, MapPosition(0f, 0f, 0f), MapSize(12f, 12f), floorMaterial, wallMaterial);
            CreateCorridor("02_NarrowCorridor", geometryRoot, MapPosition(14f, 0f, 0f), MapSize(16f, 4f), floorMaterial, wallMaterial);
            CreateRoom("03_DefenseKillzone", geometryRoot, MapPosition(30f, 0f, 0f), MapSize(16f, 16f), floorMaterial, wallMaterial);
            CreateRoom("04_CrossHub", geometryRoot, MapPosition(48f, 0f, 0f), MapSize(14f, 14f), floorMaterial, wallMaterial);
            CreateCorridor("05_TreasureBranch", geometryRoot, MapPosition(48f, 0f, 13f), MapSize(4f, 12f), floorMaterial, wallMaterial);
            CreateRoom("06_TreasurePocket", geometryRoot, MapPosition(48f, 0f, 25f), MapSize(10f, 10f), floorMaterial, wallMaterial);
            CreateRoom("07_HazardDecorRoom", geometryRoot, MapPosition(66f, 0f, 0f), MapSize(14f, 12f), floorMaterial, wallMaterial);
            CreateCorridor("08_FinalCorridor", geometryRoot, MapPosition(78f, 0f, 0f), MapSize(10f, 4f), floorMaterial, wallMaterial);
            CreateSlopeSample("09_SlopeRoom", geometryRoot, MapPosition(30f, 0f, -18f), floorMaterial, wallMaterial);

            PlaceKayKitVisual("Assets/90_ThirdParty/KayKit 1/Packs/KayKit - Platformer Pack (for Unity)/Prefabs/neutral/signage_arrows_right.prefab", "Visual_Signage_Right", propsRoot, MapPosition(13f, 0.2f, -2.4f), Quaternion.identity, Vector3.one * 2f);
            PlaceKayKitVisual("Assets/90_ThirdParty/KayKit 1/Packs/Bits/KayKit - Resource Bits (for Unity)/Prefabs/Containers_Crate_Large.prefab", "Visual_Crate_Large", propsRoot, MapPosition(31f, 0.2f, 5f), Quaternion.identity, Vector3.one * 2.5f);
            PlaceKayKitVisual("Assets/90_ThirdParty/KayKit 1/Packs/Bits/KayKit - Resource Bits (for Unity)/Prefabs/Pallet_Wood.prefab", "Visual_Pallet_Wood", propsRoot, MapPosition(35f, 0.2f, -5f), Quaternion.identity, Vector3.one * 2.5f);
            PlaceKayKitVisual("Assets/90_ThirdParty/KayKit 1/Packs/Bits/KayKit - Resource Bits (for Unity)/Prefabs/Gems_Chest.prefab", "Visual_Gems_Chest", propsRoot, MapPosition(45f, 0.2f, 26f), Quaternion.identity, Vector3.one * 2.5f);
            PlaceKayKitVisual("Assets/90_ThirdParty/KayKit 1/Packs/KayKit - Platformer Pack (for Unity)/Prefabs/neutral/floor_spikes_4x4x1.prefab", "Visual_FloorSpikes", propsRoot, MapPosition(65f, 0.15f, 0f), Quaternion.identity, Vector3.one * 3f);
            PlaceKayKitVisual("Assets/90_ThirdParty/KayKit 1/Packs/KayKit - Platformer Pack (for Unity)/Prefabs/neutral/sawblade.prefab", "Visual_Sawblade", propsRoot, MapPosition(69f, 0.35f, -3f), Quaternion.identity, Vector3.one * 3f);

            GameObject hazardCollider = CreateBlock("HazardDecor_NonDamage_ColliderMarker", geometryRoot, MapPosition(66f, 0.05f, 0f), MapBlockScale(5f, 0.1f, 5f), colliderMaterial);
            hazardCollider.GetComponent<Collider>().isTrigger = true;
        }

        private static void CreateRoom(string name, Transform parent, Vector3 center, Vector2 size, Material floorMaterial, Material wallMaterial)
        {
            CreateRoom(name, parent, center, size, floorMaterial, wallMaterial, RoomOpenings.All);
        }

        private static void CreateRoom(string name, Transform parent, Vector3 center, Vector2 size, Material floorMaterial, Material wallMaterial, RoomOpenings openings)
        {
            GameObject room = CreateChild(name, parent);
            CreateBlock("Floor", room.transform, center + new Vector3(0f, -0.1f, 0f), new Vector3(size.x, 0.2f, size.y), floorMaterial);
            CreateRoomWallsWithOpenings(room.transform, center, size, wallMaterial, openings);
        }

        private static void CreateRoomWallsWithOpenings(Transform parent, Vector3 center, Vector2 size, Material wallMaterial, RoomOpenings openings)
        {
            const float openingWidth = DoorOpeningWidth;
            float horizontalSegment = Mathf.Max(0.5f, (size.x - openingWidth) * 0.5f);
            float verticalSegment = Mathf.Max(0.5f, (size.y - openingWidth) * 0.5f);
            float horizontalOffset = openingWidth * 0.5f + horizontalSegment * 0.5f;
            float verticalOffset = openingWidth * 0.5f + verticalSegment * 0.5f;

            const float headerHeight = 1.4f;
            float headerY = WallHeight - headerHeight * 0.5f;
            if ((openings & RoomOpenings.North) != 0)
            {
                CreateBlock("Wall_North_WestSegment", parent, center + new Vector3(-horizontalOffset, WallHeight * 0.5f, size.y * 0.5f), new Vector3(horizontalSegment, WallHeight, WallThickness), wallMaterial);
                CreateBlock("Wall_North_EastSegment", parent, center + new Vector3(horizontalOffset, WallHeight * 0.5f, size.y * 0.5f), new Vector3(horizontalSegment, WallHeight, WallThickness), wallMaterial);
                CreateBlock("DoorHeader_North", parent, center + new Vector3(0f, headerY, size.y * 0.5f), new Vector3(openingWidth, headerHeight, WallThickness), wallMaterial);
            }
            else
            {
                CreateBlock("Wall_North", parent, center + new Vector3(0f, WallHeight * 0.5f, size.y * 0.5f), new Vector3(size.x, WallHeight, WallThickness), wallMaterial);
            }

            if ((openings & RoomOpenings.South) != 0)
            {
                CreateBlock("Wall_South_WestSegment", parent, center + new Vector3(-horizontalOffset, WallHeight * 0.5f, -size.y * 0.5f), new Vector3(horizontalSegment, WallHeight, WallThickness), wallMaterial);
                CreateBlock("Wall_South_EastSegment", parent, center + new Vector3(horizontalOffset, WallHeight * 0.5f, -size.y * 0.5f), new Vector3(horizontalSegment, WallHeight, WallThickness), wallMaterial);
                CreateBlock("DoorHeader_South", parent, center + new Vector3(0f, headerY, -size.y * 0.5f), new Vector3(openingWidth, headerHeight, WallThickness), wallMaterial);
            }
            else
            {
                CreateBlock("Wall_South", parent, center + new Vector3(0f, WallHeight * 0.5f, -size.y * 0.5f), new Vector3(size.x, WallHeight, WallThickness), wallMaterial);
            }

            if ((openings & RoomOpenings.West) != 0)
            {
                CreateBlock("Wall_West_NorthSegment", parent, center + new Vector3(-size.x * 0.5f, WallHeight * 0.5f, verticalOffset), new Vector3(WallThickness, WallHeight, verticalSegment), wallMaterial);
                CreateBlock("Wall_West_SouthSegment", parent, center + new Vector3(-size.x * 0.5f, WallHeight * 0.5f, -verticalOffset), new Vector3(WallThickness, WallHeight, verticalSegment), wallMaterial);
                CreateBlock("DoorHeader_West", parent, center + new Vector3(-size.x * 0.5f, headerY, 0f), new Vector3(WallThickness, headerHeight, openingWidth), wallMaterial);
            }
            else
            {
                CreateBlock("Wall_West", parent, center + new Vector3(-size.x * 0.5f, WallHeight * 0.5f, 0f), new Vector3(WallThickness, WallHeight, size.y), wallMaterial);
            }

            if ((openings & RoomOpenings.East) != 0)
            {
                CreateBlock("Wall_East_NorthSegment", parent, center + new Vector3(size.x * 0.5f, WallHeight * 0.5f, verticalOffset), new Vector3(WallThickness, WallHeight, verticalSegment), wallMaterial);
                CreateBlock("Wall_East_SouthSegment", parent, center + new Vector3(size.x * 0.5f, WallHeight * 0.5f, -verticalOffset), new Vector3(WallThickness, WallHeight, verticalSegment), wallMaterial);
                CreateBlock("DoorHeader_East", parent, center + new Vector3(size.x * 0.5f, headerY, 0f), new Vector3(WallThickness, headerHeight, openingWidth), wallMaterial);
            }
            else
            {
                CreateBlock("Wall_East", parent, center + new Vector3(size.x * 0.5f, WallHeight * 0.5f, 0f), new Vector3(WallThickness, WallHeight, size.y), wallMaterial);
            }
        }

        private static void CreateCorridor(string name, Transform parent, Vector3 center, Vector2 size, Material floorMaterial, Material wallMaterial)
        {
            GameObject corridor = CreateChild(name, parent);
            CreateBlock("Floor", corridor.transform, center + new Vector3(0f, -0.1f, 0f), new Vector3(size.x, 0.2f, size.y), floorMaterial);
            CreateBlock("Wall_North", corridor.transform, center + new Vector3(0f, CorridorWallHeight * 0.5f, size.y * 0.5f), new Vector3(size.x, CorridorWallHeight, WallThickness), wallMaterial);
            CreateBlock("Wall_South", corridor.transform, center + new Vector3(0f, CorridorWallHeight * 0.5f, -size.y * 0.5f), new Vector3(size.x, CorridorWallHeight, WallThickness), wallMaterial);
        }

        private static void CreateVerticalCorridor(string name, Transform parent, Vector3 center, Vector2 size, Material floorMaterial, Material wallMaterial)
        {
            GameObject corridor = CreateChild(name, parent);
            CreateBlock("Floor", corridor.transform, center + new Vector3(0f, -0.1f, 0f), new Vector3(size.x, 0.2f, size.y), floorMaterial);
            CreateBlock("Wall_East", corridor.transform, center + new Vector3(size.x * 0.5f, CorridorWallHeight * 0.5f, 0f), new Vector3(WallThickness, CorridorWallHeight, size.y), wallMaterial);
            CreateBlock("Wall_West", corridor.transform, center + new Vector3(-size.x * 0.5f, CorridorWallHeight * 0.5f, 0f), new Vector3(WallThickness, CorridorWallHeight, size.y), wallMaterial);
        }

        private static void CreateSlopeSample(string name, Transform parent, Vector3 center, Material floorMaterial, Material wallMaterial)
        {
            GameObject room = CreateChild(name, parent);
            CreateBlock("LowerFloor", room.transform, center + MapPosition(-4f, -0.1f, 0f), MapBlockScale(8f, 0.2f, 8f), floorMaterial);
            GameObject ramp = CreateBlock("RampVisualAndCollider", room.transform, center + MapPosition(3f, 0.6f, 0f), MapBlockScale(8f, 0.25f, 4f), floorMaterial);
            ramp.transform.rotation = Quaternion.Euler(0f, 0f, -12f);
            CreateBlock("UpperFloor", room.transform, center + MapPosition(10f, 1.1f, 0f), MapBlockScale(8f, 0.2f, 8f), floorMaterial);
            CreateBlock("SlopeWall_North", room.transform, center + MapPosition(3f, CorridorWallHeight * 0.5f, 4.2f), MapBlockScale(18f, CorridorWallHeight, 0.3f), wallMaterial);
            CreateBlock("SlopeWall_South", room.transform, center + MapPosition(3f, CorridorWallHeight * 0.5f, -4.2f), MapBlockScale(18f, CorridorWallHeight, 0.3f), wallMaterial);
        }

        private static void CreateSpawner(
            GameObject spawnerPrefab,
            GameObject routePointPrefab,
            GameObject enemyPrefab,
            Transform goal,
            Transform parent,
            string name,
            Vector3 position,
            IReadOnlyList<Vector3> waypointPositions)
        {
            GameObject spawnerObject = InstantiatePrefab(spawnerPrefab, name, parent, position, Quaternion.identity, Vector3.one);
            EnemySpawner spawner = spawnerObject.GetComponent<EnemySpawner>();
            EnemyRoute route = spawnerObject.GetComponent<EnemyRoute>();
            if (route == null)
            {
                route = spawnerObject.AddComponent<EnemyRoute>();
            }

            GameObject routeRoot = CreateChild(name + "_RoutePoints", spawnerObject.transform);
            List<Transform> waypoints = new List<Transform>();
            for (int i = 0; i < waypointPositions.Count; i++)
            {
                GameObject point = InstantiatePrefab(routePointPrefab, $"RoutePoint_{i + 1:00}", routeRoot.transform, waypointPositions[i], Quaternion.identity, Vector3.one);
                waypoints.Add(point.transform);
            }

            SerializedObject routeSo = new SerializedObject(route);
            SetObjectArray(routeSo, "waypoints", waypoints.ToArray());
            SetBool(routeSo, "includeFinalTarget", true);
            routeSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject spawnerSo = new SerializedObject(spawner);
            SetObject(spawnerSo, "enemyPrefab", enemyPrefab);
            SetObject(spawnerSo, "goal", goal);
            SetObject(spawnerSo, "route", route);
            SetInt(spawnerSo, "spawnCount", 20);
            SetBool(spawnerSo, "runUpdateLoop", false);
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePlacementPoints(GameObject placementPrefab, GameObject wallPlacementPrefab, Transform parent)
        {
            Vector3[] floorPoints =
            {
                MapPosition(0f, 0.15f, 0f),
                MapPosition(30f, 0.15f, -4f),
                MapPosition(30f, 0.15f, 4f),
                MapPosition(48f, 0.15f, 0f),
                MapPosition(66f, 0.15f, 4f),
                MapPosition(78f, 0.15f, -1.5f)
            };

            for (int i = 0; i < floorPoints.Length; i++)
            {
                InstantiatePrefab(placementPrefab, $"Sample_PlacementPoint_{i + 1:00}", parent, floorPoints[i], Quaternion.identity, Vector3.one);
            }

            InstantiatePrefab(wallPlacementPrefab, "Sample_WallPlacementPoint_North", parent, MapPosition(30f, WallHeight * 0.55f, 7.6f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            InstantiatePrefab(wallPlacementPrefab, "Sample_WallPlacementPoint_South", parent, MapPosition(30f, WallHeight * 0.55f, -7.6f), Quaternion.identity, Vector3.one);
        }

        private static void CreateTreasureChests(GameObject chestPrefab, TreasureChestRewardTable rewardTable, Transform parent)
        {
            GameObject chestA = InstantiatePrefab(chestPrefab, "Sample_TreasureChest_Pocket", parent, MapPosition(48f, 0.2f, 25f), Quaternion.identity, Vector3.one);
            GameObject chestB = InstantiatePrefab(chestPrefab, "Sample_TreasureChest_HazardReward", parent, MapPosition(69f, 0.2f, 4f), Quaternion.identity, Vector3.one);
            ConfigureChest(chestA, rewardTable, 0);
            ConfigureChest(chestB, rewardTable, 1);
        }

        private static void ConfigureChest(GameObject chestObject, TreasureChestRewardTable rewardTable, int roomIndex)
        {
            TreasureChest chest = chestObject.GetComponent<TreasureChest>();
            if (chest != null)
            {
                chest.ConfigureRewards(rewardTable, roomIndex);
            }
        }

        private static void ConfigureStagePlayerRewardServices(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            PlayerCurrencyWallet wallet = player.GetComponentInChildren<PlayerCurrencyWallet>(true);
            if (wallet == null)
            {
                wallet = player.AddComponent<PlayerCurrencyWallet>();
            }

            SerializedObject walletSo = new SerializedObject(wallet);
            SetInt(walletSo, "startingMoney", 500);
            walletSo.ApplyModifiedPropertiesWithoutUndo();

            if (player.GetComponentInChildren<PlayerItemInventory>(true) == null)
            {
                player.AddComponent<PlayerItemInventory>();
            }

            if (player.GetComponentInChildren<PlayerLevelProgression>(true) == null)
            {
                player.AddComponent<PlayerLevelProgression>();
            }
        }

        private static void CreateGates(GameObject gatePrefab, Transform parent)
        {
            InstantiatePrefab(gatePrefab, "Sample_Gate_Start_To_Corridor", parent, MapPosition(7f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 3f);
            InstantiatePrefab(gatePrefab, "Sample_Gate_Killzone_To_Hub", parent, MapPosition(40f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 3f);
            InstantiatePrefab(gatePrefab, "Sample_Gate_Hub_To_TreasureBranch", parent, MapPosition(48f, 0f, 8f), Quaternion.identity, Vector3.one * 3f);
        }

        private static GameObject CreateStageRuntime(TreasureChestRewardTable waveRewardTable)
        {
            GameObject runtimeRoot = new GameObject("StageRuntime");
            GameManager gameManager = runtimeRoot.AddComponent<GameManager>();
            UiInputCoordinator uiInputCoordinator = runtimeRoot.AddComponent<UiInputCoordinator>();
            ArtifactInventory artifactInventory = runtimeRoot.AddComponent<ArtifactInventory>();
            ArtifactStatManager artifactStatManager = runtimeRoot.AddComponent<ArtifactStatManager>();
            RewardGrantService rewardGrantService = runtimeRoot.AddComponent<RewardGrantService>();
            WaveRewardController waveRewardController = runtimeRoot.AddComponent<WaveRewardController>();
            StageRuntime stageRuntime = runtimeRoot.AddComponent<StageRuntime>();

            GameObject waveDirectorObject = CreateChild("WaveDirector", runtimeRoot.transform);
            WaveDirector waveDirector = waveDirectorObject.AddComponent<WaveDirector>();

            GameObject mainCanvasPrefab = LoadRequiredAsset<GameObject>(MainCanvasPrefabPath);
            Canvas mainCanvas = CreateMainCanvas(runtimeRoot.transform, mainCanvasPrefab);
            PlacementBuildMenuPresenter placementPresenter = GetRequiredComponentInChildren<PlacementBuildMenuPresenter>(mainCanvas.gameObject, "MainCanvas PlacementBuildMenuPresenter");
            InstalledObjectActionPresenter installedPresenter = GetRequiredComponentInChildren<InstalledObjectActionPresenter>(mainCanvas.gameObject, "MainCanvas InstalledObjectActionPresenter");
            TreasureRewardMenuPresenter treasurePresenter = GetRequiredComponentInChildren<TreasureRewardMenuPresenter>(mainCanvas.gameObject, "MainCanvas TreasureRewardMenuPresenter");
            SupportTruckShopPresenter shopPresenter = GetRequiredComponentInChildren<SupportTruckShopPresenter>(mainCanvas.gameObject, "MainCanvas SupportTruckShopPresenter");
            WaveReadyPopup readyPopup = GetRequiredComponentInChildren<WaveReadyPopup>(mainCanvas.gameObject, "MainCanvas WaveReadyPopup");
            GetRequiredComponentInChildren<PopupDimOverlayController>(mainCanvas.gameObject, "MainCanvas PopupDimOverlayController");

            Text waveStatusText = CreateText("WaveStatusText", mainCanvas.transform, new Vector2(260f, 90f), new Vector2(0.02f, 0.96f), 24, TextAnchor.UpperLeft);
            WaveStartNotificationPresenter waveStartNotification = CreateWaveStartNotification(mainCanvas.transform);
            CreateRealtimeMapHud(mainCanvas.transform, waveDirector);

            SerializedObject waveDirectorSo = new SerializedObject(waveDirector);
            SetObject(waveDirectorSo, "readyPopup", readyPopup);
            SetObject(waveDirectorSo, "statusText", waveStatusText);
            SetBool(waveDirectorSo, "startWaitingOnEnable", false);
            SetBool(waveDirectorSo, "disableSpawnerAutomationOnEnable", true);
            waveDirectorSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject artifactStatSo = new SerializedObject(artifactStatManager);
            SetObject(artifactStatSo, "inventory", artifactInventory);
            artifactStatSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject waveStartSo = new SerializedObject(waveStartNotification);
            SetObject(waveStartSo, "waveDirector", waveDirector);
            SetFloat(waveStartSo, "visibleDuration", 1.6f);
            waveStartSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject waveRewardSo = new SerializedObject(waveRewardController);
            SetObject(waveRewardSo, "waveDirector", waveDirector);
            SetObject(waveRewardSo, "rewardTable", waveRewardTable);
            SetObject(waveRewardSo, "rewardPresenter", treasurePresenter);
            SetObject(waveRewardSo, "rewardGrantService", rewardGrantService);
            SetObject(waveRewardSo, "artifactInventory", artifactInventory);
            SetInt(waveRewardSo, "rewardEveryNWave", 3);
            SetInt(waveRewardSo, "firstRewardWaveIndex", 2);
            waveRewardSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject runtimeSo = new SerializedObject(stageRuntime);
            SetObject(runtimeSo, "gameManager", gameManager);
            SetObject(runtimeSo, "uiInputCoordinator", uiInputCoordinator);
            SetObject(runtimeSo, "waveDirector", waveDirector);
            SetObject(runtimeSo, "waveReadyPopup", readyPopup);
            SetObject(runtimeSo, "mainCanvas", mainCanvas);
            SetObject(runtimeSo, "waveStatusText", waveStatusText);
            SetObject(runtimeSo, "placementBuildMenuPresenter", placementPresenter);
            SetObject(runtimeSo, "installedObjectActionPresenter", installedPresenter);
            SetObject(runtimeSo, "treasureRewardMenuPresenter", treasurePresenter);
            SetObject(runtimeSo, "supportTruckShopPresenter", shopPresenter);
            SetObject(runtimeSo, "waveStartNotificationPresenter", waveStartNotification);
            SetObject(runtimeSo, "waveRewardController", waveRewardController);
            SetObject(runtimeSo, "rewardGrantService", rewardGrantService);
            SetObject(runtimeSo, "artifactInventory", artifactInventory);
            SetObject(runtimeSo, "artifactStatManager", artifactStatManager);
            runtimeSo.ApplyModifiedPropertiesWithoutUndo();

            EnsureEventSystem(runtimeRoot.transform);
            return runtimeRoot;
        }

        private static void ConfigureWaveClearDoorConnector(GameObject runtimeRoot, StageLayoutRoot layoutRoot)
        {
            if (runtimeRoot == null || layoutRoot == null)
            {
                return;
            }

            WaveDirector waveDirector = runtimeRoot.GetComponentInChildren<WaveDirector>(true);
            WaveClearMapExpansionConnector connector = runtimeRoot.GetComponent<WaveClearMapExpansionConnector>();
            if (connector == null)
            {
                connector = runtimeRoot.AddComponent<WaveClearMapExpansionConnector>();
            }

            MapExpansionDoorOpener[] doorsByWave =
            {
                FindDoor(layoutRoot, "Stage1_Gate_Start_To_Entry"),
                FindDoor(layoutRoot, "Stage1_Gate_Entry_To_LowerDefense"),
                FindDoor(layoutRoot, "Stage1_Gate_LowerDefense_To_HighGround"),
                FindDoor(layoutRoot, "Stage1_Gate_HighGround_To_FinalApproach"),
                FindDoor(layoutRoot, "Stage1_Gate_FinalApproach_To_FinalVault")
            };

            SerializedObject connectorSo = new SerializedObject(connector);
            SetObject(connectorSo, "waveDirector", waveDirector);
            SetObjectArray(connectorSo, "doorsByWaveIndex", doorsByWave);
            SetBool(connectorSo, "openOnlyClosedDoors", true);
            connectorSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(connector);
        }

        private static MapExpansionDoorOpener FindDoor(StageLayoutRoot layoutRoot, string name)
        {
            MapExpansionDoorOpener[] doors = layoutRoot.Doors;
            for (int i = 0; doors != null && i < doors.Length; i++)
            {
                if (doors[i] != null && doors[i].GetComponentInParent<Transform>() != null && doors[i].transform.root != null)
                {
                    Transform parent = doors[i].transform;
                    while (parent != null)
                    {
                        if (parent.name == name)
                        {
                            return doors[i];
                        }

                        parent = parent.parent;
                    }
                }
            }

            return null;
        }

        private static Canvas CreateMainCanvas(Transform parent, GameObject mainCanvasPrefab)
        {
            GameObject canvasObject = InstantiatePrefab(mainCanvasPrefab, "MainCanvas", parent, Vector3.zero, Quaternion.identity, Vector3.one);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                throw new System.InvalidOperationException("Main canvas prefab is missing Canvas: " + MainCanvasPrefabPath);
            }

            return canvas;
        }

        private static T GetRequiredComponentInChildren<T>(GameObject root, string label) where T : Component
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component == null)
            {
                throw new System.InvalidOperationException(label + " is missing.");
            }

            return component;
        }

        private static PlacementBuildMenuPresenter CreatePlacementBuildMenu(Transform parent)
        {
            GameObject presenterObject = CreateChild("PlacementBuildMenuPresenter", parent);
            PlacementBuildMenuPresenter presenter = presenterObject.AddComponent<PlacementBuildMenuPresenter>();

            GameObject panel = CreatePanel("PlacementBuildMenuPanel", parent, new Vector2(520f, 380f), new Vector2(0.5f, 0.5f));
            Text titleText = CreateText("TitleText", panel.transform, new Vector2(440f, 54f), new Vector2(0.5f, 0.86f), 38, TextAnchor.MiddleCenter);
            Button slot1 = CreateButton("SlotButton_01", panel.transform, new Vector2(400f, 68f), new Vector2(0.5f, 0.62f), 28, out Text slot1Text);
            Button slot2 = CreateButton("SlotButton_02", panel.transform, new Vector2(400f, 68f), new Vector2(0.5f, 0.43f), 28, out Text slot2Text);
            Button slot3 = CreateButton("SlotButton_03", panel.transform, new Vector2(400f, 68f), new Vector2(0.5f, 0.24f), 28, out Text slot3Text);
            Text hintText = CreateText("HintText", panel.transform, new Vector2(460f, 42f), new Vector2(0.5f, 0.08f), 22, TextAnchor.MiddleCenter);

            SerializedObject so = new SerializedObject(presenter);
            SetObject(so, "panelRoot", panel);
            SetObject(so, "titleText", titleText);
            SetObject(so, "turretButton", slot1);
            SetObject(so, "turretButtonText", slot1Text);
            SetObject(so, "barricadeButton", slot2);
            SetObject(so, "barricadeButtonText", slot2Text);
            SetObject(so, "mortarButton", slot3);
            SetObject(so, "mortarButtonText", slot3Text);
            SetObject(so, "hintText", hintText);
            so.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);
            return presenter;
        }

        private static WaveReadyPopup CreateWaveReadyPopup(Transform parent)
        {
            GameObject root = CreatePanel("WaveReadyPopup", parent, new Vector2(520f, 260f), new Vector2(0.5f, 0.5f));
            Text messageText = CreateText("MessageText", root.transform, new Vector2(440f, 110f), new Vector2(0.5f, 0.68f), 30, TextAnchor.MiddleCenter);
            Button readyButton = CreateButton("ReadyButton", root.transform, new Vector2(190f, 56f), new Vector2(0.32f, 0.26f), 24, out Text readyText);
            readyText.text = "1 / E  Start";
            Button cancelButton = CreateButton("CancelButton", root.transform, new Vector2(190f, 56f), new Vector2(0.68f, 0.26f), 24, out Text cancelText);
            cancelText.text = "2  Cancel";

            WaveReadyPopup popup = root.AddComponent<WaveReadyPopup>();
            SerializedObject so = new SerializedObject(popup);
            SetObject(so, "root", root);
            SetObject(so, "messageText", messageText);
            SetObject(so, "readyButton", readyButton);
            SetObject(so, "cancelButton", cancelButton);
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return popup;
        }

        private static WaveStartNotificationPresenter CreateWaveStartNotification(Transform parent)
        {
            GameObject presenterObject = CreateChild("WaveStartNotificationPresenter", parent);
            Canvas canvas = presenterObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 250;
            CanvasScaler scaler = presenterObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            WaveStartNotificationPresenter presenter = presenterObject.AddComponent<WaveStartNotificationPresenter>();

            GameObject root = CreatePanel("WaveStartNotification", presenterObject.transform, new Vector2(560f, 150f), new Vector2(0.5f, 0.82f));
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Image panelImage = root.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.sprite = LoadRequiredAsset<Sprite>(PopupBackgroundPath);
                panelImage.type = Image.Type.Sliced;
                panelImage.color = new Color(0.035f, 0.16f, 0.38f, 0.94f);
                panelImage.raycastTarget = false;
            }

            Text messageText = CreateText("MessageText", root.transform, new Vector2(500f, 108f), new Vector2(0.5f, 0.5f), 34, TextAnchor.MiddleCenter);
            messageText.color = new Color(0.65f, 0.95f, 1f, 1f);

            SerializedObject so = new SerializedObject(presenter);
            SetObject(so, "root", root);
            SetObject(so, "messageText", messageText);
            SetObject(so, "canvasGroup", canvasGroup);
            SetFloat(so, "visibleDuration", 1.6f);
            so.ApplyModifiedPropertiesWithoutUndo();
            return presenter;
        }

        private static PopupDimOverlayController CreatePopupDimOverlay(Transform parent, Canvas canvas)
        {
            GameObject overlayObject = CreateChild("PopupDimOverlay", parent);
            RectTransform overlayRect = overlayObject.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.sizeDelta = Vector2.zero;

            Image overlayImage = overlayObject.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.48f);
            CanvasGroup canvasGroup = overlayObject.AddComponent<CanvasGroup>();
            PopupDimOverlayController overlay = overlayObject.AddComponent<PopupDimOverlayController>();
            MousePositionIconPresenter mouseIcon = CreateMousePositionIcon("PopupMouseCursorIcon", overlayObject.transform, canvas);

            SerializedObject so = new SerializedObject(overlay);
            SetObject(so, "overlayImage", overlayImage);
            SetObject(so, "canvasGroup", canvasGroup);
            SetObject(so, "mouseIconPresenter", mouseIcon);
            so.ApplyModifiedPropertiesWithoutUndo();

            overlayObject.SetActive(false);
            return overlay;
        }

        private static MousePositionIconPresenter CreateMousePositionIcon(string name, Transform parent, Canvas canvas)
        {
            GameObject iconObject = CreateChild(name, parent);
            RectTransform rect = iconObject.AddComponent<RectTransform>();
            SetRect(rect, new Vector2(54f, 54f), new Vector2(0.5f, 0.5f));
            Image image = iconObject.AddComponent<Image>();
            image.color = Color.white;
            image.sprite = LoadRequiredAsset<Sprite>(MouseCursorIconPath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            iconObject.SetActive(false);

            MousePositionIconPresenter presenter = iconObject.AddComponent<MousePositionIconPresenter>();
            SerializedObject so = new SerializedObject(presenter);
            SetObject(so, "iconRoot", rect);
            SetObject(so, "iconImage", image);
            SetObject(so, "targetCanvas", canvas);
            so.ApplyModifiedPropertiesWithoutUndo();
            return presenter;
        }

        private static RealtimeMapHudPresenter CreateRealtimeMapHud(Transform parent, WaveDirector waveDirector)
        {
            GameObject framePrefab = LoadRequiredAsset<GameObject>(MapHudFramePrefabPath);
            GameObject contentPrefab = LoadRequiredAsset<GameObject>(MapHudContentPrefabPath);
            GameObject statusFramePrefab = LoadRequiredAsset<GameObject>(MapHudStatusFramePrefabPath);
            Sprite markerSprite = EnsureSpriteAsset(MapHudMarkerPath);
            Sprite mapIconSprite = EnsureSpriteAsset(MapHudMapIconPath);
            Sprite rangeRingSprite = EnsureSpriteAsset(MapHudRangeRingPath);
            Sprite mapShadowSprite = EnsureSpriteAsset(MapHudShadowPath);

            GameObject root = CreateChild("RealtimeMapHud", parent);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            SetRect(rootRect, new Vector2(236f, 292f), new Vector2(0f, 1f));
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(126f, -150f);
            rootRect.localScale = new Vector3(0.9f, 0.9f, 1f);

            Image shadowImage = CreateMapHudImage("MapDropShadow", root.transform, new Vector2(236f, 236f), new Vector2(0.5f, 0.54f), new Color(0f, 0f, 0f, 0.36f), mapShadowSprite);
            shadowImage.transform.SetAsFirstSibling();

            GameObject mapFrame = InstantiateUiPrefab(framePrefab, "MapFrame", root.transform, new Vector2(210f, 210f), new Vector2(0.5f, 0.58f));
            RemoveChildByName(mapFrame.transform, "Icon");
            RemoveChildByName(mapFrame.transform, "Text (TMP)");
            RemoveChildByName(mapFrame.transform, "Text (TMP) (1)");
            ConfigureImage(mapFrame, new Color(0.72f, 0.93f, 1f, 1f), true);

            GameObject titleFrame = InstantiateUiPrefab(statusFramePrefab, "MapTitleFrame", root.transform, new Vector2(150f, 42f), new Vector2(0.5f, 0.965f));
            RemoveDescendantsByName(titleFrame.transform, "Image_Chest");
            RemoveDescendantsByName(titleFrame.transform, "Text (TMP)");
            RemoveDescendantsByName(titleFrame.transform, "Text (TMP) (1)");
            ConfigureImage(titleFrame, new Color(0.16f, 0.66f, 1f, 1f), true);
            CreateMapHudImage("MapIcon", titleFrame.transform, new Vector2(26f, 26f), new Vector2(0.24f, 0.5f), Color.white, mapIconSprite);
            Text titleText = CreateText("TitleText", titleFrame.transform, new Vector2(86f, 24f), new Vector2(0.61f, 0.5f), 16, TextAnchor.MiddleCenter);
            titleText.text = "MAP";
            titleText.color = Color.white;
            titleText.raycastTarget = false;

            GameObject contentObject = InstantiateUiPrefab(contentPrefab, "MapContent", root.transform, new Vector2(178f, 178f), new Vector2(0.5f, 0.58f));
            RemoveChildByName(contentObject.transform, "Icon");
            RemoveChildByName(contentObject.transform, "Icon_Check");
            RemoveChildByName(contentObject.transform, "Text (TMP)");
            RemoveChildByName(contentObject.transform, "Text (TMP) (1)");
            ConfigureImage(contentObject, new Color(0.035f, 0.13f, 0.19f, 0.94f), false);
            ConfigureChildImage(contentObject.transform, "LIght", new Color(0.55f, 0.96f, 1f, 0.34f));
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentObject.AddComponent<RectMask2D>();
            CreateMapHudImage("RangeRingOuter", contentRect, new Vector2(142f, 142f), new Vector2(0.5f, 0.5f), new Color(0.35f, 0.94f, 1f, 0.16f), rangeRingSprite);
            CreateMapHudImage("RangeRingInner", contentRect, new Vector2(86f, 86f), new Vector2(0.5f, 0.5f), new Color(0.35f, 0.94f, 1f, 0.12f), rangeRingSprite);
            CreateMapHudGridLines(contentRect);

            GameObject statusFrame = InstantiateUiPrefab(statusFramePrefab, "WaveStatusFrame", root.transform, new Vector2(204f, 54f), new Vector2(0.5f, 0.1f));
            RemoveDescendantsByName(statusFrame.transform, "Image_Chest");
            RemoveDescendantsByName(statusFrame.transform, "Text (TMP)");
            RemoveDescendantsByName(statusFrame.transform, "Text (TMP) (1)");

            Text timerText = CreateText("WaveTimerText", statusFrame.transform, new Vector2(178f, 28f), new Vector2(0.5f, 0.5f), 18, TextAnchor.MiddleCenter);
            timerText.color = new Color(0.93f, 0.99f, 1f, 1f);
            timerText.raycastTarget = false;

            RectTransform playerMarker = CreateMapMarker("PlayerMarker", contentRect, markerSprite, new Vector2(18f, 18f), new Color(0.35f, 0.95f, 1f, 1f));
            RectTransform enemyMarkerTemplate = CreateMapMarker("EnemyMarkerTemplate", contentRect, markerSprite, new Vector2(12f, 12f), new Color(1f, 0.24f, 0.2f, 0.95f));
            RectTransform doorMarkerTemplate = CreateMapMarker("DoorMarkerTemplate", contentRect, markerSprite, new Vector2(14f, 14f), new Color(1f, 0.82f, 0.25f, 0.95f));

            RealtimeMapHudPresenter presenter = root.AddComponent<RealtimeMapHudPresenter>();
            SerializedObject so = new SerializedObject(presenter);
            SetObject(so, "mapContentRoot", contentRect);
            SetObject(so, "playerMarker", playerMarker);
            SetObject(so, "enemyMarkerTemplate", enemyMarkerTemplate);
            SetObject(so, "doorMarkerTemplate", doorMarkerTemplate);
            SetObject(so, "waveTimerText", timerText);
            SetObject(so, "waveDirector", waveDirector);
            SetBool(so, "centerOnPlayer", true);
            SetVector2(so, "playerCenteredHalfExtent", new Vector2(28f, 18f));
            SetFloat(so, "refreshInterval", 0.2f);
            SetInt(so, "maxEnemyMarkers", 128);
            SetBool(so, "showWorldGeometry", true);
            SetInt(so, "maxWorldGeometryShapes", 384);
            SetColor(so, "floorGeometryColor", new Color(0.2f, 0.7f, 0.9f, 0.24f));
            SetColor(so, "wallGeometryColor", new Color(0.78f, 0.95f, 1f, 0.56f));
            SetColor(so, "rampGeometryColor", new Color(0.35f, 1f, 0.72f, 0.32f));
            so.ApplyModifiedPropertiesWithoutUndo();

            enemyMarkerTemplate.gameObject.SetActive(false);
            doorMarkerTemplate.gameObject.SetActive(false);
            return presenter;
        }

        private static RectTransform CreateMapMarker(string name, Transform parent, Sprite markerSprite, Vector2 size, Color color)
        {
            GameObject markerObject = CreateChild(name, parent);
            RectTransform markerRect = markerObject.AddComponent<RectTransform>();
            SetRect(markerRect, size, new Vector2(0.5f, 0.5f));
            Image markerImage = markerObject.AddComponent<Image>();
            markerImage.sprite = markerSprite;
            markerImage.color = color;
            markerImage.preserveAspect = true;
            markerImage.raycastTarget = false;
            return markerRect;
        }

        private static Image CreateMapHudImage(string name, Transform parent, Vector2 size, Vector2 anchor, Color color, Sprite sprite)
        {
            GameObject imageObject = CreateChild(name, parent);
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            SetRect(rect, size, anchor);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateMapHudGridLines(Transform parent)
        {
            for (int i = 1; i < 4; i++)
            {
                float normalized = i / 4f;
                CreateMapHudImage("GridVertical_" + i.ToString("00"), parent, new Vector2(2f, 154f), new Vector2(normalized, 0.5f), new Color(0.55f, 0.95f, 1f, 0.16f), null);
                CreateMapHudImage("GridHorizontal_" + i.ToString("00"), parent, new Vector2(154f, 2f), new Vector2(0.5f, normalized), new Color(0.55f, 0.95f, 1f, 0.16f), null);
            }
        }

        private static GameObject InstantiateUiPrefab(GameObject prefab, string name, Transform parent, Vector2 size, Vector2 anchor)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                throw new System.InvalidOperationException("Failed to instantiate UI prefab: " + prefab.name);
            }

            instance.name = name;
            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = instance.AddComponent<RectTransform>();
            }

            SetRect(rect, size, anchor);
            SetRaycastTarget(instance.transform, false);
            return instance;
        }

        private static void RemoveChildByName(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void RemoveDescendantsByName(Transform parent, string childName)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    Object.DestroyImmediate(child.gameObject);
                    continue;
                }

                RemoveDescendantsByName(child, childName);
            }
        }

        private static void SetRaycastTarget(Transform root, bool value)
        {
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = value;
            }
        }

        private static void ConfigureImage(GameObject target, Color color, bool preserveAspect)
        {
            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
        }

        private static void ConfigureChildImage(Transform root, string childName, Color color)
        {
            Transform child = root.Find(childName);
            Image image = child != null ? child.GetComponent<Image>() : null;
            if (image == null)
            {
                return;
            }

            image.color = color;
            image.raycastTarget = false;
        }

        private static void EnsureEventSystem(Transform parent)
        {
            GameObject eventSystemObject = CreateChild("EventSystem", parent);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private static Text CreateText(string name, Transform parent, Vector2 size, Vector2 anchor, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = CreateChild(name, parent);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            SetRect(rect, size, anchor);
            Text text = textObject.AddComponent<Text>();
            text.font = LoadRequiredAsset<Font>(KoreanFontPath);
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 size, Vector2 anchor, int fontSize, out Text text)
        {
            GameObject buttonObject = CreatePanel(name, parent, size, anchor);
            Button button = buttonObject.AddComponent<Button>();
            text = CreateText("Text", buttonObject.transform, size, new Vector2(0.5f, 0.5f), fontSize, TextAnchor.MiddleCenter);
            return button;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 size, Vector2 anchor)
        {
            GameObject panel = CreateChild(name, parent);
            RectTransform rect = panel.AddComponent<RectTransform>();
            SetRect(rect, size, anchor);
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.05f, 0.07f, 0.08f, 0.9f);
            return panel;
        }

        private static void SetRect(RectTransform rect, Vector2 size, Vector2 anchor)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = scale;
            if (material != null && block.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = material;
            }

            return block;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(prefab);
            }

            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;
            return instance;
        }

        private static void PlaceKayKitVisual(string path, string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject prefab = LoadOptionalAsset<GameObject>(path);
            if (prefab == null)
            {
                return;
            }

            InstantiatePrefab(prefab, name, parent, position, rotation, scale);
        }

        private static Vector3 MapPosition(float x, float y, float z)
        {
            return new Vector3(x * PlanScale, y, z * PlanScale);
        }

        private static Vector2 MapSize(float x, float z)
        {
            return new Vector2(x * PlanScale, z * PlanScale);
        }

        private static Vector3 MapBlockScale(float x, float y, float z)
        {
            return new Vector3(x * PlanScale, y, z * PlanScale);
        }

        private static ArtifactDefinitionSO[] CreateArtifactDefinitions()
        {
            Sprite turretIcon = CreateArtifactIcon(ArtifactIconFolder + "/Icon_Artifact_TurretLens.png", ArtifactIconShape.OpticModule, new Color(0.15f, 0.95f, 1f), new Color(0.02f, 0.12f, 0.16f));
            Sprite mortarIcon = CreateArtifactIcon(ArtifactIconFolder + "/Icon_Artifact_MortarCore.png", ArtifactIconShape.MortarCore, new Color(1f, 0.55f, 0.18f), new Color(0.13f, 0.05f, 0.02f));
            Sprite squadIcon = CreateArtifactIcon(ArtifactIconFolder + "/Icon_Artifact_SquadRelay.png", ArtifactIconShape.RelayUnit, new Color(0.55f, 1f, 0.42f), new Color(0.03f, 0.12f, 0.04f));
            Sprite playerIcon = CreateArtifactIcon(ArtifactIconFolder + "/Icon_Artifact_PlayerExoFrame.png", ArtifactIconShape.ExoFramePlate, new Color(0.85f, 0.75f, 1f), new Color(0.08f, 0.04f, 0.14f));

            ArtifactDefinitionSO turret = CreateArtifactDefinition(
                ArtifactTurretPath,
                "artifact_turret_lens",
                "포탑 조준 렌즈",
                "포탑 피해와 사거리를 증가시킵니다.",
                turretIcon,
                new ArtifactStatModifierData(ArtifactTarget.Turret, ArtifactStat.Damage, 1.12f),
                new ArtifactStatModifierData(ArtifactTarget.Turret, ArtifactStat.Range, 1.08f));
            ArtifactDefinitionSO mortar = CreateArtifactDefinition(
                ArtifactMortarPath,
                "artifact_mortar_core",
                "박격포 압축 코어",
                "박격포 피해를 증가시키고 쿨다운을 줄입니다.",
                mortarIcon,
                new ArtifactStatModifierData(ArtifactTarget.Mortar, ArtifactStat.Damage, 1.15f),
                new ArtifactStatModifierData(ArtifactTarget.Mortar, ArtifactStat.Cooldown, 0.9f));
            ArtifactDefinitionSO squad = CreateArtifactDefinition(
                ArtifactSquadPath,
                "artifact_squad_relay",
                "분대 전술 릴레이",
                "분대원 피해와 사거리를 증가시킵니다.",
                squadIcon,
                new ArtifactStatModifierData(ArtifactTarget.Squad, ArtifactStat.Damage, 1.1f),
                new ArtifactStatModifierData(ArtifactTarget.Squad, ArtifactStat.Range, 1.08f));
            ArtifactDefinitionSO player = CreateArtifactDefinition(
                ArtifactPlayerPath,
                "artifact_player_exoframe",
                "전술 외골격 프레임",
                "캐릭터 최대 체력과 이동 속도를 증가시킵니다.",
                playerIcon,
                new ArtifactStatModifierData(ArtifactTarget.Player, ArtifactStat.Health, 1.12f),
                new ArtifactStatModifierData(ArtifactTarget.Player, ArtifactStat.MoveSpeed, 1.06f));

            return new[] { turret, mortar, squad, player };
        }

        private enum ArtifactIconShape
        {
            OpticModule,
            MortarCore,
            RelayUnit,
            ExoFramePlate
        }

        private readonly struct ArtifactStatModifierData
        {
            public ArtifactStatModifierData(ArtifactTarget target, ArtifactStat stat, float multiplier)
            {
                Target = target;
                Stat = stat;
                Multiplier = multiplier;
            }

            public ArtifactTarget Target { get; }
            public ArtifactStat Stat { get; }
            public float Multiplier { get; }
        }

        private static ArtifactDefinitionSO CreateArtifactDefinition(
            string path,
            string artifactId,
            string displayName,
            string description,
            Sprite icon,
            params ArtifactStatModifierData[] modifiers)
        {
            ArtifactDefinitionSO definition = LoadOrCreateAsset<ArtifactDefinitionSO>(path);
            SerializedObject so = new SerializedObject(definition);
            SetString(so, "artifactId", artifactId);
            SetString(so, "displayName", displayName);
            SetString(so, "description", description);
            SetObject(so, "icon", icon);

            SerializedProperty modifierArray = so.FindProperty("modifiers");
            modifierArray.arraySize = modifiers.Length;
            for (int i = 0; i < modifiers.Length; i++)
            {
                SerializedProperty modifier = modifierArray.GetArrayElementAtIndex(i);
                modifier.FindPropertyRelative("target").enumValueIndex = (int)modifiers[i].Target;
                modifier.FindPropertyRelative("stat").enumValueIndex = (int)modifiers[i].Stat;
                modifier.FindPropertyRelative("multiplier").floatValue = modifiers[i].Multiplier;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static TreasureChestRewardTable CreateWaveRewardTable(ArtifactDefinitionSO[] artifacts)
        {
            TreasureChestRewardTable table = LoadOrCreateAsset<TreasureChestRewardTable>(WaveRewardTablePath);
            SerializedObject so = new SerializedObject(table);
            SerializedProperty rewards = so.FindProperty("rewards");
            int artifactCount = artifacts != null ? artifacts.Length : 0;
            rewards.arraySize = artifactCount + 3;
            Sprite statPointIcon = AssetDatabase.LoadAssetAtPath<Sprite>(WaveRewardStatPointIconPath);
            Sprite moneyIcon = AssetDatabase.LoadAssetAtPath<Sprite>(WaveRewardMoneyIconPath);
            Sprite killDataIcon = AssetDatabase.LoadAssetAtPath<Sprite>(WaveRewardKillDataIconPath);

            for (int i = 0; i < artifactCount; i++)
            {
                ArtifactDefinitionSO artifact = artifacts[i];
                SetRewardEntry(
                    rewards.GetArrayElementAtIndex(i),
                    artifact != null ? artifact.ArtifactId : $"artifact_missing_{i}",
                    artifact != null ? artifact.DisplayName : "Artifact Missing",
                    1,
                    artifact != null ? artifact.Icon : null,
                    TreasureRewardGrantType.Artifact,
                    artifact);
            }

            SetRewardEntry(rewards.GetArrayElementAtIndex(artifactCount), "wave_reward_stat_point", "전투 데이터 +1", 1, statPointIcon, TreasureRewardGrantType.StatPoint, null);
            SetRewardEntry(rewards.GetArrayElementAtIndex(artifactCount + 1), "wave_reward_gold_cache", "보급 크레딧", 80, moneyIcon, TreasureRewardGrantType.Money, null);
            SetRewardEntry(rewards.GetArrayElementAtIndex(artifactCount + 2), "wave_reward_kill_data", "전투 경험 데이터", 5, killDataIcon, TreasureRewardGrantType.KillProgress, null);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            return table;
        }

        private static void SetRewardEntry(
            SerializedProperty entry,
            string rewardId,
            string displayName,
            int amount,
            Sprite icon,
            TreasureRewardGrantType grantType,
            ArtifactDefinitionSO artifact)
        {
            entry.FindPropertyRelative("rewardId").stringValue = rewardId;
            entry.FindPropertyRelative("displayName").stringValue = displayName;
            entry.FindPropertyRelative("amount").intValue = Mathf.Max(1, amount);
            entry.FindPropertyRelative("icon").objectReferenceValue = icon;
            entry.FindPropertyRelative("grantType").enumValueIndex = (int)grantType;
            entry.FindPropertyRelative("itemDefinition").objectReferenceValue = null;
            entry.FindPropertyRelative("artifactDefinition").objectReferenceValue = artifact;
        }

        private static Sprite CreateArtifactIcon(string path, ArtifactIconShape shape, Color accent, Color background)
        {
            EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/"));
            Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existingSprite != null)
            {
                return existingSprite;
            }

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = (new Vector2(x, y) - center) / (size * 0.5f);
                    Color pixel = Color.clear;
                    bool basePlate = InRoundedBox(p, Vector2.zero, new Vector2(0.78f, 0.78f), 0.16f);
                    bool baseEdge = InRoundedBox(p, Vector2.zero, new Vector2(0.84f, 0.84f), 0.18f) && !basePlate;

                    if (basePlate)
                    {
                        pixel = Color.Lerp(background, new Color(0.01f, 0.01f, 0.012f, 1f), Mathf.Clamp01(p.magnitude * 0.65f));
                    }
                    else if (baseEdge)
                    {
                        pixel = Color.Lerp(accent, Color.black, 0.45f);
                    }

                    float mask = GetArtifactIconMask(shape, p);
                    float glow = GetArtifactIconGlow(shape, p);
                    if (glow > 0f && pixel.a > 0f)
                    {
                        pixel = Color.Lerp(pixel, accent, glow * 0.32f);
                    }

                    if (mask > 0f)
                    {
                        Color lit = Color.Lerp(accent, Color.white, Mathf.Clamp01(mask * 0.38f));
                        pixel = Color.Lerp(new Color(0.02f, 0.025f, 0.03f, 1f), lit, Mathf.Clamp01(mask));
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite EnsureSpriteAsset(string path)
        {
            Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existingSprite != null)
            {
                return existingSprite;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException("Sprite texture not found: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new System.InvalidOperationException("Sprite import failed: " + path);
            }

            return sprite;
        }

        private static float GetArtifactIconMask(ArtifactIconShape shape, Vector2 p)
        {
            switch (shape)
            {
                case ArtifactIconShape.OpticModule:
                    return MaxMask(
                        RoundedBoxMask(p, new Vector2(0.02f, 0f), new Vector2(0.5f, 0.3f), 0.1f),
                        RingMask(p, new Vector2(-0.08f, 0.02f), 0.23f, 0.08f),
                        CircleMask(p, new Vector2(-0.08f, 0.02f), 0.08f),
                        RoundedBoxMask(p, new Vector2(0.47f, 0.02f), new Vector2(0.22f, 0.12f), 0.04f),
                        SegmentMask(p, new Vector2(-0.58f, -0.33f), new Vector2(-0.35f, -0.22f), 0.045f));

                case ArtifactIconShape.MortarCore:
                    return MaxMask(
                        RoundedBoxMask(p, new Vector2(0f, -0.05f), new Vector2(0.22f, 0.58f), 0.18f),
                        RoundedBoxMask(p, new Vector2(0f, 0.47f), new Vector2(0.32f, 0.14f), 0.08f),
                        SegmentMask(p, new Vector2(-0.24f, -0.47f), new Vector2(-0.48f, -0.7f), 0.055f),
                        SegmentMask(p, new Vector2(0.24f, -0.47f), new Vector2(0.48f, -0.7f), 0.055f),
                        RingMask(p, new Vector2(0f, -0.1f), 0.13f, 0.045f));

                case ArtifactIconShape.RelayUnit:
                    return MaxMask(
                        RoundedBoxMask(p, new Vector2(0f, -0.18f), new Vector2(0.42f, 0.34f), 0.08f),
                        RoundedBoxMask(p, new Vector2(0f, 0.24f), new Vector2(0.23f, 0.14f), 0.05f),
                        SegmentMask(p, new Vector2(0.18f, 0.35f), new Vector2(0.54f, 0.68f), 0.04f),
                        SegmentMask(p, new Vector2(-0.18f, 0.35f), new Vector2(-0.54f, 0.68f), 0.04f),
                        CircleMask(p, new Vector2(0.58f, 0.72f), 0.075f),
                        CircleMask(p, new Vector2(-0.58f, 0.72f), 0.075f),
                        CircleMask(p, new Vector2(0f, -0.18f), 0.12f));

                case ArtifactIconShape.ExoFramePlate:
                    return MaxMask(
                        RoundedBoxMask(p, new Vector2(0f, 0.16f), new Vector2(0.34f, 0.32f), 0.1f),
                        SegmentMask(p, new Vector2(-0.32f, 0.12f), new Vector2(-0.6f, -0.16f), 0.07f),
                        SegmentMask(p, new Vector2(0.32f, 0.12f), new Vector2(0.6f, -0.16f), 0.07f),
                        SegmentMask(p, new Vector2(-0.12f, -0.16f), new Vector2(-0.34f, -0.62f), 0.065f),
                        SegmentMask(p, new Vector2(0.12f, -0.16f), new Vector2(0.34f, -0.62f), 0.065f),
                        RingMask(p, new Vector2(0f, 0.18f), 0.13f, 0.045f));

                default:
                    return 0f;
            }
        }

        private static float GetArtifactIconGlow(ArtifactIconShape shape, Vector2 p)
        {
            switch (shape)
            {
                case ArtifactIconShape.OpticModule:
                    return Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(-0.08f, 0.02f)) / 0.55f);
                case ArtifactIconShape.MortarCore:
                    return Mathf.Clamp01(1f - Mathf.Abs(p.x) / 0.55f) * Mathf.Clamp01(1f - Mathf.Abs(p.y + 0.05f) / 0.95f);
                case ArtifactIconShape.RelayUnit:
                    return Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(0f, 0.12f)) / 0.8f);
                case ArtifactIconShape.ExoFramePlate:
                    return Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(0f, 0f)) / 0.75f);
                default:
                    return 0f;
            }
        }

        private static float MaxMask(params float[] values)
        {
            float max = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                max = Mathf.Max(max, values[i]);
            }

            return max;
        }

        private static float RoundedBoxMask(Vector2 p, Vector2 center, Vector2 halfSize, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - halfSize + Vector2.one * radius;
            float outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude - radius;
            float inside = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
            float distance = outside + inside;
            return Mathf.Clamp01((-distance + 0.025f) / 0.05f);
        }

        private static bool InRoundedBox(Vector2 p, Vector2 center, Vector2 halfSize, float radius)
        {
            return RoundedBoxMask(p, center, halfSize, radius) > 0f;
        }

        private static float CircleMask(Vector2 p, Vector2 center, float radius)
        {
            float distance = Vector2.Distance(p, center);
            return Mathf.Clamp01((radius - distance + 0.02f) / 0.04f);
        }

        private static float RingMask(Vector2 p, Vector2 center, float radius, float thickness)
        {
            float distance = Mathf.Abs(Vector2.Distance(p, center) - radius);
            return Mathf.Clamp01((thickness - distance + 0.015f) / 0.03f);
        }

        private static float SegmentMask(Vector2 p, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, Vector2.Dot(ab, ab)));
            float distance = Vector2.Distance(p, a + ab * t);
            return Mathf.Clamp01((thickness - distance + 0.018f) / 0.036f);
        }

        private static BuildableDefinitionSO CreateBuildableDefinition(
            string path,
            string id,
            string displayName,
            BuildableKind kind,
            BuildableCategory category,
            GameObject prefab,
            bool rotateBeforeInstall,
            params BuildableRoleDefinitionSO[] roleDefinitions)
        {
            BuildableDefinitionSO definition = LoadOrCreateAsset<BuildableDefinitionSO>(path);
            SerializedObject so = new SerializedObject(definition);
            SetString(so, "buildableId", id);
            SetString(so, "displayName", displayName);
            SetEnum(so, "kind", (int)kind);
            SetEnum(so, "category", (int)category);
            SetObject(so, "prefab", prefab);
            SetBool(so, "rotateBeforeInstall", rotateBeforeInstall);
            SetObjectArray(so, "roleDefinitions", roleDefinitions);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static EnemyWaveDefinition CreateWaveDefinition(string path, string waveId, string spawnerNameContains, int spawnCount, float spawnInterval)
        {
            EnemyWaveDefinition wave = LoadOrCreateAsset<EnemyWaveDefinition>(path);
            SerializedObject so = new SerializedObject(wave);
            SetString(so, "waveId", waveId);
            SetFloat(so, "autoStartDelay", 999f);
            SerializedProperty rules = so.FindProperty("spawnRules");
            rules.arraySize = 1;
            SerializedProperty rule = rules.GetArrayElementAtIndex(0);
            rule.FindPropertyRelative("spawnerNameContains").stringValue = spawnerNameContains;
            rule.FindPropertyRelative("spawnCount").intValue = spawnCount;
            rule.FindPropertyRelative("spawnInterval").floatValue = spawnInterval;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wave);
            return wave;
        }

        private static StageDefinitionSO CreateStageDefinition(
            EnemyWaveDefinition[] waves,
            TreasureChestRewardTable rewardTable,
            SupportTruckShopCatalogSO supportTruckCatalog,
            BuildableDefinitionSO[] buildableDefinitions,
            GameObject enemyPrefab,
            EnemyCatalogSO enemyCatalog,
            DifficultyProgressionSO difficultyProgression,
            BossScheduleSO bossSchedule)
        {
            StageDefinitionSO definition = LoadOrCreateAsset<StageDefinitionSO>(StageDefinitionPath);
            SerializedObject so = new SerializedObject(definition);
            SetString(so, "stageId", "stage_room_corridor_samples");
            SetString(so, "displayName", "Stage One Prototype");
            SetObjectArray(so, "waves", waves);
            SetObject(so, "rewardTable", rewardTable);
            SetObject(so, "supportTruckCatalog", supportTruckCatalog);
            SetObjectArray(so, "buildableDefinitions", buildableDefinitions);
            SetObject(so, "enemyPrefab", enemyPrefab);
            SetObject(so, "enemyCatalog", enemyCatalog);
            SetObject(so, "difficultyProgression", difficultyProgression);
            SetObjectArray(so, "regionWaveModifiers", new RegionWaveModifierSO[0]);
            SetObjectArray(so, "periodicWaveModifiers", new PeriodicWaveModifierSO[0]);
            SetObject(so, "bossSchedule", bossSchedule);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void ValidateStageOneFootprintsOrThrow()
        {
            List<string> failures = new List<string>();
            ValidateStageOneFootprints(failures);
            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("Stage One footprint validation failed:\n" + string.Join("\n", failures));
            }
        }

        private static void ValidateStageOneFootprints(List<string> failures)
        {
            StageFootprint[] footprints = CreateStageOneFootprints();
            for (int i = 0; i < footprints.Length; i++)
            {
                for (int j = i + 1; j < footprints.Length; j++)
                {
                    float overlapX = Mathf.Min(footprints[i].MaxX, footprints[j].MaxX) - Mathf.Max(footprints[i].MinX, footprints[j].MinX);
                    float overlapZ = Mathf.Min(footprints[i].MaxZ, footprints[j].MaxZ) - Mathf.Max(footprints[i].MinZ, footprints[j].MinZ);
                    if (overlapX > 0.001f && overlapZ > 0.001f)
                    {
                        failures.Add($"{footprints[i].Name} overlaps {footprints[j].Name}. overlapX={overlapX:0.00}, overlapZ={overlapZ:0.00}");
                    }
                }
            }

            for (int i = 0; i < footprints.Length; i++)
            {
                if (!footprints[i].IsConnector)
                {
                    continue;
                }

                int neighborCount = 0;
                for (int j = 0; j < footprints.Length; j++)
                {
                    if (i == j || footprints[j].IsConnector)
                    {
                        continue;
                    }

                    if (HasSharedEdge(footprints[i], footprints[j]))
                    {
                        neighborCount++;
                    }
                }

                if (neighborCount < 2)
                {
                    failures.Add($"{footprints[i].Name} should bridge at least 2 rooms/bays, found {neighborCount}.");
                }
            }

            ValidateStageOneExpectedConnections(footprints, failures);
        }

        private static void ValidateStageOneExpectedConnections(StageFootprint[] footprints, List<string> failures)
        {
            StageConnection[] expectedConnections = CreateStageOneExpectedConnections();
            for (int i = 0; i < expectedConnections.Length; i++)
            {
                StageConnection connection = expectedConnections[i];
                if (!TryFindFootprint(footprints, connection.ConnectorName, out StageFootprint connector))
                {
                    failures.Add($"{connection.ConnectorName} expected connection is missing connector footprint.");
                    continue;
                }

                if (!TryFindFootprint(footprints, connection.RoomName, out StageFootprint room))
                {
                    failures.Add($"{connection.ConnectorName} expected connection is missing room footprint {connection.RoomName}.");
                    continue;
                }

                if (!connector.IsConnector)
                {
                    failures.Add($"{connection.ConnectorName} is expected to be a connector.");
                }

                if (room.IsConnector)
                {
                    failures.Add($"{connection.RoomName} is expected to be a room/bay.");
                }

                if ((room.Openings & connection.RoomSide) == 0)
                {
                    failures.Add($"{connection.RoomName} is missing {connection.RoomSide} opening for {connection.ConnectorName}.");
                }

                if (!HasSharedEdgeOnSide(connector, room, connection.RoomSide))
                {
                    failures.Add($"{connection.ConnectorName} is not attached to {connection.RoomName} {connection.RoomSide} edge.");
                }

                ValidateConnectorOpeningAlignment(connector, room, connection.RoomSide, failures);
            }

            for (int i = 0; i < footprints.Length; i++)
            {
                if (!footprints[i].IsConnector)
                {
                    continue;
                }

                int expectedCount = 0;
                for (int j = 0; j < expectedConnections.Length; j++)
                {
                    if (expectedConnections[j].ConnectorName == footprints[i].Name)
                    {
                        expectedCount++;
                    }
                }

                if (expectedCount != 2)
                {
                    failures.Add($"{footprints[i].Name} should have exactly 2 expected room connections, found {expectedCount}.");
                }
            }
        }

        private static StageFootprint[] CreateStageOneFootprints()
        {
            return new[]
            {
                new StageFootprint("00_StartSupplyRoom", 0f, 0f, 18f, 16f, false, RoomOpenings.East),
                new StageFootprint("01A_MainEntryCorridor", 15f, 0f, 12f, 4f, true, RoomOpenings.None),
                new StageFootprint("01_EntryForkRoom", 30f, 0f, 18f, 16f, false, RoomOpenings.West | RoomOpenings.East | RoomOpenings.North),
                new StageFootprint("01C_EntrySideLoopConnector", 30f, 11f, 4f, 6f, true, RoomOpenings.None),
                new StageFootprint("01D_EntrySupplyPocket", 30f, 18f, 12f, 8f, false, RoomOpenings.South),
                new StageFootprint("02A_EntryToLowerConnector", 43f, 0f, 8f, 4f, true, RoomOpenings.None),
                new StageFootprint("02_LowerDefenseHall", 58f, 0f, 22f, 18f, false, RoomOpenings.West | RoomOpenings.East | RoomOpenings.North | RoomOpenings.South),
                new StageFootprint("02B_LowerWorkshopConnector", 58f, -12.5f, 4f, 7f, true, RoomOpenings.None),
                new StageFootprint("02C_LowerWorkshopPocket", 58f, -20f, 14f, 8f, false, RoomOpenings.North),
                new StageFootprint("03A_LowerToRewardConnector", 58f, 13.5f, 4f, 9f, true, RoomOpenings.None),
                new StageFootprint("03_RewardVault", 58f, 24f, 18f, 12f, false, RoomOpenings.South),
                new StageFootprint("04A_LowerToHighGroundConnector", 73f, 0f, 8f, 4f, true, RoomOpenings.None),
                new StageFootprint("04_HighGroundDefense", 88f, 0f, 22f, 18f, false, RoomOpenings.West | RoomOpenings.East | RoomOpenings.South),
                new StageFootprint("05A_HighGroundToTreasureConnector", 88f, -14.5f, 4f, 11f, true, RoomOpenings.None),
                new StageFootprint("05_TreasureOverlook", 88f, -26f, 18f, 12f, false, RoomOpenings.North),
                new StageFootprint("06A_HighGroundToFinalConnector", 103f, 0f, 8f, 4f, true, RoomOpenings.None),
                new StageFootprint("06_FinalApproachHall", 120f, 0f, 26f, 12f, false, RoomOpenings.West | RoomOpenings.East | RoomOpenings.North | RoomOpenings.South),
                new StageFootprint("06B_FinalApproachNorthConnector", 120f, 9f, 4f, 6f, true, RoomOpenings.None),
                new StageFootprint("06C_FinalApproachNorthBay", 120f, 16f, 12f, 8f, false, RoomOpenings.South),
                new StageFootprint("06D_FinalApproachSouthConnector", 120f, -9f, 4f, 6f, true, RoomOpenings.None),
                new StageFootprint("06E_FinalApproachSouthBay", 120f, -16f, 12f, 8f, false, RoomOpenings.North),
                new StageFootprint("07A_FinalApproachToVaultConnector", 136.5f, 0f, 7f, 4f, true, RoomOpenings.None),
                new StageFootprint("07_FinalVault", 150f, 0f, 20f, 16f, false, RoomOpenings.West)
            };
        }

        private static StageConnection[] CreateStageOneExpectedConnections()
        {
            return new[]
            {
                new StageConnection("01A_MainEntryCorridor", "00_StartSupplyRoom", RoomOpenings.East),
                new StageConnection("01A_MainEntryCorridor", "01_EntryForkRoom", RoomOpenings.West),
                new StageConnection("01C_EntrySideLoopConnector", "01_EntryForkRoom", RoomOpenings.North),
                new StageConnection("01C_EntrySideLoopConnector", "01D_EntrySupplyPocket", RoomOpenings.South),
                new StageConnection("02A_EntryToLowerConnector", "01_EntryForkRoom", RoomOpenings.East),
                new StageConnection("02A_EntryToLowerConnector", "02_LowerDefenseHall", RoomOpenings.West),
                new StageConnection("02B_LowerWorkshopConnector", "02_LowerDefenseHall", RoomOpenings.South),
                new StageConnection("02B_LowerWorkshopConnector", "02C_LowerWorkshopPocket", RoomOpenings.North),
                new StageConnection("03A_LowerToRewardConnector", "02_LowerDefenseHall", RoomOpenings.North),
                new StageConnection("03A_LowerToRewardConnector", "03_RewardVault", RoomOpenings.South),
                new StageConnection("04A_LowerToHighGroundConnector", "02_LowerDefenseHall", RoomOpenings.East),
                new StageConnection("04A_LowerToHighGroundConnector", "04_HighGroundDefense", RoomOpenings.West),
                new StageConnection("05A_HighGroundToTreasureConnector", "04_HighGroundDefense", RoomOpenings.South),
                new StageConnection("05A_HighGroundToTreasureConnector", "05_TreasureOverlook", RoomOpenings.North),
                new StageConnection("06A_HighGroundToFinalConnector", "04_HighGroundDefense", RoomOpenings.East),
                new StageConnection("06A_HighGroundToFinalConnector", "06_FinalApproachHall", RoomOpenings.West),
                new StageConnection("06B_FinalApproachNorthConnector", "06_FinalApproachHall", RoomOpenings.North),
                new StageConnection("06B_FinalApproachNorthConnector", "06C_FinalApproachNorthBay", RoomOpenings.South),
                new StageConnection("06D_FinalApproachSouthConnector", "06_FinalApproachHall", RoomOpenings.South),
                new StageConnection("06D_FinalApproachSouthConnector", "06E_FinalApproachSouthBay", RoomOpenings.North),
                new StageConnection("07A_FinalApproachToVaultConnector", "06_FinalApproachHall", RoomOpenings.East),
                new StageConnection("07A_FinalApproachToVaultConnector", "07_FinalVault", RoomOpenings.West)
            };
        }

        private static bool HasSharedEdge(StageFootprint connector, StageFootprint room)
        {
            const float epsilon = 0.001f;
            bool touchesEastWest = Mathf.Abs(connector.MaxX - room.MinX) <= epsilon || Mathf.Abs(connector.MinX - room.MaxX) <= epsilon;
            if (touchesEastWest && OverlapLength(connector.MinZ, connector.MaxZ, room.MinZ, room.MaxZ) > 0.5f)
            {
                return true;
            }

            bool touchesNorthSouth = Mathf.Abs(connector.MaxZ - room.MinZ) <= epsilon || Mathf.Abs(connector.MinZ - room.MaxZ) <= epsilon;
            return touchesNorthSouth && OverlapLength(connector.MinX, connector.MaxX, room.MinX, room.MaxX) > 0.5f;
        }

        private static bool HasSharedEdgeOnSide(StageFootprint connector, StageFootprint room, RoomOpenings roomSide)
        {
            const float epsilon = 0.001f;
            switch (roomSide)
            {
                case RoomOpenings.North:
                    return Mathf.Abs(connector.MinZ - room.MaxZ) <= epsilon && OverlapLength(connector.MinX, connector.MaxX, room.MinX, room.MaxX) > 0.5f;
                case RoomOpenings.South:
                    return Mathf.Abs(connector.MaxZ - room.MinZ) <= epsilon && OverlapLength(connector.MinX, connector.MaxX, room.MinX, room.MaxX) > 0.5f;
                case RoomOpenings.East:
                    return Mathf.Abs(connector.MinX - room.MaxX) <= epsilon && OverlapLength(connector.MinZ, connector.MaxZ, room.MinZ, room.MaxZ) > 0.5f;
                case RoomOpenings.West:
                    return Mathf.Abs(connector.MaxX - room.MinX) <= epsilon && OverlapLength(connector.MinZ, connector.MaxZ, room.MinZ, room.MaxZ) > 0.5f;
                default:
                    return false;
            }
        }

        private static void ValidateConnectorOpeningAlignment(StageFootprint connector, StageFootprint room, RoomOpenings roomSide, List<string> failures)
        {
            const float epsilon = 0.001f;
            float doorOpeningPlanSize = DoorOpeningWidth / PlanScale;
            if (roomSide == RoomOpenings.North || roomSide == RoomOpenings.South)
            {
                if (Mathf.Abs(connector.X - room.X) > epsilon)
                {
                    failures.Add($"{connector.Name} center x={connector.X:0.00} does not match {room.Name} centered {roomSide} opening x={room.X:0.00}.");
                }

                if (connector.Width - doorOpeningPlanSize > epsilon)
                {
                    failures.Add($"{connector.Name} width={connector.Width:0.00} is wider than room door opening plan size {doorOpeningPlanSize:0.00}.");
                }

                return;
            }

            if (roomSide == RoomOpenings.East || roomSide == RoomOpenings.West)
            {
                if (Mathf.Abs(connector.Z - room.Z) > epsilon)
                {
                    failures.Add($"{connector.Name} center z={connector.Z:0.00} does not match {room.Name} centered {roomSide} opening z={room.Z:0.00}.");
                }

                if (connector.Depth - doorOpeningPlanSize > epsilon)
                {
                    failures.Add($"{connector.Name} depth={connector.Depth:0.00} is wider than room door opening plan size {doorOpeningPlanSize:0.00}.");
                }
            }
        }

        private static bool TryFindFootprint(StageFootprint[] footprints, string name, out StageFootprint footprint)
        {
            for (int i = 0; i < footprints.Length; i++)
            {
                if (footprints[i].Name == name)
                {
                    footprint = footprints[i];
                    return true;
                }
            }

            footprint = default;
            return false;
        }

        private static float OverlapLength(float minA, float maxA, float minB, float maxB)
        {
            return Mathf.Min(maxA, maxB) - Mathf.Max(minA, minB);
        }

        private static void ValidateStageDefinition(StageDefinitionSO stageDefinition, List<string> failures)
        {
            if (stageDefinition == null)
            {
                failures.Add($"StageDefinition asset is missing: {StageDefinitionPath}");
                return;
            }

            if (stageDefinition.Waves == null || stageDefinition.Waves.Count < 1)
            {
                failures.Add($"Expected at least 1 reusable stage wave template, found {stageDefinition.Waves?.Count ?? 0}.");
            }

            if (stageDefinition.BuildableDefinitions == null || stageDefinition.BuildableDefinitions.Count < 8)
            {
                failures.Add($"Expected at least 8 buildable definitions, found {stageDefinition.BuildableDefinitions?.Count ?? 0}.");
            }

            if (stageDefinition.RewardTable == null)
            {
                failures.Add("StageDefinition.rewardTable is missing.");
            }

            if (stageDefinition.SupportTruckCatalog == null)
            {
                failures.Add("StageDefinition.supportTruckCatalog is missing.");
            }

            if (stageDefinition.EnemyPrefab == null)
            {
                failures.Add("StageDefinition.enemyPrefab is missing.");
            }

            if (stageDefinition.DifficultyProgression == null)
            {
                failures.Add("StageDefinition.difficultyProgression is missing.");
            }

            if (stageDefinition.BossSchedule == null)
            {
                failures.Add("StageDefinition.bossSchedule is missing.");
            }
            else
            {
                BossScheduleSO bossSchedule = stageDefinition.BossSchedule;
                if (bossSchedule.EveryNWave != 3 || bossSchedule.FirstBossWaveIndex != 2)
                {
                    failures.Add("StageDefinition.bossSchedule should add large zombies on waves 3, 6, 9...");
                }

                if (bossSchedule.GetSpawnCount(2) != 1
                    || bossSchedule.GetSpawnCount(5) != 2
                    || bossSchedule.GetSpawnCount(8) != 3)
                {
                    failures.Add("StageDefinition.bossSchedule large zombie scaling should be 1/2/3 on waves 3/6/9.");
                }
            }
        }

        private static void ValidateStageRuntime(StageRuntime runtime, List<string> failures)
        {
            if (runtime.MainCanvas == null)
            {
                failures.Add("StageRuntime.mainCanvas is missing.");
            }
            else
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(runtime.MainCanvas.gameObject);
                string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
                if (sourcePath != MainCanvasPrefabPath)
                {
                    failures.Add("StageRuntime.mainCanvas must be a prefab instance of " + MainCanvasPrefabPath + ".");
                }

                RequireComponentInChildren<PlacementBuildMenuPresenter>(runtime.MainCanvas.gameObject, "MainCanvas PlacementBuildMenuPresenter", failures);
                RequireComponentInChildren<InstalledObjectActionPresenter>(runtime.MainCanvas.gameObject, "MainCanvas InstalledObjectActionPresenter", failures);
                RequireComponentInChildren<TreasureRewardMenuPresenter>(runtime.MainCanvas.gameObject, "MainCanvas TreasureRewardMenuPresenter", failures);
                RequireComponentInChildren<SupportTruckShopPresenter>(runtime.MainCanvas.gameObject, "MainCanvas SupportTruckShopPresenter", failures);
                RequireComponentInChildren<WaveReadyPopup>(runtime.MainCanvas.gameObject, "MainCanvas WaveReadyPopup", failures);
                RequireComponentInChildren<PopupDimOverlayController>(runtime.MainCanvas.gameObject, "MainCanvas PopupDimOverlayController", failures);
            }

            if (runtime.WaveDirector == null)
            {
                failures.Add("StageRuntime.waveDirector is missing.");
            }

            if (runtime.WaveStartNotificationPresenter == null)
            {
                failures.Add("StageRuntime.waveStartNotificationPresenter is missing.");
            }

            if (runtime.WaveRewardController == null)
            {
                failures.Add("StageRuntime.waveRewardController is missing.");
            }

            if (runtime.RewardGrantService == null)
            {
                failures.Add("StageRuntime.rewardGrantService is missing.");
            }

            if (runtime.ArtifactInventory == null)
            {
                failures.Add("StageRuntime.artifactInventory is missing.");
            }

            if (runtime.ArtifactStatManager == null)
            {
                failures.Add("StageRuntime.artifactStatManager is missing.");
            }
        }

        private static void RequireComponentInChildren<T>(GameObject root, string label, List<string> failures) where T : Component
        {
            if (root.GetComponentInChildren<T>(true) == null)
            {
                failures.Add(label + " is missing.");
            }
        }

        private static void ValidateZoneRoots(List<string> failures)
        {
            Transform zonesRoot = FindSceneTransform("Stage_Zones");
            if (zonesRoot == null)
            {
                return;
            }

            if (zonesRoot.childCount != 8)
            {
                failures.Add($"Expected 8 stage zones, found {zonesRoot.childCount}.");
            }

            for (int i = 0; i < zonesRoot.childCount; i++)
            {
                Transform zone = zonesRoot.GetChild(i);
                bool shouldBeActive = i == 0;
                if (zone.gameObject.activeSelf != shouldBeActive)
                {
                    failures.Add($"{zone.name} activeSelf should be {shouldBeActive} before gate activation.");
                }
            }
        }

        private static void ValidateActivationGroups(List<string> failures)
        {
            MapExpansionActivationTargetGroup[] groups = Object.FindObjectsByType<MapExpansionActivationTargetGroup>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < groups.Length; i++)
            {
                SerializedObject groupSo = new SerializedObject(groups[i]);
                SerializedProperty targets = groupSo.FindProperty("activationTargets");
                if (targets == null || targets.arraySize <= 0)
                {
                    failures.Add($"{groups[i].name} has no activation target.");
                    continue;
                }

                for (int targetIndex = 0; targetIndex < targets.arraySize; targetIndex++)
                {
                    if (targets.GetArrayElementAtIndex(targetIndex).objectReferenceValue == null)
                    {
                        failures.Add($"{groups[i].name} activation target {targetIndex} is null.");
                    }
                }
            }
        }

        private static void ValidateDoorVisuals(List<string> failures)
        {
            MapExpansionDoorOpener[] openers = Object.FindObjectsByType<MapExpansionDoorOpener>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < openers.Length; i++)
            {
                SerializedObject openerSo = new SerializedObject(openers[i]);
                GameObject closedRoot = GetObjectReference(openerSo, "closedDoorRoot") as GameObject;
                GameObject openedRoot = GetObjectReference(openerSo, "openedDoorRoot") as GameObject;
                if (closedRoot == null)
                {
                    failures.Add($"{openers[i].name} closedDoorRoot is missing.");
                }

                if (openedRoot == null)
                {
                    failures.Add($"{openers[i].name} openedDoorRoot is missing.");
                    continue;
                }

                Collider[] openedColliders = openedRoot.GetComponentsInChildren<Collider>(true);
                for (int colliderIndex = 0; colliderIndex < openedColliders.Length; colliderIndex++)
                {
                    if (openedColliders[colliderIndex] != null && openedColliders[colliderIndex].enabled && !openedColliders[colliderIndex].isTrigger)
                    {
                        failures.Add($"{openers[i].name} openedDoorRoot still has an enabled blocking collider: {openedColliders[colliderIndex].name}.");
                    }
                }
            }
        }

        private static void ValidateEnemySpawners(List<string> failures)
        {
            EnemySpawner[] spawners = Object.FindObjectsByType<EnemySpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < spawners.Length; i++)
            {
                SerializedObject spawnerSo = new SerializedObject(spawners[i]);
                if (GetObjectReference(spawnerSo, "enemyPrefab") == null)
                {
                    failures.Add($"{spawners[i].name} enemyPrefab is missing.");
                }

                if (GetObjectReference(spawnerSo, "spawnPoint") == null)
                {
                    failures.Add($"{spawners[i].name} spawnPoint is missing.");
                }

                if (GetObjectReference(spawnerSo, "goal") == null)
                {
                    failures.Add($"{spawners[i].name} goal is missing.");
                }

                EnemyRoute route = GetObjectReference(spawnerSo, "route") as EnemyRoute;
                if (route == null)
                {
                    failures.Add($"{spawners[i].name} route is missing.");
                }
                else if (route.Waypoints == null || route.Waypoints.Count < 2)
                {
                    failures.Add($"{spawners[i].name} route needs at least 2 waypoints.");
                }
            }
        }

        private static void ValidateRouteWaypointGeometry(List<string> failures)
        {
            Transform zonesRoot = FindSceneTransform("Stage_Zones");
            if (zonesRoot == null)
            {
                return;
            }

            bool[] originalActiveStates = new bool[zonesRoot.childCount];
            for (int i = 0; i < zonesRoot.childCount; i++)
            {
                originalActiveStates[i] = zonesRoot.GetChild(i).gameObject.activeSelf;
                zonesRoot.GetChild(i).gameObject.SetActive(true);
            }

            try
            {
                Physics.SyncTransforms();
                EnemyRoute[] routes = Object.FindObjectsByType<EnemyRoute>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                for (int routeIndex = 0; routeIndex < routes.Length; routeIndex++)
                {
                    EnemyRoute route = routes[routeIndex];
                    if (route == null || route.Waypoints == null)
                    {
                        continue;
                    }

                    for (int waypointIndex = 0; waypointIndex < route.Waypoints.Count; waypointIndex++)
                    {
                        Transform waypoint = route.Waypoints[waypointIndex];
                        if (waypoint == null)
                        {
                            failures.Add($"{route.name} waypoint {waypointIndex} is null.");
                            continue;
                        }

                        ValidateRoutePoint($"{route.name}/{waypoint.name}", waypoint.position, failures);
                    }
                }
            }
            finally
            {
                for (int i = 0; i < zonesRoot.childCount && i < originalActiveStates.Length; i++)
                {
                    zonesRoot.GetChild(i).gameObject.SetActive(originalActiveStates[i]);
                }

                Physics.SyncTransforms();
            }
        }

        private static void ValidateRoutePoint(string label, Vector3 position, List<string> failures)
        {
            Vector3 rayOrigin = position + Vector3.up * 6f;
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 12f);
            if (!TryFindRouteSupportHit(hits, out RaycastHit hit))
            {
                failures.Add($"{label} has no floor/deck/ramp support below it.");
                return;
            }

            if (hit.point.y > position.y + 0.75f || hit.point.y < position.y - 2f)
            {
                failures.Add($"{label} support height is suspicious. waypointY={position.y:0.00}, supportY={hit.point.y:0.00}.");
            }

            Collider[] blockers = Physics.OverlapSphere(position + Vector3.up * 0.4f, 0.65f);
            for (int i = 0; i < blockers.Length; i++)
            {
                Collider blocker = blockers[i];
                if (blocker == null || blocker.isTrigger || IsRouteSupportCollider(blocker) || IsControlledDoorBlocker(blocker))
                {
                    continue;
                }

                failures.Add($"{label} is too close to blocking collider: {blocker.name}.");
            }
        }

        private static bool TryFindRouteSupportHit(RaycastHit[] hits, out RaycastHit supportHit)
        {
            supportHit = default;
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            float bestDistance = float.PositiveInfinity;
            bool hasSupport = false;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || collider.isTrigger || !IsRouteSupportCollider(collider))
                {
                    continue;
                }

                if (hits[i].distance < bestDistance)
                {
                    bestDistance = hits[i].distance;
                    supportHit = hits[i];
                    hasSupport = true;
                }
            }

            return hasSupport;
        }

        private static bool IsRouteSupportCollider(Collider collider)
        {
            if (collider == null)
            {
                return false;
            }

            string name = collider.name;
            return name.Contains("Floor")
                || name.Contains("Deck")
                || name.Contains("Ramp")
                || name.Contains("Pad");
        }

        private static bool IsControlledDoorBlocker(Collider collider)
        {
            return collider != null
                && collider.name.Contains("DoorClosedBlocker")
                && collider.GetComponentInParent<MapExpansionDoorOpener>() != null;
        }

        private static List<Collider> SetControlledDoorBlockersEnabled(bool enabled)
        {
            Collider[] colliders = Object.FindObjectsByType<Collider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            List<Collider> changedColliders = new List<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.enabled == enabled || !IsControlledDoorBlocker(collider))
                {
                    continue;
                }

                collider.enabled = enabled;
                changedColliders.Add(collider);
            }

            Physics.SyncTransforms();
            return changedColliders;
        }

        private static void RestoreColliders(List<Collider> colliders)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Count; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = true;
                }
            }

            Physics.SyncTransforms();
        }

        private static void ValidateMapNavigationLinks(List<string> failures)
        {
            List<string> navigationFailures = MapNavigationValidator.CollectUnsafeNavigationLinks(disableUnsafeLinks: false);
            for (int i = 0; i < navigationFailures.Count; i++)
            {
                failures.Add("Unsafe navigation link: " + navigationFailures[i]);
            }
        }

        private static void ValidateEnemyRouteNavMeshPaths(List<string> failures)
        {
            List<string> routeFailures = MapNavigationValidator.CollectEnemyRoutePathFailures(includeInactiveSpawners: true);
            for (int i = 0; i < routeFailures.Count; i++)
            {
                failures.Add("Enemy route path: " + routeFailures[i]);
            }
        }

        private static void ValidateSupportTruckStartDistance(List<string> failures)
        {
            Transform supportTruck = FindSceneTransform("Stage1_SupportTruck_StartSupply");
            Transform player = FindSceneTransform("Stage1_Player_Start");
            if (supportTruck == null || player == null)
            {
                return;
            }

            float distance = Vector3.Distance(supportTruck.position, player.position);
            if (distance > 3f)
            {
                failures.Add($"Support truck is too far from player start: {distance:0.00}.");
            }
        }

        private static void RequireSceneTransform(string name, List<string> failures)
        {
            if (FindSceneTransform(name) == null)
            {
                failures.Add($"Missing GameObject: {name}");
            }
        }

        private static Transform FindSceneTransform(string name)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static void RequireLayoutCount(string label, int expected, int actual, List<string> failures)
        {
            if (actual != expected)
            {
                failures.Add($"Expected {expected} {label}, found {actual}.");
            }
        }

        private static int CountMissingScripts()
        {
            int missingCount = 0;
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                missingCount += CountMissingScripts(roots[i]);
            }

            return missingCount;
        }

        private static int CountMissingScripts(GameObject gameObject)
        {
            int missingCount = 0;
            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                }
            }

            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                missingCount += CountMissingScripts(gameObject.transform.GetChild(i).gameObject);
            }

            return missingCount;
        }

        private static int CountMissingPrefabAssets()
        {
            int missingCount = 0;
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (PrefabUtility.GetPrefabInstanceStatus(transforms[i].gameObject) == PrefabInstanceStatus.MissingAsset)
                {
                    missingCount++;
                }
            }

            return missingCount;
        }

        private static Object GetObjectReference(SerializedObject so, string propertyName)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue : null;
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Stage Overview Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(62f * PlanScale, 58f * PlanScale, -58f * PlanScale);
            cameraObject.transform.rotation = Quaternion.Euler(55f, 38f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 72f * PlanScale;
            cameraObject.SetActive(false);
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T LoadRequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError("Required asset missing: " + path);
            }

            return asset;
        }

        private static T LoadOptionalAsset<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void EnsureFolders()
        {
            EnsureFolder(StagePrefabFolder);
            EnsureFolder(StageSettingsFolder);
            EnsureFolder(ConstructionSettingsFolder);
            EnsureFolder(WaveSettingsFolder);
            EnsureFolder(ArtifactSettingsFolder);
            EnsureFolder(ArtifactIconFolder);
            EnsureFolder("Assets/hansol/04_Materials");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void SetObjectField(Object target, string propertyName, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SetObject(so, propertyName, value);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetBoolField(Object target, string propertyName, bool value)
        {
            SerializedObject so = new SerializedObject(target);
            SetBool(so, propertyName, value);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetString(SerializedObject so, string propertyName, string value)
        {
            so.FindProperty(propertyName).stringValue = value;
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            so.FindProperty(propertyName).floatValue = value;
        }

        private static void SetInt(SerializedObject so, string propertyName, int value)
        {
            so.FindProperty(propertyName).intValue = value;
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            so.FindProperty(propertyName).boolValue = value;
        }

        private static void SetVector2(SerializedObject so, string propertyName, Vector2 value)
        {
            so.FindProperty(propertyName).vector2Value = value;
        }

        private static void SetColor(SerializedObject so, string propertyName, Color value)
        {
            so.FindProperty(propertyName).colorValue = value;
        }

        private static void SetEnum(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            property.enumValueIndex = value;
        }

        private static void SetObject(SerializedObject so, string propertyName, Object value)
        {
            so.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void SetObjectArray(SerializedObject so, string propertyName, Object[] values)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; values != null && i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
