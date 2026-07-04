using UnityEngine;

namespace CorridorCommander
{
    public abstract class BuildableRoleDefinitionSO : ScriptableObject
    {
        [SerializeField] private string roleId = "role";
        [SerializeField] private string displayName = "Role";

        public string RoleId => roleId;
        public string DisplayName => displayName;
    }
}
