using UnityEngine;
using UnityEngine.AI;

namespace CorridorCommander
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NavMeshMovementMotor : MonoBehaviour, IMovementMotor
    {
        [SerializeField] private MovementStats stats = new MovementStats();

        private NavMeshAgent agent;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            ApplyStats();
        }

        private void OnValidate()
        {
            if (TryGetComponent(out NavMeshAgent navMeshAgent))
            {
                agent = navMeshAgent;
                ApplyStats();
            }
        }

        public void Move(Vector3 direction)
        {
            if (agent == null)
            {
                return;
            }

            Vector3 flattenedDirection = new Vector3(direction.x, 0f, direction.z);
            if (flattenedDirection.sqrMagnitude <= 0.0001f)
            {
                Stop();
                return;
            }

            Vector3 destination = transform.position + flattenedDirection.normalized;
            agent.isStopped = false;
            agent.Move(flattenedDirection.normalized * stats.moveSpeed * Time.deltaTime);
            RotateToward(destination);
        }

        public void MoveTo(Vector3 worldPosition)
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(worldPosition);
        }

        public void Stop()
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        private void ApplyStats()
        {
            if (agent == null || stats == null)
            {
                return;
            }

            agent.speed = stats.moveSpeed;
            agent.angularSpeed = stats.rotationSpeed;
            agent.acceleration = stats.acceleration;
            agent.stoppingDistance = stats.stoppingDistance;
        }

        private void RotateToward(Vector3 destination)
        {
            Vector3 direction = destination - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, stats.rotationSpeed * Time.deltaTime);
        }
    }
}
