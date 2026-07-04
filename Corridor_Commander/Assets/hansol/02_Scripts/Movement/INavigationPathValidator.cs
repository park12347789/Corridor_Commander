using UnityEngine;
using UnityEngine.AI;

namespace CorridorCommander
{
    public interface INavigationPathValidator
    {
        bool CanReach(Vector3 worldPosition, out NavMeshPathStatus pathStatus);
        bool TryGetReachableDestination(Vector3 worldPosition, out Vector3 destination, out NavMeshPathStatus pathStatus);
    }
}
