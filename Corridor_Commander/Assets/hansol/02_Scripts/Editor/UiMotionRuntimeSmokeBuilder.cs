#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CorridorCommander.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.Editor
{
    public static class UiMotionRuntimeSmokeBuilder
    {
        private const string MenuPath = "Tools/Corridor Commander/UI/Run Complete DOTween UI Runtime Smoke";
        private const string RequestPath = "Library/UiMotionRuntimeSmoke.request";
        private const string ResultPath = "Temp/UiMotionRuntimeSmoke.result";
        private const string UiPrefabFolder = "Assets/hansol/03_Prefabs/UI";
        private const string OriginalScenePathKey = "CorridorCommander.UiMotionRuntimeSmoke.OriginalScenePath";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("UI motion runtime smoke must start from Edit Mode.");
            }

            if (File.Exists(ResultPath))
            {
                File.Delete(ResultPath);
            }

            Scene originalScene = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && originalScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Save the active scene before running the isolated UI motion smoke.");
            }

            SessionState.SetString(OriginalScenePathKey, originalScene.path ?? string.Empty);
            ClearConsoleForSmoke();
            Scene smokeScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            List<DotweenUiPanelTransition> transitions = new List<DotweenUiPanelTransition>();
            List<DotweenUiButtonFeedback> buttons = new List<DotweenUiButtonFeedback>();
            List<DotweenUiValueChangeFeedback> values = new List<DotweenUiValueChangeFeedback>();

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { UiPrefabFolder });
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null
                    || prefab.GetComponentsInChildren<DotweenUiPanelTransition>(true).Length == 0)
                {
                    continue;
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, smokeScene) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Failed to instantiate UI prefab: " + prefabPath);
                }

                instance.name = Path.GetFileNameWithoutExtension(prefabPath) + "_UiMotionSmoke";
                DestroySmokeOnlyComponents<PlayerRuntimeHudBinding>(instance);
                DestroySmokeOnlyComponents<PopupDimOverlayController>(instance);
                DisableOutOfScopeBehaviours(instance);
                transitions.AddRange(instance.GetComponentsInChildren<DotweenUiPanelTransition>(true));
                buttons.AddRange(instance.GetComponentsInChildren<DotweenUiButtonFeedback>(true));
                values.AddRange(instance.GetComponentsInChildren<DotweenUiValueChangeFeedback>(true));
            }

            GameObject driverObject = new GameObject("UiMotionRuntimeSmokeDriver");
            SceneManager.MoveGameObjectToScene(driverObject, smokeScene);
            UiMotionRuntimeSmokeDriver driver = driverObject.AddComponent<UiMotionRuntimeSmokeDriver>();
            driver.Configure(transitions.ToArray(), buttons.ToArray(), values.ToArray(), ResultPath);

            EditorSceneManager.MarkSceneDirty(smokeScene);
            Debug.Log(
                "[UiMotionRuntimeSmokeBuilder] Prepared runtime smoke. Transitions=" + transitions.Count
                + ", Buttons=" + buttons.Count
                + ", Values=" + values.Count + ".");
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall += CleanupLeakedSmokeScenes;

            if (!File.Exists(RequestPath))
            {
                return;
            }

            File.Delete(RequestPath);
            EditorApplication.delayCall += Run;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += CleanupLeakedSmokeScenes;
            }
        }

        private static void CleanupLeakedSmokeScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            string originalScenePath = SessionState.GetString(OriginalScenePathKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(originalScenePath))
            {
                SessionState.EraseString(OriginalScenePathKey);
                if (SceneManager.GetActiveScene().path != originalScenePath)
                {
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                }

                return;
            }

            UiMotionRuntimeSmokeDriver[] drivers = UnityEngine.Object.FindObjectsByType<UiMotionRuntimeSmokeDriver>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            HashSet<int> closedHandles = new HashSet<int>();
            for (int i = 0; i < drivers.Length; i++)
            {
                UiMotionRuntimeSmokeDriver driver = drivers[i];
                if (driver == null || !driver.gameObject.scene.IsValid())
                {
                    continue;
                }

                Scene scene = driver.gameObject.scene;
                if (closedHandles.Add(scene.handle))
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ClearConsoleForSmoke()
        {
            Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
            MethodInfo clearMethod = logEntriesType?.GetMethod(
                "Clear",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            clearMethod?.Invoke(null, null);
        }

        private static void DisableOutOfScopeBehaviours(GameObject root)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour is DotweenUiPanelTransition
                    || behaviour is DotweenUiButtonFeedback
                    || behaviour is DotweenUiValueChangeFeedback)
                {
                    continue;
                }

                behaviour.enabled = false;
            }
        }

        private static void DestroySmokeOnlyComponents<T>(GameObject root) where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(components[i]);
            }
        }
    }
}
#endif
