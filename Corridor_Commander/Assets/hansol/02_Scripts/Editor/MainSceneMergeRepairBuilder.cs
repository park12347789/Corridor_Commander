using CorridorCommander.PlayerControl;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class MainSceneMergeRepairBuilder
    {
        private const string MainScenePath = "Assets/hansol/01_Scenes/MainScene.unity";

        [MenuItem("Corridor Commander/Repair/Validate MainScene Merge Wiring")]
        public static void Validate()
        {
            ValidateForAutomation();
        }

        public static void ValidateForAutomation()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new System.InvalidOperationException("MainScene could not be opened: " + MainScenePath);
            }

            RequireComponent<StageRuntime>(RequireGameObject("SceneSystems"));
            RequireObject(UnityEngine.Object.FindFirstObjectByType<RealtimeMapHudPresenter>(FindObjectsInactive.Include), "RealtimeMapHudPresenter");
            RequireObject(UnityEngine.Object.FindFirstObjectByType<WaveRewardController>(FindObjectsInactive.Include), "WaveRewardController");
            RequireObject(UnityEngine.Object.FindFirstObjectByType<RewardGrantService>(FindObjectsInactive.Include), "RewardGrantService");
            RequireObject(UnityEngine.Object.FindFirstObjectByType<ArtifactInventory>(FindObjectsInactive.Include), "ArtifactInventory");
            RequireObject(UnityEngine.Object.FindFirstObjectByType<ArtifactStatManager>(FindObjectsInactive.Include), "ArtifactStatManager");
            RequireObject(UnityEngine.Object.FindFirstObjectByType<WaveStartNotificationPresenter>(FindObjectsInactive.Include), "WaveStartNotificationPresenter");
            RequireObject(UnityEngine.Object.FindFirstObjectByType<PlayerGameOverBridge>(FindObjectsInactive.Include), "PlayerGameOverBridge");

            StageRuntime runtime = UnityEngine.Object.FindFirstObjectByType<StageRuntime>(FindObjectsInactive.Include);
            if (runtime.WaveRewardController == null
                || runtime.RewardGrantService == null
                || runtime.ArtifactInventory == null
                || runtime.ArtifactStatManager == null
                || runtime.WaveStartNotificationPresenter == null)
            {
                throw new System.InvalidOperationException("StageRuntime merge wiring is incomplete.");
            }

            Debug.Log("MainScene merge wiring validation passed. Scene=" + MainScenePath);
        }

        private static GameObject RequireGameObject(string name)
        {
            GameObject gameObject = GameObject.Find(name);
            if (gameObject == null)
            {
                throw new System.InvalidOperationException("GameObject missing in MainScene: " + name);
            }

            return gameObject;
        }

        private static T RequireComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                throw new System.InvalidOperationException(gameObject.name + " missing component: " + typeof(T).Name);
            }

            return component;
        }

        private static void RequireObject(UnityEngine.Object value, string label)
        {
            if (value == null)
            {
                throw new System.InvalidOperationException(label + " is missing.");
            }
        }
    }
}
