using System;
using System.Collections.Generic;
using CorridorCommander;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommanderEditor
{
    public static class ResearchLabDungeonPrefabBuilder
    {
        private const string MenuPath = "Tools/Corridor Commander/Build Research Lab Dungeon Prefab";
        private const string CaptureMenuPath = "Tools/Corridor Commander/Capture Research Lab Dungeon Preview";
        private const string CaptureLightingMenuPath = "Tools/Corridor Commander/Capture Research Lab Dungeon Lighting Review";
        private const string ValidateMenuPath = "Tools/Corridor Commander/Validate Research Lab Dungeon Prefab";
        private const string ExportPartsMenuPath = "Tools/Corridor Commander/Export Research Lab Dungeon Part Prefabs";
        private const string ValidatePartsMenuPath = "Tools/Corridor Commander/Validate Research Lab Dungeon Part Prefabs";
        private const string SourcePrefabPath = "Assets/hansol/03_Prefabs/Stage/StageLayout_RoomCorridorSamples.prefab";
        private const string TargetPrefabPath = "Assets/hansol/03_Prefabs/Stage/ResearchLab_Dungeon_Large.prefab";
        private const string PartPrefabRootPath = "Assets/hansol/03_Prefabs/Stage/ResearchLabDungeonParts";
        private const string PreviewPath = "Assets/Screenshots/research_lab_dungeon_preview.png";
        private const string LightingReviewPath = "Assets/Screenshots/research_lab_dungeon_lighting_review.png";

        private const string SciFiRoot = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M";
        private const float MapScale = 4f;
        private const float RouteWallHeight = 7.2f;
        private const float RouteWallThickness = 1.2f;
        private const float RouteCeilingThickness = 0.35f;

        [MenuItem(MenuPath)]
        public static void Build()
        {
            EnsureTargetPrefab();

            GameObject root = PrefabUtility.LoadPrefabContents(TargetPrefabPath);
            try
            {
                root.name = "ResearchLab_Dungeon_Large";
                RebuildLabDressing(root.transform);
                RebuildAlternatePlayerRoutes(root);
                RebuildCeilingLights(root);
                RebuildEntryForkObjective(root);
                RebuildRoomRewards(root);
                RebuildAlternateEnemyRoutes(root);
                ConfigureRoomSpawnManager(root);
                ConfigureStageRoot(root);
                RebuildPrefabNavMesh(root);
                DisablePrefabLightShadows(root);
                PrefabUtility.SaveAsPrefabAsset(root, TargetPrefabPath);
                Debug.Log("[ResearchLabDungeonPrefabBuilder] Built " + TargetPrefabPath);
            }
            catch (Exception exception)
            {
                Debug.LogError("[ResearchLabDungeonPrefabBuilder] Build failed: " + exception);
                throw;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(TargetPrefabPath);
            try
            {
                List<string> failures = new List<string>();
                ValidateRoomSpawnManager(root, failures);
                ValidateRoomRewards(root, failures);
                ValidateAlternatePlayerRoutes(root, failures);
                ValidateEntryForkObjective(root, failures);
                ValidateCeilingLights(root, failures);

                if (failures.Count > 0)
                {
                    throw new InvalidOperationException("[ResearchLabDungeonPrefabBuilder] Validation failed: " + string.Join("; ", failures));
                }

                Debug.Log("[ResearchLabDungeonPrefabBuilder] Validation passed: entry fork objective, ceiling lights, alternate player routes, 12 door-gated spawners, 7 door rules, " + root.GetComponentsInChildren<TreasureChest>(true).Length + " treasure chests.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[ResearchLabDungeonPrefabBuilder] Validation failed: " + exception);
                throw;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem(CaptureMenuPath)]
        public static void CapturePreview()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Target prefab missing: " + TargetPrefabPath);
            }

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject instance = null;
            Camera camera = null;
            RenderTexture renderTexture = null;
            try
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                SceneManager.MoveGameObjectToScene(instance, previewScene);
                DisablePreviewCanvases(instance);
                DisablePreviewLightShadows(instance);

                GameObject lightObject = new GameObject("ResearchLabPreview_DirectionalLight");
                SceneManager.MoveGameObjectToScene(lightObject, previewScene);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

                GameObject cameraObject = new GameObject("ResearchLabPreview_Camera");
                SceneManager.MoveGameObjectToScene(cameraObject, previewScene);
                camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.04f, 0.045f, 0.055f);
                camera.orthographic = true;
                camera.orthographicSize = 190f;
                camera.transform.position = new Vector3(0f, 450f, 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                renderTexture = new RenderTexture(1920, 1080, 24);
                camera.targetTexture = renderTexture;
                camera.Render();

                SaveRenderTexture(renderTexture, PreviewPath);
                Debug.Log("[ResearchLabDungeonPrefabBuilder] Captured " + PreviewPath);
            }
            catch (Exception exception)
            {
                Debug.LogError("[ResearchLabDungeonPrefabBuilder] Capture failed: " + exception);
                throw;
            }
            finally
            {
                if (camera != null)
                {
                    camera.targetTexture = null;
                }

                if (renderTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        [MenuItem(CaptureLightingMenuPath)]
        public static void CaptureLightingReview()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Target prefab missing: " + TargetPrefabPath);
            }

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            Camera camera = null;
            RenderTexture renderTexture = null;
            try
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                SceneManager.MoveGameObjectToScene(instance, previewScene);
                DisablePreviewCanvases(instance);
                DisablePreviewLightShadows(instance);
                HidePreviewCeilings(instance);

                GameObject lightObject = new GameObject("ResearchLabLightingReview_DirectionalLight");
                SceneManager.MoveGameObjectToScene(lightObject, previewScene);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.7f;
                light.shadows = LightShadows.None;
                lightObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                GameObject cameraObject = new GameObject("ResearchLabLightingReview_Camera");
                SceneManager.MoveGameObjectToScene(cameraObject, previewScene);
                camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.06f, 0.065f, 0.075f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 900f;
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                Transform lightRoot = FindChild(instance.transform, "ResearchLab_CeilingLights");
                if (lightRoot == null)
                {
                    throw new InvalidOperationException("Lighting review target missing: ResearchLab_CeilingLights");
                }

                Bounds bounds = CalculateRendererBounds(lightRoot.gameObject);
                float halfWidth = bounds.extents.x / (16f / 9f);
                camera.orthographicSize = Mathf.Max(bounds.extents.z, halfWidth) + 18f;
                camera.transform.position = new Vector3(bounds.center.x, bounds.max.y + 260f, bounds.center.z);

                renderTexture = new RenderTexture(1920, 1080, 24);
                camera.targetTexture = renderTexture;
                camera.Render();

                SaveRenderTexture(renderTexture, LightingReviewPath);
                Debug.Log("[ResearchLabDungeonPrefabBuilder] Captured " + LightingReviewPath);
            }
            catch (Exception exception)
            {
                Debug.LogError("[ResearchLabDungeonPrefabBuilder] Lighting review capture failed: " + exception);
                throw;
            }
            finally
            {
                if (camera != null)
                {
                    camera.targetTexture = null;
                }

                if (renderTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        [MenuItem(ExportPartsMenuPath)]
        public static void ExportPartPrefabs()
        {
            Build();

            GameObject root = PrefabUtility.LoadPrefabContents(TargetPrefabPath);
            try
            {
                EnsurePartPrefabFolders();

                PartPrefabSpec[] specs = CreatePartPrefabSpecs();
                for (int i = 0; i < specs.Length; i++)
                {
                    ExportPartPrefab(root.transform, specs[i]);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[ResearchLabDungeonPrefabBuilder] Exported " + specs.Length + " part prefabs to " + PartPrefabRootPath);
            }
            catch (Exception exception)
            {
                Debug.LogError("[ResearchLabDungeonPrefabBuilder] Export part prefabs failed: " + exception);
                throw;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem(ValidatePartsMenuPath)]
        public static void ValidatePartPrefabs()
        {
            PartPrefabSpec[] specs = CreatePartPrefabSpecs();
            List<string> failures = new List<string>();
            for (int i = 0; i < specs.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(specs[i].OutputPath) == null)
                {
                    failures.Add("Part prefab missing: " + specs[i].OutputPath);
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException("[ResearchLabDungeonPrefabBuilder] Part prefab validation failed: " + string.Join("; ", failures));
            }

            Debug.Log("[ResearchLabDungeonPrefabBuilder] Part prefab validation passed: " + specs.Length + " prefabs.");
        }

        private static void EnsureTargetPrefab()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (source == null)
            {
                throw new InvalidOperationException("Source prefab missing: " + SourcePrefabPath);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath) != null)
            {
                return;
            }

            if (!AssetDatabase.CopyAsset(SourcePrefabPath, TargetPrefabPath))
            {
                throw new InvalidOperationException("Failed to create target prefab: " + TargetPrefabPath);
            }

            AssetDatabase.ImportAsset(TargetPrefabPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void EnsurePartPrefabFolders()
        {
            EnsureAssetFolder("Assets/hansol/03_Prefabs/Stage");
            EnsureAssetFolder(PartPrefabRootPath);
            EnsureAssetFolder(PartPrefabRootPath + "/00_ZoneGeometry");
            EnsureAssetFolder(PartPrefabRootPath + "/01_Rooms");
            EnsureAssetFolder(PartPrefabRootPath + "/02_Connectors");
            EnsureAssetFolder(PartPrefabRootPath + "/03_AlternateRoutes");
            EnsureAssetFolder(PartPrefabRootPath + "/04_Objectives");
            EnsureAssetFolder(PartPrefabRootPath + "/05_Rewards");
            EnsureAssetFolder(PartPrefabRootPath + "/06_Dressing");
            EnsureAssetFolder(PartPrefabRootPath + "/07_EnemyRoutes");
            EnsureAssetFolder(PartPrefabRootPath + "/08_Lighting");
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(folderPath);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folderName))
            {
                throw new InvalidOperationException("Invalid asset folder path: " + folderPath);
            }

            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static PartPrefabSpec[] CreatePartPrefabSpecs()
        {
            List<PartPrefabSpec> specs = new List<PartPrefabSpec>();

            AddPart(specs, "Stage_Zones/Zone_00_StartSupply/Geometry", "00_ZoneGeometry/Zone_00_StartSupply_Geometry.prefab");
            AddPart(specs, "Stage_Zones/Zone_01_InterwovenEntry/Geometry", "00_ZoneGeometry/Zone_01_InterwovenEntry_Geometry.prefab");
            AddPart(specs, "Stage_Zones/Zone_02_LowerDefense/Geometry", "00_ZoneGeometry/Zone_02_LowerDefense_Geometry.prefab");
            AddPart(specs, "Stage_Zones/Zone_03_RewardVault/Geometry", "00_ZoneGeometry/Zone_03_RewardVault_Geometry.prefab");
            AddPart(specs, "Stage_Zones/Zone_04_HighGroundDefense/Geometry", "00_ZoneGeometry/Zone_04_HighGroundDefense_Geometry.prefab");
            AddPart(specs, "Stage_Zones/Zone_05_TreasureOverlook/Geometry", "00_ZoneGeometry/Zone_05_TreasureOverlook_Geometry.prefab");
            AddPart(specs, "Stage_Zones/Zone_06_FinalApproach/Geometry", "00_ZoneGeometry/Zone_06_FinalApproach_Geometry.prefab");
            AddPart(specs, "Stage_Zones/Zone_07_FinalVault/Geometry", "00_ZoneGeometry/Zone_07_FinalVault_Geometry.prefab");

            AddPart(specs, "00_StartSupplyRoom", "01_Rooms/00_StartSupplyRoom.prefab");
            AddPart(specs, "01_EntryForkRoom", "01_Rooms/01_EntryForkRoom.prefab");
            AddPart(specs, "01D_EntrySupplyPocket", "01_Rooms/01D_EntrySupplyPocket.prefab");
            AddPart(specs, "02_LowerDefenseHall", "01_Rooms/02_LowerDefenseHall.prefab");
            AddPart(specs, "02C_LowerWorkshopPocket", "01_Rooms/02C_LowerWorkshopPocket.prefab");
            AddPart(specs, "03_RewardVault", "01_Rooms/03_RewardVault.prefab");
            AddPart(specs, "04_HighGroundDefense", "01_Rooms/04_HighGroundDefense.prefab");
            AddPart(specs, "05_TreasureOverlook", "01_Rooms/05_TreasureOverlook.prefab");
            AddPart(specs, "06_FinalApproachHall", "01_Rooms/06_FinalApproachHall.prefab");
            AddPart(specs, "06C_FinalApproachNorthBay", "01_Rooms/06C_FinalApproachNorthBay.prefab");
            AddPart(specs, "06E_FinalApproachSouthBay", "01_Rooms/06E_FinalApproachSouthBay.prefab");
            AddPart(specs, "07_FinalVault", "01_Rooms/07_FinalVault.prefab");
            AddPart(specs, "NorthRoute_SpecimenArchiveRoom", "01_Rooms/NorthRoute_SpecimenArchiveRoom.prefab");

            AddPart(specs, "01A_MainEntryCorridor", "02_Connectors/01A_MainEntryCorridor.prefab");
            AddPart(specs, "01C_EntrySideLoopConnector", "02_Connectors/01C_EntrySideLoopConnector.prefab");
            AddPart(specs, "02A_EntryToLowerConnector", "02_Connectors/02A_EntryToLowerConnector.prefab");
            AddPart(specs, "02B_LowerWorkshopConnector", "02_Connectors/02B_LowerWorkshopConnector.prefab");
            AddPart(specs, "03A_LowerToRewardConnector", "02_Connectors/03A_LowerToRewardConnector.prefab");
            AddPart(specs, "04A_LowerToHighGroundConnector", "02_Connectors/04A_LowerToHighGroundConnector.prefab");
            AddPart(specs, "05A_HighGroundToTreasureConnector", "02_Connectors/05A_HighGroundToTreasureConnector.prefab");
            AddPart(specs, "06A_HighGroundToFinalConnector", "02_Connectors/06A_HighGroundToFinalConnector.prefab");
            AddPart(specs, "06B_FinalApproachNorthConnector", "02_Connectors/06B_FinalApproachNorthConnector.prefab");
            AddPart(specs, "06D_FinalApproachSouthConnector", "02_Connectors/06D_FinalApproachSouthConnector.prefab");
            AddPart(specs, "07A_FinalApproachToVaultConnector", "02_Connectors/07A_FinalApproachToVaultConnector.prefab");

            AddPart(specs, "NorthRoute_EntrySupply_To_RewardVault", "03_AlternateRoutes/NorthRoute_EntrySupply_To_RewardVault.prefab");
            AddPart(specs, "NorthRoute_RewardVault_To_Archive", "03_AlternateRoutes/NorthRoute_RewardVault_To_Archive.prefab");
            AddPart(specs, "NorthRoute_Archive_To_FinalNorth", "03_AlternateRoutes/NorthRoute_Archive_To_FinalNorth.prefab");
            AddPart(specs, "NorthRoute_Downlink_To_FinalNorthBay", "03_AlternateRoutes/NorthRoute_Downlink_To_FinalNorthBay.prefab");
            AddPart(specs, "SouthRoute_LowerWorkshop_To_Treasure", "03_AlternateRoutes/SouthRoute_LowerWorkshop_To_Treasure.prefab");
            AddPart(specs, "SouthRoute_Treasure_To_FinalSouth", "03_AlternateRoutes/SouthRoute_Treasure_To_FinalSouth.prefab");
            AddPart(specs, "SouthRoute_Uplink_To_FinalSouthBay", "03_AlternateRoutes/SouthRoute_Uplink_To_FinalSouthBay.prefab");
            AddPart(specs, "FinalRoute_NorthSouth_ServiceCross", "03_AlternateRoutes/FinalRoute_NorthSouth_ServiceCross.prefab");
            AddPart(specs, "UpperRoute_HighGround_To_FinalApproach", "03_AlternateRoutes/UpperRoute_HighGround_To_FinalApproach.prefab");
            AddPart(specs, "UpperRoute_RampDown_FinalApproach", "03_AlternateRoutes/UpperRoute_RampDown_FinalApproach.prefab");

            AddPart(specs, "ResearchLab_GameOverObjectives", "04_Objectives/ResearchLab_GameOverObjectives.prefab");
            AddPart(specs, "Stage1_EntryFork_CriticalCore_RED", "04_Objectives/Stage1_EntryFork_CriticalCore_RED.prefab");
            AddPart(specs, "ResearchLab_RoomRewards", "05_Rewards/ResearchLab_RoomRewards.prefab");
            AddPart(specs, "ResearchLab_SciFi_Dressing", "06_Dressing/ResearchLab_SciFi_Dressing.prefab");
            AddPart(specs, "ResearchLab_AlternateEnemyRoutes", "07_EnemyRoutes/ResearchLab_AlternateEnemyRoutes.prefab");
            AddPart(specs, "ResearchLab_CeilingLights", "08_Lighting/ResearchLab_CeilingLights.prefab");

            return specs.ToArray();
        }

        private static void AddPart(List<PartPrefabSpec> specs, string sourcePath, string relativeOutputPath)
        {
            specs.Add(new PartPrefabSpec(sourcePath, PartPrefabRootPath + "/" + relativeOutputPath));
        }

        private static void ExportPartPrefab(Transform root, PartPrefabSpec spec)
        {
            Transform source = FindChildByPath(root, spec.SourcePath);
            if (source == null)
            {
                source = FindChild(root, spec.SourcePath);
            }

            if (source == null)
            {
                throw new InvalidOperationException("Part source missing: " + spec.SourcePath);
            }

            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject);
            clone.name = System.IO.Path.GetFileNameWithoutExtension(spec.OutputPath);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(clone, spec.OutputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static Transform FindChildByPath(Transform root, string path)
        {
            string[] segments = path.Split('/');
            Transform current = root;
            for (int i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i]))
                {
                    continue;
                }

                current = current.Find(segments[i]);
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        private static void ConfigureStageRoot(GameObject root)
        {
            StageLayoutRoot layoutRoot = root.GetComponent<StageLayoutRoot>();
            if (layoutRoot == null)
            {
                throw new InvalidOperationException("StageLayoutRoot missing on " + root.name);
            }

            Transform finalGoal = FindChild(root.transform, "Stage1_Final_Goal_YELLOW");
            if (finalGoal == null)
            {
                throw new InvalidOperationException("Final goal missing: Stage1_Final_Goal_YELLOW");
            }

            SerializedObject serialized = new SerializedObject(layoutRoot);
            SetObject(serialized, "mainTarget", finalGoal);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RebuildPrefabNavMesh(GameObject root)
        {
            NavMeshSurface surface = root.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = root.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.Children;
            List<Collider> disabledDoorBlockers = SetDoorBlockersEnabled(root, false);
            try
            {
                Physics.SyncTransforms();
                surface.BuildNavMesh();
                EditorUtility.SetDirty(surface);
            }
            finally
            {
                RestoreColliders(disabledDoorBlockers);
            }
        }

        private static void RebuildLabDressing(Transform root)
        {
            DestroyExistingChild(root, "ResearchLab_Dungeon_Analysis");
            DestroyExistingChild(root, "ResearchLab_SciFi_Dressing");

            Transform analysisRoot = CreateEmpty("ResearchLab_Dungeon_Analysis", root, Vector3.zero).transform;
            CreateMarker("Analysis_OneLargeDungeonPrefab_BasedOnStageLayout", analysisRoot, MapPosition(0f, 6.5f, -12f), new Vector3(5f, 0.25f, 1f), new Color(0.1f, 0.55f, 1f, 0.7f));
            CreateMarker("Socket_Entry_To_LowerDefense", analysisRoot, MapPosition(39f, 0.4f, 0f), new Vector3(0.5f, 1.2f, 5f), new Color(0.25f, 0.9f, 0.45f, 0.55f));
            CreateMarker("Socket_LowerDefense_To_HighGround", analysisRoot, MapPosition(69f, 0.4f, 0f), new Vector3(0.5f, 1.2f, 5f), new Color(0.25f, 0.9f, 0.45f, 0.55f));
            CreateMarker("VerticalRoute_ContinuousRamp_LowerDefense", analysisRoot, MapPosition(51f, 3.8f, 0f), new Vector3(8f, 0.3f, 2f), new Color(1f, 0.7f, 0.1f, 0.65f));
            CreateMarker("VerticalRoute_ContinuousRamp_HighGround", analysisRoot, MapPosition(88f, 4.8f, 0f), new Vector3(12f, 0.3f, 2f), new Color(1f, 0.7f, 0.1f, 0.65f));
            CreateMarker("FinalObjective_TargetBinding_Required", analysisRoot, MapPosition(146f, 2.6f, 0f), new Vector3(4f, 0.35f, 4f), new Color(1f, 0.25f, 0.15f, 0.7f));

            Transform dressingRoot = CreateEmpty("ResearchLab_SciFi_Dressing", root, Vector3.zero).transform;
            PlacePrefab("Lab_Start_MedicalCharger", SciFiRoot + "/Scifi Furniture_M/Scifi_Medical_Charger.prefab", dressingRoot, MapPosition(4.5f, 0.05f, -5.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.5f);
            PlacePrefab("Lab_Entry_ChemistryBench", SciFiRoot + "/Scifi Furniture_M/Scifi_Chemistry_Lab.prefab", dressingRoot, MapPosition(30f, 0.05f, 18f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.7f);
            PlacePrefab("Lab_LowerDefense_RoboticArm", SciFiRoot + "/Scifi Furniture_M/Scifi_Robotic_Arm.prefab", dressingRoot, MapPosition(63f, 0.05f, -20f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.6f);
            PlacePrefab("Lab_RewardVault_SpecimenTube", SciFiRoot + "/Scifi Furniture_M/Scifi_Tube_Specimen.prefab", dressingRoot, MapPosition(55f, 0.05f, 25f), Quaternion.identity, Vector3.one * 1.8f);
            PlacePrefab("Lab_HighGround_HydroponicModule", SciFiRoot + "/Scifi Furniture_M/Scifi_Hydroponic.prefab", dressingRoot, MapPosition(95f, 4.35f, 4.2f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.35f);
            PlacePrefab("Lab_FinalApproach_Microscope", SciFiRoot + "/Scifi Furniture_M/Scifi_Microscope.prefab", dressingRoot, MapPosition(120f, 0.05f, 16f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.5f);
            PlacePrefab("Lab_FinalVault_TeleportFrame", SciFiRoot + "/Scifi Furniture_M/Scifi_Teleport.prefab", dressingRoot, MapPosition(151f, 0.05f, 5.1f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 1.8f);
            PlacePrefab("Lab_FinalVault_PortalObjective", SciFiRoot + "/Scifi_Portal.prefab", dressingRoot, MapPosition(153f, 0.05f, 0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 1.8f);
        }

        private static void RebuildAlternatePlayerRoutes(GameObject root)
        {
            DestroyExistingChild(root.transform, "ResearchLab_AlternatePlayerRoutes");

            Material floorMaterial = LoadRequiredAsset<Material>("Assets/hansol/04_Materials/StageSample_Floor_Concrete.mat");
            Material wallMaterial = LoadRequiredAsset<Material>("Assets/hansol/04_Materials/StageSample_Wall_Dark.mat");
            Transform routeRoot = CreateEmpty("ResearchLab_AlternatePlayerRoutes", root.transform, Vector3.zero).transform;

            OpenRouteWall(root, "01D_EntrySupplyPocket", "Wall_East");
            OpenRouteWall(root, "03_RewardVault", "Wall_East");
            OpenRouteWall(root, "06C_FinalApproachNorthBay", "Wall_North");
            OpenRouteWall(root, "02C_LowerWorkshopPocket", "Wall_East");
            OpenRouteWall(root, "05_TreasureOverlook", "Wall_East");
            OpenRouteWall(root, "06E_FinalApproachSouthBay", "Wall_South");

            CreateSealedCorridorX("NorthRoute_EntrySupply_To_RewardVault", routeRoot, 44f, 0f, 18f, 28f, 4f, floorMaterial, wallMaterial);
            CreateSealedCorridorX("NorthRoute_RewardVault_To_Archive", routeRoot, 75.5f, 0f, 24f, 17f, 4f, floorMaterial, wallMaterial);
            CreateSealedRoomEastWest("NorthRoute_SpecimenArchiveRoom", routeRoot, 92f, 0f, 24f, 16f, 12f, floorMaterial, wallMaterial);
            CreateSealedCorridorX("NorthRoute_Archive_To_FinalNorth", routeRoot, 107f, 0f, 24f, 14f, 4f, floorMaterial, wallMaterial);
            CreateSealedCorridorZ("NorthRoute_Downlink_To_FinalNorthBay", routeRoot, 120f, 0f, 22f, 4f, 8f, floorMaterial, wallMaterial);

            CreateSealedCorridorX("SouthRoute_LowerWorkshop_To_Treasure", routeRoot, 73f, 0f, -20f, 30f, 4f, floorMaterial, wallMaterial);
            CreateSealedCorridorX("SouthRoute_Treasure_To_FinalSouth", routeRoot, 108.5f, 0f, -26f, 43f, 4f, floorMaterial, wallMaterial);
            CreateSealedCorridorZ("SouthRoute_Uplink_To_FinalSouthBay", routeRoot, 120f, 0f, -23f, 4f, 6f, floorMaterial, wallMaterial);

            CreateSealedCorridorZ("FinalRoute_NorthSouth_ServiceCross", routeRoot, 136.5f, 0f, 0f, 4f, 52f, floorMaterial, wallMaterial);

            CreateElevatedCatwalkX("UpperRoute_HighGround_To_FinalApproach", routeRoot, 104f, 4.35f, 6.2f, 32f, 3.4f, floorMaterial, wallMaterial);
            CreateRampX("UpperRoute_RampDown_FinalApproach", routeRoot, 120f, 2.15f, 6.2f, 10f, 3.4f, 7f, floorMaterial, wallMaterial);
        }

        private static void RebuildCeilingLights(GameObject root)
        {
            DestroyExistingChild(root.transform, "ResearchLab_CeilingLights");

            Material fixtureMaterial = LoadRequiredAsset<Material>("Assets/hansol/04_Materials/StageSample_Wall_Dark.mat");
            Material lensMaterial = LoadRequiredAsset<Material>("Assets/hansol/04_Materials/TreasureChest_OpenCyan.mat");
            Transform lightRoot = CreateEmpty("ResearchLab_CeilingLights", root.transform, Vector3.zero).transform;

            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_StartSupply_A", 0f, 6.35f, 0f, 1.1f, 18f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_StartSupply_B", 5f, 6.35f, 5f, 0.9f, 16f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_MainEntryCorridor", 15f, 6.25f, 0f, 0.75f, 17f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_EntryFork_A", 30f, 6.35f, 0f, 1.2f, 19f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_EntrySupplyPocket", 30f, 6.25f, 18f, 0.9f, 17f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_EntrySideLoop", 30f, 6.2f, 11f, 0.75f, 15f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_EntryToLowerConnector", 43f, 6.25f, 0f, 0.75f, 17f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_LowerDefense_A", 58f, 6.35f, 0f, 1.1f, 19f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_LowerDefense_B", 62f, 6.35f, 7f, 0.85f, 17f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_LowerWorkshop", 58f, 6.25f, -20f, 0.9f, 17f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_LowerWorkshopConnector", 58f, 6.2f, -12.5f, 0.75f, 15f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_RewardVault", 58f, 6.35f, 24f, 1f, 18f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_LowerToRewardConnector", 58f, 6.2f, 13.5f, 0.75f, 15f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_LowerToHighGroundConnector", 73f, 6.3f, 0f, 0.8f, 18f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_HighGround_A", 88f, 10.3f, 0f, 1.1f, 19f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_HighGround_B", 95f, 10.3f, 5.5f, 0.9f, 17f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_TreasureOverlook", 88f, 6.35f, -26f, 1f, 18f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_HighGroundToTreasureConnector", 88f, 6.2f, -14.5f, 0.75f, 15f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_HighGroundToFinalConnector", 103f, 6.3f, 0f, 0.8f, 18f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_FinalApproach_A", 120f, 6.35f, 0f, 1.1f, 19f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_FinalApproachNorthBay", 120f, 6.25f, 16f, 0.9f, 17f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_FinalApproachNorthConnector", 120f, 6.2f, 9f, 0.75f, 15f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_FinalApproachSouthBay", 120f, 6.25f, -16f, 0.9f, 17f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_FinalApproachSouthConnector", 120f, 6.2f, -9f, 0.75f, 15f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_FinalApproachToVault", 136f, 6.3f, 0f, 0.8f, 18f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_FinalVault_A", 150f, 6.35f, 0f, 1.15f, 20f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_FinalVault_B", 150f, 6.35f, 6f, 0.9f, 17f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_SpecimenArchive_A", 92f, 6.35f, 24f, 1f, 18f);
            CreateCeilingLight(lightRoot, fixtureMaterial, lensMaterial, "CeilingLight_NorthArchiveConnector", 107f, 6.25f, 24f, 0.75f, 16f);
        }

        private static void RebuildEntryForkObjective(GameObject root)
        {
            DestroyExistingChild(root.transform, "ResearchLab_GameOverObjectives");

            Material coreMaterial = LoadRequiredAsset<Material>("Assets/hansol/04_Materials/TreasureChest_OpenCyan.mat");
            Material wallMaterial = LoadRequiredAsset<Material>("Assets/hansol/04_Materials/StageSample_Wall_Dark.mat");
            Transform objectiveRoot = CreateEmpty("ResearchLab_GameOverObjectives", root.transform, Vector3.zero).transform;

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "Stage1_EntryFork_CriticalCore_RED";
            core.transform.SetParent(objectiveRoot, false);
            core.transform.localPosition = MapPosition(30f, 1.05f, 0f);
            core.transform.localRotation = Quaternion.identity;
            core.transform.localScale = MapBlockScale(1.45f, 2.1f, 1.45f);

            Renderer renderer = core.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = coreMaterial;
            }

            Health health = core.AddComponent<Health>();
            ConfigureHealth(health, 250f, true);

            GameOverOnDeath gameOverOnDeath = core.AddComponent<GameOverOnDeath>();
            SerializedObject gameOverSerialized = new SerializedObject(gameOverOnDeath);
            gameOverSerialized.FindProperty("reason").stringValue = "Entry Fork critical core destroyed";
            gameOverSerialized.ApplyModifiedPropertiesWithoutUndo();

            CreateRouteBlock("CriticalCore_Base", objectiveRoot, MapPosition(30f, 0.1f, 0f), MapBlockScale(3.6f, 0.2f, 3.6f), wallMaterial, true);
            CreateObjectiveLight(objectiveRoot, MapPosition(30f, 4.4f, 0f));
        }

        private static void RebuildAlternateEnemyRoutes(GameObject root)
        {
            DestroyExistingChild(root.transform, "ResearchLab_AlternateEnemyRoutes");

            Transform finalGoal = FindChild(root.transform, "Stage1_Final_Goal_YELLOW");
            if (finalGoal == null)
            {
                throw new InvalidOperationException("Final goal missing: Stage1_Final_Goal_YELLOW");
            }

            Transform routeRoot = CreateEmpty("ResearchLab_AlternateEnemyRoutes", root.transform, Vector3.zero).transform;
            GameObject spawnerPrefab = LoadRequiredPrefab("Assets/hansol/03_Prefabs/Enemy_SpawnPoint_RED.prefab");
            GameObject routePointPrefab = LoadRequiredPrefab("Assets/hansol/03_Prefabs/EnemyRoutePoint.prefab");
            GameObject enemyPrefab = LoadRequiredPrefab("Assets/hansol/03_Prefabs/Enemy_Zombie_Basic.prefab");

            List<EnemySpawner> spawners = new List<EnemySpawner>(root.GetComponentsInChildren<EnemySpawner>(true));
            List<EnemyRoute> routes = new List<EnemyRoute>(root.GetComponentsInChildren<EnemyRoute>(true));

            CreateAlternateSpawner(
                spawnerPrefab,
                routePointPrefab,
                enemyPrefab,
                finalGoal,
                routeRoot,
                spawners,
                routes,
                "Stage1_Spawn_Entry_B_RED",
                MapPosition(30f, 0.05f, 18f),
                new[]
                {
                    MapPosition(30f, 0.5f, 11f),
                    MapPosition(30f, 0.5f, 0f),
                    MapPosition(58f, 0.5f, 0f),
                    MapPosition(88f, 0.5f, -7f),
                    MapPosition(120f, 0.5f, 0f),
                    MapPosition(144f, 0.5f, 0f),
                });

            CreateAlternateSpawner(
                spawnerPrefab,
                routePointPrefab,
                enemyPrefab,
                finalGoal,
                routeRoot,
                spawners,
                routes,
                "Stage1_Spawn_LowerDefense_B_RED",
                MapPosition(58f, 0.05f, -20f),
                new[]
                {
                    MapPosition(58f, 0.5f, -12.5f),
                    MapPosition(58f, 0.5f, -3f),
                    MapPosition(69f, 0.5f, 0f),
                    MapPosition(88f, 0.5f, -7f),
                    MapPosition(120f, 0.5f, 0f),
                    MapPosition(144f, 0.5f, 0f),
                });

            CreateAlternateSpawner(
                spawnerPrefab,
                routePointPrefab,
                enemyPrefab,
                finalGoal,
                routeRoot,
                spawners,
                routes,
                "Stage1_Spawn_LowerDefense_C_RED",
                MapPosition(58f, 0.05f, 24f),
                new[]
                {
                    MapPosition(58f, 0.5f, 13.5f),
                    MapPosition(58f, 0.5f, 3f),
                    MapPosition(69f, 0.5f, 0f),
                    MapPosition(88f, 0.5f, 7f),
                    MapPosition(120f, 0.5f, 0f),
                    MapPosition(144f, 0.5f, 0f),
                });

            CreateAlternateSpawner(
                spawnerPrefab,
                routePointPrefab,
                enemyPrefab,
                finalGoal,
                routeRoot,
                spawners,
                routes,
                "Stage1_Spawn_HighGround_B_RED",
                MapPosition(88f, 0.05f, -26f),
                new[]
                {
                    MapPosition(88f, 0.5f, -14.5f),
                    MapPosition(88f, 0.5f, -7f),
                    MapPosition(96f, 0.5f, -7f),
                    MapPosition(103f, 0.5f, 0f),
                    MapPosition(120f, 0.5f, 0f),
                    MapPosition(144f, 0.5f, 0f),
                });

            CreateAlternateSpawner(
                spawnerPrefab,
                routePointPrefab,
                enemyPrefab,
                finalGoal,
                routeRoot,
                spawners,
                routes,
                "Stage1_Spawn_FinalApproach_B_RED",
                MapPosition(120f, 0.05f, 16f),
                new[]
                {
                    MapPosition(120f, 0.5f, 9f),
                    MapPosition(120f, 0.5f, 0f),
                    MapPosition(136f, 0.5f, 0f),
                    MapPosition(144f, 0.5f, 0f),
                });

            CreateAlternateSpawner(
                spawnerPrefab,
                routePointPrefab,
                enemyPrefab,
                finalGoal,
                routeRoot,
                spawners,
                routes,
                "Stage1_Spawn_FinalApproach_C_RED",
                MapPosition(120f, 0.05f, -16f),
                new[]
                {
                    MapPosition(120f, 0.5f, -9f),
                    MapPosition(120f, 0.5f, 0f),
                    MapPosition(136f, 0.5f, 0f),
                    MapPosition(144f, 0.5f, 0f),
                });

            CreateAlternateSpawner(
                spawnerPrefab,
                routePointPrefab,
                enemyPrefab,
                finalGoal,
                routeRoot,
                spawners,
                routes,
                "Stage1_Spawn_FinalVault_B_RED",
                MapPosition(150f, 0.05f, 6f),
                new[]
                {
                    MapPosition(150f, 0.5f, 3f),
                    MapPosition(146f, 0.5f, 0f),
                });

            StageLayoutRoot layoutRoot = root.GetComponent<StageLayoutRoot>();
            SerializedObject serialized = new SerializedObject(layoutRoot);
            SetObjectArray(serialized, "enemySpawners", spawners);
            SetObjectArray(serialized, "enemyRoutes", routes);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RebuildRoomRewards(GameObject root)
        {
            DestroyExistingChild(root.transform, "ResearchLab_RoomRewards");

            Transform rewardRoot = CreateEmpty("ResearchLab_RoomRewards", root.transform, Vector3.zero).transform;
            GameObject chestPrefab = LoadRequiredPrefab("Assets/hansol/03_Prefabs/TreasureChest_Basic.prefab");
            TreasureChestRewardTable rewardTable = LoadRequiredAsset<TreasureChestRewardTable>("Assets/hansol/09_Settings/Rewards/Test_TreasureChestRewards.asset");

            CreateRoomChest(chestPrefab, rewardRoot, rewardTable, "Stage1_TreasureChest_StartSupply", 0, MapPosition(4f, 0.05f, -5.5f), Quaternion.Euler(0f, 135f, 0f));
            CreateRoomChest(chestPrefab, rewardRoot, rewardTable, "Stage1_TreasureChest_EntryFork", 1, MapPosition(32f, 0.05f, -5.5f), Quaternion.Euler(0f, 180f, 0f));
            CreateRoomChest(chestPrefab, rewardRoot, rewardTable, "Stage1_TreasureChest_LowerDefense", 2, MapPosition(62f, 0.05f, 7.5f), Quaternion.Euler(0f, 180f, 0f));
            CreateRoomChest(chestPrefab, rewardRoot, rewardTable, "Stage1_TreasureChest_HighGround", 3, MapPosition(95f, 4.35f, 5.5f), Quaternion.Euler(0f, 180f, 0f));
            CreateRoomChest(chestPrefab, rewardRoot, rewardTable, "Stage1_TreasureChest_FinalApproach_SouthBay", 4, MapPosition(121f, 0.05f, -16f), Quaternion.Euler(0f, 0f, 0f));

            StageLayoutRoot layoutRoot = root.GetComponent<StageLayoutRoot>();
            if (layoutRoot == null)
            {
                throw new InvalidOperationException("StageLayoutRoot missing on " + root.name);
            }

            SerializedObject serialized = new SerializedObject(layoutRoot);
            SetObjectArray(serialized, "treasureChests", root.GetComponentsInChildren<TreasureChest>(true));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TreasureChest CreateRoomChest(
            GameObject chestPrefab,
            Transform parent,
            TreasureChestRewardTable rewardTable,
            string name,
            int roomIndex,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject chestObject = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab, parent);
            chestObject.name = name;
            chestObject.transform.localPosition = position;
            chestObject.transform.localRotation = rotation;
            chestObject.transform.localScale = Vector3.one;

            TreasureChest chest = chestObject.GetComponent<TreasureChest>();
            if (chest == null)
            {
                throw new InvalidOperationException("TreasureChest missing on " + name);
            }

            chest.ConfigureRewards(rewardTable, roomIndex);
            return chest;
        }

        private static void ConfigureRoomSpawnManager(GameObject root)
        {
            EnemySpawnManager spawnManager = root.GetComponent<EnemySpawnManager>();
            if (spawnManager == null)
            {
                spawnManager = root.AddComponent<EnemySpawnManager>();
            }

            EnemySpawner[] entrySpawners =
            {
                FindSpawner(root, "Stage1_Spawn_Entry_A_RED"),
                FindSpawner(root, "Stage1_Spawn_Entry_B_RED"),
            };

            EnemySpawner[] lowerDefenseSpawners =
            {
                FindSpawner(root, "Stage1_Spawn_LowerDefense_A_RED"),
                FindSpawner(root, "Stage1_Spawn_LowerDefense_B_RED"),
            };

            EnemySpawner[] rewardVaultSpawners =
            {
                FindSpawner(root, "Stage1_Spawn_LowerDefense_C_RED"),
            };

            EnemySpawner[] highGroundSpawners =
            {
                FindSpawner(root, "Stage1_Spawn_HighGround_A_RED"),
            };

            EnemySpawner[] treasureOverlookSpawners =
            {
                FindSpawner(root, "Stage1_Spawn_HighGround_B_RED"),
            };

            EnemySpawner[] finalApproachSpawners =
            {
                FindSpawner(root, "Stage1_Spawn_FinalApproach_A_RED"),
                FindSpawner(root, "Stage1_Spawn_FinalApproach_B_RED"),
                FindSpawner(root, "Stage1_Spawn_FinalApproach_C_RED"),
            };

            EnemySpawner[] finalVaultSpawners =
            {
                FindSpawner(root, "Stage1_Spawn_FinalVault_A_RED"),
                FindSpawner(root, "Stage1_Spawn_FinalVault_B_RED"),
            };

            List<EnemySpawner> allSpawners = new List<EnemySpawner>();
            AddUnique(allSpawners, entrySpawners);
            AddUnique(allSpawners, lowerDefenseSpawners);
            AddUnique(allSpawners, rewardVaultSpawners);
            AddUnique(allSpawners, highGroundSpawners);
            AddUnique(allSpawners, treasureOverlookSpawners);
            AddUnique(allSpawners, finalApproachSpawners);
            AddUnique(allSpawners, finalVaultSpawners);

            for (int i = 0; i < allSpawners.Count; i++)
            {
                allSpawners[i].gameObject.SetActive(false);
            }

            SerializedObject serialized = new SerializedObject(spawnManager);
            SetObjectArray(serialized, "initialActiveSpawners", Array.Empty<EnemySpawner>());
            SetObjectArray(serialized, "initialInactiveSpawners", allSpawners);
            serialized.FindProperty("spawnGroups").arraySize = 0;
            SerializedProperty rules = serialized.FindProperty("doorSpawnRules");
            rules.arraySize = 7;
            ConfigureDoorSpawnRule(rules.GetArrayElementAtIndex(0), FindDoor(root, "Stage1_Gate_Start_To_Entry"), "Entry", entrySpawners, Array.Empty<EnemySpawner>());
            ConfigureDoorSpawnRule(rules.GetArrayElementAtIndex(1), FindDoor(root, "Stage1_Gate_Entry_To_LowerDefense"), "LowerDefense", lowerDefenseSpawners, entrySpawners);
            ConfigureDoorSpawnRule(rules.GetArrayElementAtIndex(2), FindDoor(root, "Stage1_Gate_LowerDefense_To_RewardVault"), "RewardVault", rewardVaultSpawners, Array.Empty<EnemySpawner>());
            ConfigureDoorSpawnRule(rules.GetArrayElementAtIndex(3), FindDoor(root, "Stage1_Gate_LowerDefense_To_HighGround"), "HighGround", highGroundSpawners, MergeSpawners(entrySpawners, lowerDefenseSpawners, rewardVaultSpawners));
            ConfigureDoorSpawnRule(rules.GetArrayElementAtIndex(4), FindDoor(root, "Stage1_Gate_HighGround_To_TreasureOverlook"), "TreasureOverlook", treasureOverlookSpawners, Array.Empty<EnemySpawner>());
            ConfigureDoorSpawnRule(rules.GetArrayElementAtIndex(5), FindDoor(root, "Stage1_Gate_HighGround_To_FinalApproach"), "FinalApproach", finalApproachSpawners, MergeSpawners(lowerDefenseSpawners, rewardVaultSpawners, highGroundSpawners, treasureOverlookSpawners));
            ConfigureDoorSpawnRule(rules.GetArrayElementAtIndex(6), FindDoor(root, "Stage1_Gate_FinalApproach_To_FinalVault"), "FinalVault", finalVaultSpawners, finalApproachSpawners);
            serialized.FindProperty("applyInitialStateOnAwake").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDoorSpawnRule(
            SerializedProperty rule,
            MapExpansionDoorOpener door,
            string regionId,
            IReadOnlyList<EnemySpawner> enableSpawners,
            IReadOnlyList<EnemySpawner> disableSpawners)
        {
            rule.FindPropertyRelative("door").objectReferenceValue = door;
            rule.FindPropertyRelative("regionId").stringValue = regionId;
            SetObjectArray(rule.FindPropertyRelative("enableSpawners"), enableSpawners);
            SetObjectArray(rule.FindPropertyRelative("disableSpawners"), disableSpawners);
            rule.FindPropertyRelative("applyOnlyOnce").boolValue = true;
        }

        private static void ValidateRoomSpawnManager(GameObject root, List<string> failures)
        {
            EnemySpawnManager spawnManager = root.GetComponent<EnemySpawnManager>();
            if (spawnManager == null)
            {
                failures.Add("EnemySpawnManager missing on " + root.name);
                return;
            }

            SerializedObject serialized = new SerializedObject(spawnManager);
            SerializedProperty initialActive = serialized.FindProperty("initialActiveSpawners");
            SerializedProperty initialInactive = serialized.FindProperty("initialInactiveSpawners");
            SerializedProperty rules = serialized.FindProperty("doorSpawnRules");
            if (initialActive == null || initialActive.arraySize != 0)
            {
                failures.Add("initialActiveSpawners must be empty for door-gated lab flow.");
            }

            if (initialInactive == null || initialInactive.arraySize != 12)
            {
                failures.Add("initialInactiveSpawners must contain 12 managed spawners.");
            }

            if (rules == null || rules.arraySize != 7)
            {
                failures.Add("doorSpawnRules must contain 7 room progression rules.");
            }
            else
            {
                for (int i = 0; i < rules.arraySize; i++)
                {
                    SerializedProperty rule = rules.GetArrayElementAtIndex(i);
                    if (rule.FindPropertyRelative("door").objectReferenceValue == null)
                    {
                        failures.Add("doorSpawnRules[" + i + "] has no door.");
                    }

                    if (rule.FindPropertyRelative("enableSpawners").arraySize == 0)
                    {
                        failures.Add("doorSpawnRules[" + i + "] has no enable spawners.");
                    }
                }
            }

            string[] expectedSpawnerNames =
            {
                "Stage1_Spawn_Entry_A_RED",
                "Stage1_Spawn_Entry_B_RED",
                "Stage1_Spawn_LowerDefense_A_RED",
                "Stage1_Spawn_LowerDefense_B_RED",
                "Stage1_Spawn_LowerDefense_C_RED",
                "Stage1_Spawn_HighGround_A_RED",
                "Stage1_Spawn_HighGround_B_RED",
                "Stage1_Spawn_FinalApproach_A_RED",
                "Stage1_Spawn_FinalApproach_B_RED",
                "Stage1_Spawn_FinalApproach_C_RED",
                "Stage1_Spawn_FinalVault_A_RED",
                "Stage1_Spawn_FinalVault_B_RED",
            };

            for (int i = 0; i < expectedSpawnerNames.Length; i++)
            {
                EnemySpawner spawner = FindSpawner(root, expectedSpawnerNames[i]);
                if (spawner.gameObject.activeSelf)
                {
                    failures.Add(expectedSpawnerNames[i] + " must be inactive before a door opens.");
                }
            }
        }

        private static void ValidateRoomRewards(GameObject root, List<string> failures)
        {
            TreasureChest[] chests = root.GetComponentsInChildren<TreasureChest>(true);
            if (chests.Length < 12)
            {
                failures.Add("Research lab should contain at least 12 treasure chests, found " + chests.Length + ".");
            }

            StageLayoutRoot layoutRoot = root.GetComponent<StageLayoutRoot>();
            if (layoutRoot == null)
            {
                failures.Add("StageLayoutRoot missing.");
                return;
            }

            SerializedObject serialized = new SerializedObject(layoutRoot);
            SerializedProperty serializedChests = serialized.FindProperty("treasureChests");
            if (serializedChests == null || serializedChests.arraySize != chests.Length)
            {
                failures.Add("StageLayoutRoot.treasureChests does not match child TreasureChest count.");
            }
        }

        private static void ValidateAlternatePlayerRoutes(GameObject root, List<string> failures)
        {
            Transform routeRoot = FindChild(root.transform, "ResearchLab_AlternatePlayerRoutes");
            if (routeRoot == null)
            {
                failures.Add("ResearchLab_AlternatePlayerRoutes missing.");
                return;
            }

            string[] expectedRoutes =
            {
                "NorthRoute_EntrySupply_To_RewardVault",
                "NorthRoute_RewardVault_To_Archive",
                "NorthRoute_SpecimenArchiveRoom",
                "NorthRoute_Archive_To_FinalNorth",
                "NorthRoute_Downlink_To_FinalNorthBay",
                "SouthRoute_LowerWorkshop_To_Treasure",
                "SouthRoute_Treasure_To_FinalSouth",
                "SouthRoute_Uplink_To_FinalSouthBay",
                "FinalRoute_NorthSouth_ServiceCross",
                "UpperRoute_HighGround_To_FinalApproach",
                "UpperRoute_RampDown_FinalApproach",
            };

            for (int i = 0; i < expectedRoutes.Length; i++)
            {
                if (routeRoot.Find(expectedRoutes[i]) == null)
                {
                    failures.Add("Alternate player route missing: " + expectedRoutes[i]);
                }
            }

            ValidateRouteWallOpened(root, "01D_EntrySupplyPocket", "Wall_East", failures);
            ValidateRouteWallOpened(root, "03_RewardVault", "Wall_East", failures);
            ValidateRouteWallOpened(root, "06C_FinalApproachNorthBay", "Wall_North", failures);
            ValidateRouteWallOpened(root, "02C_LowerWorkshopPocket", "Wall_East", failures);
            ValidateRouteWallOpened(root, "05_TreasureOverlook", "Wall_East", failures);
            ValidateRouteWallOpened(root, "06E_FinalApproachSouthBay", "Wall_South", failures);

            if (root.GetComponent<NavMeshSurface>() == null)
            {
                failures.Add("Research lab NavMeshSurface is missing.");
            }
        }

        private static void ValidateEntryForkObjective(GameObject root, List<string> failures)
        {
            Transform core = FindChild(root.transform, "Stage1_EntryFork_CriticalCore_RED");
            if (core == null)
            {
                failures.Add("Stage1_EntryFork_CriticalCore_RED missing.");
                return;
            }

            Health health = core.GetComponent<Health>();
            if (health == null)
            {
                failures.Add("Entry fork critical core has no Health.");
            }

            if (core.GetComponent<GameOverOnDeath>() == null)
            {
                failures.Add("Entry fork critical core has no GameOverOnDeath.");
            }

            Collider collider = core.GetComponent<Collider>();
            if (collider == null || !collider.enabled)
            {
                failures.Add("Entry fork critical core needs enabled collider for damage hits.");
            }
        }

        private static void ValidateCeilingLights(GameObject root, List<string> failures)
        {
            Transform lightRoot = FindChild(root.transform, "ResearchLab_CeilingLights");
            if (lightRoot == null)
            {
                failures.Add("ResearchLab_CeilingLights missing.");
                return;
            }

            string[] expectedLights =
            {
                "CeilingLight_StartSupply_A",
                "CeilingLight_EntryFork_A",
                "CeilingLight_LowerDefense_A",
                "CeilingLight_RewardVault",
                "CeilingLight_HighGround_A",
                "CeilingLight_FinalApproach_A",
                "CeilingLight_FinalVault_A",
                "CeilingLight_SpecimenArchive_A",
            };

            for (int i = 0; i < expectedLights.Length; i++)
            {
                if (lightRoot.Find(expectedLights[i]) == null)
                {
                    failures.Add("Ceiling light missing: " + expectedLights[i]);
                }
            }

            Light[] lights = lightRoot.GetComponentsInChildren<Light>(true);
            if (lights.Length < 29)
            {
                failures.Add("Research lab should contain at least 29 dedicated ceiling lights, found " + lights.Length + ".");
            }

            for (int i = 0; i < lights.Length; i++)
            {
                if (!lights[i].enabled)
                {
                    failures.Add(lights[i].name + " is disabled.");
                }

                if (lights[i].type != LightType.Point)
                {
                    failures.Add(lights[i].name + " must be a point light.");
                }

                if (lights[i].shadows != LightShadows.None)
                {
                    failures.Add(lights[i].name + " must have shadows disabled for prefab performance.");
                }
            }
        }

        private static void ValidateRouteWallOpened(GameObject root, string roomName, string wallName, List<string> failures)
        {
            Transform room = FindChild(root.transform, roomName);
            if (room == null)
            {
                failures.Add("Route room missing: " + roomName);
                return;
            }

            if (room.Find(wallName) != null || FindDirectOrNestedChild(room, wallName) != null)
            {
                failures.Add(roomName + "/" + wallName + " still blocks alternate player route.");
            }
        }

        private static void CreateAlternateSpawner(
            GameObject spawnerPrefab,
            GameObject routePointPrefab,
            GameObject enemyPrefab,
            Transform finalGoal,
            Transform parent,
            List<EnemySpawner> spawners,
            List<EnemyRoute> routes,
            string name,
            Vector3 position,
            Vector3[] waypointPositions)
        {
            GameObject spawnerObject = (GameObject)PrefabUtility.InstantiatePrefab(spawnerPrefab, parent);
            spawnerObject.name = name;
            spawnerObject.transform.localPosition = position;
            spawnerObject.transform.localRotation = Quaternion.identity;
            spawnerObject.transform.localScale = Vector3.one;

            EnemyRoute route = spawnerObject.GetComponent<EnemyRoute>();
            if (route == null)
            {
                route = spawnerObject.AddComponent<EnemyRoute>();
            }

            Transform routePointRoot = CreateEmpty(name + "_RoutePoints", spawnerObject.transform, Vector3.zero).transform;
            List<Transform> waypoints = new List<Transform>(waypointPositions.Length);
            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = (GameObject)PrefabUtility.InstantiatePrefab(routePointPrefab, routePointRoot);
                waypoint.name = name + "_RoutePoint_" + (i + 1).ToString("00");
                waypoint.transform.localPosition = waypointPositions[i] - position;
                waypoint.transform.localRotation = Quaternion.identity;
                waypoint.transform.localScale = Vector3.one;
                waypoints.Add(waypoint.transform);
            }

            EnemySpawner spawner = spawnerObject.GetComponent<EnemySpawner>();
            if (spawner == null)
            {
                throw new InvalidOperationException("EnemySpawner missing on " + name);
            }

            SerializedObject routeSerialized = new SerializedObject(route);
            SetObjectArray(routeSerialized, "waypoints", waypoints);
            routeSerialized.FindProperty("includeFinalTarget").boolValue = true;
            routeSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject spawnerSerialized = new SerializedObject(spawner);
            SetObject(spawnerSerialized, "enemyPrefab", enemyPrefab);
            SetObject(spawnerSerialized, "spawnPoint", spawnerObject.transform);
            SetObject(spawnerSerialized, "goal", finalGoal);
            SetObject(spawnerSerialized, "route", route);
            spawnerSerialized.FindProperty("spawnCount").intValue = 3;
            spawnerSerialized.FindProperty("spawnInterval").floatValue = 2.5f;
            spawnerSerialized.FindProperty("initialDelay").floatValue = 0.35f;
            spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();

            spawners.Add(spawner);
            routes.Add(route);
        }

        private static GameObject PlacePrefab(string name, string prefabPath, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject prefab = LoadRequiredPrefab(prefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            Transform transform = instance.transform;
            transform.localPosition = position;
            transform.localRotation = rotation;
            transform.localScale = scale;
            return instance;
        }

        private static void OpenRouteWall(GameObject root, string roomName, string wallName)
        {
            Transform room = FindChild(root.transform, roomName);
            if (room == null)
            {
                throw new InvalidOperationException("Route room missing: " + roomName);
            }

            Transform wall = room.Find(wallName);
            if (wall == null)
            {
                wall = FindDirectOrNestedChild(room, wallName);
            }

            if (wall != null)
            {
                UnityEngine.Object.DestroyImmediate(wall.gameObject);
            }
        }

        private static void CreateSealedCorridorX(
            string name,
            Transform parent,
            float x,
            float y,
            float z,
            float length,
            float width,
            Material floorMaterial,
            Material wallMaterial)
        {
            Transform corridor = CreateEmpty(name, parent, Vector3.zero).transform;
            Vector3 center = MapPosition(x, y, z);
            CreateRouteBlock("Floor", corridor, center + new Vector3(0f, -0.1f, 0f), MapBlockScale(length, 0.2f, width), floorMaterial, true);
            CreateRouteBlock("Wall_North", corridor, center + MapPosition(0f, RouteWallHeight * 0.5f, width * 0.5f), MapBlockScale(length, RouteWallHeight, RouteWallThickness), wallMaterial, true);
            CreateRouteBlock("Wall_South", corridor, center + MapPosition(0f, RouteWallHeight * 0.5f, -width * 0.5f), MapBlockScale(length, RouteWallHeight, RouteWallThickness), wallMaterial, true);
            CreateRouteBlock("Ceiling", corridor, center + new Vector3(0f, RouteWallHeight, 0f), MapBlockScale(length, RouteCeilingThickness, width), wallMaterial, false);
            CreateRouteLights(corridor, center, true, length);
        }

        private static void CreateSealedCorridorZ(
            string name,
            Transform parent,
            float x,
            float y,
            float z,
            float width,
            float length,
            Material floorMaterial,
            Material wallMaterial)
        {
            Transform corridor = CreateEmpty(name, parent, Vector3.zero).transform;
            Vector3 center = MapPosition(x, y, z);
            CreateRouteBlock("Floor", corridor, center + new Vector3(0f, -0.1f, 0f), MapBlockScale(width, 0.2f, length), floorMaterial, true);
            CreateRouteBlock("Wall_East", corridor, center + MapPosition(width * 0.5f, RouteWallHeight * 0.5f, 0f), MapBlockScale(RouteWallThickness, RouteWallHeight, length), wallMaterial, true);
            CreateRouteBlock("Wall_West", corridor, center + MapPosition(-width * 0.5f, RouteWallHeight * 0.5f, 0f), MapBlockScale(RouteWallThickness, RouteWallHeight, length), wallMaterial, true);
            CreateRouteBlock("Ceiling", corridor, center + new Vector3(0f, RouteWallHeight, 0f), MapBlockScale(width, RouteCeilingThickness, length), wallMaterial, false);
            CreateRouteLights(corridor, center, false, length);
        }

        private static void CreateSealedRoomEastWest(
            string name,
            Transform parent,
            float x,
            float y,
            float z,
            float width,
            float depth,
            Material floorMaterial,
            Material wallMaterial)
        {
            const float openingDepth = 4f;
            Transform room = CreateEmpty(name, parent, Vector3.zero).transform;
            Vector3 center = MapPosition(x, y, z);
            CreateRouteBlock("Floor", room, center + new Vector3(0f, -0.1f, 0f), MapBlockScale(width, 0.2f, depth), floorMaterial, true);
            CreateRouteBlock("Wall_North", room, center + MapPosition(0f, RouteWallHeight * 0.5f, depth * 0.5f), MapBlockScale(width, RouteWallHeight, RouteWallThickness), wallMaterial, true);
            CreateRouteBlock("Wall_South", room, center + MapPosition(0f, RouteWallHeight * 0.5f, -depth * 0.5f), MapBlockScale(width, RouteWallHeight, RouteWallThickness), wallMaterial, true);

            float segmentDepth = Mathf.Max(0.5f, (depth - openingDepth) * 0.5f);
            float segmentOffset = openingDepth * 0.5f + segmentDepth * 0.5f;
            CreateRouteBlock("Wall_West_NorthSegment", room, center + MapPosition(-width * 0.5f, RouteWallHeight * 0.5f, segmentOffset), MapBlockScale(RouteWallThickness, RouteWallHeight, segmentDepth), wallMaterial, true);
            CreateRouteBlock("Wall_West_SouthSegment", room, center + MapPosition(-width * 0.5f, RouteWallHeight * 0.5f, -segmentOffset), MapBlockScale(RouteWallThickness, RouteWallHeight, segmentDepth), wallMaterial, true);
            CreateRouteBlock("Wall_East_NorthSegment", room, center + MapPosition(width * 0.5f, RouteWallHeight * 0.5f, segmentOffset), MapBlockScale(RouteWallThickness, RouteWallHeight, segmentDepth), wallMaterial, true);
            CreateRouteBlock("Wall_East_SouthSegment", room, center + MapPosition(width * 0.5f, RouteWallHeight * 0.5f, -segmentOffset), MapBlockScale(RouteWallThickness, RouteWallHeight, segmentDepth), wallMaterial, true);
            CreateRouteBlock("Ceiling", room, center + new Vector3(0f, RouteWallHeight, 0f), MapBlockScale(width, RouteCeilingThickness, depth), wallMaterial, false);
            CreateRouteBlock("Archive_Console_North", room, center + MapPosition(-2.5f, 0.45f, 3.5f), MapBlockScale(4f, 0.9f, 1f), wallMaterial, true);
            CreateRouteBlock("Archive_Console_South", room, center + MapPosition(3f, 0.45f, -3.5f), MapBlockScale(4f, 0.9f, 1f), wallMaterial, true);
            CreateRouteLights(room, center, true, width);
        }

        private static void CreateElevatedCatwalkX(
            string name,
            Transform parent,
            float x,
            float y,
            float z,
            float length,
            float width,
            Material floorMaterial,
            Material wallMaterial)
        {
            Transform catwalk = CreateEmpty(name, parent, Vector3.zero).transform;
            Vector3 center = MapPosition(x, y, z);
            CreateRouteBlock("Deck", catwalk, center + new Vector3(0f, -0.1f, 0f), MapBlockScale(length, 0.25f, width), floorMaterial, true);
            CreateRouteBlock("Guard_North", catwalk, center + MapPosition(0f, 0.75f, width * 0.5f), MapBlockScale(length, 1.5f, 0.35f), wallMaterial, true);
            CreateRouteBlock("Guard_South", catwalk, center + MapPosition(0f, 0.75f, -width * 0.5f), MapBlockScale(length, 1.5f, 0.35f), wallMaterial, true);
            CreateRouteLights(catwalk, center + new Vector3(0f, 1.8f, 0f), true, length);
        }

        private static void CreateRampX(
            string name,
            Transform parent,
            float x,
            float y,
            float z,
            float length,
            float width,
            float zRotation,
            Material floorMaterial,
            Material wallMaterial)
        {
            Transform rampRoot = CreateEmpty(name, parent, Vector3.zero).transform;
            Vector3 center = MapPosition(x, y, z);
            GameObject ramp = CreateRouteBlock("Ramp", rampRoot, center, MapBlockScale(length, 0.3f, width), floorMaterial, true);
            ramp.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
            CreateRouteBlock("Guard_North", rampRoot, center + MapPosition(0f, 0.8f, width * 0.5f), MapBlockScale(length, 1.3f, 0.35f), wallMaterial, true).transform.localRotation = ramp.transform.localRotation;
            CreateRouteBlock("Guard_South", rampRoot, center + MapPosition(0f, 0.8f, -width * 0.5f), MapBlockScale(length, 1.3f, 0.35f), wallMaterial, true).transform.localRotation = ramp.transform.localRotation;
        }

        private static GameObject CreateRouteBlock(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localRotation = Quaternion.identity;
            block.transform.localScale = scale;

            if (!keepCollider)
            {
                Collider collider = block.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return block;
        }

        private static void CreateRouteLights(Transform parent, Vector3 center, bool alongX, float length)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(length / 18f));
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float offset = Mathf.Lerp(-length * 0.42f, length * 0.42f, t);
                Vector3 localOffset = alongX ? MapPosition(offset, RouteWallHeight - 1.1f, 0f) : MapPosition(0f, RouteWallHeight - 1.1f, offset);
                GameObject lightObject = CreateEmpty("CeilingLight_" + (i + 1).ToString("00"), parent, center + localOffset);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.55f, 0.85f, 1f);
                light.intensity = 0.55f;
                light.range = 13f;
                light.shadows = LightShadows.None;
            }
        }

        private static void CreateCeilingLight(
            Transform parent,
            Material fixtureMaterial,
            Material lensMaterial,
            string name,
            float x,
            float y,
            float z,
            float intensity,
            float range)
        {
            Transform fixtureRoot = CreateEmpty(name, parent, MapPosition(x, y, z)).transform;
            CreateRouteBlock("FixturePlate", fixtureRoot, Vector3.zero, new Vector3(3.2f, 0.16f, 3.2f), fixtureMaterial, false);
            CreateRouteBlock("LightPanel", fixtureRoot, new Vector3(0f, -0.12f, 0f), new Vector3(2.3f, 0.08f, 2.3f), lensMaterial, false);

            GameObject lightObject = CreateEmpty("PointLight", fixtureRoot, new Vector3(0f, -0.35f, 0f));
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.58f, 0.88f, 1f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void CreateObjectiveLight(Transform parent, Vector3 position)
        {
            GameObject lightObject = CreateEmpty("EntryForkCriticalCore_Light", parent, position);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.18f, 0.12f);
            light.intensity = 1.4f;
            light.range = 16f;
            light.shadows = LightShadows.None;
        }

        private static GameObject CreateMarker(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = position;
            marker.transform.localScale = scale;

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.name = name + "_MarkerMaterial";
                material.color = color;
                renderer.sharedMaterial = material;
            }

            return marker;
        }

        private static GameObject CreateEmpty(string name, Transform parent, Vector3 position)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child;
        }

        private static void DestroyExistingChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindDirectOrNestedChild(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                Transform nested = FindDirectOrNestedChild(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Serialized property missing: " + propertyName);
            }

            property.objectReferenceValue = value;
        }

        private static void ConfigureHealth(Health health, float maxHitPoints, bool destroyOnDeath)
        {
            if (health == null)
            {
                throw new InvalidOperationException("Health component missing.");
            }

            SerializedObject serialized = new SerializedObject(health);
            serialized.FindProperty("maxHitPoints").floatValue = maxHitPoints;
            serialized.FindProperty("destroyOnDeath").boolValue = destroyOnDeath;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray<T>(SerializedObject serialized, string propertyName, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Serialized array property missing: " + propertyName);
            }

            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void SetObjectArray<T>(SerializedProperty property, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            if (property == null)
            {
                throw new InvalidOperationException("Serialized array property missing.");
            }

            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static EnemySpawner FindSpawner(GameObject root, string name)
        {
            Transform transform = FindChild(root.transform, name);
            if (transform == null)
            {
                throw new InvalidOperationException("Enemy spawner missing: " + name);
            }

            EnemySpawner spawner = transform.GetComponent<EnemySpawner>();
            if (spawner == null)
            {
                throw new InvalidOperationException("EnemySpawner component missing on " + name);
            }

            return spawner;
        }

        private static MapExpansionDoorOpener FindDoor(GameObject root, string name)
        {
            Transform transform = FindChild(root.transform, name);
            if (transform == null)
            {
                throw new InvalidOperationException("Door missing: " + name);
            }

            MapExpansionDoorOpener door = transform.GetComponentInChildren<MapExpansionDoorOpener>(true);
            if (door == null)
            {
                throw new InvalidOperationException("MapExpansionDoorOpener missing on " + name);
            }

            return door;
        }

        private static void AddUnique<T>(List<T> result, IReadOnlyList<T> values)
            where T : class
        {
            for (int i = 0; i < values.Count; i++)
            {
                T value = values[i];
                if (value != null && !result.Contains(value))
                {
                    result.Add(value);
                }
            }
        }

        private static EnemySpawner[] MergeSpawners(params IReadOnlyList<EnemySpawner>[] groups)
        {
            List<EnemySpawner> result = new List<EnemySpawner>();
            for (int i = 0; i < groups.Length; i++)
            {
                AddUnique(result, groups[i]);
            }

            return result.ToArray();
        }

        private static List<Collider> SetDoorBlockersEnabled(GameObject root, bool enabled)
        {
            List<Collider> changedColliders = new List<Collider>();
            MapExpansionDoorOpener[] openers = root.GetComponentsInChildren<MapExpansionDoorOpener>(true);
            for (int i = 0; i < openers.Length; i++)
            {
                SerializedObject serialized = new SerializedObject(openers[i]);
                SerializedProperty blockerProperty = serialized.FindProperty("passageBlocker");
                GameObject blocker = blockerProperty != null ? blockerProperty.objectReferenceValue as GameObject : null;
                if (blocker == null)
                {
                    continue;
                }

                Collider[] colliders = blocker.GetComponentsInChildren<Collider>(true);
                for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                {
                    Collider collider = colliders[colliderIndex];
                    if (collider != null && collider.enabled != enabled)
                    {
                        collider.enabled = enabled;
                        changedColliders.Add(collider);
                    }
                }
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

        private static GameObject LoadRequiredPrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Required prefab missing: " + prefabPath);
            }

            return prefab;
        }

        private static T LoadRequiredAsset<T>(string assetPath)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException("Required asset missing: " + assetPath);
            }

            return asset;
        }

        private static Vector3 MapPosition(float x, float y, float z)
        {
            return new Vector3(x * MapScale, y, z * MapScale);
        }

        private static Vector3 MapBlockScale(float x, float y, float z)
        {
            return new Vector3(x * MapScale, y, z * MapScale);
        }

        private static void SaveRenderTexture(RenderTexture renderTexture, string assetPath)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D image = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            string absolutePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), assetPath);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolutePath));
            System.IO.File.WriteAllBytes(absolutePath, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void DisablePreviewCanvases(GameObject root)
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].enabled = false;
            }
        }

        private static void HidePreviewCeilings(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.name == "Ceiling" || renderers[i].gameObject.name == "FixturePlate")
                {
                    renderers[i].enabled = false;
                }
            }
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds combinedBounds = new Bounds(root.transform.position, Vector3.one);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled || !renderers[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException("Preview render bounds could not be calculated.");
            }

            return combinedBounds;
        }

        private static void DisablePreviewLightShadows(GameObject root)
        {
            Light[] lights = root.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].shadows = LightShadows.None;
            }
        }

        private static void DisablePrefabLightShadows(GameObject root)
        {
            Light[] lights = root.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].shadows = LightShadows.None;
            }
        }

        private readonly struct PartPrefabSpec
        {
            public PartPrefabSpec(string sourcePath, string outputPath)
            {
                SourcePath = sourcePath;
                OutputPath = outputPath;
            }

            public string SourcePath { get; }
            public string OutputPath { get; }
        }
    }
}
