using CorridorCommander;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CorridorCommander.EditorTools
{
    public static class MissionClearSettlementUiBuilder
    {
        private const string MainCanvasPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvas.prefab";
        private const string MainScenePath = "Assets/hansol/01_Scenes/MainScene.unity";
        private const string SupportShopPresenterPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/SupportTruckShopPresenter.prefab";
        private const string PauseMenuPresenterPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PauseMenuPresenter.prefab";
        private const string PresenterName = "MissionClearSettlementPresenter";
        private const string RootName = "MissionClearSettlementRoot";
        private const string ShopFrameRootName = "Background_Common";
        private const string OptionsFrameRootName = "OptionsFrame";
        private const string InstallRequestPath = "Library/MissionClearSettlementUiBuilder.request";

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedBuild()
        {
            if (!System.IO.File.Exists(InstallRequestPath))
            {
                return;
            }

            System.IO.File.Delete(InstallRequestPath);
            EditorApplication.delayCall += BuildMainCanvasMissionClearSettlementUi;
        }

        [MenuItem("Corridor Commander/UI/Build Mission Clear Settlement UI")]
        public static void BuildMainCanvasMissionClearSettlementUi()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MainCanvasPrefabPath);
            try
            {
                InGameUiChromeAssets assets = InGameUiChromeAssets.Load();
                BuildOrUpdate(prefabRoot.transform, assets);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, MainCanvasPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            WireMainScene();
            ValidateForAutomation();
            Debug.Log("[MissionClearSettlementUiBuilder] MainCanvas mission clear settlement UI built and wired.");
        }

        [MenuItem("Corridor Commander/UI/Validate Mission Clear Settlement UI")]
        public static void Validate()
        {
            ValidateForAutomation();
        }

        [MenuItem("Corridor Commander/UI/Smoke Mission Clear Settlement")]
        public static void SmokeMissionClearSettlement()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new System.InvalidOperationException(
                    "Enter Play Mode before running the mission-clear smoke check.");
            }

            MissionClearSettlementPresenter presenter = Object.FindFirstObjectByType<MissionClearSettlementPresenter>(
                FindObjectsInactive.Include);
            if (presenter == null)
            {
                throw new System.InvalidOperationException("Mission-clear presenter is missing in the active scene.");
            }

            presenter.ShowFinalSettlement();
            Debug.Log("Mission clear settlement runtime smoke requested.");
        }

        [MenuItem("Corridor Commander/UI/Smoke Mission Clear Settlement", true)]
        private static bool CanSmokeMissionClearSettlement()
        {
            return EditorApplication.isPlaying;
        }

        private static void BuildOrUpdate(Transform canvasTransform, InGameUiChromeAssets assets)
        {
            Transform presenterTransform = canvasTransform.Find(PresenterName);
            GameObject presenterObject = presenterTransform != null
                ? presenterTransform.gameObject
                : new GameObject(PresenterName, typeof(RectTransform));
            presenterObject.transform.SetParent(canvasTransform, false);
            presenterObject.SetActive(true);

            RectTransform presenterRect = presenterObject.GetComponent<RectTransform>();
            if (presenterRect == null)
            {
                presenterRect = presenterObject.AddComponent<RectTransform>();
            }

            InGameUiChromeFactory.ApplyBounds(presenterRect, InGameUiChromeFactory.Stretch());

            MissionClearSettlementPresenter presenter =
                presenterObject.GetComponent<MissionClearSettlementPresenter>()
                ?? presenterObject.AddComponent<MissionClearSettlementPresenter>();

            Transform existingRoot = presenterObject.transform.Find(RootName);
            GameObject rootObject;
            if (existingRoot != null)
            {
                rootObject = existingRoot.gameObject;
                InGameUiChromeFactory.ClearChildren(existingRoot);
            }
            else
            {
                rootObject = new GameObject(RootName, typeof(RectTransform), typeof(CanvasGroup));
                rootObject.transform.SetParent(presenterObject.transform, false);
            }

            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            InGameUiChromeFactory.ApplyBounds(rootRect, InGameUiChromeFactory.Stretch());
            CanvasGroup canvasGroup = rootObject.GetComponent<CanvasGroup>() ?? rootObject.AddComponent<CanvasGroup>();
            rootObject.SetActive(false);

            Transform panel = CreateSettlementFrame(rootObject.transform);

            InGameUiChromeFactory.CreatePanel(
                "HeaderFrame",
                panel,
                new Vector2(860f, 74f),
                new Vector2(0.5f, 0.86f),
                InGameUiChromeFunction.HudHeader,
                assets);

            TMP_Text titleText = InGameUiChromeFactory.CreateTmpText(
                "TitleText",
                panel,
                new RectTransformBounds(new Vector2(0.10f, 0.79f), new Vector2(0.90f, 0.93f), Vector2.zero, Vector2.zero),
                "MISSION CLEAR",
                40f,
                TextAlignmentOptions.Center,
                Color.white,
                assets);

            TMP_Text summaryText = InGameUiChromeFactory.CreateTmpText(
                "SummaryText",
                panel,
                new RectTransformBounds(new Vector2(0.16f, 0.70f), new Vector2(0.84f, 0.78f), Vector2.zero, Vector2.zero),
                "탈출 지점 복귀 완료",
                21f,
                TextAlignmentOptions.Center,
                new Color(0.78f, 0.92f, 1f, 1f),
                assets);

            TMP_Text moneyText = CreateRow(panel, assets, "MoneyRow", "보유 크레딧  0  (+0 / 시작 0)", 0.61f);
            TMP_Text spentText = CreateRow(panel, assets, "SpentRow", "사용 크레딧  0", 0.52f);
            TMP_Text levelText = CreateRow(panel, assets, "LevelRow", "전투 레벨  1", 0.43f);
            TMP_Text killText = CreateRow(panel, assets, "KillProgressRow", "전투 데이터  0/0", 0.34f);
            TMP_Text statText = CreateRow(panel, assets, "StatPointRow", "남은 강화 포인트  0", 0.25f);
            TMP_Text timeText = CreateRow(panel, assets, "MissionTimeRow", "임무 시간  00:00", 0.16f);

            Button lobbyButton = InGameUiChromeFactory.CreateButton(
                "LobbyButton",
                panel,
                new Vector2(230f, 58f),
                new Vector2(0.5f, 0.07f),
                "로비로",
                InGameUiChromeFunction.PrimaryButton,
                assets);

            DotweenUiPanelTransition transition = presenterObject.GetComponent<DotweenUiPanelTransition>()
                ?? presenterObject.AddComponent<DotweenUiPanelTransition>();
            ConfigureTransition(transition, rootObject, panel as RectTransform, canvasGroup);

            SerializedObject serializedPresenter = new SerializedObject(presenter);
            SetObject(serializedPresenter, "screenRoot", rootObject);
            SetObject(serializedPresenter, "screenTransition", transition);
            SetObject(serializedPresenter, "titleTmpText", titleText);
            SetObject(serializedPresenter, "summaryTmpText", summaryText);
            SetObject(serializedPresenter, "moneyTmpText", moneyText);
            SetObject(serializedPresenter, "spentTmpText", spentText);
            SetObject(serializedPresenter, "levelTmpText", levelText);
            SetObject(serializedPresenter, "killProgressTmpText", killText);
            SetObject(serializedPresenter, "statPointTmpText", statText);
            SetObject(serializedPresenter, "timeTmpText", timeText);
            SetObject(serializedPresenter, "lobbyButton", lobbyButton);
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTransition(
            DotweenUiPanelTransition transition,
            GameObject activationRoot,
            RectTransform motionRoot,
            CanvasGroup canvasGroup)
        {
            if (motionRoot == null)
            {
                throw new System.InvalidOperationException("Mission clear settlement motion root is missing.");
            }

            SerializedObject serializedTransition = new SerializedObject(transition);
            SetObject(serializedTransition, "activationRoot", activationRoot);
            SetObject(serializedTransition, "motionRoot", motionRoot);
            SetObject(serializedTransition, "canvasGroup", canvasGroup);
            serializedTransition.FindProperty("useFade").boolValue = true;
            serializedTransition.FindProperty("useScale").boolValue = true;
            serializedTransition.FindProperty("useHorizontalOffset").boolValue = false;
            serializedTransition.FindProperty("useVerticalOffset").boolValue = true;
            serializedTransition.FindProperty("playShowOnEnable").boolValue = false;
            serializedTransition.FindProperty("manageCanvasInteraction").boolValue = true;
            serializedTransition.FindProperty("hiddenScaleMultiplier").floatValue = 0.965f;
            serializedTransition.FindProperty("hiddenHorizontalOffset").floatValue = 0f;
            serializedTransition.FindProperty("hiddenVerticalOffset").floatValue = -18f;
            serializedTransition.FindProperty("showDuration").floatValue = 0.22f;
            serializedTransition.FindProperty("hideDuration").floatValue = 0.14f;
            serializedTransition.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireMainScene()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            MissionClearSettlementPresenter presenter = Object.FindFirstObjectByType<MissionClearSettlementPresenter>(
                FindObjectsInactive.Include);
            ExtractionObjectiveController objective = Object.FindFirstObjectByType<ExtractionObjectiveController>(
                FindObjectsInactive.Include);
            if (presenter == null || objective == null)
            {
                throw new System.InvalidOperationException(
                    "MainScene mission-clear presenter or extraction objective is missing.");
            }

            SerializedObject serializedObjective = new SerializedObject(objective);
            SetObject(serializedObjective, "missionClearSettlementPresenter", presenter);
            serializedObjective.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(objective);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void ValidateForAutomation()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            MissionClearSettlementPresenter presenter = Object.FindFirstObjectByType<MissionClearSettlementPresenter>(
                FindObjectsInactive.Include);
            ExtractionObjectiveController objective = Object.FindFirstObjectByType<ExtractionObjectiveController>(
                FindObjectsInactive.Include);
            if (presenter == null || objective == null)
            {
                throw new System.InvalidOperationException(
                    "MainScene mission-clear presenter or extraction objective is missing.");
            }

            SerializedObject serializedPresenter = new SerializedObject(presenter);
            GameObject screenRoot = serializedPresenter.FindProperty("screenRoot").objectReferenceValue as GameObject;
            if (screenRoot == null || screenRoot.activeSelf)
            {
                throw new System.InvalidOperationException(
                    "Mission clear screen root must exist and be inactive by default.");
            }

            RequireReference(serializedPresenter, "screenTransition");
            RequireReference(serializedPresenter, "titleTmpText");
            RequireReference(serializedPresenter, "summaryTmpText");
            RequireReference(serializedPresenter, "moneyTmpText");
            RequireReference(serializedPresenter, "spentTmpText");
            RequireReference(serializedPresenter, "levelTmpText");
            RequireReference(serializedPresenter, "killProgressTmpText");
            RequireReference(serializedPresenter, "statPointTmpText");
            RequireReference(serializedPresenter, "timeTmpText");
            RequireReference(serializedPresenter, "lobbyButton");

            SerializedObject serializedObjective = new SerializedObject(objective);
            if (serializedObjective.FindProperty("missionClearSettlementPresenter").objectReferenceValue != presenter)
            {
                throw new System.InvalidOperationException(
                    "Extraction objective is not wired to the mission-clear presenter.");
            }

            Debug.Log("Mission clear settlement UI validation passed. Scene=" + scene.path);
        }

        private static void RequireReference(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                throw new System.InvalidOperationException("Missing mission-clear reference: " + propertyName);
            }
        }

        private static TMP_Text CreateRow(Transform parent, InGameUiChromeAssets assets, string name, string value, float centerY)
        {
            Image row = InGameUiChromeFactory.CreatePanel(
                name,
                parent,
                new Vector2(780f, 42f),
                new Vector2(0.5f, centerY),
                InGameUiChromeFunction.HudContent,
                assets);
            row.raycastTarget = false;

            return InGameUiChromeFactory.CreateTmpText(
                "ValueText",
                row.transform,
                new RectTransformBounds(new Vector2(0.06f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero),
                value,
                20f,
                TextAlignmentOptions.MidlineLeft,
                new Color(0.92f, 0.98f, 1f, 1f),
                assets);
        }

        private static Transform CreateSettlementFrame(Transform parent)
        {
            GameObject frameObject = CreateFrameFromPrefab(
                SupportShopPresenterPrefabPath,
                ShopFrameRootName,
                parent);

            if (frameObject == null)
            {
                frameObject = CreateFrameFromPrefab(
                    PauseMenuPresenterPrefabPath,
                    OptionsFrameRootName,
                    parent);
            }

            if (frameObject == null)
            {
                throw new System.InvalidOperationException("Mission clear settlement frame source prefab is missing.");
            }

            frameObject.name = "SettlementFrame";
            RectTransform frameRect = frameObject.GetComponent<RectTransform>();
            if (frameRect == null)
            {
                frameRect = frameObject.AddComponent<RectTransform>();
            }

            InGameUiChromeFactory.SetRect(frameRect, new Vector2(980f, 560f), new Vector2(0.5f, 0.5f));
            DisableRuntimeBehaviours(frameObject);
            DisableGraphicRaycasts(frameObject);

            Image raycastBlocker = frameObject.GetComponent<Image>();
            if (raycastBlocker == null)
            {
                raycastBlocker = frameObject.AddComponent<Image>();
                raycastBlocker.color = new Color(0f, 0f, 0f, 0f);
            }

            raycastBlocker.raycastTarget = true;
            return frameObject.transform;
        }

        private static GameObject CreateFrameFromPrefab(string prefabPath, string childName, Transform parent)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform source = FindChildRecursive(prefabRoot.transform, childName);
                if (source == null)
                {
                    return null;
                }

                GameObject frame = Object.Instantiate(source.gameObject);
                frame.transform.SetParent(parent, false);
                frame.SetActive(true);
                return frame;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void DisableRuntimeBehaviours(GameObject root)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = behaviours.Length - 1; i >= 0; i--)
            {
                if (ShouldRemoveRuntimeBehaviour(behaviours[i]))
                {
                    Object.DestroyImmediate(behaviours[i]);
                }
            }

            Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
            for (int i = selectables.Length - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(selectables[i]);
            }
        }

        private static bool ShouldRemoveRuntimeBehaviour(MonoBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return false;
            }

            return behaviour is not Graphic
                && behaviour is not LayoutGroup
                && behaviour is not LayoutElement
                && behaviour is not ContentSizeFitter
                && behaviour is not Shadow
                && behaviour is not Mask
                && behaviour is not RectMask2D;
        }

        private static void DisableGraphicRaycasts(GameObject root)
        {
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new System.InvalidOperationException("Missing serialized field: " + propertyName);
            }

            property.objectReferenceValue = value;
        }
    }
}
