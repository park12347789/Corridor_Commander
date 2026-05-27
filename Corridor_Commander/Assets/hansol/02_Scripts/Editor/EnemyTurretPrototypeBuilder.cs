using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CorridorCommander;
using Unity.AI.Navigation;
using Unity.Behavior;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CorridorCommander.EditorTools
{
    public static class EnemyTurretPrototypeBuilder
    {
        private const string ScenePath = "Assets/hansol/01_Scenes/EnemyBackgroundTest.unity";
        private const string EnemyPrefabPath = "Assets/hansol/03_Prefabs/Enemy_Basic.prefab";
        private const string BarricadePrefabPath = "Assets/hansol/03_Prefabs/Barricade_Basic.prefab";
        private const string TurretPrefabPath = "Assets/hansol/03_Prefabs/Turret_Basic.prefab";
        private const string ProjectilePrefabPath = "Assets/hansol/03_Prefabs/Prototype_Bullet.prefab";
        private const string EnemyBehaviorPath = "Assets/hansol/09_Settings/Behavior/Enemy_Basic_Unity_Behavior.asset";
        private const string TurretBehaviorPath = "Assets/hansol/09_Settings/Behavior/Turret_Basic_Unity_Behavior.asset";

        [MenuItem("Corridor Commander/Build Enemy Turret Prototype")]
        public static void Build()
        {
            EnsureFolders();

            Material enemyMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_Enemy_Red.mat", new Color(0.95f, 0.12f, 0.08f));
            Material turretMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_Turret_Blue.mat", new Color(0.05f, 0.35f, 1f));
            Material barricadeMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_Barricade_Orange.mat", new Color(0.9f, 0.48f, 0.12f));
            Material playerMaterial = CreateMaterial("Assets/hansol/04_Materials/TEMP_Prototype_Player_Purple.mat", new Color(0.55f, 0.18f, 0.95f));
            Material placementMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_PlacementPoint_Green.mat", new Color(0.05f, 1f, 0.2f));
            Material spawnMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_SpawnPoint_Red.mat", new Color(1f, 0.15f, 0.15f));
            Material routeMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_RouteLine_Cyan.mat", new Color(0f, 0.9f, 1f));
            Material bulletMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_Bullet_White.mat", Color.white);
            Material floorMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_Floor_Green.mat", new Color(0.24f, 0.65f, 0.3f));
            Material wallMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_Wall_Gray.mat", new Color(0.55f, 0.55f, 0.6f));
            Material goalMaterial = CreateMaterial("Assets/hansol/04_Materials/Prototype_Goal_Yellow.mat", new Color(1f, 0.85f, 0.12f));

            BehaviorGraph enemyGraph = CreateBehaviorGraph(EnemyBehaviorPath);
            BehaviorGraph turretGraph = CreateBehaviorGraph(TurretBehaviorPath);

            Projectile projectilePrefab = CreateProjectilePrefab(bulletMaterial);
            GameObject enemyPrefab = CreateEnemyPrefab(enemyMaterial, enemyGraph);
            GameObject barricadePrefab = CreateBarricadePrefab(barricadeMaterial);
            GameObject turretPrefab = CreateTurretPrefab(turretMaterial, projectilePrefab, turretGraph);

            CreateScene(enemyPrefab, barricadePrefab, turretPrefab, floorMaterial, wallMaterial, goalMaterial, playerMaterial, placementMaterial, spawnMaterial, routeMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets/hansol/02_Scripts/Common");
            CreateFolder("Assets/hansol/02_Scripts/Construction");
            CreateFolder("Assets/hansol/02_Scripts/Combat");
            CreateFolder("Assets/hansol/02_Scripts/Enemies");
            CreateFolder("Assets/hansol/02_Scripts/Editor");
            CreateFolder("Assets/hansol/02_Scripts/Movement");
            CreateFolder("Assets/hansol/02_Scripts/Prototype");
            CreateFolder("Assets/hansol/03_Prefabs");
            CreateFolder("Assets/hansol/04_Materials");
            CreateFolder("Assets/hansol/09_Settings/Behavior");
        }

        private static void CreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                CreateFolder(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static BehaviorGraph CreateBehaviorGraph(string path)
        {
            Type authoringType = Type.GetType("Unity.Behavior.BehaviorAuthoringGraph, Unity.Behavior.Authoring");
            if (authoringType == null)
            {
                return CreateRuntimeBehaviorGraph(path);
            }

            UnityEngine.Object authoringAsset = AssetDatabase.LoadAssetAtPath(path, authoringType);
            if (authoringAsset == null)
            {
                authoringAsset = ScriptableObject.CreateInstance(authoringType);
                authoringAsset.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(authoringAsset, path);
            }

            MethodInfo buildMethod = authoringType.GetMethod("BuildRuntimeGraph", BindingFlags.Public | BindingFlags.Instance);
            BehaviorGraph graph = buildMethod?.Invoke(authoringAsset, new object[] { true }) as BehaviorGraph;
            if (graph == null)
            {
                graph = AssetDatabase.LoadAllAssetsAtPath(path).OfType<BehaviorGraph>().FirstOrDefault();
            }

            EditorUtility.SetDirty(authoringAsset);
            return graph != null ? graph : CreateRuntimeBehaviorGraph(path);
        }

        private static BehaviorGraph CreateRuntimeBehaviorGraph(string path)
        {
            BehaviorGraph graph = AssetDatabase.LoadAssetAtPath<BehaviorGraph>(path);
            if (graph != null)
            {
                return graph;
            }

            graph = ScriptableObject.CreateInstance<BehaviorGraph>();
            graph.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(graph, path);
            EditorUtility.SetDirty(graph);
            return graph;
        }

        private static Projectile CreateProjectilePrefab(Material bulletMaterial)
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Prototype_Bullet";
            projectile.transform.localScale = Vector3.one * 0.25f;

            if (projectile.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = bulletMaterial;
            }

            Collider collider = projectile.GetComponent<Collider>();
            collider.isTrigger = true;

            Rigidbody rigidbody = projectile.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            Projectile projectileComponent = projectile.AddComponent<Projectile>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, ProjectilePrefabPath);
            UnityEngine.Object.DestroyImmediate(projectile);
            return prefab.GetComponent<Projectile>();
        }

        private static GameObject CreateEnemyPrefab(Material enemyMaterial, BehaviorGraph behaviorGraph)
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            enemy.name = "Enemy_Basic";
            enemy.transform.localScale = new Vector3(0.9f, 1f, 0.9f);

            if (enemy.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = enemyMaterial;
            }

            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = 2.6f;
            agent.angularSpeed = 540f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.35f;

            enemy.AddComponent<NavMeshMovementMotor>();
            enemy.AddComponent<EnemyMovementController>();
            enemy.AddComponent<EnemyMeleeAttackController>();
            Health enemyHealth = enemy.AddComponent<Health>();
            SetHealthValues(enemyHealth, 120f, true);

            BehaviorGraphAgent behaviorAgent = enemy.AddComponent<BehaviorGraphAgent>();
            behaviorAgent.Graph = behaviorGraph;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
            UnityEngine.Object.DestroyImmediate(enemy);
            return prefab;
        }

        private static GameObject CreateBarricadePrefab(Material barricadeMaterial)
        {
            GameObject barricade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barricade.name = "Barricade_Basic";
            barricade.transform.localScale = new Vector3(3.6f, 1.1f, 0.7f);

            if (barricade.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = barricadeMaterial;
            }

            Health health = barricade.AddComponent<Health>();
            SetHealthValues(health, 40f, true);
            MapObstacle obstacle = barricade.AddComponent<MapObstacle>();
            SerializedObject serializedObstacle = new SerializedObject(obstacle);
            serializedObstacle.FindProperty("obstacleKind").enumValueIndex = (int)MapObstacleKind.Breakable;
            serializedObstacle.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(barricade, BarricadePrefabPath);
            UnityEngine.Object.DestroyImmediate(barricade);
            return prefab;
        }

        private static GameObject CreateTurretPrefab(Material turretMaterial, Projectile projectilePrefab, BehaviorGraph behaviorGraph)
        {
            GameObject turret = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            turret.name = "Turret_Basic";
            turret.transform.localScale = new Vector3(1.1f, 0.8f, 1.1f);

            if (turret.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = turretMaterial;
            }

            GameObject muzzle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzle.name = "Muzzle";
            muzzle.transform.SetParent(turret.transform);
            muzzle.transform.localPosition = new Vector3(0f, 0.65f, 0.75f);
            muzzle.transform.localScale = Vector3.one * 0.2f;
            if (muzzle.TryGetComponent(out Collider muzzleCollider))
            {
                UnityEngine.Object.DestroyImmediate(muzzleCollider);
            }

            TurretTargetingController turretController = turret.AddComponent<TurretTargetingController>();
            SerializedObject serializedTurret = new SerializedObject(turretController);
            serializedTurret.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
            serializedTurret.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            serializedTurret.FindProperty("damage").floatValue = 6f;
            serializedTurret.ApplyModifiedPropertiesWithoutUndo();

            Health health = turret.AddComponent<Health>();
            SetHealthValues(health, 30f, true);

            BehaviorGraphAgent behaviorAgent = turret.AddComponent<BehaviorGraphAgent>();
            behaviorAgent.Graph = behaviorGraph;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(turret, TurretPrefabPath);
            UnityEngine.Object.DestroyImmediate(turret);
            return prefab;
        }

        private static void SetHealthValues(Health health, float maxHitPoints, bool destroyOnDeath)
        {
            SerializedObject serializedHealth = new SerializedObject(health);
            serializedHealth.FindProperty("maxHitPoints").floatValue = maxHitPoints;
            serializedHealth.FindProperty("destroyOnDeath").boolValue = destroyOnDeath;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateScene(
            GameObject enemyPrefab,
            GameObject barricadePrefab,
            GameObject turretPrefab,
            Material floorMaterial,
            Material wallMaterial,
            Material goalMaterial,
            Material playerMaterial,
            Material placementMaterial,
            Material spawnMaterial,
            Material routeMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "EnemyBackgroundTest";

            GameObject camera = new GameObject("Main Camera");
            camera.tag = "MainCamera";
            Camera cameraComponent = camera.AddComponent<Camera>();
            cameraComponent.fieldOfView = 62f;

            GameObject light = new GameObject("Directional Light");
            Light lightComponent = light.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject gameManagerObject = new GameObject("GameManager");
            GameManager gameManager = gameManagerObject.AddComponent<GameManager>();

            GameObject environment = new GameObject("Environment");
            GameObject floor = CreateCube("Corridor_Floor", environment.transform, new Vector3(0f, -0.05f, 0f), new Vector3(14f, 0.1f, 30f), floorMaterial);
            floor.isStatic = true;
            CreateCube("Left_Wall", environment.transform, new Vector3(-7.1f, 1f, 0f), new Vector3(0.2f, 2f, 30f), wallMaterial).isStatic = true;
            CreateCube("Right_Wall", environment.transform, new Vector3(7.1f, 1f, 0f), new Vector3(0.2f, 2f, 30f), wallMaterial).isStatic = true;
            CreateCube("Center_Obstacle", environment.transform, new Vector3(0f, 0.5f, 0f), new Vector3(2.2f, 1f, 3.2f), wallMaterial).isStatic = true;

            GameObject goal = CreateCube("Enemy_Goal", null, new Vector3(0f, 0.25f, 12f), new Vector3(3f, 0.5f, 1f), goalMaterial);
            Collider goalCollider = goal.GetComponent<Collider>();
            goalCollider.isTrigger = false;
            Rigidbody goalRigidbody = goal.AddComponent<Rigidbody>();
            goalRigidbody.useGravity = false;
            goalRigidbody.isKinematic = true;
            Health goalHealth = goal.AddComponent<Health>();
            SetHealthValues(goalHealth, 60f, true);
            GameOverOnDeath gameOverOnDeath = goal.AddComponent<GameOverOnDeath>();
            SerializedObject serializedGameOver = new SerializedObject(gameOverOnDeath);
            serializedGameOver.FindProperty("gameManager").objectReferenceValue = gameManager;
            serializedGameOver.FindProperty("reason").stringValue = "Goal destroyed";
            serializedGameOver.ApplyModifiedPropertiesWithoutUndo();

            NavMeshSurface surface = environment.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();

            Transform enemySpawnAnchor = CreateEnemySpawner(enemyPrefab, goal.transform, spawnMaterial);
            CreateRouteLine(enemySpawnAnchor, goal.transform, routeMaterial);
            CreateTemporaryPlayer(cameraComponent, playerMaterial);

            CreatePlacementPoint("PlacementPoint_01_GREEN_TurretOrBarricade", new Vector3(-3.5f, 0.04f, -7.2f), turretPrefab, barricadePrefab, placementMaterial);
            CreatePlacementPoint("PlacementPoint_02_GREEN_TurretOrBarricade", new Vector3(0f, 0.04f, -5.2f), turretPrefab, barricadePrefab, placementMaterial);
            CreatePlacementPoint("PlacementPoint_03_GREEN_TurretOrBarricade", new Vector3(3.5f, 0.04f, -2.2f), turretPrefab, barricadePrefab, placementMaterial);
            CreatePlacementPoint("PlacementPoint_04_GREEN_TurretOrBarricade", new Vector3(0f, 0.04f, 3.2f), turretPrefab, barricadePrefab, placementMaterial);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Transform CreateEnemySpawner(GameObject enemyPrefab, Transform goal, Material spawnMaterial)
        {
            GameObject spawnPoint = CreateCube("Enemy_SpawnPoint_RED", null, new Vector3(0f, 0.15f, -13f), new Vector3(1.4f, 0.3f, 1.4f), spawnMaterial);
            Collider spawnCollider = spawnPoint.GetComponent<Collider>();
            spawnCollider.isTrigger = true;

            GameObject spawnAnchor = new GameObject("EnemySpawnAnchor");
            spawnAnchor.transform.SetParent(spawnPoint.transform);
            spawnAnchor.transform.position = new Vector3(0f, 0.55f, -13f);
            spawnAnchor.transform.rotation = Quaternion.identity;

            EnemySpawner spawner = spawnPoint.AddComponent<EnemySpawner>();
            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
            serializedSpawner.FindProperty("spawnPoint").objectReferenceValue = spawnAnchor.transform;
            serializedSpawner.FindProperty("goal").objectReferenceValue = goal;
            serializedSpawner.FindProperty("spawnCount").intValue = 5;
            serializedSpawner.FindProperty("spawnInterval").floatValue = 2f;
            serializedSpawner.FindProperty("initialDelay").floatValue = 0.5f;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            return spawnAnchor.transform;
        }

        private static void CreateRouteLine(Transform spawnPoint, Transform goal, Material routeMaterial)
        {
            GameObject routeLineObject = new GameObject("Enemy_RouteLine_CYAN_SpawnToGoal");
            LineRenderer lineRenderer = routeLineObject.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = routeMaterial;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.16f;
            lineRenderer.endWidth = 0.16f;
            lineRenderer.numCapVertices = 4;

            EnemyRouteLineVisualizer visualizer = routeLineObject.AddComponent<EnemyRouteLineVisualizer>();
            SerializedObject serializedVisualizer = new SerializedObject(visualizer);
            serializedVisualizer.FindProperty("startPoint").objectReferenceValue = spawnPoint;
            serializedVisualizer.FindProperty("goalPoint").objectReferenceValue = goal;
            serializedVisualizer.FindProperty("heightOffset").floatValue = 0.08f;
            serializedVisualizer.ApplyModifiedPropertiesWithoutUndo();
            visualizer.Refresh();
        }

        private static void CreateTemporaryPlayer(Camera cameraComponent, Material playerMaterial)
        {
            GameObject playerRoot = new GameObject("TEMP_DO_NOT_FINALIZE_GhostOperator_PlayerRoot");
            playerRoot.transform.position = new Vector3(4.5f, 0f, -10.5f);
            playerRoot.transform.rotation = Quaternion.Euler(0f, -25f, 0f);

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
                UnityEngine.Object.DestroyImmediate(bodyCollider);
            }
            if (body.TryGetComponent(out Renderer bodyRenderer))
            {
                bodyRenderer.sharedMaterial = playerMaterial;
            }

            GameObject cameraPivot = new GameObject("TEMP_CameraPitchPivot_DoNotShip");
            cameraPivot.transform.SetParent(playerRoot.transform);
            cameraPivot.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            cameraPivot.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

            cameraComponent.transform.SetParent(cameraPivot.transform);
            cameraComponent.transform.localPosition = new Vector3(0.7f, 0.15f, -3.6f);
            cameraComponent.transform.localRotation = Quaternion.identity;

            TEMP_GhostOperatorPlaceholderController controller = playerRoot.AddComponent<TEMP_GhostOperatorPlaceholderController>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("cameraPitchPivot").objectReferenceValue = cameraPivot.transform;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            TEMP_GhostOperatorBuildInput buildInput = playerRoot.AddComponent<TEMP_GhostOperatorBuildInput>();
            SerializedObject serializedBuildInput = new SerializedObject(buildInput);
            serializedBuildInput.ApplyModifiedPropertiesWithoutUndo();
            CreateTemporaryBuildUi(buildInput);
        }

        private static void CreateTemporaryBuildUi(TEMP_GhostOperatorBuildInput buildInput)
        {
            GameObject canvasObject = new GameObject("TEMP_UI_BuildInteractionCanvas_DoNotShip");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject promptRoot = CreateUiPanel("TEMP_UI_InteractionPrompt_E", canvasObject.transform, new Vector2(0.5f, 0.16f), new Vector2(360f, 64f), new Color(0f, 0f, 0f, 0.72f));
            Text promptText = CreateUiText("PromptText", promptRoot.transform, font, 30, TextAnchor.MiddleCenter);
            promptText.text = "E  건설 메뉴";
            promptRoot.SetActive(false);

            GameObject panelRoot = CreateUiPanel("TEMP_UI_BuildChoicePanel_1_2_DoNotShip", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.82f));
            Text panelText = CreateUiText("BuildChoiceText", panelRoot.transform, font, 26, TextAnchor.MiddleCenter);
            panelText.text = "건설 선택\n\n[1] 포탑\n사거리 안의 적을 자동으로 공격\n\n[2] 바리케이드\n적 진로를 막고 체력으로 버팀\n\n[E] 닫기";
            panelRoot.SetActive(false);

            SerializedObject serializedBuildInput = new SerializedObject(buildInput);
            serializedBuildInput.FindProperty("interactionPromptRoot").objectReferenceValue = promptRoot;
            serializedBuildInput.FindProperty("interactionPromptText").objectReferenceValue = promptText;
            serializedBuildInput.FindProperty("buildPanelRoot").objectReferenceValue = panelRoot;
            serializedBuildInput.FindProperty("buildPanelText").objectReferenceValue = panelText;
            serializedBuildInput.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateUiPanel(string name, Transform parent, Vector2 anchor, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = size;

            Image image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Text CreateUiText(string name, Transform parent, Font font, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(18f, 14f);
            rectTransform.offsetMax = new Vector2(-18f, -14f);

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void CreatePlacementPoint(string name, Vector3 position, GameObject turretPrefab, GameObject barricadePrefab, Material placementMaterial)
        {
            GameObject point = CreateCube(name, null, position, new Vector3(1.8f, 0.08f, 1.8f), placementMaterial);
            Collider collider = point.GetComponent<Collider>();
            collider.isTrigger = true;

            GameObject anchor = new GameObject("BuildAnchor");
            anchor.transform.SetParent(point.transform);
            anchor.transform.position = new Vector3(position.x, 0.55f, position.z);
            anchor.transform.rotation = Quaternion.identity;

            PlacementPoint placementPoint = point.AddComponent<PlacementPoint>();
            SerializedObject serializedPoint = new SerializedObject(placementPoint);
            serializedPoint.FindProperty("buildAnchor").objectReferenceValue = anchor.transform;
            serializedPoint.FindProperty("turretPrefab").objectReferenceValue = turretPrefab;
            serializedPoint.FindProperty("barricadePrefab").objectReferenceValue = barricadePrefab;
            serializedPoint.FindProperty("indicatorRenderer").objectReferenceValue = point.GetComponent<Renderer>();
            serializedPoint.ApplyModifiedPropertiesWithoutUndo();
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
