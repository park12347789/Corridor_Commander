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
                if (artifacts[i] == artifact)
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

            artifacts.Add(artifact);
            message = $"Received: {artifact.DisplayName}";
            Debug.Log($"[ArtifactInventory] Artifact added: {artifact.ArtifactId}", this);
            Changed?.Invoke();
            ArtifactStatManager.Current?.NotifyArtifactInventoryChanged();
            return true;
        }
    }
}
