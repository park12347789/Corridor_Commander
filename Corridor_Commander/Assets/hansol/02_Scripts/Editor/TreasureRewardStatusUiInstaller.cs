using CorridorCommander;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CorridorCommander.EditorTools
{
    public static class TreasureRewardStatusUiInstaller
    {
        private const string RewardPrefabPath =
            "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/TreasureRewardMenuPresenter.prefab";
        private const string StatusRootName = "RewardStatusRoot";
        private const string InstallRequestPath = "Library/TreasureRewardStatusUiInstaller.request";

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedInstall()
        {
            if (!System.IO.File.Exists(InstallRequestPath))
            {
                return;
            }

            System.IO.File.Delete(InstallRequestPath);
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Corridor Commander/UI/Install Treasure Reward Status Feedback")]
        public static void Install()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(RewardPrefabPath);
            try
            {
                TreasureRewardMenuPresenter presenter =
                    prefabRoot.GetComponentInChildren<TreasureRewardMenuPresenter>(true);
                if (presenter == null)
                {
                    throw new System.InvalidOperationException(
                        "Treasure reward presenter is missing: " + RewardPrefabPath);
                }

                InGameUiChromeAssets assets = InGameUiChromeAssets.Load();
                Transform existing = presenter.transform.Find(StatusRootName);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }

                UnityEngine.UI.Image statusPanel = InGameUiChromeFactory.CreatePanel(
                    StatusRootName,
                    presenter.transform,
                    new Vector2(560f, 64f),
                    new Vector2(0.5f, 0.24f),
                    InGameUiChromeFunction.HudContent,
                    assets);
                statusPanel.raycastTarget = false;

                TMP_Text statusText = InGameUiChromeFactory.CreateTmpText(
                    "StatusText",
                    statusPanel.transform,
                    new RectTransformBounds(
                        new Vector2(0.05f, 0.08f),
                        new Vector2(0.95f, 0.92f),
                        Vector2.zero,
                        Vector2.zero),
                    "보상 획득",
                    22f,
                    TextAlignmentOptions.Center,
                    new Color(0.92f, 0.98f, 1f, 1f),
                    assets);
                statusText.raycastTarget = false;
                statusPanel.gameObject.SetActive(false);

                SerializedObject serializedPresenter = new SerializedObject(presenter);
                SetObject(serializedPresenter, "statusRoot", statusPanel.gameObject);
                SetObject(serializedPresenter, "statusTmpText", statusText);
                serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, RewardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateForAutomation();
            Debug.Log("[TreasureRewardStatusUiInstaller] Reward status feedback installed.");
        }

        [MenuItem("Corridor Commander/UI/Validate Treasure Reward Status Feedback")]
        public static void Validate()
        {
            ValidateForAutomation();
        }

        [MenuItem("Corridor Commander/UI/Smoke Treasure Reward Status Feedback")]
        public static void SmokeStatusFeedback()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new System.InvalidOperationException(
                    "Enter Play Mode before running the reward-status smoke check.");
            }

            TreasureRewardMenuPresenter presenter = Object.FindFirstObjectByType<TreasureRewardMenuPresenter>(
                FindObjectsInactive.Include);
            if (presenter == null)
            {
                throw new System.InvalidOperationException("Treasure reward presenter is missing in the active scene.");
            }

            presenter.ShowSelected(presenter, "Reward feedback smoke check");
            Debug.Log("Treasure reward status feedback runtime smoke requested.");
        }

        [MenuItem("Corridor Commander/UI/Smoke Treasure Reward Status Feedback", true)]
        private static bool CanSmokeStatusFeedback()
        {
            return EditorApplication.isPlaying;
        }

        public static void ValidateForAutomation()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(RewardPrefabPath);
            try
            {
                TreasureRewardMenuPresenter presenter =
                    prefabRoot.GetComponentInChildren<TreasureRewardMenuPresenter>(true);
                if (presenter == null)
                {
                    throw new System.InvalidOperationException("Treasure reward presenter is missing.");
                }

                SerializedObject serializedPresenter = new SerializedObject(presenter);
                GameObject statusRoot =
                    serializedPresenter.FindProperty("statusRoot").objectReferenceValue as GameObject;
                TMP_Text statusText =
                    serializedPresenter.FindProperty("statusTmpText").objectReferenceValue as TMP_Text;
                if (statusRoot == null || statusText == null || statusRoot.activeSelf)
                {
                    throw new System.InvalidOperationException(
                        "Reward status feedback must be wired and inactive by default.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            Debug.Log("Treasure reward status feedback validation passed.");
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
