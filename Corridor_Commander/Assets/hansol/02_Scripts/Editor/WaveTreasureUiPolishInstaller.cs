using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CorridorCommander.EditorTools
{
    public static class WaveTreasureUiPolishInstaller
    {
        private const string RequestPath = "Library/WaveTreasureUiPolishInstaller.request";
        private const string RewardPrefabPath =
            "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/TreasureRewardMenuPresenter.prefab";
        private const string WaveReadyPrefabPath =
            "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/WaveReadyPopupRoot.prefab";
        private const string ArtifactPopupPrefabPath =
            "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PlayerStatsArtifactPopup.prefab";
        private const string MainCanvasPrefabPath =
            "Assets/hansol/03_Prefabs/UI/InGame/MainCanvas.prefab";
        private const string MainScenePath =
            "Assets/hansol/01_Scenes/MainScene.unity";
        private const string StageRuntimePrefabPath =
            "Assets/hansol/03_Prefabs/Stage/StageRuntime.prefab";
        private const string WaveIconPath =
            "Assets/hansol/04_Art/UI/Generated/wave_start_icon_imagegen.png";
        private const string PlayerIconPath =
            "Assets/hansol/07_UI/Icons/Icon_Artifact_PlayerExoFrame.png";
        private const string TurretIconPath =
            "Assets/hansol/07_UI/Icons/Icon_Artifact_TurretLens.png";
        private const string MortarIconPath =
            "Assets/hansol/07_UI/Icons/Icon_Artifact_MortarCore.png";
        private const string SquadIconPath =
            "Assets/hansol/07_UI/Icons/Icon_Artifact_SquadRelay.png";
        private const string ArtifactIconPath =
            "Assets/hansol/07_UI/Icons/Icon_ExperienceDataCore.png";

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedInstall()
        {
            if (!File.Exists(RequestPath))
            {
                return;
            }

            File.Delete(RequestPath);
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Corridor Commander/UI/Install Wave Treasure UI Polish")]
        public static void Install()
        {
            Sprite waveIcon = LoadSprite(WaveIconPath);
            InstallRewardLayout();
            InstallWaveReadyIcon(waveIcon);
            InstallArtifactPopup();
            InstallStageRuntimeOwnershipAndNotification(waveIcon);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[WaveTreasureUiPolishInstaller] Wave, treasure, and artifact UI polish installed.");
        }

        [MenuItem("Corridor Commander/UI/Validate Wave Treasure UI Polish")]
        public static void Validate()
        {
            ValidateRewardPrefab();
            ValidateWaveReadyPrefab();
            ValidateArtifactPopupPrefab();
            ValidateMainCanvas();
            ValidateMainScene();
            ValidateStageRuntime();
            Debug.Log("[WaveTreasureUiPolishInstaller] Validation passed.");
        }

        private static void InstallRewardLayout()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(RewardPrefabPath);
            try
            {
                TreasureRewardMenuPresenter presenter =
                    prefabRoot.GetComponentInChildren<TreasureRewardMenuPresenter>(true);
                Require(presenter != null, "Treasure reward presenter is missing.");

                SerializedObject serializedPresenter = new SerializedObject(presenter);
                GameObject panel = serializedPresenter.FindProperty("panelRoot").objectReferenceValue as GameObject;
                Require(panel != null, "Treasure reward panel is missing.");

                RectTransform panelRect = panel.GetComponent<RectTransform>();
                panelRect.sizeDelta = new Vector2(1120f, 760f);
                panelRect.anchoredPosition = new Vector2(0f, 20f);

                SerializedProperty icons = serializedPresenter.FindProperty("choiceIconImages");
                Require(icons != null && icons.arraySize == TreasureRewardMenuPresenter.MaxChoiceCount,
                    "Reward icon array is incomplete.");

                RectTransform[] cards = FindNamedRects(panel.transform, "RewardCard_");
                Require(cards.Length == TreasureRewardMenuPresenter.MaxChoiceCount,
                    "Reward card hierarchy is incomplete.");
                Array.Sort(cards, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));

                for (int i = 0; i < cards.Length; i++)
                {
                    cards[i].anchoredPosition = new Vector2((i - 1) * 350f, 120f);

                    Image icon = icons.GetArrayElementAtIndex(i).objectReferenceValue as Image;
                    Require(icon != null, "Reward icon is missing at index " + i + ".");
                    ConfigureRect(icon.rectTransform, new Vector2(0f, 48f), new Vector2(112f, 112f));
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    EnsureIconBackplate(icon.rectTransform, i);
                }

                GameObject descriptionRoot =
                    serializedPresenter.FindProperty("artifactDescriptionRoot").objectReferenceValue as GameObject;
                TMP_Text descriptionText =
                    serializedPresenter.FindProperty("artifactDescriptionTmpText").objectReferenceValue as TMP_Text;
                Require(descriptionRoot != null && descriptionText != null,
                    "Artifact description UI is incomplete.");
                ConfigureRect(
                    descriptionRoot.GetComponent<RectTransform>(),
                    new Vector2(0f, -230f),
                    new Vector2(920f, 92f));
                descriptionText.enableAutoSizing = true;
                descriptionText.fontSizeMin = 14f;
                descriptionText.fontSizeMax = 19f;
                descriptionText.overflowMode = TextOverflowModes.Ellipsis;

                Transform actionRow = FindChildRecursive(panel.transform, "RewardActionRow");
                Require(actionRow != null, "Reward action row is missing.");
                ConfigureRect(
                    actionRow as RectTransform,
                    new Vector2(0f, -330f),
                    new Vector2(720f, 84f));

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, RewardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void InstallWaveReadyIcon(Sprite waveIcon)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(WaveReadyPrefabPath);
            try
            {
                Transform messageTransform = FindChildRecursive(prefabRoot.transform, "MessageText");
                TMP_Text message = messageTransform != null ? messageTransform.GetComponent<TMP_Text>() : null;
                Require(message != null, "Wave ready popup message is missing.");

                EnsureWaveIconBadge(prefabRoot.transform, waveIcon, new Vector2(-198f, 74f), 72f, 50f);
                RectTransform messageRect = message.rectTransform;
                messageRect.anchoredPosition = new Vector2(30f, messageRect.anchoredPosition.y);
                messageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 370f);
                message.enableAutoSizing = true;
                message.fontSizeMin = 18f;
                message.fontSizeMax = 25f;
                message.overflowMode = TextOverflowModes.Ellipsis;

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, WaveReadyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void InstallArtifactPopup()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MainCanvasPrefabPath);
            try
            {
                PlayerStatsArtifactPopupPresenter presenter = RequireCanonicalArtifactPopup(
                    prefabRoot,
                    MainCanvasPrefabPath,
                    out _,
                    out _);

                SerializedObject serializedPresenter = new SerializedObject(presenter);
                SetObject(serializedPresenter, "playerIcon", LoadSprite(PlayerIconPath));
                SetObject(serializedPresenter, "turretIcon", LoadSprite(TurretIconPath));
                SetObject(serializedPresenter, "mortarIcon", LoadSprite(MortarIconPath));
                SetObject(serializedPresenter, "squadIcon", LoadSprite(SquadIconPath));
                SetObject(serializedPresenter, "artifactIcon", LoadSprite(ArtifactIconPath));
                serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, MainCanvasPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void InstallStageRuntimeOwnershipAndNotification(Sprite waveIcon)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(StageRuntimePrefabPath);
            try
            {
                StageRuntime runtime = prefabRoot.GetComponentInChildren<StageRuntime>(true);
                Require(runtime != null && runtime.WaveDirector != null, "StageRuntime wave owner is missing.");

                WaveDirector[] directors = prefabRoot.GetComponentsInChildren<WaveDirector>(true);
                for (int i = 0; i < directors.Length; i++)
                {
                    directors[i].enabled = directors[i] == runtime.WaveDirector;
                }

                WaveStartNotificationPresenter notification = runtime.WaveStartNotificationPresenter;
                Require(notification != null, "Wave start notification is missing.");
                SerializedObject serializedNotification = new SerializedObject(notification);
                GameObject notificationRoot =
                    serializedNotification.FindProperty("root").objectReferenceValue as GameObject;
                Require(notificationRoot != null, "Wave start notification root is missing.");
                RectTransform notificationRect = notificationRoot.GetComponent<RectTransform>();
                SetObject(serializedNotification, "motionRoot", notificationRect);
                serializedNotification.FindProperty("showDuration").floatValue = 0.18f;
                serializedNotification.FindProperty("hideDuration").floatValue = 0.2f;
                serializedNotification.FindProperty("hiddenVerticalOffset").floatValue = 18f;
                serializedNotification.ApplyModifiedPropertiesWithoutUndo();
                EnsureWaveIconBadge(notificationRoot.transform, waveIcon, new Vector2(-220f, 0f), 88f, 64f);

                Text legacyMessage = serializedNotification.FindProperty("messageText").objectReferenceValue as Text;
                if (legacyMessage != null)
                {
                    RectTransform messageRect = legacyMessage.rectTransform;
                    messageRect.offsetMin = new Vector2(104f, messageRect.offsetMin.y);
                    legacyMessage.resizeTextForBestFit = true;
                    legacyMessage.resizeTextMinSize = 20;
                    legacyMessage.resizeTextMaxSize = 34;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, StageRuntimePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void ValidateRewardPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RewardPrefabPath);
            try
            {
                TreasureRewardMenuPresenter presenter =
                    root.GetComponentInChildren<TreasureRewardMenuPresenter>(true);
                SerializedObject serializedPresenter = new SerializedObject(presenter);
                GameObject panel = serializedPresenter.FindProperty("panelRoot").objectReferenceValue as GameObject;
                Require(panel != null && panel.GetComponent<RectTransform>().sizeDelta.x >= 1100f,
                    "Reward panel layout was not expanded.");
                Require(panel.GetComponentsInChildren<Outline>(true).Length >= 3,
                    "Reward icon backplates are incomplete.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateWaveReadyPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(WaveReadyPrefabPath);
            try
            {
                Require(FindChildRecursive(root.transform, "WaveIcon") != null,
                    "Canonical wave ready icon is missing.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateArtifactPopupPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ArtifactPopupPrefabPath);
            try
            {
                Require(root.name == "PlayerStatsArtifactPopup",
                    "Artifact popup prefab root name is invalid.");

                RectTransform rect = root.GetComponent<RectTransform>();
                CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
                DotweenUiPanelTransition[] transitions =
                    root.GetComponentsInChildren<DotweenUiPanelTransition>(true);
                Require(rect != null, "Artifact popup RectTransform is missing.");
                Require(canvasGroup != null, "Artifact popup CanvasGroup is missing.");
                Require(transitions.Length == 1 && transitions[0].gameObject == root,
                    "Artifact popup must contain exactly one root DOTween transition.");
                RequireCanonicalTransition(root, rect, canvasGroup, transitions[0], ArtifactPopupPrefabPath);
                Require(FindChildRecursive(root.transform, "StatsContent_EditMe") != null,
                    "Artifact popup authored stats content is missing.");
                Require(FindChildRecursive(root.transform, "StatsContentLayout_V2") != null,
                    "Artifact popup authored layout marker is missing.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateMainCanvas()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MainCanvasPrefabPath);
            try
            {
                RequireCanonicalArtifactPopup(root, MainCanvasPrefabPath, out _, out _);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateMainScene()
        {
            Scene scene = SceneManager.GetSceneByPath(MainScenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
            {
                scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
            }

            try
            {
                GameObject mainCanvasInstance = FindMainCanvasInstance(scene);
                ValidateNoArtifactPopupOverrides(mainCanvasInstance);
                RequireCanonicalArtifactPopup(
                    mainCanvasInstance,
                    MainScenePath,
                    out GameObject panelRoot,
                    out DotweenUiPanelTransition panelTransition);
                Require(panelRoot.scene == scene && panelTransition.gameObject.scene == scene,
                    "MainScene artifact popup references must resolve to scene instances.");
            }
            finally
            {
                if (openedForValidation && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static PlayerStatsArtifactPopupPresenter RequireCanonicalArtifactPopup(
            GameObject ownerRoot,
            string ownerPath,
            out GameObject panelRoot,
            out DotweenUiPanelTransition panelTransition)
        {
            PlayerStatsArtifactPopupPresenter[] presenters =
                ownerRoot.GetComponentsInChildren<PlayerStatsArtifactPopupPresenter>(true);
            Require(presenters.Length == 1,
                ownerPath + " must contain exactly one artifact popup presenter.");

            PlayerStatsArtifactPopupPresenter presenter = presenters[0];
            SerializedObject serializedPresenter = new SerializedObject(presenter);
            panelRoot =
                serializedPresenter.FindProperty("panelRoot").objectReferenceValue as GameObject;
            panelTransition =
                serializedPresenter.FindProperty("panelTransition").objectReferenceValue
                as DotweenUiPanelTransition;

            Require(panelRoot != null && panelTransition != null,
                ownerPath + " artifact popup references are incomplete.");
            Require(panelRoot.transform.parent == presenter.transform,
                ownerPath + " artifact popup must be a direct child of its presenter.");
            Require(PrefabUtility.GetNearestPrefabInstanceRoot(panelRoot) == panelRoot,
                ownerPath + " artifact popup is not a canonical nested prefab root.");
            Require(string.Equals(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(panelRoot),
                    ArtifactPopupPrefabPath,
                    StringComparison.Ordinal),
                ownerPath + " artifact popup is not connected to " + ArtifactPopupPrefabPath + ".");

            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            Require(rect != null && canvasGroup != null,
                ownerPath + " artifact popup root components are incomplete.");
            Vector2 configuredSize =
                serializedPresenter.FindProperty("panelSize").vector2Value;
            Vector2 configuredPosition =
                serializedPresenter.FindProperty("anchoredPosition").vector2Value;
            Vector2 expectedSize = new Vector2(660f, 760f);
            Vector2 expectedPosition = new Vector2(-24f, -86f);
            Require((configuredSize - expectedSize).sqrMagnitude <= 0.01f
                    && (configuredPosition - expectedPosition).sqrMagnitude <= 0.01f
                    && (rect.sizeDelta - expectedSize).sqrMagnitude <= 0.01f
                    && (rect.anchoredPosition - expectedPosition).sqrMagnitude <= 0.01f,
                ownerPath + " artifact popup layout does not match the canonical 660x760 placement.");
            Require(panelRoot.GetComponentsInChildren<DotweenUiPanelTransition>(true).Length == 1,
                ownerPath + " artifact popup must contain exactly one DOTween transition.");
            RequireCanonicalTransition(
                panelRoot,
                rect,
                canvasGroup,
                panelTransition,
                ownerPath);
            return presenter;
        }

        private static void RequireCanonicalTransition(
            GameObject panelRoot,
            RectTransform rect,
            CanvasGroup canvasGroup,
            DotweenUiPanelTransition panelTransition,
            string ownerPath)
        {
            Require(panelTransition != null
                    && panelTransition.gameObject == panelRoot
                    && panelTransition.ActivationRoot == panelRoot
                    && panelTransition.MotionRoot == rect
                    && panelTransition.CanvasGroup == canvasGroup,
                ownerPath + " artifact popup DOTween references are not canonical.");
        }

        private static GameObject FindMainCanvasInstance(Scene scene)
        {
            List<GameObject> matches = new List<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] candidates = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                {
                    GameObject candidate = candidates[candidateIndex].gameObject;
                    if (!PrefabUtility.IsOutermostPrefabInstanceRoot(candidate))
                    {
                        continue;
                    }

                    string assetPath =
                        PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate);
                    if (string.Equals(assetPath, MainCanvasPrefabPath, StringComparison.Ordinal))
                    {
                        matches.Add(candidate);
                    }
                }
            }

            Require(matches.Count == 1,
                "MainScene must contain exactly one MainCanvas prefab instance.");
            return matches[0];
        }

        private static void ValidateNoArtifactPopupOverrides(GameObject mainCanvasInstance)
        {
            foreach (var added in PrefabUtility.GetAddedComponents(mainCanvasInstance))
            {
                Require(!IsArtifactPopupRelated(added.instanceComponent),
                    "MainScene contains an added artifact popup component override.");
            }

            foreach (var removed in PrefabUtility.GetRemovedComponents(mainCanvasInstance))
            {
                Require(!IsArtifactPopupRelated(removed.assetComponent),
                    "MainScene contains a removed artifact popup component override.");
            }

            foreach (var added in PrefabUtility.GetAddedGameObjects(mainCanvasInstance))
            {
                Require(!IsArtifactPopupRelated(added.instanceGameObject),
                    "MainScene contains an added artifact popup GameObject override.");
            }

            foreach (var removed in PrefabUtility.GetRemovedGameObjects(mainCanvasInstance))
            {
                Require(!IsArtifactPopupRelated(removed.assetGameObject),
                    "MainScene contains a removed artifact popup GameObject override.");
            }
        }

        private static bool IsArtifactPopupRelated(Component component)
        {
            return component != null
                && (component is PlayerStatsArtifactPopupPresenter
                    || IsArtifactPopupRelated(component.gameObject));
        }

        private static bool IsArtifactPopupRelated(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            Transform current = gameObject.transform;
            while (current != null)
            {
                if (current.name == "PlayerStatsArtifactPopup")
                {
                    return true;
                }

                string currentAssetPath = AssetDatabase.GetAssetPath(current.gameObject);
                if (string.Equals(
                    currentAssetPath,
                    ArtifactPopupPrefabPath,
                    StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            string assetPath = AssetDatabase.GetAssetPath(gameObject);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            }

            return string.Equals(assetPath, ArtifactPopupPrefabPath, StringComparison.Ordinal);
        }

        private static void ValidateStageRuntime()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(StageRuntimePrefabPath);
            try
            {
                StageRuntime runtime = root.GetComponentInChildren<StageRuntime>(true);
                WaveDirector[] directors = root.GetComponentsInChildren<WaveDirector>(true);
                int enabledCount = 0;
                for (int i = 0; i < directors.Length; i++)
                {
                    if (directors[i].enabled)
                    {
                        enabledCount++;
                    }
                }

                Require(runtime != null && runtime.WaveDirector != null && runtime.WaveDirector.enabled,
                    "StageRuntime wave owner is disabled.");
                Require(enabledCount == 1, "StageRuntime must contain exactly one enabled WaveDirector.");
                Require(FindChildRecursive(root.transform, "WaveIcon") != null,
                    "Wave start notification icon is missing.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureIconBackplate(RectTransform icon, int index)
        {
            Transform existing = icon.parent.Find("RewardIconBackplate");
            GameObject backplateObject = existing != null
                ? existing.gameObject
                : new GameObject("RewardIconBackplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            backplateObject.transform.SetParent(icon.parent, false);
            backplateObject.transform.SetSiblingIndex(icon.GetSiblingIndex());

            RectTransform rect = backplateObject.GetComponent<RectTransform>();
            ConfigureRect(rect, new Vector2(0f, 48f), new Vector2(132f, 132f));
            Image image = backplateObject.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.015f, 0.06f, 0.13f, 0.94f);
            image.raycastTarget = false;
            Outline outline = backplateObject.GetComponent<Outline>();
            Color[] colors =
            {
                new Color(0.12f, 0.86f, 1f, 0.72f),
                new Color(0.32f, 0.58f, 1f, 0.72f),
                new Color(0.68f, 0.44f, 1f, 0.72f)
            };
            outline.effectColor = colors[Mathf.Clamp(index, 0, colors.Length - 1)];
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }

        private static void EnsureWaveIconBadge(
            Transform parent,
            Sprite sprite,
            Vector2 position,
            float diskSize,
            float iconSize)
        {
            Transform existingBadge = parent.Find("WaveIconBadge");
            GameObject badgeObject = existingBadge != null
                ? existingBadge.gameObject
                : new GameObject("WaveIconBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            badgeObject.transform.SetParent(parent, false);
            ConfigureRect(badgeObject.GetComponent<RectTransform>(), position, Vector2.one * diskSize);

            Image disk = badgeObject.GetComponent<Image>();
            disk.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            disk.type = Image.Type.Sliced;
            disk.color = new Color(0.025f, 0.12f, 0.28f, 0.96f);
            disk.raycastTarget = false;
            Outline outline = badgeObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.16f, 0.88f, 1f, 0.82f);
            outline.effectDistance = new Vector2(2f, -2f);

            Transform existingIcon = badgeObject.transform.Find("WaveIcon");
            GameObject iconObject = existingIcon != null
                ? existingIcon.gameObject
                : new GameObject("WaveIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(badgeObject.transform, false);
            ConfigureRect(iconObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one * iconSize);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = sprite;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        private static RectTransform[] FindNamedRects(Transform root, string prefix)
        {
            RectTransform[] all = root.GetComponentsInChildren<RectTransform>(true);
            return Array.FindAll(all, rect => rect.name.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            Require(rect != null, "RectTransform is missing.");
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Require(sprite != null, "Sprite is missing: " + path);
            return sprite;
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Require(property != null, "Serialized field is missing: " + propertyName);
            property.objectReferenceValue = value;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
