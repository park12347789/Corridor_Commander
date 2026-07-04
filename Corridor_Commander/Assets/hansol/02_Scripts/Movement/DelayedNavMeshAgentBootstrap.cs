using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class DelayedNavMeshAgentBootstrap : MonoBehaviour
    {
        [SerializeField] private float sampleDistance = 3f;
        [SerializeField] private int maxFramesToWait = 10;

        private NavMeshAgent agent;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }
        }

        private IEnumerator Start()
        {
            if (agent == null)
            {
                yield break;
            }

            int areaMask = NavMesh.AllAreas;
            int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");
            if (notWalkableArea >= 0)
            {
                areaMask &= ~(1 << notWalkableArea);
            }

            for (int i = 0; i <= maxFramesToWait; i++)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleDistance, areaMask))
                {
                    agent.enabled = true;
                    agent.Warp(hit.position);
                    yield break;
                }

                yield return null;
            }

            agent.enabled = true;
        }
    }
}
