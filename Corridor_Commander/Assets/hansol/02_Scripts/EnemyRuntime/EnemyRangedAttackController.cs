using System;
using UnityEngine;

namespace CorridorCommander.Enemy
{
    public enum EnemyRangedAttackState
    {
        Chasing,
        Windup,
        Cooldown,
        Dead
    }

    [DisallowMultipleComponent]
    public sealed class EnemyRangedAttackController : MonoBehaviour
    {
        private const int TargetBufferSize = 128;

        [Header("Configuration")]
        [SerializeField] private EnemyRangedAttackDefinitionSO definition;

        [Header("References")]
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private EnemyMovementController movementController;
        [SerializeField] private Health health;

        [Header("Targeting")]
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private LayerMask obstructionLayers = ~(1 << 7);
        [SerializeField] private bool rotateTowardTarget = true;

        [Header("Debug")]
        [SerializeField] private bool logAttacks;

        private readonly Collider[] targetBuffer = new Collider[TargetBufferSize];
        private Health currentTarget;
        private float nextTargetRefreshTime;
        private float stateEndTime;
        private float nextDebugLogTime;

        public EnemyRangedAttackState CurrentState { get; private set; } = EnemyRangedAttackState.Chasing;
        public Health CurrentTarget => currentTarget;

        public event Action AttackWindupStarted;
        public event Action ProjectileReleased;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (health != null)
            {
                health.Died -= HandleDied;
                health.Died += HandleDied;
            }

            ChangeState(health != null && !health.IsAlive
                ? EnemyRangedAttackState.Dead
                : EnemyRangedAttackState.Chasing);
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }

