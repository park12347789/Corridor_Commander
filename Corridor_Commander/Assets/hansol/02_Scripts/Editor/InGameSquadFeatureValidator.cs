using System;
using System.Collections.Generic;
using System.Reflection;
using CorridorCommander.PlayerControl;
using CorridorCommander.PlayerUI;
using UnityEditor;
using UnityEngine;

namespace CorridorCommander.EditorTools
{
    public static class InGameSquadFeatureValidator
    {
        private const string MainCanvasPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvas.prefab";
        private const string SquadListPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/SquadListRoot.prefab";
        private const string PlayerSetupPrefabPath = "Assets/hansol/03_Prefabs/Player/PlayerSetup.prefab";

        [MenuItem("Corridor Commander/UI/Validate Squad Feature")]
        public static void Validate()
        {
            ValidateForAutomation();
        }

        public static void ValidateForAutomation()
        {
            ValidateMainCanvasConnection();
            ValidateSquadListPrefab();
            ValidatePlayerSetupPrefabs();
            ValidateRosterCommandSmoke();
            Debug.Log("[InGameSquadFeatureValidator] Squad feature validation passed.");
        }

        private static void ValidateMainCanvasConnection()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainCanvasPrefabPath);
            Require(prefab != null, "MainCanvas prefab not found.");

