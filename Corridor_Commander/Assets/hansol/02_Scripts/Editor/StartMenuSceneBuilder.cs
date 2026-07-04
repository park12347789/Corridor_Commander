using System.Collections.Generic;
using CorridorCommander.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class StartMenuSceneBuilder
    {
        private const string ScenePath = "Assets/hansol/01_Scenes/StartMenu.unity";
        private const string MainScenePath = "Assets/hansol/01_Scenes/MainScene.unity";
        private const string TutorialScenePath = "Assets/hansol/01_Scenes/TutorialMap.unity";
        private const string MainRootName = "StartMenu_MainRoot";
        private const string OptionsRootName = "StartMenu_OptionsRoot";
        private const string StageSelectRootName = "StartMenu_StageSelectRoot";

        [MenuItem("Corridor Commander/UI/Validate Start Menu Scene")]
        public static void Validate()
        {
            ValidateInternal(askBeforeOpeningScene: true);
        }

        [MenuItem("Corridor Commander/UI/Validate Start Menu Scene No Prompt")]
        public static void ValidateNoPrompt()
        {
            ValidateForAutomation();
        }

        public static void ValidateForAutomation()
        {
            ValidateInternal(askBeforeOpeningScene: false);
        }

        private static void ValidateInternal(bool askBeforeOpeningScene)
        {
            if (askBeforeOpeningScene && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> failures = new List<string>();

            if (!scene.IsValid())
            {
                failures.Add("Start menu scene could not be opened: " + ScenePath);
            }

            RequireComponent<Camera>("Main Camera", failures);
            RequireComponent<Light>("Directional Light", failures);
            RequireComponent<EventSystem>("EventSystem", failures);
            RequireComponent<BgmPlayer>("BgmSystem", failures);
            RequireComponent<Canvas>("StartMenuCanvas", failures);
            RequireComponent<StartMenuPresenter>("StartMenuController", failures);
            RequireComponent<StartMenuStageSelectPresenter>("StartMenuController", failures);
            RequireTransform(MainRootName, failures);
            RequireTransform(OptionsRootName, failures);
            RequireTransform(StageSelectRootName, failures);
            RequireTransform("StageSelectMouseCursorIcon", failures);

            StartMenuPresenter presenter = UnityEngine.Object.FindFirstObjectByType<StartMenuPresenter>(FindObjectsInactive.Include);
            if (presenter != null)
            {
                SerializedObject serializedObject = new SerializedObject(presenter);
                RequireObject(serializedObject, "mainRoot", failures);
                RequireObject(serializedObject, "optionsRoot", failures);
                RequireObject(serializedObject, "stageSelectPopup", failures);
                RequireObject(serializedObject, "startGameButton", failures);
                RequireObject(serializedObject, "tutorialButton", failures);
                RequireObject(serializedObject, "optionsButton", failures);
                RequireObject(serializedObject, "quitButton", failures);
                RequireObject(serializedObject, "closeOptionsButton", failures);
                RequireObject(serializedObject, "optionsController", failures);
                RequireObject(serializedObject, "masterVolumeSlider", failures);
                RequireObject(serializedObject, "mouseSensitivitySlider", failures);
                RequireObject(serializedObject, "fullscreenLockedButton", failures);
                RequireObject(serializedObject, "windowConfinedButton", failures);
                RequireObject(serializedObject, "windowFreeButton", failures);
            }

            StartMenuStageSelectPresenter stageSelectPresenter = UnityEngine.Object.FindFirstObjectByType<StartMenuStageSelectPresenter>(FindObjectsInactive.Include);
            if (stageSelectPresenter != null)
            {
                SerializedObject serializedObject = new SerializedObject(stageSelectPresenter);
                RequireObject(serializedObject, "mouseIconPresenter", failures);
            }

            if (!BuildSettingsContains(ScenePath))
            {
                failures.Add("Build Settings is missing StartMenu scene.");
            }

            if (!BuildSettingsContains(MainScenePath))
            {
                failures.Add("Build Settings is missing MainScene.");
            }

            if (!BuildSettingsContains(TutorialScenePath))
            {
                failures.Add("Build Settings is missing TutorialMap scene.");
            }

            int missingScriptCount = CountMissingScripts();
            if (missingScriptCount > 0)
            {
                failures.Add("Missing script components found: " + missingScriptCount + ".");
            }

            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("Start menu validation failed: " + string.Join(" | ", failures));
            }

            Debug.Log("Start menu validation passed. Scene=" + ScenePath);
        }

        private static void RequireComponent<T>(string objectName, List<string> failures) where T : Component
        {
            T[] components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component != null && component.gameObject.name == objectName)
                {
                    return;
                }
            }

            failures.Add(objectName + " is missing " + typeof(T).Name + ".");
        }

        private static void RequireTransform(string name, List<string> failures)
        {
            if (FindTransformByName(name) == null)
            {
                failures.Add(name + " is missing.");
            }
        }

        private static void RequireObject(SerializedObject serializedObject, string propertyName, List<string> failures)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                failures.Add("StartMenuPresenter." + propertyName + " is not assigned.");
            }
        }

        private static Transform FindTransformByName(string name)
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static int CountMissingScripts()
        {
            int count = 0;
            GameObject[] objects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < objects.Length; i++)
            {
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(objects[i]);
            }

            return count;
        }

        private static bool BuildSettingsContains(string path)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == path && scenes[i].enabled)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
