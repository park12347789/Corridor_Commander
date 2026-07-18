using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class ArtifactInventory : MonoBehaviour
    {
        [SerializeField] private List<ArtifactDefinitionSO> artifacts = new List<ArtifactDefinitionSO>();

        public IReadOnlyList<ArtifactDefinitionSO> Artifacts => artifacts;

        public event Action Changed;

        public bool HasArtifact(ArtifactDefinitionSO artifact)
        {
            if (artifact == null)
            {
                return false;
            }

            for (int i = 0; i < artifacts.Count; i++)
            {
                ArtifactDefinitionSO ownedArtifact = artifacts[i];
                if (ownedArtifact == artifact
                    || ownedArtifact != null
                    && !string.IsNullOrWhiteSpace(ownedArtifact.ArtifactId)
                    && ownedArtifact.ArtifactId == artifact.ArtifactId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAddArtifact(ArtifactDefinitionSO artifact, out string message)
        {
            if (artifact == null)
            {
                message = "Artifact data missing";
                return false;
            }

            if (HasArtifact(artifact))
            {
                message = $"이미 보유 중: {artifact.DisplayName}";
                return false;
            }

            artifacts.Add(artifact);
            message = $"Received: {artifact.DisplayName}";
            Debug.Log($"[ArtifactInventory] Artifact added: {artifact.ArtifactId}", this);
            Changed?.Invoke();
            ArtifactStatManager.Current?.NotifyArtifactInventoryChanged();
            return true;
        }
    }
}
