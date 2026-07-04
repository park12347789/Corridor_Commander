using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class ArtifactStatManager : MonoBehaviour
    {
        private static ArtifactStatManager current;
        private static bool missingManagerLogged;

        [SerializeField] private ArtifactInventory inventory;

        public static ArtifactStatManager Current => current;

        public event Action StatsChanged;

        public void Configure(ArtifactInventory configuredInventory)
        {
            inventory = configuredInventory;
            NotifyArtifactInventoryChanged();
        }

        private void Awake()
        {
            if (current != null && current != this)
            {
                Debug.LogError("[ArtifactStatManager] Duplicate manager exists.", this);
                enabled = false;
                return;
            }

            current = this;
            if (inventory == null)
            {
                inventory = GetComponent<ArtifactInventory>();
            }

            if (inventory == null)
            {
                Debug.LogError("[ArtifactStatManager] ArtifactInventory is not assigned.", this);
            }
        }

        private void OnDestroy()
        {
            if (current == this)
            {
                current = null;
            }
        }

        public static float Apply(ArtifactTarget target, ArtifactStat stat, float baseValue)
        {
            ArtifactStatManager manager = current;
            if (manager == null)
            {
                LogMissingManager();
                return baseValue;
            }

            return manager.ApplyMultiplier(target, stat, baseValue);
        }

        public float GetMultiplier(ArtifactTarget target, ArtifactStat stat)
        {
            if (inventory == null)
            {
                Debug.LogError("[ArtifactStatManager] Cannot calculate stats without ArtifactInventory.", this);
                return 1f;
            }

            float multiplier = 1f;
            IReadOnlyList<ArtifactDefinitionSO> artifacts = inventory.Artifacts;
            for (int artifactIndex = 0; artifactIndex < artifacts.Count; artifactIndex++)
            {
                ArtifactDefinitionSO artifact = artifacts[artifactIndex];
                if (artifact == null || artifact.Modifiers == null)
                {
                    continue;
                }

                IReadOnlyList<ArtifactStatModifier> modifiers = artifact.Modifiers;
                for (int modifierIndex = 0; modifierIndex < modifiers.Count; modifierIndex++)
                {
                    ArtifactStatModifier modifier = modifiers[modifierIndex];
                    if (modifier == null || modifier.Target != target || modifier.Stat != stat)
                    {
                        continue;
                    }

                    multiplier *= modifier.Multiplier;
                }
            }

            return Mathf.Max(0.01f, multiplier);
        }

        public float ApplyMultiplier(ArtifactTarget target, ArtifactStat stat, float baseValue)
        {
            return baseValue * GetMultiplier(target, stat);
        }

        public void NotifyArtifactInventoryChanged()
        {
            StatsChanged?.Invoke();
        }

        private static void LogMissingManager()
        {
            if (missingManagerLogged)
            {
                return;
            }

            Debug.LogError("[ArtifactStatManager] No ArtifactStatManager exists in the active scene.");
            missingManagerLogged = true;
        }
    }
}
