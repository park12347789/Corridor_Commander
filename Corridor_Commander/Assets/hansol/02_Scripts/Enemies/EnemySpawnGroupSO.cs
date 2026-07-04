using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(menuName = "Corridor Commander/Enemies/Enemy Spawn Group")]
    public sealed class EnemySpawnGroupSO : ScriptableObject
    {
        [SerializeField] private string displayName = "Spawn Group";

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