            movementController?.SetPaused(false);
        }

        private void Update()
        {
            if (definition == null || CurrentState == EnemyRangedAttackState.Dead)
            {
                return;
            }

            switch (CurrentState)
            {
                case EnemyRangedAttackState.Chasing:
                    TickChasing();
                    break;
                case EnemyRangedAttackState.Windup:
                    TickWindup();
                    break;
                case EnemyRangedAttackState.Cooldown:
                    TickCooldown();
                    break;
            }
        }

        private void TickChasing()
        {
            RefreshTargetIfNeeded();
            movementController?.SetPaused(false);

            if (!CanAttack(currentTarget))
            {
                return;
            }

            movementController?.SetPaused(true);
            if (rotateTowardTarget)
            {
                FaceTarget(currentTarget.transform.position);
            }

            stateEndTime = Time.time + definition.WindupDuration;
            ChangeState(EnemyRangedAttackState.Windup);
            AttackWindupStarted?.Invoke();
        }

        private void TickWindup()
        {
            if (!IsTargetAlive(currentTarget))
            {
                currentTarget = null;
                ChangeState(EnemyRangedAttackState.Chasing);
                return;
            }

            movementController?.SetPaused(true);
            if (rotateTowardTarget)
            {
                FaceTarget(currentTarget.transform.position);
            }

            if (Time.time < stateEndTime)
            {
                return;
            }

            FireProjectile();
            stateEndTime = Time.time + definition.AttackInterval;
            ChangeState(EnemyRangedAttackState.Cooldown);
        }

        private void TickCooldown()
        {
            if (IsTargetAlive(currentTarget) && IsWithinDetectionRange(currentTarget))
            {
                movementController?.SetPaused(true);
                if (rotateTowardTarget)
                {
                    FaceTarget(currentTarget.transform.position);
                }
            }
            else
            {
                currentTarget = null;
                movementController?.SetPaused(false);
            }

            if (Time.time < stateEndTime)
            {
                return;
            }

            ChangeState(EnemyRangedAttackState.Chasing);
        }

        private void RefreshTargetIfNeeded()
        {
            if (IsTargetAlive(currentTarget) && IsWithinDetectionRange(currentTarget)
                && Time.time < nextTargetRefreshTime)
            {
                return;
            }

            nextTargetRefreshTime = Time.time + definition.TargetRefreshInterval;
            Health previousTarget = currentTarget;
            currentTarget = FindClosestTarget();

            if (logAttacks && previousTarget != currentTarget)
            {
                if (currentTarget != null)
                {
                    Debug.Log($"[EnemyRangedAttackController] Target acquired: {currentTarget.name}.", this);
                }
            }

            if (currentTarget == null)
            {
                LogDebugThrottled("[EnemyRangedAttackController] No valid target found in detection range.");
            }
        }

        private Health FindClosestTarget()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                definition.DetectionRange,
                targetBuffer,
                targetLayers,
                QueryTriggerInteraction.Collide);

            Health closest = null;
            float closestDistance = float.MaxValue;

            if (logAttacks && hitCount >= targetBuffer.Length)
            {
                LogDebugThrottled(
                    "[EnemyRangedAttackController] Target buffer is full. Narrow Target Layers if targets are still missed.");
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider candidateCollider = targetBuffer[i];
                Health candidate = candidateCollider != null
                    ? ResolveHealth(candidateCollider)
                    : null;

                if (!IsValidCombatTarget(candidate))
                {
                    continue;
                }

                float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance >= closestDistance)
                {
                    continue;
                }

                closestDistance = sqrDistance;
                closest = candidate;
            }

            Health mainTargetHealth = ResolveMainTargetHealth();
            if (IsValidCombatTarget(mainTargetHealth))
            {
                float mainTargetDistance =
                    (mainTargetHealth.transform.position - transform.position).sqrMagnitude;
                if (mainTargetDistance <= definition.DetectionRange * definition.DetectionRange
                    && mainTargetDistance < closestDistance)
                {
                    closest = mainTargetHealth;
                }
            }

            return closest;
        }

        private static Health ResolveMainTargetHealth()
        {
            Transform mainTarget = GameManager.Instance?.MainTarget;
            if (mainTarget == null)
            {
                return null;
            }

            Health targetHealth = mainTarget.GetComponent<Health>();
            if (targetHealth == null)
            {
                targetHealth = mainTarget.GetComponentInParent<Health>();
            }

            return targetHealth != null
                ? targetHealth
                : mainTarget.GetComponentInChildren<Health>(true);
        }

        private bool CanAttack(Health target)
        {
            if (!IsValidCombatTarget(target))
            {
                return false;
            }

            float sqrDistance = (target.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance > definition.AttackRange * definition.AttackRange)
            {
                LogDebugThrottled(
                    $"[EnemyRangedAttackController] Target {target.name} is detected but out of attack range.");
                return false;
            }

            bool hasLineOfSight = !definition.RequireLineOfSight || HasLineOfSight(target);
            if (!hasLineOfSight)
            {
                LogDebugThrottled(
                    $"[EnemyRangedAttackController] Target {target.name} is blocked by line of sight.");
            }

            return hasLineOfSight;
        }

        private bool HasLineOfSight(Health target)
        {
            Vector3 origin = ResolveProjectileOrigin().position;
            Vector3 aimPoint = ResolveAimPoint(target);
            Vector3 direction = aimPoint - origin;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction / distance,
                distance,
                obstructionLayers,
                QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
            {
                return true;
            }

            Array.Sort(hits, CompareHitsByDistance);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                if (hit.collider.transform.root == transform.root)
                {
                    continue;
                }

                Health hitHealth = ResolveHealth(hit.collider);
                bool hitTarget = hitHealth == target;
                if (!hitTarget)
                {
                    LogDebugThrottled(
                        $"[EnemyRangedAttackController] Line of sight blocked by {hit.collider.name} on layer {hit.collider.gameObject.layer}.");
                }

                return hitTarget;
            }

            return true;
        }

        private void FireProjectile()
        {
            if (definition.ProjectilePrefab == null || currentTarget == null)
            {
                Debug.LogWarning("[EnemyRangedAttackController] Projectile prefab or target is missing.", this);
                return;
            }

            Transform origin = ResolveProjectileOrigin();
            Vector3 aimPoint = ResolveAimPoint(currentTarget);
            Vector3 direction = aimPoint - origin.position;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = transform.forward;
            }

            EnemyRangedProjectile projectile = Instantiate(
                definition.ProjectilePrefab,
                origin.position,
                Quaternion.LookRotation(direction.normalized, Vector3.up));

            projectile.ConfigureVisual(
                definition.ProjectileVisualPrefab,
                definition.ProjectileVisualScale);

            projectile.Launch(
                CalculateLaunchVelocity(direction),
                definition.Damage,
                definition.ProjectileGravity,
                definition.ProjectileLifetime,
                gameObject);

            ProjectileReleased?.Invoke();
            if (logAttacks)
            {
                Debug.Log($"[EnemyRangedAttackController] Projectile fired at {currentTarget.name}.", this);
            }
        }

        private bool IsValidCombatTarget(Health target)
        {
            if (!IsTargetAlive(target) || target.transform.root == transform.root)
            {
                return false;
            }

            if (target.GetComponentInParent<EnemyMovementController>() != null)
            {
                return false;
            }

            if (target.GetComponentInParent<EnemyRangedAttackController>() != null)
            {
                return false;
            }

            return target.GetComponentInParent<TurretTargetingController>() == null;
        }

        private static Health ResolveHealth(Collider targetCollider)
        {
            if (targetCollider == null)
            {
                return null;
            }

            Health resolvedHealth = targetCollider.GetComponentInParent<Health>();
            if (resolvedHealth != null)
            {
                return resolvedHealth;
            }

            resolvedHealth = targetCollider.GetComponentInChildren<Health>(true);
            if (resolvedHealth != null)
            {
                return resolvedHealth;
            }

            Transform root = targetCollider.transform.root;
            return root != null ? root.GetComponentInChildren<Health>(true) : null;
        }

        private static bool IsTargetAlive(Health target)
        {
            return target != null && target.IsAlive;
        }

        private bool IsWithinDetectionRange(Health target)
        {
            return IsTargetAlive(target)
                && (target.transform.position - transform.position).sqrMagnitude
                <= definition.DetectionRange * definition.DetectionRange;
        }

        private Vector3 ResolveAimPoint(Health target)
        {
            Collider targetCollider = target.GetComponentInChildren<Collider>(true);
            Vector3 basePoint = targetCollider != null
                ? targetCollider.bounds.center
                : target.transform.position;

            return basePoint + Vector3.up * definition.TargetHeightOffset;
        }

        private Vector3 CalculateLaunchVelocity(Vector3 displacement)
        {
            if (definition.UseBallisticArc && definition.ProjectileGravity < -0.01f)
            {
                return CalculateBallisticArcVelocity(displacement);
            }

            float distance = displacement.magnitude;
            float travelTime = Mathf.Max(0.05f, distance / definition.ProjectileSpeed);
            Vector3 gravityAcceleration = Vector3.up * definition.ProjectileGravity;
            return displacement / travelTime - 0.5f * gravityAcceleration * travelTime;
        }

        private Vector3 CalculateBallisticArcVelocity(Vector3 displacement)
        {
            float gravityMagnitude = -definition.ProjectileGravity;
            float apexHeightFromOrigin = Mathf.Max(
                definition.BallisticArcHeight,
                displacement.y + definition.BallisticArcHeight);
            float apexHeightFromTarget = apexHeightFromOrigin - displacement.y;

            float riseTime = Mathf.Sqrt(2f * apexHeightFromOrigin / gravityMagnitude);
            float fallTime = Mathf.Sqrt(2f * Mathf.Max(0.01f, apexHeightFromTarget) / gravityMagnitude);
            float totalTime = Mathf.Max(0.1f, riseTime + fallTime);

            Vector3 horizontalDisplacement = new Vector3(displacement.x, 0f, displacement.z);
            Vector3 horizontalVelocity = horizontalDisplacement / totalTime;
            float verticalVelocity = gravityMagnitude * riseTime;
            return horizontalVelocity + Vector3.up * verticalVelocity;
        }

        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private static int CompareHitsByDistance(RaycastHit left, RaycastHit right)
        {
            return left.distance.CompareTo(right.distance);
        }

        private Transform ResolveProjectileOrigin()
        {
            return projectileOrigin != null ? projectileOrigin : transform;
        }

        private void LogDebugThrottled(string message)
        {
            if (!logAttacks || Time.time < nextDebugLogTime)
            {
                return;
            }

            nextDebugLogTime = Time.time + 1f;
            Debug.Log(message, this);
        }

        private void HandleDied(Health deadHealth)
        {
            currentTarget = null;
            movementController?.SetPaused(true);
            ChangeState(EnemyRangedAttackState.Dead);
        }

        private void ChangeState(EnemyRangedAttackState nextState)
        {
            CurrentState = nextState;
        }

        private void ResolveReferences()
        {
            if (movementController == null)
            {
                movementController = GetComponent<EnemyMovementController>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (projectileOrigin == null)
            {
                Transform foundOrigin = transform.Find("ProjectileOrigin");
                projectileOrigin = foundOrigin != null ? foundOrigin : transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (definition == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, definition.DetectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, definition.AttackRange);
        }
    }
}

/*
Unity setup:
1. Add this component to the ranged enemy root.
2. Assign EnemyRangedAttackDefinitionSO and a ProjectileOrigin child transform.
3. Disable the melee BehaviorGraphAgent and EnemyMeleeAttackController on this prefab.
4. Keep EnemyMovementController enabled so the enemy follows its wave route between attacks.
*/
