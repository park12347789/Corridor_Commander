using UnityEngine;
using UnityEngine.AI;

namespace CorridorCommander
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NavMeshMovementMotor : MonoBehaviour, IMovementMotor, INavigationPathValidator, IMoveSpeedMultiplierReceiver
    {
        [SerializeField] private MovementStats stats = new MovementStats();
        [SerializeField] private float targetSampleDistance = 2f;
        [SerializeField] private float selfSampleDistance = 2f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float maxOffMeshLinkDistance = 4f;
        [SerializeField] private float maxOffMeshLinkHeightDelta = 1.5f;
        [SerializeField] private LayerMask offMeshLinkBlockMask = ~0;

        private NavMeshAgent agent;
        private CharacterController characterController;
        private NavMeshPath path;
        private IStatusEffectReceiver statusEffectReceiver;
        private float moveSpeedMultiplier = 1f;
        private int walkableAreaMask = NavMesh.AllAreas;
        private float verticalVelocity;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            characterController = GetComponent<CharacterController>();
            path = new NavMeshPath();
            ResolveStatusEffectReceiver();
            ApplyStats();
        }

        private void OnEnable()
        {
            TryWarpOntoNavMesh();
        }

        private void OnValidate()
        {
            if (TryGetComponent(out NavMeshAgent navMeshAgent))
            {
                agent = navMeshAgent;
                ApplyStats();
            }

            characterController = GetComponent<CharacterController>();
        }

        public void Move(Vector3 direction)
        {
            if (!TryWarpOntoNavMesh())
            {
                return;
            }

            Vector3 flattenedDirection = new Vector3(direction.x, 0f, direction.z);
            if (flattenedDirection.sqrMagnitude <= 0.0001f)
            {
                Stop();
                return;
            }

            ApplyRuntimeSpeed();
            Vector3 destination = transform.position + flattenedDirection.normalized;
            agent.isStopped = false;
            agent.Move(flattenedDirection.normalized * GetMoveSpeed() * Time.deltaTime);
            RotateToward(destination);
        }

        public void MoveTo(Vector3 worldPosition)
        {
            if (!TryWarpOntoNavMesh())
            {
                return;
            }

            if (!TryGetReachableDestination(worldPosition, out Vector3 destination, out _))
            {
                Stop();
                return;
            }

            if (agent.isOnOffMeshLink)
            {
                if (!TryCompleteSafeOffMeshLink())
                {
                    Stop();
                }

                return;
            }

            ApplyRuntimeSpeed();
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (!TryWarpOntoNavMesh())
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        public void SetMoveSpeedMultiplier(float multiplier)
        {
            moveSpeedMultiplier = Mathf.Max(0.01f, multiplier);
            ApplyRuntimeSpeed();
        }

        private void ApplyStats()
        {
            if (agent == null || stats == null)
            {
                return;
            }

            walkableAreaMask = NavMesh.AllAreas;
            int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");
            if (notWalkableArea >= 0)
            {
                walkableAreaMask &= ~(1 << notWalkableArea);
            }

            agent.areaMask = walkableAreaMask;
            agent.autoTraverseOffMeshLink = false;
            ApplyRuntimeSpeed();
            agent.angularSpeed = stats.rotationSpeed;
            agent.acceleration = stats.acceleration;
            agent.stoppingDistance = stats.stoppingDistance;
        }

        private void ApplyRuntimeSpeed()
        {
            if (agent == null || stats == null)
            {
                return;
            }

            agent.speed = GetMoveSpeed();
        }

        private float GetMoveSpeed()
        {
            ResolveStatusEffectReceiver();
            float statusMultiplier = statusEffectReceiver != null ? statusEffectReceiver.MoveSpeedMultiplier : 1f;
            return stats.moveSpeed * moveSpeedMultiplier * statusMultiplier;
        }

        private void ResolveStatusEffectReceiver()
        {
            if (statusEffectReceiver != null)
            {
                return;
            }

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IStatusEffectReceiver receiver)
                {
                    statusEffectReceiver = receiver;
                    return;
                }
            }
        }

        public bool CanReach(Vector3 worldPosition, out NavMeshPathStatus pathStatus)
        {
            return TryGetReachableDestination(worldPosition, out _, out pathStatus);
        }

        public bool TryGetReachableDestination(Vector3 worldPosition, out Vector3 destination, out NavMeshPathStatus pathStatus)
        {
            destination = worldPosition;
            pathStatus = NavMeshPathStatus.PathInvalid;

            if (!TryWarpOntoNavMesh())
            {
                return false;
            }

            if (!NavMesh.SamplePosition(worldPosition, out NavMeshHit targetHit, targetSampleDistance, walkableAreaMask))
            {
                return false;
            }

            path ??= new NavMeshPath();
            destination = targetHit.position;
            if (!agent.CalculatePath(destination, path))
            {
                return false;
            }

            pathStatus = path.status;
            return pathStatus == NavMeshPathStatus.PathComplete;
        }

        private bool TryWarpOntoNavMesh()
        {
            if (agent == null)
            {
                return false;
            }

            if (agent.isOnNavMesh)
            {
                verticalVelocity = 0f;
                return true;
            }

            if (characterController != null && !characterController.isGrounded)
            {
                verticalVelocity += gravity * Time.deltaTime;
                characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
                return false;
            }

            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, selfSampleDistance, walkableAreaMask))
            {
                return false;
            }

            verticalVelocity = 0f;
            return agent.Warp(hit.position);
        }

        private bool TryCompleteSafeOffMeshLink()
        {
            if (agent == null || !agent.isOnOffMeshLink)
            {
                return true;
            }

            OffMeshLinkData linkData = agent.currentOffMeshLinkData;
            Vector3 start = linkData.startPos;
            Vector3 end = linkData.endPos;
            Vector3 horizontalDelta = end - start;
            horizontalDelta.y = 0f;

            if (horizontalDelta.magnitude > maxOffMeshLinkDistance)
            {
                return false;
            }

            if (Mathf.Abs(end.y - start.y) > maxOffMeshLinkHeightDelta)
            {
                return false;
            }

            Vector3 direction = end - transform.position;
            float distance = direction.magnitude;
            if (distance > 0.001f)
            {
                float radius = Mathf.Max(0.01f, agent.radius);
                float capsuleBottomOffset = radius + 0.05f;
                float capsuleTopOffset = Mathf.Max(capsuleBottomOffset + 0.01f, agent.height - radius - 0.05f);
                Vector3 capsuleBottom = transform.position + Vector3.up * capsuleBottomOffset;
                Vector3 capsuleTop = transform.position + Vector3.up * capsuleTopOffset;
                if (Physics.CapsuleCast(
                        capsuleBottom,
                        capsuleTop,
                        radius,
                        direction.normalized,
                        distance,
                        offMeshLinkBlockMask,
                        QueryTriggerInteraction.Ignore))
                {
                    return false;
                }
            }

            agent.Warp(end);
            agent.CompleteOffMeshLink();
            return true;
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
