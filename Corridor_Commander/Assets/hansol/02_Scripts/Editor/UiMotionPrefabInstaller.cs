#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CorridorCommander.Editor
{
    public static class UiMotionPrefabInstaller
    {
        private const string MenuPath = "Tools/Corridor Commander/UI/Apply DOTween Portfolio Motion";
        private const string RequestPath = "Library/UiMotionPrefabInstaller.request";
        private const string PauseSmokeRequestPath = "Library/UiMotionPauseSmoke.request";
        private const string SupportTruckSmokeRequestPath = "Library/UiMotionSupportTruckSmoke.request";

        private readonly struct MotionTarget
        {
            public MotionTarget(string prefabPath, Type presenterType, string rootPropertyName, string transitionPropertyName)
            {
                PrefabPath = prefabPath;
                PresenterType = presenterType;
                RootPropertyName = rootPropertyName;
                TransitionPropertyName = transitionPropertyName;
            }

            public string PrefabPath { get; }
            public Type PresenterType { get; }
            public string RootPropertyName { get; }
            public string TransitionPropertyName { get; }
        }

        private static readonly MotionTarget[] Targets =
        {
            new MotionTarget(
                "Assets/hansol/03_Prefabs/UI/InGame/MainCanvas.prefab",
                typeof(PlacementBuildMenuPresenter),
                "panelRoot",
                "panelTransition"),
            new MotionTarget(
                "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/SupportTruckShopPresenter.prefab",
                typeof(SupportTruckShopPresenter),
                "panelRoot",
                "panelTransition"),
            new MotionTarget(
                "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/TreasureRewardMenuPresenter.prefab",
                typeof(TreasureRewardMenuPresenter),
                "panelRoot",
                "panelTransition"),
            new MotionTarget(
                "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PauseMenuPresenter.prefab",
                typeof(PauseMenuPresenter),
                "menuRoot",
                "menuTransition")
        };

        [MenuItem(MenuPath)]
        public static void ApplyPortfolioMotion()
        {
            int configuredCount = 0;
            for (int i = 0; i < Targets.Length; i++)
            {
                if (Configure(Targets[i]))
                {
                    configuredCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UiMotionPrefabInstaller] Configured {configuredCount}/{Targets.Length} UI motion prefabs.");
        }

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedApply()
        {
            if (!File.Exists(RequestPath))
            {
                return;
            }

            File.Delete(RequestPath);
            EditorApplication.delayCall += ApplyPortfolioMotion;
        }

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedPauseSmoke()
        {
            if (!File.Exists(PauseSmokeRequestPath))
            {
                return;
            }

            File.Delete(PauseSmokeRequestPath);
            EditorApplication.delayCall += OpenPauseForSmoke;
        }

        private static void OpenPauseForSmoke()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("[UiMotionPrefabInstaller] Pause smoke requires PlayMode.");
                return;
            }

            PauseMenuPresenter presenter = UnityEngine.Object.FindFirstObjectByType<PauseMenuPresenter>(
                FindObjectsInactive.Include);
            if (presenter == null)
            {
                Debug.LogError("[UiMotionPrefabInstaller] PauseMenuPresenter missing during smoke.");
                return;
            }

            presenter.OpenPause();
            Debug.Log("[UiMotionPrefabInstaller] Pause smoke opened through presenter runtime path.");
        }

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedSupportTruckSmoke()
        {
            if (!File.Exists(SupportTruckSmokeRequestPath))
            {
                return;
            }

            File.Delete(SupportTruckSmokeRequestPath);
            EditorApplication.delayCall += OpenSupportTruckForSmoke;
        }

        private static void OpenSupportTruckForSmoke()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("[UiMotionPrefabInstaller] Support-truck smoke requires PlayMode.");
                return;
            }

            PauseMenuPresenter pausePresenter = UnityEngine.Object.FindFirstObjectByType<PauseMenuPresenter>(
                FindObjectsInactive.Include);
            pausePresenter?.ClosePause();

            SupportTruckShopPresenter presenter = UnityEngine.Object.FindFirstObjectByType<SupportTruckShopPresenter>(
                FindObjectsInactive.Include);
            SupportTruckShopInteraction interaction = UnityEngine.Object.FindFirstObjectByType<SupportTruckShopInteraction>(
                FindObjectsInactive.Exclude);
            SupportTruckShop shop = interaction != null ? interaction.GetComponent<SupportTruckShop>() : null;
            GameObject player = GameObject.FindWithTag("Player");
            if (presenter == null || interaction == null || shop == null)
            {
                Debug.LogError("[UiMotionPrefabInstaller] Support-truck runtime references missing during smoke.");
                return;
            }

            presenter.Show(interaction, shop, player != null ? player.transform : null);
            Debug.Log("[UiMotionPrefabInstaller] Support-truck smoke opened through presenter runtime path.");
        }

        private static bool Configure(MotionTarget target)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(target.PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[UiMotionPrefabInstaller] Could not load prefab: {target.PrefabPath}");
                return false;
            }

            try
            {
                Component presenter = prefabRoot.GetComponentInChildren(target.PresenterType, true);
                if (presenter == null)
                {
                    Debug.LogError($"[UiMotionPrefabInstaller] Presenter missing in {target.PrefabPath}");
                    return false;
                }

                SerializedObject presenterObject = new SerializedObject(presenter);
                SerializedProperty rootProperty = presenterObject.FindProperty(target.RootPropertyName);
                SerializedProperty transitionProperty = presenterObject.FindProperty(target.TransitionPropertyName);
                GameObject activationRoot = rootProperty?.objectReferenceValue as GameObject;
                if (activationRoot == null || transitionProperty == null)
                {
                    Debug.LogError(
                        $"[UiMotionPrefabInstaller] Required serialized fields are missing in {target.PrefabPath}");
                    return false;
                }

                RectTransform motionRoot = ResolveMotionRoot(activationRoot, target.PresenterType);
                if (motionRoot == null)
                {
                    Debug.LogError($"[UiMotionPrefabInstaller] Panel root is not a RectTransform: {target.PrefabPath}");
                    return false;
                }

                CanvasGroup canvasGroup = activationRoot.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = activationRoot.AddComponent<CanvasGroup>();
                }

                DotweenUiPanelTransition transition = presenter.GetComponent<DotweenUiPanelTransition>();
                if (transition == null)
                {
                    transition = presenter.gameObject.AddComponent<DotweenUiPanelTransition>();
                }

                SerializedObject transitionObject = new SerializedObject(transition);
                transitionObject.FindProperty("activationRoot").objectReferenceValue = activationRoot;
                transitionObject.FindProperty("motionRoot").objectReferenceValue = motionRoot;
                transitionObject.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
                transitionObject.FindProperty("useFade").boolValue = true;
                bool isSupportShop = target.PresenterType == typeof(SupportTruckShopPresenter);
                bool isPause = target.PresenterType == typeof(PauseMenuPresenter);
                transitionObject.FindProperty("useScale").boolValue = !isSupportShop;
                transitionObject.FindProperty("useHorizontalOffset").boolValue = isSupportShop;
                transitionObject.FindProperty("useVerticalOffset").boolValue = !isSupportShop;
                transitionObject.FindProperty("playShowOnEnable").boolValue = false;
                transitionObject.FindProperty("manageCanvasInteraction").boolValue = true;
                transitionObject.FindProperty("hiddenScaleMultiplier").floatValue = isPause ? 0.97f : 0.965f;
                transitionObject.FindProperty("hiddenHorizontalOffset").floatValue = isSupportShop ? -24f : 0f;
                transitionObject.FindProperty("hiddenVerticalOffset").floatValue = isPause ? -12f : -18f;
                transitionObject.FindProperty("showDuration").floatValue = isSupportShop ? 0.20f : 0.18f;
                transitionObject.FindProperty("hideDuration").floatValue = isSupportShop ? 0.14f : 0.13f;
                transitionObject.ApplyModifiedPropertiesWithoutUndo();

                transitionProperty.objectReferenceValue = transition;
                presenterObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, target.PrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static RectTransform ResolveMotionRoot(GameObject activationRoot, Type presenterType)
        {
            string[] preferredNames = presenterType == typeof(PlacementBuildMenuPresenter)
                ? new[] { "PlacementBuildMenuPanel", "Background_Common" }
                : presenterType == typeof(SupportTruckShopPresenter)
                    ? new[] { "Background_Common", "SupportTruckShopNewPanel" }
                    : new[] { "Background_Common", "OptionsFrame" };

            for (int i = 0; i < preferredNames.Length; i++)
            {
                Transform found = FindChildRecursive(activationRoot.transform, preferredNames[i]);
                if (found is RectTransform foundRect)
                {
                    return foundRect;
                }
            }

            return activationRoot.GetComponent<RectTransform>();
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == targetName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, targetName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
#endif
