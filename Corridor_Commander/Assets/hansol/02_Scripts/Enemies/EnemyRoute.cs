using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class EnemyRoute : MonoBehaviour
    {
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField] private bool includeFinalTarget = true;

        public IReadOnlyList<Transform> Waypoints => waypoints;
        public bool IncludeFinalTarget => includeFinalTarget;

        private void OnValidate()
        {
            waypoints.RemoveAll(waypoint => waypoint == null);
        }
    }
}
