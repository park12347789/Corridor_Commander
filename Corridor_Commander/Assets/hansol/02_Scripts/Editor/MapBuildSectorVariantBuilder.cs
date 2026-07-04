using System.Collections.Generic;
using System.IO;
using CorridorCommander;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class MapBuildSectorVariantBuilder
    {
        private const string VariantFolder = "Assets/hansol/03_Prefabs/MapBuildSets/Variants";
        private const string ScenePath = "Assets/hansol/01_Scenes/mapbuild_sector_variants.unity";
        private const string StartPrefabPath = "Assets/hansol/03_Prefabs/MapBuildSets/startponit.prefab";
        private const string Sector01Path = "Assets/hansol/03_Prefabs/MapBuildSets/sector01.prefab";
        private const string Sector02Path = "Assets/hansol/03_Prefabs/MapBuildSets/sector02.prefab";
        private const string Sector03Path = "Assets/hansol/03_Prefabs/MapBuildSets/sector03.prefab";
        private const string RightFinalLanePath = "Assets/hansol/03_Prefabs/MapBuildSets/Sector_11_RightFinalLane.prefab";

        private static readonly SectorPrefabSpec[] CurrentSectorPrefabs =
        {
            new SectorPrefabSpec(StartPrefabPath, 0, 0, 0, 0),
            new SectorPrefabSpec(Sector01Path, 1, 1, 2, 1),
            new SectorPrefabSpec(Sector02Path, 1, 1, 2, 1),
            new SectorPrefabSpec(Sector03Path, 1, 1, 2, 1),
            new SectorPrefabSpec(RightFinalLanePath, 1, 1, 1, 0),
        };

        private static readonly string[] LegacyVariantPaths =
        {
            VariantFolder + "/MapVariant_LinearGateRun.prefab",
            VariantFolder + "/MapVariant_OffsetAdvance.prefab",
            VariantFolder + "/MapVariant_WidePressure.prefab",
        };

        [MenuItem("Corridor Commander/MapBuild/Build Sector Variant Maps")]
        public static void Build()
        {
            BuildInternal(askBeforeReplacingOpenScene: true);
        }

        public static string BuildForAutomation()
        {
            return BuildInternal(askBeforeReplacingOpenScene: false);
        }

        [MenuItem("Corridor Commander/MapBuild/Validate Current Sector Set")]
        public static void Validate()
        {
            ValidateInternal(askBeforeOpeningScene: true);
        }

        public static string ValidateForAutomation()
        {
            return ValidateInternal(askBeforeOpeningScene: false);
        }

        private static string BuildInternal(bool askBeforeReplacingOpenScene)
        {
            if (askBeforeReplacingOpenScene && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return "Cancelled.";
            }

            EnsureSourcePrefab(StartPrefabPath);
            EnsureSourcePrefab(Sector01Path);
            EnsureSourcePrefab(Sector02Path);
            EnsureSourcePrefab(Sector03Path);
            EnsureFolder(VariantFolder);

            List<string> builtPaths = new List<string>();
            BuildVariant(
                "MapVariant_LinearGateRun",
                new Vector3(0f, 0f, -46f),
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 86f),
                new Vector3(0f, 0f, 158f),
                builtPaths);

            BuildVariant(
                "MapVariant_OffsetAdvance",
                new Vector3(-18f, 0f, -46f),
                new Vector3(0f, 0f, 0f),
                new Vector3(36f, 0f, 82f),
                new Vector3(10f, 0f, 156f),
                builtPaths);

            BuildVariant(
                "MapVariant_WidePressure",
                new Vector3(0f, 0f, -46f),
                new Vector3(0f, 0f, 0f),
                new Vector3(-42f, 0f, 78f),
                new Vector3(42f, 0f, 150f),
                builtPaths);

            BuildPreviewScene(builtPaths);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string validation = ValidateInternal(askBeforeOpeningScene: false);
            string result = "Built sector variant maps:\n"
                + string.Join("\n", builtPaths)
                + "\n" + ScenePath
                + "\n" + validation;
            Debug.Log(result);
            return result;
        }

        private static void BuildVariant(
            string name,
            Vector3 startPosition,
            Vector3 sector01Position,
            Vector3 sector02Position,
            Vector3 sector03Position,
            List<string> builtPaths)
        {
            GameObject root = new GameObject(name);
            GameObject map = new GameObject("map");
            map.transform.SetParent(root.transform, false);

            InstantiatePrefab(StartPrefabPath, map.transform, "startponit", startPosition);
            InstantiatePrefab(Sector01Path, map.transform, "sector01", sector01Position);
            InstantiatePrefab(Sector02Path, map.transform, "sector02", sector02Position);
            InstantiatePrefab(Sector03Path, map.transform, "sector03", sector03Position);

            AddDesignNote(
                root.transform,
                name + "_DesignNote",
                "Sector variant. Source sector01/02/03 preserved. Re-bake NavMesh before rotated or geometry edits.",
                new Vector3(0f, 7f, -58f));

            string savePath = VariantFolder + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, savePath);
            builtPaths.Add(savePath);
            Object.DestroyImmediate(root);
        }

        private static GameObject InstantiatePrefab(string path, Transform parent, string name, Vector3 localPosition)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null)
            {
                throw new System.InvalidOperationException("Missing source prefab: " + path);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            instance.SetActive(true);
            return instance;
        }

        private static void BuildPreviewScene(IReadOnlyList<string> builtPaths)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("MapBuildSectorVariants_PreviewRoot");
            Vector3[] offsets =
            {
                new Vector3(-230f, 0f, 0f),
                new Vector3(0f, 0f, 0f),
                new Vector3(230f, 0f, 0f)
            };

            for (int i = 0; i < builtPaths.Count; i++)
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(builtPaths[i]);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                instance.transform.localPosition = offsets[i];
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                AddDesignNote(
                    root.transform,
                    Path.GetFileNameWithoutExtension(builtPaths[i]) + "_Label",
                    Path.GetFileNameWithoutExtension(builtPaths[i]),
                    offsets[i] + new Vector3(0f, 12f, -70f));
            }

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 190f, -210f);
            cameraObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 210f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 700f;

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, 330f, 0f);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException("Failed to save sector variant scene: " + ScenePath);
            }
        }

        private static void AddDesignNote(Transform parent, string name, string text, Vector3 localPosition)
        {
            GameObject note = new GameObject(name);
            note.transform.SetParent(parent, false);
            note.transform.localPosition = localPosition;
            note.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);

            TextMesh mesh = note.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = 0.8f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
        }

        private static string ValidateInternal(bool askBeforeOpeningScene)
        {
            if (askBeforeOpeningScene && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return "Cancelled.";
            }

            List<string> failures = new List<string>();
            for (int i = 0; i < CurrentSectorPrefabs.Length; i++)
            {
                ValidateCurrentSectorPrefab(CurrentSectorPrefabs[i], failures);
            }

            string legacyStatus = ValidateLegacyVariantsIfPresent(failures);

            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("MapBuild sector validation failed:\n" + string.Join("\n", failures));
            }

            string result = "MapBuild sector validation passed. CurrentPrefabs=" + CurrentSectorPrefabs.Length + ", " + legacyStatus;
            Debug.Log(result);
            return result;
        }

        private static string ValidateLegacyVariantsIfPresent(List<string> failures)
        {
            int existingCount = 0;
            for (int i = 0; i < LegacyVariantPaths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(LegacyVariantPaths[i]) != null)
                {
                    existingCount++;
                }
            }

            if (existingCount == 0)
            {
                return "LegacyVariants=retired";
            }

            if (existingCount != LegacyVariantPaths.Length)
            {
                failures.Add("Legacy variant prefab set is partial. Expected "
                    + LegacyVariantPaths.Length
                    + ", found "
                    + existingCount
                    + ".");
                return "LegacyVariants=partial";
            }

            for (int i = 0; i < LegacyVariantPaths.Length; i++)
            {
                ValidateVariantPrefab(LegacyVariantPaths[i], failures);
            }

            ValidateLegacyPreviewScene(failures);
            return "LegacyVariants=validated";
        }

        private static void ValidateLegacyPreviewScene(List<string> failures)
        {
            if (!File.Exists(ScenePath))
            {
                failures.Add("Missing legacy preview scene: " + ScenePath);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                failures.Add("Legacy preview scene could not be opened: " + ScenePath);
            }

            if (FindTransformByName("MapBuildSectorVariants_PreviewRoot") == null)
            {
                failures.Add("Legacy preview root is missing.");
            }

            if (FindTransformByName("Main Camera") == null)
            {
                failures.Add("Legacy preview Main Camera is missing.");
            }

            if (FindTransformByName("Directional Light") == null)
            {
                failures.Add("Legacy preview Directional Light is missing.");
            }
        }

        private static void ValidateCurrentSectorPrefab(SectorPrefabSpec spec, List<string> failures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.Path);
            if (prefab == null)
            {
                failures.Add("Missing current sector prefab: " + spec.Path);
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(spec.Path);
            try
            {
                int missingScripts = CountMissingScripts(root);
                if (missingScripts > 0)
                {
                    failures.Add(spec.Path + " has missing scripts: " + missingScripts + ".");
                }

                int gateCount = root.GetComponentsInChildren<MapExpansionDoorOpener>(true).Length;
                int spawnerCount = root.GetComponentsInChildren<EnemySpawner>(true).Length;
                int chestCount = root.GetComponentsInChildren<TreasureChest>(true).Length;
                int navMeshSurfaceCount = root.GetComponentsInChildren<NavMeshSurface>(true).Length;

                RequireMinimumCount(spec.Path, "MapExpansionDoorOpener", gateCount, spec.MinGateCount, failures);
                RequireMinimumCount(spec.Path, "EnemySpawner", spawnerCount, spec.MinSpawnerCount, failures);
                RequireMinimumCount(spec.Path, "TreasureChest", chestCount, spec.MinChestCount, failures);
                RequireMinimumCount(spec.Path, "NavMeshSurface", navMeshSurfaceCount, spec.MinNavMeshSurfaceCount, failures);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RequireMinimumCount(
            string path,
            string label,
            int actual,
            int expected,
            List<string> failures)
        {
            if (actual < expected)
            {
                failures.Add(path + " expected at least " + expected + " " + label + ", found " + actual + ".");
            }
        }

        private static void ValidateVariantPrefab(string path, List<string> failures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                failures.Add("Missing variant prefab: " + path);
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;

            Transform map = instance.transform.Find("map");
            if (map == null)
            {
                failures.Add(path + " missing child: map.");
            }
            else
            {
                RequireChild(map, "startponit", path, failures);
                RequireChild(map, "sector01", path, failures);
                RequireChild(map, "sector02", path, failures);
                RequireChild(map, "sector03", path, failures);
            }

            int gateCount = instance.GetComponentsInChildren<MapExpansionDoorOpener>(true).Length;
            int spawnerCount = instance.GetComponentsInChildren<EnemySpawner>(true).Length;
            int chestCount = instance.GetComponentsInChildren<TreasureChest>(true).Length;
            if (gateCount < 3)
            {
                failures.Add(path + " expected at least 3 gates, found " + gateCount + ".");
            }

            if (spawnerCount < 3)
            {
                failures.Add(path + " expected at least 3 enemy spawners, found " + spawnerCount + ".");
            }

            if (chestCount < 6)
            {
                failures.Add(path + " expected at least 6 treasure chests, found " + chestCount + ".");
            }

            int missingScripts = CountMissingScripts(instance);
            if (missingScripts > 0)
            {
                failures.Add(path + " has missing scripts: " + missingScripts + ".");
            }

            Object.DestroyImmediate(instance);
        }

        private static void RequireChild(Transform parent, string childName, string path, List<string> failures)
        {
            if (parent.Find(childName) == null)
            {
                failures.Add(path + " missing child: map/" + childName + ".");
            }
        }

        private static Transform FindTransformByName(string name)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    return transforms[i];
                }
            }

            return null;
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

        private static void EnsureSourcePrefab(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                throw new System.InvalidOperationException("Missing source prefab: " + path);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
            {
                throw new System.InvalidOperationException("Invalid folder path: " + path);
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct SectorPrefabSpec
        {
            public SectorPrefabSpec(
                string path,
                int minGateCount,
                int minSpawnerCount,
                int minChestCount,
                int minNavMeshSurfaceCount)
            {
                Path = path;
                MinGateCount = minGateCount;
                MinSpawnerCount = minSpawnerCount;
                MinChestCount = minChestCount;
                MinNavMeshSurfaceCount = minNavMeshSurfaceCount;
            }

            public string Path { get; }
            public int MinGateCount { get; }
            public int MinSpawnerCount { get; }
            public int MinChestCount { get; }
            public int MinNavMeshSurfaceCount { get; }
        }
    }
}
