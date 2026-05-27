using UnityEngine;
using UnityEngine.AI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class MapObstacle : MonoBehaviour
    {
        [SerializeField] private MapObstacleKind obstacleKind = MapObstacleKind.Solid;

        public MapObstacleKind ObstacleKind => obstacleKind;
        public bool BlocksNavigation => obstacleKind == MapObstacleKind.Solid;
        public bool CanBeDestroyed => obstacleKind == MapObstacleKind.Breakable && TryGetComponent(out Health _);

        private void Awake()
        {
            ConfigureCollider();
            ConfigureNavigationObstacle();
        }

        private void OnValidate()
        {
            ConfigureCollider();
            ConfigureNavigationObstacle();
        }

        private void ConfigureCollider()
        {
            if (TryGetComponent(out Collider obstacleCollider))
            {
                obstacleCollider.isTrigger = false;
            }
        }

        private void ConfigureNavigationObstacle()
        {
            if (!TryGetComponent(out NavMeshObstacle navMeshObstacle))
            {
                return;
            }

            navMeshObstacle.enabled = BlocksNavigation;
            navMeshObstacle.carving = BlocksNavigation;
        }
    }
}
