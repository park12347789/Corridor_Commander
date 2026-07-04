using CorridorCommander;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander.EditorTools
{
    public static class MissionClearSettlementUiBuilder
    {
        private const string MainCanvasPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvas.prefab";
        private const string SupportShopPresenterPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/SupportTruckShopPresenter.prefab";
        private const string PauseMenuPresenterPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PauseMenuPresenter.prefab";
        private const string PresenterName = "MissionClearSettlementPresenter";
        private const string RootName = "MissionClearSettlementRoot";
        private const string ShopFrameRootName = "Background_Common";
        private const string OptionsFrameRootName = "OptionsFrame";

        [MenuItem("Corridor Commander/UI/Build Mission Clear Settlement UI")]
        public static void BuildMainCanvasMissionClearSettlementUi()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MainCanvasPrefabPath);
            try
            {
                InGameUiChromeAssets assets = InGameUiChromeAssets.Load();
                BuildOrUpdate(prefabRoot.transform, assets);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, MainCanvasPrefabPath);
                Debug.Log("[MissionClearSettlementUiBuilder] MainCanvas mission clear settlement UI built.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
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

            SerializedObject serializedPresenter = new SerializedObject(presenter);
            SetObject(serializedPresenter, "screenRoot", rootObject);
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
                && behaviour is not CanvasGroup
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
