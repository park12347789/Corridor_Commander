using System;
using System.Collections.Generic;
using System.IO;
using CorridorCommander;
using CorridorCommander.Tests;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class MapExpansionGateAnimationBuilder
    {
        private const string AnimationFolder = "Assets/hansol/09_Settings/Animation/World";
        private const string ClipPath = AnimationFolder + "/MapExpansionGate_DoorOpen.anim";
        private const string ControllerPath = AnimationFolder + "/MapExpansionGate_Door.controller";
        private const string RuntimeSmokeResultPath = "Temp/MapExpansionGateRuntimeSmoke.result";
        private const string OpenTriggerName = "Open";
        private const float OpenDuration = 1f;
        private const float SlideDistance = 0.72f;

        private static readonly string[] PrefabPaths =
        {
            "Assets/hansol/03_Prefabs/MapExpansionGate.prefab",
            "Assets/hansol/03_Prefabs/MapBuildSets/sector01.prefab",
            "Assets/hansol/03_Prefabs/MapBuildSets/sector02.prefab",
            "Assets/hansol/03_Prefabs/MapBuildSets/sector03.prefab",
            "Assets/hansol/03_Prefabs/MapBuildSets/Sector_11_RightFinalLane.prefab",
        };

        [MenuItem("Corridor Commander/World/Configure Map Expansion Gate Animation")]
        public static void Configure()
        {
            ConfigureForAutomation();
        }

        public static void ConfigureForAutomation()
        {
            EnsureFolder(AnimationFolder);

            AnimationClip openClip = BuildOpenClip();
            AnimatorController controller = BuildController(openClip);
            List<string> configuredPrefabs = new List<string>();
            List<string> failures = new List<string>();

            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                ConfigurePrefab(PrefabPaths[i], controller, configuredPrefabs, failures);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (failures.Count > 0)
            {
                throw new InvalidOperationException("Map expansion gate animation setup failed:\n" + string.Join("\n", failures));
            }

            Debug.Log("[MapExpansionGateAnimationBuilder] Configured gate animation:\n" + string.Join("\n", configuredPrefabs));
        }

        [MenuItem("Corridor Commander/World/Validate Map Expansion Gate Animation")]
        public static void Validate()
        {
            ValidateForAutomation();
        }

        public static void ValidateForAutomation()
        {
            List<string> failures = new List<string>();
            AnimatorController expectedController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (expectedController == null)
            {
                failures.Add("Missing controller: " + ControllerPath);
            }

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) == null)
            {
                failures.Add("Missing clip: " + ClipPath);
            }

            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                ValidatePrefab(PrefabPaths[i], expectedController, failures);
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException("Map expansion gate animation validation failed:\n" + string.Join("\n", failures));
            }

            Debug.Log("[MapExpansionGateAnimationBuilder] Gate animation validation passed.");
        }

        [MenuItem("Corridor Commander/World/Run Map Expansion Gate Runtime Smoke")]
        public static void RunRuntimeSmokeForAutomation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Map expansion gate runtime smoke must start from Edit Mode.");
            }

            if (File.Exists(RuntimeSmokeResultPath))
            {
                File.Delete(RuntimeSmokeResultPath);
            }

            NewSceneMode sceneMode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, sceneMode);
            List<MapExpansionDoorOpener> openers = new List<MapExpansionDoorOpener>();

            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[i]);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Missing prefab: " + PrefabPaths[i]);
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Failed to instantiate prefab: " + PrefabPaths[i]);
                }

                instance.name = Path.GetFileNameWithoutExtension(PrefabPaths[i]) + "_RuntimeSmoke";
                instance.transform.position = new Vector3(i * 8f, 0f, 0f);
                DisableOutOfScopeRuntimeComponents(instance);
                openers.AddRange(instance.GetComponentsInChildren<MapExpansionDoorOpener>(true));
            }

            if (openers.Count == 0)
            {
                throw new InvalidOperationException("No MapExpansionDoorOpener found in runtime smoke scene.");
            }

            GameObject driverObject = new GameObject("MapExpansionGateRuntimeSmokeDriver");
            MapExpansionGateRuntimeSmokeDriver driver = driverObject.AddComponent<MapExpansionGateRuntimeSmokeDriver>();
            driver.Configure(openers.ToArray(), RuntimeSmokeResultPath);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorApplication.EnterPlaymode();
        }

        private static void DisableOutOfScopeRuntimeComponents(GameObject root)
        {
            DestroyComponents<MapExpansionDoorInteraction>(root);
            DestroyComponents<PlacementPointInteraction>(root);
            DestroyComponents<TreasureChest>(root);
            DestroyComponents<SupportTruckShopInteraction>(root);
            DestroyComponents<SupportTruckShop>(root);
            DestroyComponents<EnemySpawner>(root);
            DestroyComponents<EnemyRouteLineVisualizer>(root);
        }

        private static void DestroyComponents<T>(GameObject root) where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(components[i]);
            }
        }

        private static AnimationClip BuildOpenClip()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = Path.GetFileNameWithoutExtension(ClipPath),
                    frameRate = 60f,
                };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            Undo.RecordObject(clip, "Configure Map Expansion Gate Open Clip");
            ClearClipCurves(clip);
            SetLocalPositionXCurve(clip, "DoorClosedRoot/Scifi_Door_Metal/Scifi_Door_Metal_L", 0f, SlideDistance);
            SetLocalPositionXCurve(clip, "DoorClosedRoot/Scifi_Door_Metal/Scifi_Door_Metal_R", 0f, -SlideDistance);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.stopTime = OpenDuration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController BuildController(AnimationClip openClip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            Undo.RecordObject(controller, "Configure Map Expansion Gate Door Controller");
            EnsureTrigger(controller, OpenTriggerName);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            ClearStateMachine(stateMachine);

            AnimatorState closedState = stateMachine.AddState("Closed");
            AnimatorState openState = stateMachine.AddState("Open");
            openState.motion = openClip;
            stateMachine.defaultState = closedState;

            AnimatorStateTransition transition = closedState.AddTransition(openState);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, OpenTriggerName);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigurePrefab(
            string prefabPath,
            AnimatorController controller,
            List<string> configuredPrefabs,
            List<string> failures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                failures.Add("Missing prefab: " + prefabPath);
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                MapExpansionDoorOpener[] openers = root.GetComponentsInChildren<MapExpansionDoorOpener>(true);
                int configuredCount = 0;
                for (int i = 0; i < openers.Length; i++)
                {
                    if (TryConfigureOpener(openers[i], controller, failures))
                    {
                        configuredCount++;
                    }
                }

                if (configuredCount > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    configuredPrefabs.Add(prefabPath + " openers=" + configuredCount);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidatePrefab(string prefabPath, AnimatorController expectedController, List<string> failures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                failures.Add("Missing prefab: " + prefabPath);
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                MapExpansionDoorOpener[] openers = root.GetComponentsInChildren<MapExpansionDoorOpener>(true);
                int validatedCount = 0;
                for (int i = 0; i < openers.Length; i++)
                {
                    if (ValidateOpener(prefabPath, openers[i], expectedController, failures))
                    {
                        validatedCount++;
                    }
                }

                if (validatedCount == 0)
                {
                    failures.Add(prefabPath + " has no configured MapExpansionGate opener.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool ValidateOpener(
            string prefabPath,
            MapExpansionDoorOpener opener,
            AnimatorController expectedController,
            List<string> failures)
        {
            if (opener == null)
            {
                return false;
            }

            Transform openedRoot = FindChildByName(opener.transform, "DoorOpenedVisual");
            Transform blocker = FindChildByName(opener.transform, "DoorClosedBlocker");
            Transform leftDoor = FindChildByName(opener.transform, "Scifi_Door_Metal_L");
            Transform rightDoor = FindChildByName(opener.transform, "Scifi_Door_Metal_R");
            if (openedRoot == null || blocker == null || leftDoor == null || rightDoor == null)
            {
                return false;
            }

            SerializedObject openerSo = new SerializedObject(opener);
            Animator animator = GetObject(openerSo, "doorAnimator") as Animator;
            if (animator == null)
            {
                failures.Add(prefabPath + "/" + opener.name + " doorAnimator is missing.");
            }
            else if (expectedController != null && animator.runtimeAnimatorController != expectedController)
            {
                failures.Add(prefabPath + "/" + opener.name + " has wrong AnimatorController.");
            }

            if (GetObject(openerSo, "passageBlocker") != blocker.gameObject)
            {
                failures.Add(prefabPath + "/" + opener.name + " passageBlocker is not DoorClosedBlocker.");
            }

            if (Mathf.Abs(GetFloat(openerSo, "openCompletionDelay") - OpenDuration) > 0.001f)
            {
                failures.Add(prefabPath + "/" + opener.name + " openCompletionDelay mismatch.");
            }

            if (!GetBool(openerSo, "keepOpenedDoorRootHiddenUntilOpenComplete"))
            {
                failures.Add(prefabPath + "/" + opener.name + " should hide opened root until complete.");
            }

            if (ActivationGroupsContain(opener.transform.root, openedRoot.gameObject))
            {
                failures.Add(prefabPath + "/" + opener.name + " activationTargets still include DoorOpenedVisual.");
            }

            return true;
        }

        private static bool TryConfigureOpener(
            MapExpansionDoorOpener opener,
            AnimatorController controller,
            List<string> failures)
        {
            if (opener == null)
            {
                return false;
            }

            Transform closedRoot = FindChildByName(opener.transform, "DoorClosedRoot");
            Transform openedRoot = FindChildByName(opener.transform, "DoorOpenedVisual");
            Transform blocker = FindChildByName(opener.transform, "DoorClosedBlocker");
            Transform leftDoor = FindChildByName(opener.transform, "Scifi_Door_Metal_L");
            Transform rightDoor = FindChildByName(opener.transform, "Scifi_Door_Metal_R");

            if (closedRoot == null || openedRoot == null || blocker == null || leftDoor == null || rightDoor == null)
            {
                return false;
            }

            string leftPath = AnimationUtility.CalculateTransformPath(leftDoor, opener.transform);
            string rightPath = AnimationUtility.CalculateTransformPath(rightDoor, opener.transform);
            if (leftPath != "DoorClosedRoot/Scifi_Door_Metal/Scifi_Door_Metal_L"
                || rightPath != "DoorClosedRoot/Scifi_Door_Metal/Scifi_Door_Metal_R")
            {
                failures.Add(opener.name + " has unsupported door animation paths: " + leftPath + ", " + rightPath);
                return false;
            }

            Animator animator = opener.GetComponent<Animator>();
            if (animator == null)
            {
                animator = opener.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            SerializedObject openerSo = new SerializedObject(opener);
            SetObject(openerSo, "closedDoorRoot", closedRoot.gameObject);
            SetObject(openerSo, "openedDoorRoot", openedRoot.gameObject);
            SetObject(openerSo, "doorAnimator", animator);
            SetString(openerSo, "openTriggerName", OpenTriggerName);
            SetObject(openerSo, "passageBlocker", blocker.gameObject);
            SetFloat(openerSo, "openCompletionDelay", OpenDuration);
            SetBool(openerSo, "keepOpenedDoorRootHiddenUntilOpenComplete", true);
            openerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(opener);

            RemoveOpenedVisualFromActivationGroups(opener.transform.root, openedRoot.gameObject);
            return true;
        }

        private static void RemoveOpenedVisualFromActivationGroups(Transform root, GameObject openedRoot)
        {
            MapExpansionActivationTargetGroup[] groups = root.GetComponentsInChildren<MapExpansionActivationTargetGroup>(true);
            for (int i = 0; i < groups.Length; i++)
            {
                SerializedObject groupSo = new SerializedObject(groups[i]);
                SerializedProperty targets = groupSo.FindProperty("activationTargets");
                if (targets == null || !targets.isArray)
                {
                    continue;
                }

                for (int targetIndex = targets.arraySize - 1; targetIndex >= 0; targetIndex--)
                {
                    if (targets.GetArrayElementAtIndex(targetIndex).objectReferenceValue == openedRoot)
                    {
                        targets.DeleteArrayElementAtIndex(targetIndex);
                    }
                }

                groupSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(groups[i]);
            }
        }

        private static bool ActivationGroupsContain(Transform root, GameObject openedRoot)
        {
            MapExpansionActivationTargetGroup[] groups = root.GetComponentsInChildren<MapExpansionActivationTargetGroup>(true);
            for (int i = 0; i < groups.Length; i++)
            {
                SerializedObject groupSo = new SerializedObject(groups[i]);
                SerializedProperty targets = groupSo.FindProperty("activationTargets");
                if (targets == null || !targets.isArray)
                {
                    continue;
                }

                for (int targetIndex = 0; targetIndex < targets.arraySize; targetIndex++)
                {
                    if (targets.GetArrayElementAtIndex(targetIndex).objectReferenceValue == openedRoot)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void ClearClipCurves(AnimationClip clip)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < bindings.Length; i++)
            {
                AnimationUtility.SetEditorCurve(clip, bindings[i], null);
            }
        }

        private static void SetLocalPositionXCurve(AnimationClip clip, string relativePath, float startValue, float endValue)
        {
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(relativePath, typeof(Transform), "m_LocalPosition.x");
            AnimationCurve curve = AnimationCurve.EaseInOut(0f, startValue, OpenDuration, endValue);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void ClearStateMachine(AnimatorStateMachine stateMachine)
        {
            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                stateMachine.RemoveState(states[i].state);
            }

            AnimatorStateTransition[] transitions = stateMachine.anyStateTransitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                stateMachine.RemoveAnyStateTransition(transitions[i]);
            }
        }

        private static void EnsureTrigger(AnimatorController controller, string parameterName)
        {
            for (int i = 0; i < controller.parameters.Length; i++)
            {
                if (controller.parameters[i].name == parameterName)
                {
                    return;
                }
            }

            controller.AddParameter(parameterName, AnimatorControllerParameterType.Trigger);
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                {
                    return children[i];
                }
            }

            return null;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void SetObject(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property missing: " + propertyName);
            }

            property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject so, string propertyName, string value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property missing: " + propertyName);
            }

            property.stringValue = value;
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property missing: " + propertyName);
            }

            property.floatValue = value;
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property missing: " + propertyName);
            }

            property.boolValue = value;
        }

        private static UnityEngine.Object GetObject(SerializedObject so, string propertyName)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue : null;
        }

        private static float GetFloat(SerializedObject so, string propertyName)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            return property != null ? property.floatValue : 0f;
        }

        private static bool GetBool(SerializedObject so, string propertyName)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            return property != null && property.boolValue;
        }
    }
}
