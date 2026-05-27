using UnityEngine;
using UnityEngine.AI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class EnemyRouteLineVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform goalPoint;
        [SerializeField] private float heightOffset = 0.08f;

        private LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            Refresh();
        }

        public void Refresh()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            if (lineRenderer == null || startPoint == null || goalPoint == null)
            {
                return;
            }

            NavMeshPath path = new NavMeshPath();
            bool hasPath = NavMesh.CalculatePath(startPoint.position, goalPoint.position, NavMesh.AllAreas, path);
            Vector3[] corners = hasPath && path.corners.Length > 0
                ? path.corners
                : new[] { startPoint.position, goalPoint.position };

            lineRenderer.positionCount = corners.Length;
            for (int i = 0; i < corners.Length; i++)
            {
                lineRenderer.SetPosition(i, corners[i] + Vector3.up * heightOffset);
            }
        }
    }
}
