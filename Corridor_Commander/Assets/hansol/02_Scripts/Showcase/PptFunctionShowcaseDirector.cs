using System.Collections;
using System.Collections.Generic;
using System.IO;
using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerControl;
using CorridorCommander.PlayerUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class PptFunctionShowcaseDirector : MonoBehaviour
    {
        private const string PlayerPrefabPath = "Assets/hansol/03_Prefabs/Player/PlayerSetup.prefab";
        private const string MainCanvasPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvas.prefab";
        private const string PlacementPointPrefabPath = "Assets/hansol/03_Prefabs/PlacementPoint.prefab";
        private const string SupportTruckPrefabPath = "Assets/hansol/03_Prefabs/TEMP_SupportTruck_Shop.prefab";
        private const string TurretDefinitionPath = "Assets/hansol/09_Settings/Construction/Buildable_Turret.asset";
        private const string CaptureFolder = "_captures/ppt_function_showcase_20260623/auto_frames";

        [SerializeField] private Camera showcaseCamera;
        [SerializeField] private Vector3 cameraPosition = new Vector3(5.8f, 5.2f, -8.2f);
        [SerializeField] private Vector3 cameraLookTarget = new Vector3(0.4f, 1.15f, 1.2f);
        [SerializeField] private Vector2 captureSafeResolution = new Vector2(1280f, 720f);
        [SerializeField] private float playerMoveDuration = 2.2f;
        [SerializeField] private float turretFireDuration = 4.4f;
        [SerializeField] private float frameCaptureInterval = 0.42f;
        [SerializeField] private int maxFrameCaptures = 36;

        private GameObject playerRoot;
        private GameObject playerObject;
        private GameObject mainCanvas;
        private GameObject supportTruck;
        private PlacementPoint placementPoint;
        private PlacementPointInteraction placementInteraction;
        private PlacementBuildMenuPresenter buildMenuPresenter;
        private PlacementPreviewController previewController;
        private PlayerCommandRadialPresenter commandRadialPresenter;
        private PlayerCommandHotbarPresenter commandHotbarPresenter;
        private PlayerLocomotionController locomotionController;
        private PlayerFacingController facingController;
        private PlayerProjectileLauncher projectileLauncher;
        private PlayerWeaponRuntime weaponRuntime;
        private BuildableDefinitionSO turretDefinition;
        private GameObject placedTurret;
        private string captureOutputFolder;
        private int capturedFrameCount;
        private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

        private void Awake()
        {
            Application.targetFrameRate = 30;
            Application.runInBackground = true;
            Screen.SetResolution(Mathf.RoundToInt(captureSafeResolution.x), Mathf.RoundToInt(captureSafeResolution.y), false);
            EnsureBasics();
            LoadAssets();
            BuildShowcaseStage();
            BindPlayerSystems();
            BindUiSystems();
        }

        private void Start()
        {
            Debug.Log("[PptFunctionShowcaseDirector] Start showcase sequence.");
            StartCoroutine(RunShowcase());
        }

        private IEnumerator RunShowcase()
        {
            Coroutine recorder = StartCoroutine(RecordFrames());
            yield return null;
            yield return DemonstratePlayerAndUi();
            yield return DemonstratePlacementAndTurret();
            StopCoroutine(recorder);
            yield return CaptureFrame();
            Debug.Log($"[PptFunctionShowcaseDirector] Captured {capturedFrameCount} showcase frames to {captureOutputFolder}");
        }

        private void EnsureBasics()
        {
            if (FindFirstObjectByType<UiInputCoordinator>(FindObjectsInactive.Include) == null)
            {
                new GameObject("UiInputCoordinator").AddComponent<UiInputCoordinator>();
            }

            if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.AddComponent<EventSystem>();
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }

            EnsureArtifactServices();

            if (showcaseCamera == null)
            {
                GameObject cameraObject = new GameObject("PPT_ShowcaseCamera");
                showcaseCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            showcaseCamera.fieldOfView = 45f;
            showcaseCamera.nearClipPlane = 0.03f;
            showcaseCamera.farClipPlane = 100f;
            showcaseCamera.depth = 100f;
            DisableOtherCameras();
            Camera.SetupCurrent(showcaseCamera);

            Light existingLight = FindFirstObjectByType<Light>(FindObjectsInactive.Include);
            if (existingLight == null)
            {
                GameObject lightObject = new GameObject("PPT_ShowcaseKeyLight");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            }
        }

        private void LoadAssets()
        {
            turretDefinition = LoadAsset<BuildableDefinitionSO>(TurretDefinitionPath);
        }

        private void BuildShowcaseStage()
        {
            CreateFloor();
            playerRoot = InstantiatePrefab(PlayerPrefabPath, new Vector3(-2.2f, 0.2f, -0.2f), Quaternion.Euler(0f, 38f, 0f));
            playerObject = FindChildByName(playerRoot, "Player") ?? playerRoot;
            supportTruck = InstantiatePrefab(SupportTruckPrefabPath, new Vector3(-4.0f, 0.0f, 1.4f), Quaternion.Euler(0f, 90f, 0f));
            GameObject placementObject = InstantiatePrefab(PlacementPointPrefabPath, new Vector3(0.65f, 0.06f, 1.25f), Quaternion.identity);
            placementPoint = placementObject != null ? placementObject.GetComponent<PlacementPoint>() : null;
            placementInteraction = placementObject != null ? placementObject.GetComponent<PlacementPointInteraction>() : null;
            placementPoint?.ConfigureBuildableDefinitions(new[] { turretDefinition });

            mainCanvas = InstantiatePrefab(MainCanvasPrefabPath, Vector3.zero, Quaternion.identity);
            DisableNoisyMainCanvasSystems();
            PositionCamera(cameraPosition, cameraLookTarget);
        }

        private void BindPlayerSystems()
        {
            if (playerObject == null)
            {
                return;
            }

            locomotionController = playerObject.GetComponent<PlayerLocomotionController>();
            facingController = playerObject.GetComponent<PlayerFacingController>();
            projectileLauncher = playerRoot.GetComponentInChildren<PlayerProjectileLauncher>(true);
            weaponRuntime = playerRoot.GetComponentInChildren<PlayerWeaponRuntime>(true);
            Camera[] cameras = playerRoot.GetComponentsInChildren<Camera>(true);
            foreach (Camera camera in cameras)
            {
                if (camera != showcaseCamera)
                {
                    camera.enabled = false;
                }
            }

            DisableOtherCameras();
        }

        private void BindUiSystems()
        {
            buildMenuPresenter = FindFirstObjectByType<PlacementBuildMenuPresenter>(FindObjectsInactive.Include);
            previewController = FindFirstObjectByType<PlacementPreviewController>(FindObjectsInactive.Include);
            commandRadialPresenter = FindFirstObjectByType<PlayerCommandRadialPresenter>(FindObjectsInactive.Include);
            commandHotbarPresenter = FindFirstObjectByType<PlayerCommandHotbarPresenter>(FindObjectsInactive.Include);

            if (commandHotbarPresenter != null)
            {
                commandHotbarPresenter.Show(
                    "Q 1/3 - Weapons",
                    new[] { "Laser Gun", "Medkit", "Grenade", "", "", "", "", "", "" },
                    "PPT showcase: player runtime / UI hotbar",
                    slotIcons: null);
            }
        }

        private IEnumerator DemonstratePlayerAndUi()
        {
            PositionCamera(new Vector3(4.6f, 3.4f, -5.8f), new Vector3(-0.6f, 1.1f, 0.4f));
            commandRadialPresenter?.Hide();

            float elapsed = 0f;
            while (elapsed < playerMoveDuration)
            {
                elapsed += Time.deltaTime;
                locomotionController?.SetRunHeld(true);
                locomotionController?.SetMoveInput(new Vector2(0.35f, 1f));
                facingController?.SetAimHeld(elapsed > 0.8f);
                if (elapsed > 1.05f && elapsed < 1.8f)
                {
                    projectileLauncher?.SetFireHeld(true);
                    projectileLauncher?.RequestFirePressed();
                }

                yield return null;
            }

            locomotionController?.ClearMoveInput();
            projectileLauncher?.ClearFireInput();
            facingController?.SetAimHeld(false);

            commandRadialPresenter?.Show(PlayerCommandCategory.Weapons);
            yield return new WaitForSeconds(1.6f);
            commandRadialPresenter?.Show(PlayerCommandCategory.SquadCommands);
            yield return new WaitForSeconds(1.2f);
            commandRadialPresenter?.Hide();
        }

        private IEnumerator DemonstratePlacementAndTurret()
        {
            if (placementPoint == null || turretDefinition == null)
            {
                yield break;
            }

            PositionCamera(new Vector3(4.9f, 3.35f, -5.3f), new Vector3(0.7f, 0.75f, 1.25f));
            if (buildMenuPresenter != null && placementInteraction != null)
            {
                buildMenuPresenter.Show(placementInteraction);
                yield return new WaitForSeconds(1.8f);
                buildMenuPresenter.Hide(placementInteraction);
                yield return new WaitForSeconds(0.25f);
            }

            PositionCamera(new Vector3(3.9f, 2.85f, -3.25f), new Vector3(0.75f, 0.55f, 2.25f));
            if (previewController != null)
            {
                previewController.Begin(placementPoint, turretDefinition, playerObject);
                yield return new WaitForSeconds(1.5f);
                previewController.Cancel();
            }

            placedTurret = placementPoint.Build(turretDefinition, playerObject, Quaternion.Euler(0f, 180f, 0f));
            ConfigureInstalledTurret();
            yield return new WaitForSeconds(0.4f);

            PositionCamera(new Vector3(4.35f, 3.2f, -2.15f), new Vector3(1.4f, 0.75f, 3.65f));
            SpawnEnemyLine();
            yield return new WaitForSeconds(turretFireDuration);
        }

        private void ConfigureInstalledTurret()
        {
            if (placedTurret == null)
            {
                return;
            }

            TurretTargetingController targeting = placedTurret.GetComponentInChildren<TurretTargetingController>(true);
            targeting?.Configure(12f, 0.32f, 4f);
        }

        private void SpawnEnemyLine()
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 position = new Vector3(0.65f + i * 0.85f, 0.1f, 5.2f + i * 0.25f);
                GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemy.name = $"PPT_RuntimeEnemy_{i + 1:00}";
                enemy.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f));
                enemy.transform.localScale = new Vector3(0.62f, 0.92f, 0.62f);

                Renderer enemyRenderer = enemy.GetComponent<Renderer>();
                if (enemyRenderer != null)
                {
                    enemyRenderer.material.color = new Color(0.95f, 0.24f, 0.18f);
                }

                enemy.layer = 0;
                Health health = EnsureComponent<Health>(enemy);
                health.Configure(12f, true);
                EnemyMovementController movement = EnsureComponent<EnemyMovementController>(enemy);
                movement.SetUpdateLoopEnabled(false);
                spawnedEnemies.Add(enemy);
            }
        }

        private void DisableNoisyMainCanvasSystems()
        {
            if (mainCanvas == null)
            {
                return;
            }

            PlayerRuntimeHudBinding hudBinding = mainCanvas.GetComponent<PlayerRuntimeHudBinding>();
            if (hudBinding != null)
            {
                hudBinding.enabled = false;
            }

            WaveDirector waveDirector = mainCanvas.GetComponent<WaveDirector>();
            if (waveDirector != null)
            {
                waveDirector.enabled = false;
            }
        }

        private void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "PPT_ShowcaseFloor";
            floor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            floor.transform.localScale = new Vector3(10f, 0.12f, 9f);
            floor.GetComponent<Renderer>().material.color = new Color(0.15f, 0.32f, 0.36f);

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "PPT_ShowcaseBackWall";
            wall.transform.position = new Vector3(0f, 1.3f, 5.8f);
            wall.transform.localScale = new Vector3(10f, 2.6f, 0.18f);
            wall.GetComponent<Renderer>().material.color = new Color(0.18f, 0.2f, 0.24f);

            GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lane.name = "PPT_ShowcasePlacementLane";
            lane.transform.position = new Vector3(0.65f, 0.14f, 1.25f);
            lane.transform.localScale = new Vector3(1.65f, 0.04f, 1.65f);
            lane.GetComponent<Renderer>().material.color = new Color(0.0f, 0.88f, 1f, 0.85f);
        }

        private void PositionCamera(Vector3 position, Vector3 lookTarget)
        {
            if (showcaseCamera == null)
            {
                return;
            }

            showcaseCamera.transform.position = position;
            Vector3 direction = lookTarget - position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                showcaseCamera.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private IEnumerator RecordFrames()
        {
            PrepareCaptureDirectory();
            while (capturedFrameCount < maxFrameCaptures)
            {
                yield return CaptureFrame();
                yield return new WaitForSeconds(frameCaptureInterval);
            }
        }

        private IEnumerator CaptureFrame()
        {
            if (string.IsNullOrEmpty(captureOutputFolder))
            {
                PrepareCaptureDirectory();
            }

            yield return new WaitForEndOfFrame();
            string filePath = Path.Combine(captureOutputFolder, $"function_{capturedFrameCount:00}.png");
            ScreenCapture.CaptureScreenshot(filePath);
            capturedFrameCount++;
        }

        private void PrepareCaptureDirectory()
        {
            DirectoryInfo projectDirectory = Directory.GetParent(Application.dataPath);
            string projectRoot = projectDirectory != null ? projectDirectory.FullName : Application.dataPath;
            captureOutputFolder = Path.Combine(projectRoot, CaptureFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(captureOutputFolder);

            string[] staleFrames = Directory.GetFiles(captureOutputFolder, "function_*.png");
            foreach (string staleFrame in staleFrames)
            {
                File.Delete(staleFrame);
            }

            capturedFrameCount = 0;
        }

        private static void EnsureArtifactServices()
        {
            if (ArtifactStatManager.Current != null)
            {
                return;
            }

            GameObject servicesObject = new GameObject("PPT_ArtifactServices");
            ArtifactInventory inventory = servicesObject.AddComponent<ArtifactInventory>();
            ArtifactStatManager statManager = servicesObject.AddComponent<ArtifactStatManager>();
            statManager.Configure(inventory);
        }

        private void DisableOtherCameras()
        {
            AudioListener showcaseListener = EnsureComponent<AudioListener>(showcaseCamera.gameObject);
            showcaseListener.enabled = true;

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Camera camera in cameras)
            {
                if (camera == showcaseCamera)
                {
                    camera.enabled = true;
                    camera.tag = "MainCamera";
                    continue;
                }

                camera.enabled = false;
            }

            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (AudioListener listener in listeners)
            {
                if (listener != showcaseListener)
                {
                    listener.enabled = false;
                }
            }
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static GameObject FindChildByName(GameObject root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == targetName)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static GameObject InstantiatePrefab(string assetPath, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = LoadAsset<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogError("[PptFunctionShowcaseDirector] Missing asset: " + assetPath);
                return null;
            }

            GameObject instance = Instantiate(prefab, position, rotation);
            instance.name = prefab.name;
            return instance;
        }

        private static T LoadAsset<T>(string assetPath) where T : Object
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
            return null;
#endif
        }
    }
}
