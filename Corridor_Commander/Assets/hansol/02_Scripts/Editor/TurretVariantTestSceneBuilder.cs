using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class TurretVariantTestSceneBuilder
    {
        private const string ScenePath = "Assets/hansol/01_Scenes/test1/turret_variant_test.unity";
        private const string PlacementPointPrefabPath = "Assets/hansol/03_Prefabs/PlacementPoint.prefab";
        private const string EnemyPrefabPath = "Assets/hansol/03_Prefabs/Enemy_Basic.prefab";
        private const string PlayerPrefabPath = "Assets/hansol/03_Prefabs/TEMP_KayKitThirdPersonTestPlayer.prefab";
        private const string MaterialFolder = "Assets/hansol/04_Materials";

        private static readonly string[] BuildableDefinitionPaths =
        {
            "Assets/hansol/09_Settings/Construction/Buildable_Turret.asset",
            "Assets/hansol/09_Settings/Construction/Buildable_Turret_Rapid.asset",
            "Assets/hansol/09_Settings/Construction/Buildable_Turret_LongRange.asset",
            "Assets/hansol/09_Settings/Construction/Buildable_Mortar.asset",
            "Assets/hansol/09_Settings/Construction/Buildable_Mortar_Rapid.asset",
            "Assets/hansol/09_Settings/Construction/Buildable_Mortar_Heavy.asset"
        };

        [MenuItem("Corridor Commander/Tests/Build Turret Variant Test Scene")]
        public static void Build()
        {
            BuildInternal(askBeforeReplacingOpenScene: true, createAdditively: false);
        }

        public static void BuildForAutomation()
        {
            BuildInternal(askBeforeReplacingOpenScene: false, createAdditively: true);
        }

        private static void BuildInternal(bool askBeforeReplacingOpenScene, bool createAdditively)
        {
            if (askBeforeReplacingOpenScene && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder("Assets/hansol/01_Scenes");
            EnsureFolder(MaterialFolder);

            GameObject placementPointPrefab = LoadRequiredAsset<GameObject>(PlacementPointPrefabPath);
            GameObject enemyPrefab = LoadRequiredAsset<GameObject>(EnemyPrefabPath);
            GameObject playerPrefab = LoadRequiredAsset<GameObject>(PlayerPrefabPath);
            BuildableDefinitionSO[] definitions = LoadBuildableDefinitions();
            if (placementPointPrefab == null || enemyPrefab == null || playerPrefab == null || definitions == null)
            {
                return;
            }

            CloseExistingTargetScene();
            Scene scene = CreateNewScene(createAdditively);
            scene.name = "turret_variant_test";
            EditorSceneManager.SetActiveScene(scene);

            Material floorMaterial = CreateMaterial(MaterialFolder + "/TurretVariantTest_Floor.mat", new Color(0.22f, 0.25f, 0.24f));
            Material laneMaterial = CreateMaterial(MaterialFolder + "/TurretVariantTest_Lane.mat", new Color(0.12f, 0.18f, 0.22f));
            Material targetMaterial = CreateMaterial(MaterialFolder + "/TurretVariantTest_Target.mat", new Color(0.65f, 0.12f, 0.12f));

            GameObject root = new GameObject("TurretVariantTestRoot");
            Transform floorRoot = CreateChild("Floor", root.transform).transform;
            Transform placementRoot = CreateChild("InstalledBuildables", root.transform).transform;
            Transform targetRoot = CreateChild("TargetDummies", root.transform).transform;
            Transform labelRoot = CreateChild("Labels", root.transform).transform;
            Transform playerRoot = CreateChild("Player", root.transform).transform;
            CreateRuntimeController(root.transform);

            CreateBlock("MainFloor", floorRoot, new Vector3(0f, -0.05f, 3f), new Vector3(24f, 0.1f, 18f), floorMaterial);
            CreateBlock("TargetLane", floorRoot, new Vector3(0f, 0.01f, 8f), new Vector3(22f, 0.04f, 2.5f), laneMaterial);
            GameObject player = CreatePlayer(playerPrefab, playerRoot);
            Camera playerCamera = ResolvePlayerCamera(player);
            Camera overviewCamera = CreateOverviewCamera(playerCamera == null);
            Camera testCamera = playerCamera != null ? playerCamera : overviewCamera;
            CreateLight();

            Vector3[] positions =
            {
                new Vector3(-8f, 0f, -2.5f),
                new Vector3(0f, 0f, -2.5f),
                new Vector3(8f, 0f, -2.5f),
                new Vector3(-8f, 0f, 2.5f),
                new Vector3(0f, 0f, 2.5f),
                new Vector3(8f, 0f, 2.5f)
            };

            for (int i = 0; i < definitions.Length; i++)
            {
                PlacementPoint point = CreatePlacementPoint(placementPointPrefab, placementRoot, definitions[i], positions[i], Quaternion.identity);
                GameObject builtObject = point.Build(definitions[i], null);
                if (builtObject == null)
                {
                    throw new System.InvalidOperationException("Failed to build " + definitions[i].BuildableId);
                }

                builtObject.transform.SetParent(placementRoot, true);
                ConfigureBillboards(builtObject, testCamera);
                CreateLabel(definitions[i].DisplayName, labelRoot, positions[i] + new Vector3(0f, 2.8f, -1.3f));
            }

            for (int i = 0; i < 6; i++)
            {
                Vector3 targetPosition = new Vector3(-10f + i * 4f, 0.05f, 8f);
                GameObject target = InstantiatePrefab(enemyPrefab, $"TargetDummy_{i + 1:00}", targetRoot, targetPosition, Quaternion.Euler(0f, 180f, 0f), Vector3.one);
                ConfigureTargetDummy(target, targetMaterial);
            }

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException("Failed to save turret variant test scene: " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Turret variant test scene built: " + ScenePath);
        }

        private static void CloseExistingTargetScene()
        {
            Scene existingScene = SceneManager.GetSceneByPath(ScenePath);
            if (!existingScene.IsValid() || !existingScene.isLoaded)
            {
                return;
            }

            if (!EditorSceneManager.CloseScene(existingScene, true))
            {
                throw new System.InvalidOperationException("Failed to close already open test scene: " + ScenePath);
            }
        }

        private static Scene CreateNewScene(bool preferAdditive)
        {
            if (preferAdditive)
            {
                try
                {
                    return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                }
                catch (System.InvalidOperationException exception)
                {
                    if (HasDirtyNonTargetScene())
                    {
                        throw new System.InvalidOperationException(
                            "Cannot replace open scenes while another scene has unsaved changes.",
                            exception);
                    }
                }
            }

            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static bool HasDirtyNonTargetScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && scene.path != ScenePath && scene.isDirty)
                {
                    return true;
                }
            }

            return false;
        }

        private static BuildableDefinitionSO[] LoadBuildableDefinitions()
        {
            List<BuildableDefinitionSO> definitions = new List<BuildableDefinitionSO>(BuildableDefinitionPaths.Length);
            for (int i = 0; i < BuildableDefinitionPaths.Length; i++)
            {
                BuildableDefinitionSO definition = LoadRequiredAsset<BuildableDefinitionSO>(BuildableDefinitionPaths[i]);
                if (definition == null)
                {
                    return null;
                }

                definitions.Add(definition);
            }

            return definitions.ToArray();
        }

        private static PlacementPoint CreatePlacementPoint(
            GameObject placementPointPrefab,
            Transform parent,
            BuildableDefinitionSO definition,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject pointObject = InstantiatePrefab(
                placementPointPrefab,
                "Placement_" + definition.BuildableId,
                parent,
                position,
                rotation,
                Vector3.one);

            PlacementPoint point = pointObject.GetComponent<PlacementPoint>();
            if (point == null)
            {
                throw new System.InvalidOperationException("PlacementPoint prefab missing PlacementPoint component.");
            }

            point.ConfigureBuildableDefinitions(new[] { definition });
            return point;
        }

        private static void ConfigureTargetDummy(GameObject target, Material targetMaterial)
        {
            EnemyMovementController movement = target.GetComponent<EnemyMovementController>();
            movement?.SetUpdateLoopEnabled(false);

            Health health = target.GetComponent<Health>();
            health?.Configure(250f, false);

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = targetMaterial;
            }
        }

        private static void ConfigureBillboards(GameObject root, Camera targetCamera)
        {
            if (root == null || targetCamera == null)
            {
                return;
            }

            WorldSpaceCameraBillboard[] billboards = root.GetComponentsInChildren<WorldSpaceCameraBillboard>(true);
            for (int i = 0; i < billboards.Length; i++)
            {
                SerializedObject serializedObject = new SerializedObject(billboards[i]);
                SerializedProperty targetCameraProperty = serializedObject.FindProperty("targetCamera");
                if (targetCameraProperty == null)
                {
                    continue;
                }

                targetCameraProperty.objectReferenceValue = targetCamera;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(billboards[i]);
            }
        }

        private static GameObject CreatePlayer(GameObject playerPrefab, Transform parent)
        {
            return InstantiatePrefab(
                playerPrefab,
                "TestPlayer_Start",
                parent,
                new Vector3(0f, 0.05f, -7f),
                Quaternion.identity,
                Vector3.one);
        }

        private static Camera ResolvePlayerCamera(GameObject player)
        {
            return player != null ? player.GetComponentInChildren<Camera>(true) : null;
        }

        private static Camera CreateOverviewCamera(bool enabled)
        {
            GameObject cameraObject = new GameObject("Turret Variant Overview Camera");
            cameraObject.tag = enabled ? "MainCamera" : "Untagged";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(0f, 16f, -16f);
            cameraObject.transform.rotation = Quaternion.Euler(54f, 0f, 0f);
            camera.enabled = enabled;
            camera.orthographic = true;
            camera.orthographicSize = 11f;
            camera.clearFlags = CameraClearFlags.Skybox;
            return camera;
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        private static void CreateRuntimeController(Transform parent)
        {
            GameObject registryObject = new GameObject("InstalledSkillRegistry");
            registryObject.transform.SetParent(parent, false);
            registryObject.AddComponent<InstalledSkillRegistry>();
            registryObject.AddComponent<TurretVariantTestRuntimeController>();
        }

        private static void CreateLabel(string text, Transform parent, Vector3 position)
        {
            GameObject labelObject = new GameObject("Label_" + text.Replace(" ", "_"));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = position;
            labelObject.transform.rotation = Quaternion.Euler(65f, 0f, 0f);

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.characterSize = 0.22f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = scale;

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return block;
        }

        private static GameObject InstantiatePrefab(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
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

        private static T LoadRequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError("Required asset missing: " + path);
            }

            return asset;
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
    }
}