            Transform squadListRoot = FindChildRecursive(prefab.transform, "SquadListRoot");
            Require(squadListRoot != null, "MainCanvas missing SquadListRoot.");
        }

        private static void ValidateSquadListPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SquadListPrefabPath);
            Require(prefab != null, "SquadListRoot prefab not found.");

            PlayerSquadListPresenter presenter = prefab.GetComponent<PlayerSquadListPresenter>();
            Require(presenter != null, "SquadListRoot missing PlayerSquadListPresenter.");

            PlayerSquadSlotView[] slots = prefab.GetComponentsInChildren<PlayerSquadSlotView>(true);
            Require(slots.Length == PlayerSquadRoster.MaxMemberCount, "SquadListRoot must have five PlayerSquadSlotView components.");

            Require(FindChildRecursive(prefab.transform, "Title") != null, "SquadListRoot missing Title.");
            for (int i = 1; i <= PlayerSquadRoster.MaxMemberCount; i++)
            {
                Require(FindChildRecursive(prefab.transform, "SquadSlot_F" + i) != null, "SquadListRoot missing SquadSlot_F" + i + ".");
            }

        }

        private static void ValidatePlayerSetupPrefabs()
        {
            ValidateSquadCommandSlots(PlayerSetupPrefabPath);
        }

        private static void ValidateSquadCommandSlots(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Require(prefab != null, "Player setup prefab not found: " + prefabPath);

            PlayerCommandPanelController panel = prefab.GetComponentInChildren<PlayerCommandPanelController>(true);
            Require(panel != null, prefabPath + " missing PlayerCommandPanelController.");

            SerializedObject serializedPanel = new SerializedObject(panel);
            SerializedProperty slots = serializedPanel.FindProperty("squadCommandSlots");
            Require(slots != null && slots.arraySize >= 4, prefabPath + " squadCommandSlots must contain SelectAll.");
            Require(slots.GetArrayElementAtIndex(3).FindPropertyRelative("commandType").enumValueIndex == (int)PlayerSquadCommandType.SelectAll,
                prefabPath + " squadCommandSlots[3] must be SelectAll.");

            for (int i = 0; i < 4; i++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);
                Require(!string.IsNullOrWhiteSpace(slot.FindPropertyRelative("displayName").stringValue),
                    prefabPath + " squadCommandSlots[" + i.ToString() + "] displayName is empty.");
                Require(slot.FindPropertyRelative("icon").objectReferenceValue != null,
                    prefabPath + " squadCommandSlots[" + i.ToString() + "] icon is missing.");
            }

            PlayerSquadCommandController commandController = prefab.GetComponentInChildren<PlayerSquadCommandController>(true);
            Require(commandController != null, prefabPath + " missing PlayerSquadCommandController.");

            PlayerSquadRoster roster = prefab.GetComponent<PlayerSquadRoster>();
            Require(roster != null, prefabPath + " missing PlayerSquadRoster on root.");

            PlayerSquadSelectionWorldPresenter worldPresenter = prefab.GetComponent<PlayerSquadSelectionWorldPresenter>();
            Require(worldPresenter != null, prefabPath + " missing PlayerSquadSelectionWorldPresenter on root.");

            SerializedObject serializedCommand = new SerializedObject(commandController);
            Require(serializedCommand.FindProperty("roster").objectReferenceValue == roster,
                prefabPath + " PlayerSquadCommandController.roster is not wired.");

            SerializedObject serializedWorldPresenter = new SerializedObject(worldPresenter);
            Require(serializedWorldPresenter.FindProperty("roster").objectReferenceValue == roster,
                prefabPath + " PlayerSquadSelectionWorldPresenter.roster is not wired.");
        }

        private static void ValidateRosterCommandSmoke()
        {
            PlayerSquadRoster previousInstance = PlayerSquadRoster.Instance;
            GameObject root = new GameObject("SquadFeatureSmokeRoot");
            List<GameObject> members = new List<GameObject>();

            try
            {
                SetRosterInstance(null);
                PlayerSquadRoster roster = root.AddComponent<PlayerSquadRoster>();
                PlayerSquadCommandController commandController = root.AddComponent<PlayerSquadCommandController>();

                InvokePrivate(roster, "Awake");
                for (int i = 0; i < PlayerSquadRoster.MaxMemberCount; i++)
                {
                    members.Add(CreateSquadMember("SquadFeatureSmokeMemberF" + (i + 1).ToString()));
                }

                InvokePrivate(roster, "OnEnable");

                Require(roster.MemberCount >= PlayerSquadRoster.MaxMemberCount, "Roster did not discover all smoke squad members.");
                Require(commandController.SelectMemberSlot(1, out _), "SelectMemberSlot(1) failed.");
                Require(commandController.TryIssueCommand(PlayerSquadCommandType.HoldPosition, "Hold", out _), "Hold command failed for selected member.");
                Require(commandController.SelectAdjacentMember(1, out _), "SelectAdjacentMember(1) failed.");
                for (int slotNumber = 1; slotNumber <= PlayerSquadRoster.MaxMemberCount; slotNumber++)
                {
                    Require(commandController.TryCallMemberSlot(slotNumber, out _), "TryCallMemberSlot(" + slotNumber + ") failed.");
                }

                Require(commandController.SelectAll(out _), "SelectAll failed.");

                List<AlliedSquadMemberFollower> targets = new List<AlliedSquadMemberFollower>();
                roster.FillCommandTargets(targets);
                Require(targets.Count == roster.MemberCount, "SelectAll target count mismatch.");
                Require(commandController.TryIssueCommand(PlayerSquadCommandType.ReturnToPlayer, "Return", out _), "Return command failed for selected squad.");
            }
            finally
            {
                for (int i = 0; i < members.Count; i++)
                {
                    if (members[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(members[i]);
                    }
                }

                UnityEngine.Object.DestroyImmediate(root);
                SetRosterInstance(previousInstance);
            }
        }

        private static GameObject CreateSquadMember(string name)
        {
            GameObject gameObject = new GameObject(name);
            Health health = gameObject.AddComponent<Health>();
            InvokePrivate(health, "Awake");
            gameObject.AddComponent<AlliedSquadMemberFollower>();
            return gameObject;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(method != null, target.GetType().Name + " missing private method " + methodName + ".");
            method.Invoke(target, null);
        }

        private static void SetRosterInstance(PlayerSquadRoster instance)
        {
            FieldInfo backingField = typeof(PlayerSquadRoster).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            Require(backingField != null, "PlayerSquadRoster.Instance backing field not found.");
            backingField.SetValue(null, instance);
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildRecursive(root.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("[InGameSquadFeatureValidator] " + message);
            }
        }
    }
}
