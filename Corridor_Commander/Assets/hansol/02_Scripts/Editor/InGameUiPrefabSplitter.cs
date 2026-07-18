using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CorridorCommander.EditorTools
{
    public static class InGameUiPrefabSplitter
    {
        private const string MainCanvasPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvas.prefab";
        private const string WaveDirectorCanvasPrefabPath = "Assets/hansol/03_Prefabs/UI/InGame/WaveDirectorCanvas.prefab";
        private const string PartsFolder = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts";

        private static readonly PartSpec[] MainCanvasParts =
        {
            new PartSpec("PlacementBuildMenuPanel", "PlacementBuildMenuPanel.prefab"),
            new PartSpec("InstalledSkillSlotBar", "InstalledSkillSlotBar.prefab"),
            new PartSpec("TEMP_PlayerCrosshair_RaycastCenter", "TEMP_PlayerCrosshair_RaycastCenter.prefab"),
            new PartSpec("SupportTruckShopPresenter", "SupportTruckShopPresenter.prefab"),
            new PartSpec("PlayerCommandHotbarPresenter", "PlayerCommandHotbarPresenter.prefab"),
            new PartSpec("PlayerItemRadialPresenter", "PlayerItemRadialPresenter.prefab"),
            new PartSpec("InstalledObjectActionPresenter", "InstalledObjectActionPresenter.prefab"),
            new PartSpec("PlayerCommandRadialPresenter", "PlayerCommandRadialPresenter.prefab"),
            new PartSpec("Commodity", "Commodity.prefab"),
            new PartSpec("status", "StatusHud.prefab"),
            new PartSpec("TreasureRewardMenuPresenter", "TreasureRewardMenuPresenter.prefab"),
            new PartSpec("InteractionPromptPresenter", "InteractionPromptPresenter.prefab"),
            new PartSpec("PlacementPreviewInstructionRoot", "PlacementPreviewInstructionRoot.prefab"),
            new PartSpec("WeaponHudPanel", "PlayerWeaponHudPresenter.prefab"),
            new PartSpec("PauseMenuPresenter", "PauseMenuPresenter.prefab"),
            new PartSpec("PopupDimOverlay", "PopupDimOverlay.prefab"),
            new PartSpec("WaveReadyPopupRoot", "WaveReadyPopupRoot.prefab"),
            new PartSpec("InstalledObjectAimInfoPresenter", "InstalledObjectAimInfoPresenter.prefab"),
            new PartSpec("SquadListRoot", "SquadListRoot.prefab"),
            new PartSpec("PlayerStatsArtifactPopup", "PlayerStatsArtifactPopup.prefab"),
        };

        private static readonly PartSpec[] WaveDirectorParts =
        {
            new PartSpec("WaveReadyPopupRoot", "WaveDirectorCanvas_WaveReadyPopupRoot.prefab"),
        };

        [MenuItem("Corridor Commander/UI/Validate In-Game UI Prefab Split")]
        public static void Validate()
        {
            ValidateForAutomation();
        }

        public static void ValidateForAutomation()
        {
            List<string> failures = new List<string>();
            ValidatePrefabChildren(MainCanvasPrefabPath, MainCanvasParts, failures);
            ValidatePrefabChildren(WaveDirectorCanvasPrefabPath, WaveDirectorParts, failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException("[InGameUiPrefabSplitter] Validation failed: " + string.Join(" | ", failures.ToArray()));
            }

            Debug.Log("[InGameUiPrefabSplitter] Validation passed.");
        }

        private static void ValidatePrefabChildren(string hostPrefabPath, PartSpec[] parts, List<string> failures)
        {
            GameObject hostRoot = PrefabUtility.LoadPrefabContents(hostPrefabPath);
            try
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    Transform target = FindChildRecursive(hostRoot.transform, parts[i].ChildName);
                    if (target == null)
                    {
                        failures.Add(hostPrefabPath + " missing child " + parts[i].ChildName);
                        continue;
                    }

                    string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target.gameObject);
                    if (!string.Equals(assetPath, parts[i].PrefabPath, StringComparison.Ordinal))
                    {
                        failures.Add(hostPrefabPath + " child " + parts[i].ChildName + " not connected to " + parts[i].PrefabPath + " (actual: " + assetPath + ")");
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hostRoot);
            }
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

        private readonly struct PartSpec
        {
            public PartSpec(string childName, string prefabName)
            {
                ChildName = childName;
                PrefabPath = PartsFolder + "/" + prefabName;
            }

            public string ChildName { get; }
            public string PrefabPath { get; }
        }
    }
}
