using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    public enum BuildableCategory
    {
        Defense = 0,
        Offense = 1,
        Skill = 2
    }

    [CreateAssetMenu(
        menuName = "Corridor Commander/Construction/Buildable Definition",
        fileName = "BuildableDefinition")]
    public sealed class BuildableDefinitionSO : ScriptableObject
    {
        [SerializeField] private string buildableId = "buildable";
        [SerializeField] private string displayName = "Buildable";
        [SerializeField] private BuildableKind kind;
        [SerializeField] private BuildableCategory category;
        [SerializeField] private GameObject prefab;
        [SerializeField] [Min(0)] private int price;
        [SerializeField] [TextArea] private string explanation = "";
        [SerializeField] private Sprite icon;
        [SerializeField] private bool rotateBeforeInstall;
        [SerializeField] private BuildableRoleDefinitionSO[] roleDefinitions;

        public string BuildableId => buildableId;
        public string DisplayName => displayName;
        public BuildableKind Kind => kind;
        public BuildableCategory Category => category;
        public GameObject Prefab => prefab;
        public int Price => price;
        public string Explanation => explanation;
        public Sprite Icon => icon;
        public bool RotateBeforeInstall => rotateBeforeInstall;
        public IReadOnlyList<BuildableRoleDefinitionSO> RoleDefinitions => roleDefinitions;
    }
}
