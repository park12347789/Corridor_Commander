#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using CorridorCommander.EditorTools;
using CorridorCommander.PlayerUI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CorridorCommander.Editor
{
    public static class UiMotionCoverageInstaller
    {
        private const string MenuPath = "Tools/Corridor Commander/UI/Apply Complete DOTween UI Coverage";
        private const string ValidateMenuPath = "Tools/Corridor Commander/UI/Validate Complete DOTween UI Coverage";
        private const string RequestPath = "Library/UiMotionCoverageInstaller.request";
        private const string ValidateRequestPath = "Library/UiMotionCoverageValidator.request";
        private const string UiPrefabFolder = "Assets/hansol/03_Prefabs/UI";
        private const string MainCanvasPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvas.prefab";
        private const string StartMenuCanvasPath = "Assets/hansol/03_Prefabs/UI/StartMenu/StartMenuCanvas.prefab";
        private const string TutorialScenePath = "Assets/hansol/01_Scenes/TutorialMap.unity";

        private readonly struct MotionProfile
        {
            public MotionProfile(
                bool fade,
                bool scale,
                bool horizontal,
                bool vertical,
                float hiddenScale,
                float horizontalOffset,
                float verticalOffset,
                float showDuration,
                float hideDuration,
                bool playOnEnable = false)
            {
                Fade = fade;
                Scale = scale;
                Horizontal = horizontal;
                Vertical = vertical;
                HiddenScale = hiddenScale;
                HorizontalOffset = horizontalOffset;
                VerticalOffset = verticalOffset;
                ShowDuration = showDuration;
                HideDuration = hideDuration;
                PlayOnEnable = playOnEnable;
            }

            public bool Fade { get; }
            public bool Scale { get; }
            public bool Horizontal { get; }
            public bool Vertical { get; }
            public float HiddenScale { get; }
            public float HorizontalOffset { get; }
            public float VerticalOffset { get; }
            public float ShowDuration { get; }
            public float HideDuration { get; }
            public bool PlayOnEnable { get; }
        }

        private readonly struct PrefabTransitionTarget
        {
            public PrefabTransitionTarget(
                string prefabPath,
                Type presenterType,
                string rootProperty,
                string transitionProperty,
                MotionProfile profile,
                string motionRootProperty = null)
            {
                PrefabPath = prefabPath;
                PresenterType = presenterType;
                RootProperty = rootProperty;
                TransitionProperty = transitionProperty;
                MotionRootProperty = motionRootProperty;
                Profile = profile;
            }

            public string PrefabPath { get; }
            public Type PresenterType { get; }
            public string RootProperty { get; }
            public string TransitionProperty { get; }
            public string MotionRootProperty { get; }
            public MotionProfile Profile { get; }
        }

        private readonly struct SceneTransitionTarget
        {
            public SceneTransitionTarget(Type presenterType, string rootProperty, MotionProfile profile)
            {
                PresenterType = presenterType;
                RootProperty = rootProperty;
                Profile = profile;
            }

            public Type PresenterType { get; }
            public string RootProperty { get; }
            public MotionProfile Profile { get; }
        }

        private static readonly MotionProfile CinematicModal =
            new MotionProfile(true, true, false, true, 0.94f, 0f, -18f, 0.22f, 0.14f);
        private static readonly MotionProfile CompactModal =
            new MotionProfile(true, true, false, true, 0.97f, 0f, -10f, 0.14f, 0.10f);
        private static readonly MotionProfile StandardModal =
            new MotionProfile(true, true, false, true, 0.965f, 0f, -16f, 0.18f, 0.13f);
        private static readonly MotionProfile RadialMicro =
            new MotionProfile(true, true, false, false, 0.90f, 0f, 0f, 0.08f, 0.06f);
        private static readonly MotionProfile AimInfoMicro =
            new MotionProfile(true, true, false, false, 0.98f, 0f, 0f, 0.10f, 0.07f);
        private static readonly MotionProfile StartMain =
            new MotionProfile(true, false, true, false, 1f, -16f, 0f, 0.20f, 0.10f, true);
        private static readonly MotionProfile StartOptions =
            new MotionProfile(true, false, true, false, 1f, 16f, 0f, 0.16f, 0.10f);
        private static readonly MotionProfile TutorialDialogue =
            new MotionProfile(true, false, false, true, 1f, 0f, -12f, 0.16f, 0.12f, true);

        private static readonly PrefabTransitionTarget[] PrefabTargets =
        {
            new PrefabTransitionTarget(
                "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PauseMenuPresenter.prefab",
                typeof(GameOverScreenPresenter),
                "screenRoot",
                "screenTransition",
                CinematicModal),
            new PrefabTransitionTarget(
                StartMenuCanvasPath,
                typeof(StartMenuPresenter),
                "mainRoot",
                "mainTransition",
                StartMain),
            new PrefabTransitionTarget(
                StartMenuCanvasPath,
                typeof(StartMenuPresenter),
                "optionsRoot",
                "optionsTransition",
                StartOptions),
            new PrefabTransitionTarget(
                StartMenuCanvasPath,
                typeof(StartMenuStageSelectPresenter),
                "panelRoot",
                "panelTransition",
                StandardModal),
            new PrefabTransitionTarget(
                "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PlayerCommandRadialPresenter.prefab",
                typeof(PlayerCommandRadialPresenter),
                "panelRoot",
                "panelTransition",
                RadialMicro),
            new PrefabTransitionTarget(
                "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PlayerItemRadialPresenter.prefab",
                typeof(PlayerItemRadialPresenter),
                "panelRoot",
                "panelTransition",
                RadialMicro),
            new PrefabTransitionTarget(
                "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/InstalledObjectActionPresenter.prefab",
                typeof(InstalledObjectActionPresenter),
                "panelRoot",
                "panelTransition",
                CompactModal,
                "popupFrameRoot"),
            new PrefabTransitionTarget(
                "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/InstalledObjectAimInfoPresenter.prefab",
                typeof(InstalledObjectAimInfoPresenter),
                "panelRoot",
                "panelTransition",
                AimInfoMicro),
            new PrefabTransitionTarget(
                MainCanvasPath,
                typeof(WaveReadyPopup),
                "root",
                "panelTransition",
                StandardModal),
            new PrefabTransitionTarget(
                "Assets/hansol/03_Prefabs/UI/InGame/WaveDirectorCanvas.prefab",
                typeof(WaveReadyPopup),
                "root",
                "panelTransition",
                StandardModal)
        };

        private static readonly SceneTransitionTarget[] TutorialSceneTargets =
        {
            new SceneTransitionTarget(typeof(TutorialDialoguePresenter), "panelRoot", TutorialDialogue),
            new SceneTransitionTarget(typeof(TutorialChapterSelectPresenter), "panelRoot", StandardModal),
            new SceneTransitionTarget(typeof(TutorialChapterCompletionPresenter), "panelRoot", CinematicModal)
        };

        [MenuItem(MenuPath)]
        public static void ApplyCompleteCoverage()
        {
            MissionClearSettlementUiBuilder.BuildMainCanvasMissionClearSettlementUi();
            TreasureRewardStatusUiInstaller.Install();
            UiMotionPrefabInstaller.ApplyPortfolioMotion();

            int transitionCount = 0;
            for (int i = 0; i < PrefabTargets.Length; i++)
            {
                ConfigurePrefabTransition(PrefabTargets[i]);
                transitionCount++;
            }

            transitionCount += ConfigureTutorialSceneTransitions();
            int buttonCount = 0;
            int valueCount = 0;
            ConfigureMicroFeedback(ref buttonCount, ref valueCount);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateForAutomation();
            Debug.Log(
                $"[UiMotionCoverageInstaller] Complete. Transitions={transitionCount}, "
                + $"button feedback={buttonCount}, value feedback={valueCount}.");
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            ValidateForAutomation();
        }

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedApply()
        {
            if (!File.Exists(RequestPath))
            {
                return;
            }

            File.Delete(RequestPath);
            EditorApplication.delayCall += ApplyCompleteCoverage;
        }

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedValidation()
        {
            if (!File.Exists(ValidateRequestPath))
            {
                return;
            }

            File.Delete(ValidateRequestPath);
            EditorApplication.delayCall += ValidateForAutomation;
        }

        public static void ValidateForAutomation()
        {
            MissionClearSettlementUiBuilder.ValidateForAutomation();
            TreasureRewardStatusUiInstaller.ValidateForAutomation();

            for (int i = 0; i < PrefabTargets.Length; i++)
            {
                ValidatePrefabTarget(PrefabTargets[i]);
            }

            ValidateTutorialSceneTransitions();

            int transitionCount = 0;
            int buttonCount = 0;
            int valueCount = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { UiPrefabFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ValidatePrefabFeedback(root, path, ref transitionCount, ref buttonCount, ref valueCount);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            if (transitionCount < PrefabTargets.Length)
            {
                throw new InvalidOperationException(
                    $"Complete UI motion coverage expected at least {PrefabTargets.Length} "
                    + $"direct prefab transitions, found {transitionCount}.");
            }

            if (buttonCount == 0 || valueCount == 0)
            {
                throw new InvalidOperationException("UI motion feedback coverage is empty.");
            }

            Debug.Log(
                $"Complete DOTween UI coverage validation passed. Transitions={transitionCount}, "
                + $"buttons={buttonCount}, values={valueCount}.");
        }

        private static void ConfigurePrefabTransition(PrefabTransitionTarget target)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(target.PrefabPath);
            try
            {
                Component presenter = root.GetComponentInChildren(target.PresenterType, true);
                if (presenter == null)
                {
                    throw new InvalidOperationException(
                        $"Presenter {target.PresenterType.Name} missing: {target.PrefabPath}");
                }

                SerializedObject presenterObject = new SerializedObject(presenter);
                GameObject activationRoot = ReadGameObject(presenterObject, target.RootProperty);
                RectTransform motionRoot = activationRoot != null
                    ? activationRoot.GetComponent<RectTransform>()
                    : null;
                if (!string.IsNullOrWhiteSpace(target.MotionRootProperty))
                {
                    GameObject configuredMotionRoot = ReadGameObject(presenterObject, target.MotionRootProperty);
                    motionRoot = configuredMotionRoot != null
                        ? configuredMotionRoot.GetComponent<RectTransform>()
                        : motionRoot;
                }

                DotweenUiPanelTransition transition = ConfigureTransition(
                    activationRoot,
                    motionRoot,
                    target.Profile,
                    true);
                SerializedProperty transitionProperty = presenterObject.FindProperty(target.TransitionProperty);
                if (transitionProperty == null)
                {
                    throw new InvalidOperationException(
                        $"Transition property {target.TransitionProperty} missing: {target.PrefabPath}");
                }

                transitionProperty.objectReferenceValue = transition;
                presenterObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, target.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int ConfigureTutorialSceneTransitions()
        {
            string originalScenePath = SceneManager.GetActiveScene().path;
            Scene scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
            int configured = 0;
            try
            {
                for (int i = 0; i < TutorialSceneTargets.Length; i++)
                {
                    SceneTransitionTarget target = TutorialSceneTargets[i];
                    Component presenter = FindSceneComponent(scene, target.PresenterType);
                    if (presenter == null)
                    {
                        throw new InvalidOperationException(
                            $"Tutorial scene presenter {target.PresenterType.Name} is missing.");
                    }

                    SerializedObject presenterObject = new SerializedObject(presenter);
                    GameObject activationRoot = ReadGameObject(presenterObject, target.RootProperty);
                    ConfigureTransition(
                        activationRoot,
                        activationRoot != null ? activationRoot.GetComponent<RectTransform>() : null,
                        target.Profile,
                        true);
                    configured++;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(originalScenePath)
                    && originalScenePath != TutorialScenePath)
                {
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                }
            }

            return configured;
        }

        private static void ValidateTutorialSceneTransitions()
        {
            string originalScenePath = SceneManager.GetActiveScene().path;
            Scene scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
            try
            {
                for (int i = 0; i < TutorialSceneTargets.Length; i++)
                {
                    SceneTransitionTarget target = TutorialSceneTargets[i];
                    Component presenter = FindSceneComponent(scene, target.PresenterType);
                    if (presenter == null)
                    {
                        throw new InvalidOperationException(
                            $"Tutorial scene presenter {target.PresenterType.Name} is missing.");
                    }

                    SerializedObject presenterObject = new SerializedObject(presenter);
                    GameObject activationRoot = ReadGameObject(presenterObject, target.RootProperty);
                    DotweenUiPanelTransition transition =
                        activationRoot.GetComponent<DotweenUiPanelTransition>();
                    if (transition == null)
                    {
                        throw new InvalidOperationException(
                            $"Tutorial scene transition missing: {target.PresenterType.Name}");
                    }

                    ValidateTransition(transition, TutorialScenePath);
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(originalScenePath)
                    && originalScenePath != TutorialScenePath)
                {
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                }
            }
        }

        private static DotweenUiPanelTransition ConfigureTransition(
            GameObject activationRoot,
            RectTransform motionRoot,
            MotionProfile profile,
            bool manageInteraction)
        {
            if (activationRoot == null || motionRoot == null)
            {
                throw new InvalidOperationException("UI motion activation root or motion root is missing.");
            }

            CanvasGroup canvasGroup = activationRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = activationRoot.AddComponent<CanvasGroup>();
            }

            if (!manageInteraction)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            DotweenUiPanelTransition transition = activationRoot.GetComponent<DotweenUiPanelTransition>();
            if (transition == null)
            {
                transition = activationRoot.AddComponent<DotweenUiPanelTransition>();
            }

            SerializedObject transitionObject = new SerializedObject(transition);
            SetObject(transitionObject, "activationRoot", activationRoot);
            SetObject(transitionObject, "motionRoot", motionRoot);
            SetObject(transitionObject, "canvasGroup", canvasGroup);
            transitionObject.FindProperty("useFade").boolValue = profile.Fade;
            transitionObject.FindProperty("useScale").boolValue = profile.Scale;
            transitionObject.FindProperty("useHorizontalOffset").boolValue = profile.Horizontal;
            transitionObject.FindProperty("useVerticalOffset").boolValue = profile.Vertical;
            transitionObject.FindProperty("playShowOnEnable").boolValue = profile.PlayOnEnable;
            transitionObject.FindProperty("manageCanvasInteraction").boolValue = manageInteraction;
            transitionObject.FindProperty("hiddenScaleMultiplier").floatValue = profile.HiddenScale;
            transitionObject.FindProperty("hiddenHorizontalOffset").floatValue = profile.HorizontalOffset;
            transitionObject.FindProperty("hiddenVerticalOffset").floatValue = profile.VerticalOffset;
            transitionObject.FindProperty("showDuration").floatValue = profile.ShowDuration;
            transitionObject.FindProperty("hideDuration").floatValue = profile.HideDuration;
            transitionObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(transition);
            return transition;
        }

        private static void ConfigureMicroFeedback(ref int buttonCount, ref int valueCount)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { UiPrefabFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                try
                {
                    Button[] buttons = root.GetComponentsInChildren<Button>(true);
                    for (int buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
                    {
                        Button button = buttons[buttonIndex];
                        if (IsNestedPrefabContent(button.gameObject, root))
                        {
                            continue;
                        }

                        if (button.GetComponent<DotweenUiButtonFeedback>() == null)
                        {
                            button.gameObject.AddComponent<DotweenUiButtonFeedback>();
                            changed = true;
                        }

                        buttonCount++;
                    }

                    TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                    for (int textIndex = 0; textIndex < texts.Length; textIndex++)
                    {
                        TMP_Text text = texts[textIndex];
                        if (IsNestedPrefabContent(text.gameObject, root) || !ShouldPulseValue(text.name))
                        {
                            continue;
                        }

                        DotweenUiValueChangeFeedback feedback =
                            text.GetComponent<DotweenUiValueChangeFeedback>();
                        if (feedback == null)
                        {
                            feedback = text.gameObject.AddComponent<DotweenUiValueChangeFeedback>();
                            changed = true;
                        }

                        SerializedObject feedbackObject = new SerializedObject(feedback);
                        feedbackObject.FindProperty("pulseScaleMultiplier").floatValue = 1.05f;
                        feedbackObject.FindProperty("pulseDuration").floatValue = 0.14f;
                        feedbackObject.ApplyModifiedPropertiesWithoutUndo();
                        valueCount++;
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void ValidatePrefabTarget(PrefabTransitionTarget target)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(target.PrefabPath);
            try
            {
                Component presenter = root.GetComponentInChildren(target.PresenterType, true);
                if (presenter == null)
                {
                    throw new InvalidOperationException(
                        $"Presenter {target.PresenterType.Name} missing: {target.PrefabPath}");
                }

                SerializedObject presenterObject = new SerializedObject(presenter);
                SerializedProperty transitionProperty = presenterObject.FindProperty(target.TransitionProperty);
                if (transitionProperty == null
                    || transitionProperty.objectReferenceValue is not DotweenUiPanelTransition transition)
                {
                    throw new InvalidOperationException(
                        $"Transition {target.TransitionProperty} is not wired: {target.PrefabPath}");
                }

                ValidateTransition(transition, target.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidatePrefabFeedback(
            GameObject root,
            string path,
            ref int transitionCount,
            ref int buttonCount,
            ref int valueCount)
        {
            DotweenUiPanelTransition[] transitions = root.GetComponentsInChildren<DotweenUiPanelTransition>(true);
            for (int i = 0; i < transitions.Length; i++)
            {
                if (IsNestedPrefabContent(transitions[i].gameObject, root))
                {
                    continue;
                }

                ValidateTransition(transitions[i], path);
                if (transitions[i].GetComponents<DotweenUiPanelTransition>().Length != 1)
                {
                    throw new InvalidOperationException("Duplicate panel transition: " + path);
                }

                transitionCount++;
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (IsNestedPrefabContent(buttons[i].gameObject, root))
                {
                    continue;
                }

                if (buttons[i].GetComponents<DotweenUiButtonFeedback>().Length != 1)
                {
                    throw new InvalidOperationException("Button DOTween feedback coverage mismatch: " + path);
                }

                buttonCount++;
            }

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (IsNestedPrefabContent(texts[i].gameObject, root) || !ShouldPulseValue(texts[i].name))
                {
                    continue;
                }

                if (texts[i].GetComponents<DotweenUiValueChangeFeedback>().Length != 1)
                {
                    throw new InvalidOperationException("Value DOTween feedback coverage mismatch: " + path);
                }

                valueCount++;
            }
        }

        private static void ValidateTransition(DotweenUiPanelTransition transition, string ownerPath)
        {
            SerializedObject transitionObject = new SerializedObject(transition);
            string[] referenceNames = { "activationRoot", "motionRoot", "canvasGroup" };
            for (int i = 0; i < referenceNames.Length; i++)
            {
                SerializedProperty property = transitionObject.FindProperty(referenceNames[i]);
                if (property == null || property.objectReferenceValue == null)
                {
                    throw new InvalidOperationException(
                        $"Transition reference {referenceNames[i]} missing: {ownerPath}");
                }
            }
        }

        private static Component FindSceneComponent(Scene scene, Type componentType)
        {
            UnityEngine.Object[] candidates = Resources.FindObjectsOfTypeAll(componentType);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is Component component && component.gameObject.scene == scene)
                {
                    return component;
                }
            }

            return null;
        }

        private static GameObject ReadGameObject(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Serialized UI root property is missing: " + propertyName);
            }

            if (property.objectReferenceValue is GameObject gameObject)
            {
                return gameObject;
            }

            if (property.objectReferenceValue is Component component)
            {
                return component.gameObject;
            }

            throw new InvalidOperationException("Serialized UI root reference is missing: " + propertyName);
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Serialized motion property is missing: " + propertyName);
            }

            property.objectReferenceValue = value;
        }

        private static bool IsNestedPrefabContent(GameObject target, GameObject loadedRoot)
        {
            GameObject nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(target);
            return nearestRoot != null && nearestRoot != loadedRoot;
        }

        private static bool ShouldPulseValue(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            string lowerName = objectName.ToLowerInvariant();
            return lowerName == "valuetext"
                || lowerName == "moneytext"
                || lowerName == "ammotext"
                || lowerName == "statpointtext"
                || lowerName == "counttext"
                || lowerName == "healthtext"
                || lowerName.EndsWith("leveltext", StringComparison.Ordinal)
                || lowerName.EndsWith("volumevalue", StringComparison.Ordinal)
                || lowerName == "mousesensitivityvalue";
        }
    }
}
#endif
