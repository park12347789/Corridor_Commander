using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    public enum ArtifactTarget
    {
        Turret = 0,
        Mortar = 1,
        Squad = 2,
        Player = 3
    }

    public enum ArtifactStat
    {
        Damage = 0,
        AttackInterval = 1,
        Range = 2,
        Cooldown = 3,
        Health = 4,
        MoveSpeed = 5
    }

    [Serializable]
    public sealed class ArtifactStatModifier
    {
        [SerializeField] private ArtifactTarget target;
        [SerializeField] private ArtifactStat stat;
        [SerializeField] private float multiplier = 1f;

        public ArtifactTarget Target => target;
        public ArtifactStat Stat => stat;
        public float Multiplier => Mathf.Max(0.01f, multiplier);
    }

    [CreateAssetMenu(
        menuName = "Corridor Commander/Artifacts/Artifact Definition",
        fileName = "ArtifactDefinition")]
    public sealed class ArtifactDefinitionSO : ScriptableObject
    {
        [SerializeField] private string artifactId = "artifact";
        [SerializeField] private string displayName = "Artifact";
        [SerializeField, TextArea] private string description = "Passive artifact.";
        [SerializeField] private Sprite icon;
        [SerializeField] private ArtifactStatModifier[] modifiers;

        public string ArtifactId => string.IsNullOrWhiteSpace(artifactId) ? name : artifactId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public IReadOnlyList<ArtifactStatModifier> Modifiers => modifiers;
    }
}
