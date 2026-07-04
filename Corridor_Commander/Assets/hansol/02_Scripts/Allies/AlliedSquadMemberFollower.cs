using UnityEngine;
using UnityEngine.AI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class AlliedSquadMemberFollower : MonoBehaviour
    {
        private enum CommandMode
        {
            Follow,
            HoldPosition,
            Charge
        }

        [SerializeField] private Transform followTarget;
        [SerializeField] private int formationIndex;
        [SerializeField] private float behindDistance = 2.4f;
        [SerializeField] private float lateralSpacing = 1.1f;
        [SerializeField] private float rowSpacing = 1.2f;
        [SerializeField] private float verticalOffset;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float stoppingDistance = 0.35f;
        [SerializeField] private float commandSpacing = 1.25f;
        [SerializeField] private bool letCombatControlRotation = true;
        [SerializeField] private float movementCommandRefreshInterval = 0.1f;
        [SerializeField] private float movementDestinationUpdateDistance = 0.15f;
        [SerializeField] private Sprite rosterIcon;

        [Header("Debug")]
        [SerializeField] private bool logCommandDebug;
        [SerializeField] private float stuckCheckInterval = 0.5f;
        [SerializeField] private float stuckDistanceThreshold = 0.05f;
        [SerializeField] private float stuckWarningDelay = 1.5f;

        [Header("Recovery")]
        [SerializeField] private bool recoverWhenStuck = true;
        [SerializeField] private float recoveryCooldown = 1f;
        [SerializeField] private float recoveryOffsetStep = 0.6f;
        [SerializeField] private int recoveryRingCount = 2;
        [SerializeField] private float recoveryNavMeshSampleDistance = 1.5f;
        [SerializeField] private bool drawRecoveryGizmos = true;

        private CommandMode commandMode;
        private Vector3 commandTargetPosition;
        private IMovementMotor movementMotor;
        private INavigationPathValidator pathValidator;
        private AlliedSquadMemberCombat combatController;
        private Vector3 lastIssuedMoveDestination;
        private float nextMoveCommandTime;
        private bool hasIssuedMoveDestination;
        private Vector3 lastStuckCheckPosition;
        private float nextStuckCheckTime;
        private float stuckStartedAt;
        private bool isPotentiallyStuck;
        private bool hasLoggedArrival;
        private float nextRecoveryTime;
        private Vector3 lastRecoveryOriginalDestination;
        private Vector3 lastRecoveryDestination;
        private bool hasRecoveryDestination;
        private Vector3 lastReachableDestination;
        private bool hasReachableDestination;

        public Transform FollowTarget => followTarget;
        public int FormationIndex => formationIndex;
        public Sprite RosterIcon => rosterIcon;

        private void Awake()
        {
            ResolveMovementComponents();
            ResolveCombatComponent();
        }

        private void Update()
        {
            ResolveMovementComponents();
            ResolveCombatComponent();

            if (commandMode == CommandMode.Follow)
            {
                TickFollow();
                return;
            }

            TickCommand();
        }

        public void Configure(Transform target, int slotIndex)
        {
            followTarget = target;
            formationIndex = Mathf.Max(0, slotIndex);
            commandMode = CommandMode.Follow;
            ResetMoveCommandCache();
            ResetStuckDetection();
            LogCommand($"Configured Follow Target={ResolveName(followTarget)} Slot={formationIndex}");
        }

        public void SetRosterIcon(Sprite icon)
        {
            rosterIcon = icon;
        }

        public void SetHoldPosition()
        {
            commandMode = CommandMode.HoldPosition;
            commandTargetPosition = ResolveReachablePosition(transform.position);
            ResetMoveCommandCache();
            ResetStuckDetection();
            LogCommand($"Command HoldPosition Destination={FormatVector(commandTargetPosition)}");
        }

        public void ReturnToPlayer(Transform target, int slotIndex)
        {
            followTarget = target != null ? target : followTarget;
            formationIndex = Mathf.Max(0, slotIndex);
            commandMode = CommandMode.Follow;
            ResetMoveCommandCache();
            ResetStuckDetection();
            LogCommand($"Command ReturnToPlayer Target={ResolveName(followTarget)} Slot={formationIndex}");
        }

        public void SetChargeTarget(Vector3 targetPosition, int slotIndex)
        {
            formationIndex = Mathf.Max(0, slotIndex);
            commandMode = CommandMode.Charge;
            Vector3 sideOffset = transform.right * ((formationIndex % 3) - 1) * commandSpacing;
            commandTargetPosition = ResolveReachablePosition(targetPosition + sideOffset);
            ResetMoveCommandCache();
            ResetStuckDetection();
            LogCommand($"Command Charge Requested={FormatVector(targetPosition)} Destination={FormatVector(commandTargetPosition)} Slot={formationIndex}");
        }

        public void ResumeFollow()
        {
            commandMode = CommandMode.Follow;
            ResetMoveCommandCache();
            ResetStuckDetection();
            LogCommand("Command ResumeFollow");
        }

        private void TickFollow()
        {
            if (followTarget == null)
            {
                LogThrottledWarning("Follow mode has no target.");
                return;
            }

            Vector3 targetPosition = followTarget.TransformPoint(GetFormationOffset());
            targetPosition = ResolveReachablePosition(targetPosition);
            MoveToward(targetPosition, followTarget.position - transform.position);
        }

        private void TickCommand()
        {
            Vector3 lookDirection = commandMode == CommandMode.HoldPosition && followTarget != null
                ? followTarget.position - transform.position
                : commandTargetPosition - transform.position;

            MoveToward(commandTargetPosition, lookDirection);
        }

        private void MoveToward(Vector3 targetPosition, Vector3 lookDirection)
        {
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                movementMotor?.Stop();
                hasIssuedMoveDestination = false;
                ResetStuckDetection();
                LogArrival(targetPosition);

                if (ShouldLetCombatControlRotation())
                {
                    return;
                }

                RotateToward(lookDirection);
                return;
            }

            if (movementMotor != null)
            {
                if (ShouldIssueMoveCommand(targetPosition))
                {
                    movementMotor.MoveTo(targetPosition);
                    lastIssuedMoveDestination = targetPosition;
                    nextMoveCommandTime = Time.time + Mathf.Max(0.02f, movementCommandRefreshInterval);
                    hasIssuedMoveDestination = true;
                    hasLoggedArrival = false;
                    LogMoveIssued(targetPosition);
                }

                TickStuckDetection(targetPosition, toTarget.magnitude);
                return;
            }

            Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            Vector3 moveDirection = nextPosition - transform.position;
            transform.position = nextPosition;
            hasLoggedArrival = false;
            TickStuckDetection(targetPosition, toTarget.magnitude);
            RotateToward(moveDirection);
        }

        private Vector3 GetFormationOffset()
        {
            int row = formationIndex / 3;
            int column = formationIndex % 3;
            float lateralOffset = (column - 1) * lateralSpacing;
            float backOffset = behindDistance + row * rowSpacing;
            return new Vector3(lateralOffset, verticalOffset, -backOffset);
        }

        private Vector3 ResolveReachablePosition(Vector3 targetPosition)
        {
            if (pathValidator == null)
            {
                return targetPosition;
            }

            if (pathValidator.TryGetReachableDestination(targetPosition, out Vector3 destination, out UnityEngine.AI.NavMeshPathStatus pathStatus))
            {
                RememberReachableDestination(destination);
                return destination;
            }

            if (TryFindRecoveryDestination(targetPosition, out Vector3 nearbyDestination))
            {
                RememberReachableDestination(nearbyDestination);
                LogCommand($"Path resolve fallback used. Mode={commandMode} Requested={FormatVector(targetPosition)} Fallback={FormatVector(nearbyDestination)} PathStatus={pathStatus}");
                return nearbyDestination;
            }

            if (hasReachableDestination)
            {
                LogCommand($"Path resolve failed. Reusing last reachable destination. Mode={commandMode} Requested={FormatVector(targetPosition)} Last={FormatVector(lastReachableDestination)} PathStatus={pathStatus}");
                return lastReachableDestination;
            }

            LogCommand($"Path resolve failed. No fallback destination yet. Mode={commandMode} Requested={FormatVector(targetPosition)} PathStatus={pathStatus}");
            return targetPosition;
        }

        private void ResolveMovementComponents()
        {
            if (movementMotor != null && pathValidator != null)
            {
                return;
            }

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (movementMotor == null && behaviours[i] is IMovementMotor foundMotor)
                {
                    movementMotor = foundMotor;
                }

                if (pathValidator == null && behaviours[i] is INavigationPathValidator foundValidator)
                {
                    pathValidator = foundValidator;
                }
            }
        }

        private void ResolveCombatComponent()
        {
            if (combatController != null)
            {
                return;
            }

            combatController = GetComponent<AlliedSquadMemberCombat>();

            if (combatController == null)
            {
                combatController = GetComponentInChildren<AlliedSquadMemberCombat>(true);
            }
        }

        private bool ShouldLetCombatControlRotation()
        {
            return letCombatControlRotation
                && combatController != null
                && combatController.IsAiming;
        }

        private bool ShouldIssueMoveCommand(Vector3 targetPosition)
        {
            if (!hasIssuedMoveDestination)
            {
                return true;
            }

            Vector3 delta = targetPosition - lastIssuedMoveDestination;
            delta.y = 0f;

            if (delta.sqrMagnitude >= movementDestinationUpdateDistance * movementDestinationUpdateDistance)
            {
                return true;
            }

            return Time.time >= nextMoveCommandTime;
        }

        private void ResetMoveCommandCache()
        {
            hasIssuedMoveDestination = false;
            nextMoveCommandTime = 0f;
            hasLoggedArrival = false;
        }

        private void ResetStuckDetection()
        {
            lastStuckCheckPosition = transform.position;
            nextStuckCheckTime = Time.time + Mathf.Max(0.1f, stuckCheckInterval);
            stuckStartedAt = 0f;
            isPotentiallyStuck = false;
        }

        private void RememberReachableDestination(Vector3 destination)
        {
            lastReachableDestination = destination;
            hasReachableDestination = true;
        }

        private void TickStuckDetection(Vector3 targetPosition, float remainingDistance)
        {
            if (Time.time < nextStuckCheckTime)
            {
                return;
            }

            nextStuckCheckTime = Time.time + Mathf.Max(0.1f, stuckCheckInterval);

            Vector3 delta = transform.position - lastStuckCheckPosition;
            delta.y = 0f;
            lastStuckCheckPosition = transform.position;

            if (remainingDistance <= stoppingDistance)
            {
                isPotentiallyStuck = false;
                stuckStartedAt = 0f;
                return;
            }

            if (delta.magnitude > Mathf.Max(0.001f, stuckDistanceThreshold))
            {
                isPotentiallyStuck = false;
                stuckStartedAt = 0f;
                return;
            }

            if (!isPotentiallyStuck)
            {
                isPotentiallyStuck = true;
                stuckStartedAt = Time.time;
                return;
            }

            if (Time.time - stuckStartedAt < Mathf.Max(0.1f, stuckWarningDelay))
            {
                return;
            }

            if (logCommandDebug)
            {
                Debug.LogWarning(
                    $"[AlliedSquadMemberFollower] Movement bottleneck suspected. Mode={commandMode} Position={FormatVector(transform.position)} Destination={FormatVector(targetPosition)} Remaining={remainingDistance:0.00} HasMotor={movementMotor != null} HasPathValidator={pathValidator != null} CombatAiming={ShouldLetCombatControlRotation()}",
                    this);
            }

            TryRecoverMovement(targetPosition);
            stuckStartedAt = Time.time;
        }

        private void TryRecoverMovement(Vector3 originalTargetPosition)
        {
            if (!recoverWhenStuck || movementMotor == null || Time.time < nextRecoveryTime)
            {
                return;
            }

            nextRecoveryTime = Time.time + Mathf.Max(0.1f, recoveryCooldown);
            movementMotor.Stop();

            if (!TryFindRecoveryDestination(originalTargetPosition, out Vector3 recoveryDestination))
            {
                lastRecoveryOriginalDestination = originalTargetPosition;
                hasRecoveryDestination = false;
                LogCommand($"Recovery failed. No reachable nearby destination around {FormatVector(originalTargetPosition)}");
                return;
            }

            ResetMoveCommandCache();
            movementMotor.MoveTo(recoveryDestination);
            lastIssuedMoveDestination = recoveryDestination;
            nextMoveCommandTime = Time.time + Mathf.Max(0.02f, movementCommandRefreshInterval);
            hasIssuedMoveDestination = true;
            lastRecoveryOriginalDestination = originalTargetPosition;
            lastRecoveryDestination = recoveryDestination;
            hasRecoveryDestination = true;

            LogCommand($"Recovery MoveTo issued. Original={FormatVector(originalTargetPosition)} Recovery={FormatVector(recoveryDestination)}");
        }

        private bool TryFindRecoveryDestination(Vector3 originalTargetPosition, out Vector3 recoveryDestination)
        {
            recoveryDestination = originalTargetPosition;

            if (TrySampleReachablePosition(originalTargetPosition, out recoveryDestination))
            {
                return true;
            }

            Vector3 right = transform.right;
            Vector3 forward = transform.forward;
            right.y = 0f;
            forward.y = 0f;

            if (right.sqrMagnitude <= 0.0001f)
            {
                right = Vector3.right;
            }

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            right.Normalize();
            forward.Normalize();

            Vector3[] directions =
            {
                right,
                -right,
                forward,
                -forward,
                (right + forward).normalized,
                (-right + forward).normalized,
                (right - forward).normalized,
                (-right - forward).normalized
            };

            int ringCount = Mathf.Max(1, recoveryRingCount);
            float step = Mathf.Max(0.05f, recoveryOffsetStep);

            for (int ring = 1; ring <= ringCount; ring++)
            {
                float distance = step * ring;

                for (int i = 0; i < directions.Length; i++)
                {
                    Vector3 candidate = originalTargetPosition + directions[i] * distance;

                    if (TrySampleReachablePosition(candidate, out recoveryDestination))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TrySampleReachablePosition(Vector3 candidate, out Vector3 sampledPosition)
        {
            sampledPosition = candidate;

            if (!NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    Mathf.Max(0.1f, recoveryNavMeshSampleDistance),
                    NavMesh.AllAreas))
            {
                return false;
            }

            sampledPosition = hit.position;

            if (pathValidator == null)
            {
                return true;
            }

            return pathValidator.CanReach(sampledPosition, out NavMeshPathStatus pathStatus)
                && pathStatus == NavMeshPathStatus.PathComplete;
        }

        private void LogMoveIssued(Vector3 targetPosition)
        {
            if (!logCommandDebug)
            {
                return;
            }

            Debug.Log(
                $"[AlliedSquadMemberFollower] MoveTo issued. Mode={commandMode} Destination={FormatVector(targetPosition)} Position={FormatVector(transform.position)}",
                this);
        }

        private void LogArrival(Vector3 targetPosition)
        {
            if (!logCommandDebug || hasLoggedArrival)
            {
                return;
            }

            hasLoggedArrival = true;
            Debug.Log(
                $"[AlliedSquadMemberFollower] Arrived/Stopped. Mode={commandMode} Destination={FormatVector(targetPosition)} Position={FormatVector(transform.position)} CombatAiming={ShouldLetCombatControlRotation()}",
                this);
        }

        private void LogCommand(string message)
        {
            if (!logCommandDebug)
            {
                return;
            }

            Debug.Log($"[AlliedSquadMemberFollower] {message}", this);
        }

        private void LogThrottledWarning(string message)
        {
            if (!logCommandDebug || Time.time < nextStuckCheckTime)
            {
                return;
            }

            nextStuckCheckTime = Time.time + Mathf.Max(0.1f, stuckCheckInterval);
            Debug.LogWarning($"[AlliedSquadMemberFollower] {message}", this);
        }

        private static string ResolveName(Object target)
        {
            return target != null ? target.name : "None";
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.00}, {value.y:0.00}, {value.z:0.00})";
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawRecoveryGizmos)
            {
                return;
            }

            Vector3 origin = Application.isPlaying && hasIssuedMoveDestination
                ? lastIssuedMoveDestination
                : commandTargetPosition;

            if (origin == Vector3.zero)
            {
                origin = transform.position;
            }

            DrawRecoveryCandidateGizmos(origin);

            if (Application.isPlaying && hasRecoveryDestination)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(lastRecoveryDestination, 0.18f);
                Gizmos.DrawLine(lastRecoveryOriginalDestination, lastRecoveryDestination);
            }
        }

        private void DrawRecoveryCandidateGizmos(Vector3 originalTargetPosition)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(originalTargetPosition, 0.2f);

            Vector3 right = transform.right;
            Vector3 forward = transform.forward;
            right.y = 0f;
            forward.y = 0f;

            if (right.sqrMagnitude <= 0.0001f)
            {
                right = Vector3.right;
            }

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            right.Normalize();
            forward.Normalize();

            Vector3[] directions =
            {
                right,
                -right,
                forward,
                -forward,
                (right + forward).normalized,
                (-right + forward).normalized,
                (right - forward).normalized,
                (-right - forward).normalized
            };

            int ringCount = Mathf.Max(1, recoveryRingCount);
            float step = Mathf.Max(0.05f, recoveryOffsetStep);

            for (int ring = 1; ring <= ringCount; ring++)
            {
                float distance = step * ring;
                Gizmos.color = new Color(1f, 0.75f, 0f, 0.35f);
                Gizmos.DrawWireSphere(originalTargetPosition, distance);

                for (int i = 0; i < directions.Length; i++)
                {
                    Vector3 candidate = originalTargetPosition + directions[i] * distance;
                    bool isOnNavMesh = NavMesh.SamplePosition(
                        candidate,
                        out NavMeshHit hit,
                        Mathf.Max(0.1f, recoveryNavMeshSampleDistance),
                        NavMesh.AllAreas);

                    Gizmos.color = isOnNavMesh ? Color.cyan : Color.red;
                    Gizmos.DrawSphere(isOnNavMesh ? hit.position : candidate, 0.08f);
                    Gizmos.DrawLine(originalTargetPosition, isOnNavMesh ? hit.position : candidate);
                }
            }
        }

        private void RotateToward(Vector3 direction)
        {
            if (ShouldLetCombatControlRotation())
            {
                return;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
