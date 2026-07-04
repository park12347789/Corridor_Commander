using System.Collections.Generic;
using System.IO;
using CorridorCommander;
using Unity.AI.Navigation;
using Unity.Behavior;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class SlopedTurretEnemyTestMapBuilder
    {
        private const string ScenePath = "Assets/hansol/01_Scenes/test1/SlopedTurretEnemyTest.unity";
        private const string EnemyPrefabPath = "Assets/hansol/03_Prefabs/Enemy_Basic.prefab";
        private const string BarricadePrefabPath = "Assets/hansol/03_Prefabs/Barricade_Basic.prefab";
        private const string TurretPrefabPath = "Assets/hansol/03_Prefabs/Turret_Basic.prefab";
        private const string SpawnerBehaviorPath = "Assets/hansol/09_Settings/Behavior/EnemySpawner_Unity_Behavior.asset";
        private const string TestMapPrefabFolder = "Assets/hansol/03_Prefabs/TestMap";
        private const string SolidBlockPrefabPath = TestMapPrefabFolder + "/Map_Solid_Block.prefab";
        private const string BreakableBlockPrefabPath = TestMapPrefabFolder + "/Map_Breakable_HP_Block.prefab";
        private const string EnemyGoalPrefabPath = TestMapPrefabFolder + "/Enemy_Goal.prefab";
        private const string EnemySpawnerPrefabPath = TestMapPrefabFolder + "/Enemy_SpawnPoint.prefab";
        private const string PlacementPointPrefabPath = TestMapPrefabFolder + "/PlacementPoint.prefab";
        private const string ExpansionDoorPrefabPath = TestMapPrefabFolder + "/MapExpansion_Door.prefab";
        private const string TemporaryPlayerPrefabPath = TestMapPrefabFolder + "/TEMP_GhostOperator_Player.prefab";

        private const float LowerSurfaceY = 0f;
        private const float UpperSurfaceY = 1.5f;

        [MenuItem("Corridor Commander/Build Sloped Turret Enemy Test Map")]
        public static void Build()
        {
            BuildInternal(askBeforeReplacingOpenScene: true);
        }

        public static void BuildForAutomation()
        {
            BuildInternal(askBeforeReplacingOpenScene: false);
        }

        [MenuItem("Corridor Commander/Create Sloped Test Reusable Prefabs")]
        public static void CreateReusablePrefabs()
        {
            EnsureFolders();

            GameObject enemyPrefab = LoadRequiredAsset<GameObject>(EnemyPrefabPath);
            GameObject barricadePrefab = LoadRequiredAsset<GameObject>(BarricadePrefabPath);
            GameObject turretPrefab = LoadRequiredAsset<GameObject>(TurretPrefabPath);
            if (enemyPrefab == null || barricadePrefab == null || turretPrefab == null)
            {
                return;
            }

            Material obstacleMaterial = CreateMaterial("Assets/hansol/04_Materials/TestMap_Obstacle_Stone.mat", new Color(0.38f, 0.36f, 0.32f));
            Material breakableMaterial = CreateMaterial("Assets/hansol/04_Materials/TestMap_Breakable_Blocker_Orange.mat", new Color(0.95f, 0.5f, 0.12f));
            Material placementMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_PlacementPoint_Green.mat", new Color(0.05f, 1f, 0.2f));
            Material spawnMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_SpawnPoint_Red.mat", new Color(1f, 0.15f, 0.12f));
            Material goalMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_Goal_Yellow.mat", new Color(1f, 0.86f, 0.05f));
            Material playerMaterial = CreateMaterial("Assets/hansol/04_Materials/TEMP_Prototype_Player_Purple.mat", new Color(0.55f, 0.18f, 0.95f));
            Material doorMaterial = CreateMaterial("Assets/hansol/04_Materials/TestMap_Expansion_Door_Blue.mat", new Color(0.05f, 0.35f, 0.95f));

            SavePrefab(
                CreateBlockingCube("Map_Solid_Block", null, Vector3.zero, Vector3.one, obstacleMaterial),
                SolidBlockPrefabPath);
            SavePrefab(CreateBreakableBlockPrefabRoot(breakableMaterial), BreakableBlockPrefabPath);
            SavePrefab(CreateGoal(null, goalMaterial), EnemyGoalPrefabPath);
            SavePrefab(CreateEnemySpawnerPrefabRoot(enemyPrefab, spawnMaterial), EnemySpawnerPrefabPath);
            SavePrefab(CreatePlacementPointPrefabRoot(turretPrefab, barricadePrefab, placementMaterial), PlacementPointPrefabPath);
            SavePrefab(CreateExpansionDoorPrefabRoot(doorMaterial), ExpansionDoorPrefabPath);
            SavePrefab(CreateTemporaryPlayerPrefabRoot(playerMaterial), TemporaryPlayerPrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Sloped test reusable prefabs created under {TestMapPrefabFolder}");
        }

        private static void BuildInternal(bool askBeforeReplacingOpenScene)
        {
            if (askBeforeReplacingOpenScene && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolders();

            GameObject enemyPrefab = LoadRequiredAsset<GameObject>(EnemyPrefabPath);
            GameObject barricadePrefab = LoadRequiredAsset<GameObject>(BarricadePrefabPath);
            GameObject turretPrefab = LoadRequiredAsset<GameObject>(TurretPrefabPath);
            if (enemyPrefab == null || barricadePrefab == null || turretPrefab == null)
            {
                return;
            }

            Material floorMaterial = CreateMaterial("Assets/hansol/04_Materials/TestMap_Floor_Moss.mat", new Color(0.22f, 0.45f, 0.28f));
            Material rampMaterial = CreateMaterial("Assets/hansol/04_Materials/TestMap_Ramp_Concrete.mat", new Color(0.47f, 0.48f, 0.52f));
            Material wallMaterial = CreateMaterial("Assets/hansol/04_Materials/TestMap_Wall_Dark.mat", new Color(0.08f, 0.08f, 0.09f));
            Material obstacleMaterial = CreateMaterial("Assets/hansol/04_Materials/TestMap_Obstacle_Stone.mat", new Color(0.38f, 0.36f, 0.32f));
            Material breakableMaterial = CreateMaterial("Assets/hansol/04_Materials/TestMap_Breakable_Blocker_Orange.mat", new Color(0.95f, 0.5f, 0.12f));
            Material placementMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_PlacementPoint_Green.mat", new Color(0.05f, 1f, 0.2f));
            Material spawnMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_SpawnPoint_Red.mat", new Color(1f, 0.15f, 0.12f));
            Material goalMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_Goal_Yellow.mat", new Color(1f, 0.86f, 0.05f));
            Material routeMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_RouteLine_Cyan.mat", new Color(0f, 0.9f, 1f));
            Material playerMaterial = CreateMaterial("Assets/hansol/04_Materials/TEMP_Prototype_Player_Purple.mat", new Color(0.55f, 0.18f, 0.95f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SlopedTurretEnemyTest";

            Camera camera = CreateCamera();
            LightScene();
            GameManager gameManager = new GameObject("GameManager").AddComponent<GameManager>();

            GameObject environment = new GameObject("Environment");
            BuildEnvironment(environment.transform, floorMaterial, rampMaterial, wallMaterial, obstacleMaterial);

            GameObject goal = CreateGoal(gameManager, goalMaterial);
            CreateBreakableHealthBlocker(breakableMaterial);
            Transform spawnAnchor = CreateEnemySpawner(enemyPrefab, goal.transform, spawnMaterial);
            CreatePlacementPoints(turretPrefab, barricadePrefab, placementMaterial);
            CreateTemporaryPlayer(camera, playerMaterial);

            NavMeshSurface surface = environment.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.defaultArea = 0;
            surface.BuildNavMesh();
            CreateRouteLine(spawnAnchor, goal.transform, routeMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Corridor Commander/Validate Sloped Turret Enemy Test Map")]
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

            RequireGameObject("Environment", failures);
            RequireGameObject("Enemy_SpawnPoint_RED", failures);
            RequireGameObject("EnemySpawnAnchor", failures);
            RequireGameObject("Enemy_Goal_YELLOW", failures);
            RequireGameObject("Breakable_HP_Blocker_ORANGE", failures);
            RequireGameObject("Main_Slope_Lower_To_Upper", failures);
            RequireGameObject("Enemy_RouteLine_CYAN_SlopePath", failures);

            EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner == null)
            {
                failures.Add("EnemySpawner is missing.");
            }

            NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                failures.Add("NavMeshSurface is missing.");
            }
            else if (surface.navMeshData == null)
            {
                failures.Add("NavMeshSurface has no baked NavMeshData.");
            }

            PlacementPoint[] placementPoints = Object.FindObjectsByType<PlacementPoint>(FindObjectsSortMode.None);
            if (placementPoints.Length != 6)
            {
                failures.Add($"Expected 6 placement points, found {placementPoints.Length}.");
            }

            RequireObstacleSetup("Lower_Box_Obstacle", MapObstacleKind.Solid, failures);
            RequireObstacleSetup("Upper_Box_Obstacle_A", MapObstacleKind.Solid, failures);
            RequireObstacleSetup("Upper_Box_Obstacle_B", MapObstacleKind.Solid, failures);
            RequireObstacleSetup("Breakable_HP_Blocker_ORANGE", MapObstacleKind.Breakable, failures);

            Transform spawnAnchor = GameObject.Find("EnemySpawnAnchor")?.transform;
            Transform goal = GameObject.Find("Enemy_Goal_YELLOW")?.transform;
            if (spawnAnchor != null && goal != null && !HasCompleteNavMeshPath(spawnAnchor.position, goal.position))
            {
                failures.Add("No complete NavMesh path from EnemySpawnAnchor to Enemy_Goal_YELLOW.");
            }

            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("Sloped turret enemy test map validation failed:\n" + string.Join("\n", failures));
            }

            Debug.Log($"Sloped turret enemy test map validation passed. PlacementPoints={placementPoints.Length}, Scene={ScenePath}");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/hansol/01_Scenes");
            EnsureFolder(TestMapPrefabFolder);
            EnsureFolder("Assets/hansol/04_Materials");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static T LoadRequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError($"Missing required test map asset: {path}");
            }

            return asset;
        }

        private static void RequireGameObject(string name, List<string> failures)
        {
            if (GameObject.Find(name) == null)
            {
                failures.Add($"Missing GameObject: {name}");
            }
        }

        private static void RequireObstacleSetup(string name, MapObstacleKind expectedKind, List<string> failures)
        {
            GameObject obstacleObject = GameObject.Find(name);
            if (obstacleObject == null)
            {
                failures.Add($"Missing obstacle: {name}");
                return;
            }

            if (!obstacleObject.TryGetComponent(out Collider obstacleCollider) || obstacleCollider.isTrigger)
            {
                failures.Add($"{name} must have a non-trigger Collider.");
            }

            if (!obstacleObject.TryGetComponent(out MapObstacle mapObstacle) || mapObstacle.ObstacleKind != expectedKind)
            {
                failures.Add($"{name} must have MapObstacle kind {expectedKind}.");
            }

            if (expectedKind == MapObstacleKind.Solid)
            {
                if (!obstacleObject.TryGetComponent(out NavMeshObstacle navMeshObstacle) || !navMeshObstacle.enabled || !navMeshObstacle.carving)
                {
                    failures.Add($"{name} must have an active carving NavMeshObstacle.");
                }
            }
            else if (!obstacleObject.TryGetComponent(out Health health) || !health.IsAlive)
            {
                failures.Add($"{name} must have live Health.");
            }
        }

        private static bool HasCompleteNavMeshPath(Vector3 start, Vector3 goal)
        {
            if (!NavMesh.SamplePosition(start, out NavMeshHit startHit, 2f, NavMesh.AllAreas))
            {
                return false;
            }

            if (!NavMesh.SamplePosition(goal, out NavMeshHit goalHit, 2f, NavMesh.AllAreas))
            {
                return false;
            }

            NavMeshPath path = new NavMeshPath();
            return NavMesh.CalculatePath(startHit.position, goalHit.position, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete
                && path.corners.Length >= 2;
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 8f, -14f);
            cameraObject.transform.rotation = Quaternion.Euler(48f, 0f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 120f;
            return camera;
        }

        private static void LightScene()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        private static void BuildEnvironment(
            Transform parent,
            Material floorMaterial,
            Material rampMaterial,
            Material wallMaterial,
            Material obstacleMaterial)
        {
            CreateCube("Lower_Test_Floor", parent, new Vector3(0f, -0.05f, -8f), new Vector3(11f, 0.1f, 11f), floorMaterial).isStatic = true;
            CreateCube("Upper_Long_Platform", parent, new Vector3(-1f, 1.4f, 2.8f), new Vector3(13f, 0.2f, 10f), floorMaterial).isStatic = true;
            CreateCube("Goal_Side_Platform", parent, new Vector3(5.7f, 1.4f, 8.3f), new Vector3(6.2f, 0.2f, 6.2f), floorMaterial).isStatic = true;

            CreateRamp("Main_Slope_Lower_To_Upper", parent, new Vector3(-1.7f, LowerSurfaceY, -4f), 5f, 7f, UpperSurfaceY - LowerSurfaceY, rampMaterial).isStatic = true;
            CreateRamp("Side_Slope_To_Goal_Platform", parent, new Vector3(3.2f, UpperSurfaceY, 4.4f), 4.4f, 5.3f, 0f, rampMaterial).isStatic = true;

            CreateBlockingCube("Left_Lower_Guard_Wall", parent, new Vector3(-5.6f, 0.9f, -8f), new Vector3(0.25f, 1.8f, 11f), wallMaterial);
            CreateBlockingCube("Right_Lower_Guard_Wall", parent, new Vector3(5.6f, 0.9f, -8f), new Vector3(0.25f, 1.8f, 11f), wallMaterial);
            CreateBlockingCube("Upper_Back_Guard_Wall", parent, new Vector3(-1f, 2.25f, 7.9f), new Vector3(13f, 1.5f, 0.25f), wallMaterial);
            CreateBlockingCube("Goal_Platform_Guard_Wall", parent, new Vector3(8.9f, 2.2f, 8.3f), new Vector3(0.25f, 1.4f, 6.2f), wallMaterial);

            CreateBlockingCube("Lower_Box_Obstacle", parent, new Vector3(1.9f, 0.45f, -5.8f), new Vector3(1.8f, 0.9f, 1.8f), obstacleMaterial);
            CreateBlockingCube("Upper_Box_Obstacle_A", parent, new Vector3(-2.9f, 2.05f, 1.6f), new Vector3(2f, 1.1f, 1.5f), obstacleMaterial);
            CreateBlockingCube("Upper_Box_Obstacle_B", parent, new Vector3(2.4f, 1.95f, 5.4f), new Vector3(2.2f, 0.9f, 1.8f), obstacleMaterial);
        }

        private static GameObject CreateBlockingCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = CreateCube(name, parent, position, scale, material);
            cube.isStatic = true;

            NavMeshModifier modifier = cube.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = GetNotWalkableArea();

            NavMeshObstacle obstacle = cube.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = Vector3.zero;
            obstacle.size = Vector3.one;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;

            cube.AddComponent<MapObstacle>();
            return cube;
        }

        private static int GetNotWalkableArea()
        {
            int area = NavMesh.GetAreaFromName("Not Walkable");
            return area >= 0 ? area : 1;
        }

        private static GameObject CreateRamp(string name, Transform parent, Vector3 lowCenter, float width, float length, float height, Material material)
        {
            GameObject ramp = new GameObject(name);
            ramp.transform.SetParent(parent);
            ramp.transform.position = lowCenter;

            const float thickness = 0.18f;
            float halfWidth = width * 0.5f;
            float halfLength = length * 0.5f;

            Vector3[] vertices =
            {
                new Vector3(-halfWidth, 0f, -halfLength),
                new Vector3(halfWidth, 0f, -halfLength),
                new Vector3(-halfWidth, height, halfLength),
                new Vector3(halfWidth, height, halfLength),
                new Vector3(-halfWidth, -thickness, -halfLength),
                new Vector3(halfWidth, -thickness, -halfLength),
                new Vector3(-halfWidth, height - thickness, halfLength),
                new Vector3(halfWidth, height - thickness, halfLength)
            };

            int[] triangles =
            {
                0, 2, 1, 1, 2, 3,
                4, 5, 6, 5, 7, 6,
                0, 4, 2, 2, 4, 6,
                1, 3, 5, 3, 7, 5,
                0, 1, 4, 1, 5, 4,
                2, 6, 3, 3, 6, 7
            };

            Mesh mesh = new Mesh
            {
                name = $"{name}_Mesh",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = ramp.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = ramp.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            MeshCollider collider = ramp.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;

            return ramp;
        }

        private static GameObject CreateGoal(GameManager gameManager, Material goalMaterial)
        {
            GameObject goal = CreateCube("Enemy_Goal_YELLOW", null, new Vector3(6.7f, UpperSurfaceY + 0.25f, 8.4f), new Vector3(1.8f, 0.5f, 1.8f), goalMaterial);
            Rigidbody rigidbody = goal.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            Health health = goal.AddComponent<Health>();
            SetHealthValues(health, 90f, true);

            GameOverOnDeath gameOverOnDeath = goal.AddComponent<GameOverOnDeath>();
            SerializedObject serializedGameOver = new SerializedObject(gameOverOnDeath);
            serializedGameOver.FindProperty("gameManager").objectReferenceValue = gameManager;
            serializedGameOver.FindProperty("reason").stringValue = "Sloped test goal destroyed";
            serializedGameOver.ApplyModifiedPropertiesWithoutUndo();

            return goal;
        }

        private static GameObject CreateBreakableHealthBlocker(Material breakableMaterial)
        {
            GameObject blocker = CreateCube(
                "Breakable_HP_Blocker_ORANGE",
                null,
                new Vector3(-1.2f, UpperSurfaceY + 0.55f, 1.2f),
                new Vector3(3.2f, 1.1f, 0.65f),
                breakableMaterial);

            Rigidbody rigidbody = blocker.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            Health health = blocker.AddComponent<Health>();
            SetHealthValues(health, 55f, true);
            MapObstacle mapObstacle = blocker.AddComponent<MapObstacle>();
            SerializedObject serializedObstacle = new SerializedObject(mapObstacle);
            serializedObstacle.FindProperty("obstacleKind").enumValueIndex = (int)MapObstacleKind.Breakable;
            serializedObstacle.ApplyModifiedPropertiesWithoutUndo();
            return blocker;
        }

        private static Transform CreateEnemySpawner(GameObject enemyPrefab, Transform goal, Material spawnMaterial)
        {
            GameObject spawnPoint = CreateCube("Enemy_SpawnPoint_RED", null, new Vector3(-3.6f, LowerSurfaceY + 0.15f, -12.2f), new Vector3(1.5f, 0.3f, 1.5f), spawnMaterial);
            Collider spawnCollider = spawnPoint.GetComponent<Collider>();
            spawnCollider.isTrigger = true;

            GameObject spawnAnchor = new GameObject("EnemySpawnAnchor");
            spawnAnchor.transform.SetParent(spawnPoint.transform);
            spawnAnchor.transform.position = new Vector3(-3.6f, LowerSurfaceY + 0.55f, -12.2f);
            spawnAnchor.transform.rotation = Quaternion.identity;

            EnemySpawner spawner = spawnPoint.AddComponent<EnemySpawner>();
            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
            serializedSpawner.FindProperty("spawnPoint").objectReferenceValue = spawnAnchor.transform;
            serializedSpawner.FindProperty("goal").objectReferenceValue = goal;
            serializedSpawner.FindProperty("spawnCount").intValue = 10;
            serializedSpawner.FindProperty("spawnInterval").floatValue = 1.35f;
            serializedSpawner.FindProperty("initialDelay").floatValue = 0.4f;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            BehaviorGraph spawnerGraph = AssetDatabase.LoadAssetAtPath<BehaviorGraph>(SpawnerBehaviorPath);
            if (spawnerGraph != null)
            {
                BehaviorGraphAgent behaviorAgent = spawnPoint.AddComponent<BehaviorGraphAgent>();
                SerializedObject serializedAgent = new SerializedObject(behaviorAgent);
                serializedAgent.FindProperty("m_Graph").objectReferenceValue = spawnerGraph;
                serializedAgent.ApplyModifiedPropertiesWithoutUndo();
            }

            return spawnAnchor.transform;
        }

        private static void CreateRouteLine(Transform spawnPoint, Transform goal, Material routeMaterial)
        {
            GameObject routeLineObject = new GameObject("Enemy_RouteLine_CYAN_SlopePath");
            LineRenderer lineRenderer = routeLineObject.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = routeMaterial;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.14f;
            lineRenderer.endWidth = 0.14f;
            lineRenderer.numCapVertices = 4;

            EnemyRouteLineVisualizer visualizer = routeLineObject.AddComponent<EnemyRouteLineVisualizer>();
            SerializedObject serializedVisualizer = new SerializedObject(visualizer);
            serializedVisualizer.FindProperty("startPoint").objectReferenceValue = spawnPoint;
            serializedVisualizer.FindProperty("goalPoint").objectReferenceValue = goal;
            serializedVisualizer.FindProperty("heightOffset").floatValue = 0.1f;
            serializedVisualizer.ApplyModifiedPropertiesWithoutUndo();
            visualizer.Refresh();
        }

        private static GameObject CreateBreakableBlockPrefabRoot(Material breakableMaterial)
        {
            GameObject blocker = CreateCube("Map_Breakable_HP_Block", null, Vector3.zero, Vector3.one, breakableMaterial);

            Rigidbody rigidbody = blocker.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            Health health = blocker.AddComponent<Health>();
            SetHealthValues(health, 55f, true);

            MapObstacle mapObstacle = blocker.AddComponent<MapObstacle>();
            SerializedObject serializedObstacle = new SerializedObject(mapObstacle);
            serializedObstacle.FindProperty("obstacleKind").enumValueIndex = (int)MapObstacleKind.Breakable;
            serializedObstacle.ApplyModifiedPropertiesWithoutUndo();
            return blocker;
        }

        private static GameObject CreateEnemySpawnerPrefabRoot(GameObject enemyPrefab, Material spawnMaterial)
        {
            GameObject spawnPoint = CreateCube("Enemy_SpawnPoint", null, Vector3.zero, Vector3.one, spawnMaterial);
            Collider spawnCollider = spawnPoint.GetComponent<Collider>();
            spawnCollider.isTrigger = true;

            GameObject spawnAnchor = new GameObject("EnemySpawnAnchor");
            spawnAnchor.transform.SetParent(spawnPoint.transform);
            spawnAnchor.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            spawnAnchor.transform.localRotation = Quaternion.identity;

            EnemySpawner spawner = spawnPoint.AddComponent<EnemySpawner>();
            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
            serializedSpawner.FindProperty("spawnPoint").objectReferenceValue = spawnAnchor.transform;
            serializedSpawner.FindProperty("spawnCount").intValue = 10;
            serializedSpawner.FindProperty("spawnInterval").floatValue = 1.35f;
            serializedSpawner.FindProperty("initialDelay").floatValue = 0.4f;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            BehaviorGraph spawnerGraph = AssetDatabase.LoadAssetAtPath<BehaviorGraph>(SpawnerBehaviorPath);
            if (spawnerGraph != null)
            {
                BehaviorGraphAgent behaviorAgent = spawnPoint.AddComponent<BehaviorGraphAgent>();
                SerializedObject serializedAgent = new SerializedObject(behaviorAgent);
                serializedAgent.FindProperty("m_Graph").objectReferenceValue = spawnerGraph;
                serializedAgent.ApplyModifiedPropertiesWithoutUndo();
            }

            return spawnPoint;
        }

        private static GameObject CreatePlacementPointPrefabRoot(GameObject turretPrefab, GameObject barricadePrefab, Material placementMaterial)
        {
            GameObject point = CreateCube("PlacementPoint", null, Vector3.zero, new Vector3(1.55f, 0.08f, 1.55f), placementMaterial);
            Collider collider = point.GetComponent<Collider>();
            collider.isTrigger = true;

            GameObject anchor = new GameObject("BuildAnchor");
            anchor.transform.SetParent(point.transform);
            anchor.transform.localPosition = new Vector3(0f, 0.51f, 0f);
            anchor.transform.localRotation = Quaternion.identity;

            PlacementPoint placementPoint = point.AddComponent<PlacementPoint>();
            SerializedObject serializedPoint = new SerializedObject(placementPoint);
            serializedPoint.FindProperty("buildAnchor").objectReferenceValue = anchor.transform;
            serializedPoint.FindProperty("turretPrefab").objectReferenceValue = turretPrefab;
            serializedPoint.FindProperty("barricadePrefab").objectReferenceValue = barricadePrefab;
            serializedPoint.FindProperty("indicatorRenderer").objectReferenceValue = point.GetComponent<Renderer>();
            serializedPoint.ApplyModifiedPropertiesWithoutUndo();
            return point;
        }

        private static GameObject CreateExpansionDoorPrefabRoot(Material doorMaterial)
        {
            GameObject doorRoot = new GameObject("MapExpansion_Door");

            GameObject blocker = CreateBlockingCube("MapExpansion_Door_Blocker", doorRoot.transform, Vector3.zero, Vector3.one, doorMaterial);

            GameObject trigger = new GameObject("MapExpansion_Door_Trigger");
            trigger.transform.SetParent(doorRoot.transform);
            trigger.transform.localPosition = new Vector3(-0.85f, 0f, 0f);
            BoxCollider triggerCollider = trigger.AddComponent<BoxCollider>();
            triggerCollider.size = new Vector3(2.4f, 1.8f, 3.2f);
            triggerCollider.isTrigger = true;

            GameObject prompt = CreateDoorPrompt(trigger.transform);
            prompt.SetActive(false);

            MapExpansionDoorOpener doorOpener = doorRoot.AddComponent<MapExpansionDoorOpener>();
            SerializedObject serializedOpener = new SerializedObject(doorOpener);
            serializedOpener.FindProperty("closedDoorRoot").objectReferenceValue = blocker;
            serializedOpener.ApplyModifiedPropertiesWithoutUndo();

            MapExpansionDoorInteraction interaction = trigger.AddComponent<MapExpansionDoorInteraction>();
            SerializedObject serializedInteraction = new SerializedObject(interaction);
            serializedInteraction.FindProperty("doorOpener").objectReferenceValue = doorOpener;
            serializedInteraction.FindProperty("interactionPromptRoot").objectReferenceValue = prompt;
            serializedInteraction.ApplyModifiedPropertiesWithoutUndo();
            return doorRoot;
        }

        private static GameObject CreateDoorPrompt(Transform parent)
        {
            GameObject prompt = new GameObject("MapExpansion_Door_Prompt_E_Open");
            prompt.transform.SetParent(parent);
            prompt.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            prompt.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);

            TextMesh textMesh = prompt.AddComponent<TextMesh>();
            textMesh.text = "E Open";
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.22f;
            textMesh.color = Color.white;
            return prompt;
        }

        private static GameObject CreateTemporaryPlayerPrefabRoot(Material playerMaterial)
        {
            GameObject playerRoot = new GameObject("TEMP_GhostOperator_Player");
            playerRoot.tag = "Player";

            CharacterController characterController = playerRoot.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            playerRoot.AddComponent<CharacterMovementMotor>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "TEMP_VISUAL_ReplaceWithRealCharacter";
            body.transform.SetParent(playerRoot.transform);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            if (body.TryGetComponent(out Collider bodyCollider))
            {
                Object.DestroyImmediate(bodyCollider);
            }
            if (body.TryGetComponent(out Renderer bodyRenderer))
            {
                bodyRenderer.sharedMaterial = playerMaterial;
            }

            GameObject cameraPivot = new GameObject("TEMP_CameraPitchPivot_DoNotShip");
            cameraPivot.transform.SetParent(playerRoot.transform);
            cameraPivot.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            cameraPivot.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(cameraPivot.transform);
            cameraObject.transform.localPosition = new Vector3(0.7f, 0.15f, -3.6f);
            cameraObject.transform.localRotation = Quaternion.identity;
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();

            TEMP_GhostOperatorPlaceholderController controller = playerRoot.AddComponent<TEMP_GhostOperatorPlaceholderController>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("cameraPitchPivot").objectReferenceValue = cameraPivot.transform;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            playerRoot.AddComponent<TEMP_GhostOperatorBuildInput>();
            return playerRoot;
        }

        private static void CreatePlacementPoints(GameObject turretPrefab, GameObject barricadePrefab, Material placementMaterial)
        {
            CreatePlacementPoint("PlacementPoint_01_GREEN_LowerCorner", new Vector3(-4.1f, LowerSurfaceY, -7.2f), turretPrefab, barricadePrefab, placementMaterial);
            CreatePlacementPoint("PlacementPoint_02_GREEN_RampEntry", new Vector3(-0.8f, LowerSurfaceY, -6.2f), turretPrefab, barricadePrefab, placementMaterial);
            CreatePlacementPoint("PlacementPoint_03_GREEN_UpperLeft", new Vector3(-4.3f, UpperSurfaceY, 0.6f), turretPrefab, barricadePrefab, placementMaterial);
            CreatePlacementPoint("PlacementPoint_04_GREEN_UpperCenter", new Vector3(-1f, UpperSurfaceY, 2.8f), turretPrefab, barricadePrefab, placementMaterial);
            CreatePlacementPoint("PlacementPoint_05_GREEN_UpperRight", new Vector3(2.3f, UpperSurfaceY, 3.4f), turretPrefab, barricadePrefab, placementMaterial);
            CreatePlacementPoint("PlacementPoint_06_GREEN_GoalSide", new Vector3(5.1f, UpperSurfaceY, 6.8f), turretPrefab, barricadePrefab, placementMaterial);
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreatePlacementPoint(string name, Vector3 surfacePosition, GameObject turretPrefab, GameObject barricadePrefab, Material placementMaterial)
        {
            GameObject point = CreateCube(
                name,
                null,
                new Vector3(surfacePosition.x, surfacePosition.y + 0.04f, surfacePosition.z),
                new Vector3(1.55f, 0.08f, 1.55f),
                placementMaterial);

            Collider collider = point.GetComponent<Collider>();
            collider.isTrigger = true;

            GameObject anchor = new GameObject("BuildAnchor");
            anchor.transform.SetParent(point.transform);
            anchor.transform.position = new Vector3(surfacePosition.x, surfacePosition.y + 0.55f, surfacePosition.z);
            anchor.transform.rotation = Quaternion.identity;

            PlacementPoint placementPoint = point.AddComponent<PlacementPoint>();
            SerializedObject serializedPoint = new SerializedObject(placementPoint);
            serializedPoint.FindProperty("buildAnchor").objectReferenceValue = anchor.transform;
            serializedPoint.FindProperty("turretPrefab").objectReferenceValue = turretPrefab;
            serializedPoint.FindProperty("barricadePrefab").objectReferenceValue = barricadePrefab;
            serializedPoint.FindProperty("indicatorRenderer").objectReferenceValue = point.GetComponent<Renderer>();
            serializedPoint.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateTemporaryPlayer(Camera camera, Material playerMaterial)
        {
            GameObject playerRoot = new GameObject("TEMP_DO_NOT_FINALIZE_GhostOperator_PlayerRoot");
            playerRoot.transform.position = new Vector3(1.8f, LowerSurfaceY, -10.5f);
            playerRoot.transform.rotation = Quaternion.Euler(0f, -22f, 0f);

            CharacterController characterController = playerRoot.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            playerRoot.AddComponent<CharacterMovementMotor>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "TEMP_VISUAL_ReplaceWithRealCharacter";
            body.transform.SetParent(playerRoot.transform);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            if (body.TryGetComponent(out Collider bodyCollider))
            {
                Object.DestroyImmediate(bodyCollider);
            }
            if (body.TryGetComponent(out Renderer bodyRenderer))
            {
                bodyRenderer.sharedMaterial = playerMaterial;
            }

            GameObject cameraPivot = new GameObject("TEMP_CameraPitchPivot_DoNotShip");
            cameraPivot.transform.SetParent(playerRoot.transform);
            cameraPivot.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            cameraPivot.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

            camera.transform.SetParent(cameraPivot.transform);
            camera.transform.localPosition = new Vector3(0.7f, 0.15f, -3.6f);
            camera.transform.localRotation = Quaternion.identity;

            TEMP_GhostOperatorPlaceholderController controller = playerRoot.AddComponent<TEMP_GhostOperatorPlaceholderController>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("cameraPitchPivot").objectReferenceValue = cameraPivot.transform;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            playerRoot.AddComponent<TEMP_GhostOperatorBuildInput>();
        }

        private static void SetHealthValues(Health health, float maxHitPoints, bool destroyOnDeath)
        {
            SerializedObject serializedHealth = new SerializedObject(health);
            serializedHealth.FindProperty("maxHitPoints").floatValue = maxHitPoints;
            serializedHealth.FindProperty("destroyOnDeath").boolValue = destroyOnDeath;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;

            if (cube.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = material;
            }

            return cube;
        }
    }
}
