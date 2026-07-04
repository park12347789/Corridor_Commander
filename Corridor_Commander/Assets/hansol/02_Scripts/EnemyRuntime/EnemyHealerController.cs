using System;
using UnityEngine;

namespace CorridorCommander.Enemy
{
    public enum EnemyHealerState
    {
        Moving,
        Casting,
        Cooldown,
        Dead
    }

    [DisallowMultipleComponent]
    public sealed class EnemyHealerController : MonoBehaviour
    {
        private const int TargetBufferSize = 64;

        [Header("Configuration")]
        [SerializeField] private EnemyHealerDefinitionSO definition;

        [Header("References")]
        [SerializeField] private EnemyMovementController movementController;
        [SerializeField] private Health health;
        [SerializeField] private Transform healingOrigin;

        [Header("Targeting")]
        [SerializeField] private LayerMask enemyLayers = 1 << 7;
        [SerializeField] private LayerMask obstructionLayers = ~(1 << 7);

        [Header("Debug")]
        [SerializeField] private bool logHealing;

        private readonly Collider[] targetBuffer = new Collider[TargetBufferSize];
        private Health currentTarget;
        private float nextTargetRefreshTime;
        private float stateEndTime;

        public EnemyHealerState CurrentState { get; private set; } = EnemyHealerState.Moving;
        public Health CurrentTarget => currentTarget;

        public event Action HealCastStarted;
        public event Action<Health, float> HealApplied;

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
                ? EnemyHealerState.Dead
                : EnemyHealerState.Moving);
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
            if (definition == null || CurrentState == EnemyHealerState.Dead)
            {
                return;
            }

            switch (CurrentState)
            {
                case EnemyHealerState.Moving:
                    TickMoving();
                    break;
                case EnemyHealerState.Casting:
                    TickCasting();
                    break;
                case EnemyHealerState.Cooldown:
                    TickCooldown();
                    break;
            }
        }

        private void TickMoving()
        {
            movementController?.SetPaused(false);
            RefreshTargetIfNeeded();
            if (!CanHeal(currentTarget))
            {
                return;
            }

            movementController?.SetPaused(true);
            FaceTarget(currentTarget.transform.position);
            stateEndTime = Time.time + definition.CastDelay;
            ChangeState(EnemyHealerState.Casting);
            HealCastStarted?.Invoke();
        }

        private void TickCasting()
        {
            if (!CanHeal(currentTarget))
            {
                ClearTargetAndResumeMoving();
                return;
            }

            movementController?.SetPaused(true);
            FaceTarget(currentTarget.transform.position);
            if (Time.time < stateEndTime)
            {
                return;
            }

            ApplyHealing();
            float cooldownDuration = Mathf.Max(
                0.05f,
                definition.HealingInterval - definition.CastDelay);
            stateEndTime = Time.time + cooldownDuration;
            ChangeState(EnemyHealerState.Cooldown);
        }

        private void TickCooldown()
        {
            if (CanHeal(currentTarget))
            {
                movementController?.SetPaused(true);
                FaceTarget(currentTarget.transform.position);
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

            ChangeState(EnemyHealerState.Moving);
            nextTargetRefreshTime = 0f;
        }

        private void RefreshTargetIfNeeded()
        {
            if (CanHeal(currentTarget) && Time.time < nextTargetRefreshTime)
            {
                return;
            }

            nextTargetRefreshTime = Time.time + definition.TargetRefreshInterval;
            currentTarget = FindLowestHealthTarget();
        }

        private Health FindLowestHealthTarget()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                definition.DetectionRadius,
                targetBuffer,
                enemyLayers,
                QueryTriggerInteraction.Ignore);

            Health bestTarget = null;
            float lowestHealthRatio = float.MaxValue;
            float closestSqrDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider candidateCollider = targetBuffer[i];
                Health candidate = candidateCollider != null
                    ? candidateCollider.GetComponentInParent<Health>()
                    : null;

                if (!IsDamagedAlliedZombie(candidate))
                {
                    continue;
                }

                float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance > definition.HealingRange * definition.HealingRange)
                {
                    continue;
                }

                float healthRatio = candidate.MaxHitPoints > 0f
                    ? candidate.CurrentHitPoints / candidate.MaxHitPoints
                    : 1f;

                bool hasLowerHealth = healthRatio < lowestHealthRatio - 0.0001f;
                bool isCloserTie = Mathf.Approximately(healthRatio, lowestHealthRatio)
                    && sqrDistance < closestSqrDistance;
                if (!hasLowerHealth && !isCloserTie)
                {
                    continue;
                }

                bestTarget = candidate;
                lowestHealthRatio = healthRatio;
                closestSqrDistance = sqrDistance;
            }

            return bestTarget;
        }

        private bool CanHeal(Health target)
        {
            if (!IsDamagedAlliedZombie(target))
            {
                return false;
            }

            float sqrDistance = (target.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance > definition.HealingRange * definition.HealingRange)
            {
                return false;
            }

            return !definition.RequireLineOfSight || HasLineOfSight(target);
        }

        private bool IsDamagedAlliedZombie(Health target)
        {
            return target != null
                && target != health
                && target.IsAlive
                && target.CurrentHitPoints < target.MaxHitPoints
                && target.GetComponentInParent<EnemyMovementController>() != null;
        }

        private bool HasLineOfSight(Health target)
        {
            Vector3 origin = healingOrigin != null
                ? healingOrigin.position
                : transform.position + Vector3.up;
            Collider targetCollider = target.GetComponentInChildren<Collider>(true);
            Vector3 targetPoint = targetCollider != null
                ? targetCollider.bounds.center
                : target.transform.position + Vector3.up;
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;

            if (distance <= 0.001f || !Physics.Raycast(
                    origin,
                    direction / distance,
                    out RaycastHit hit,
                    distance,
                    obstructionLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.collider.GetComponentInParent<Health>() == target;
        }

        private void ApplyHealing()
        {
            if (currentTarget == null)
            {
                return;
            }

            float previousHitPoints = currentTarget.CurrentHitPoints;
            if (!currentTarget.Repair(definition.HealingAmount))
            {
                return;
            }

            float restoredAmount = currentTarget.CurrentHitPoints - previousHitPoints;
            HealApplied?.Invoke(currentTarget, restoredAmount);

            if (logHealing)
            {
                Debug.Log(
                    $"[EnemyHealerController] Healed {currentTarget.name} by {restoredAmount:0.##}.",
                    this);
            }
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

        private void ClearTargetAndResumeMoving()
        {
            currentTarget = null;
            movementController?.SetPaused(false);
            ChangeState(EnemyHealerState.Moving);
            nextTargetRefreshTime = 0f;
        }

        private void HandleDied(Health deadHealth)
        {
            currentTarget = null;
            movementController?.SetPaused(true);
            ChangeState(EnemyHealerState.Dead);
        }

        private void ChangeState(EnemyHealerState nextState)
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

            if (healingOrigin == null)
            {
                healingOrigin = transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (definition == null)
            {
                return;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, definition.HealingRange);
        }
    }
}

/*
Unity setup:
1. Add EnemyHealerController to the healer enemy root.
2. Assign EnemyHealerDefinitionSO, EnemyMovementController, and Health.
3. Disable EnemyMeleeAttackController and the melee BehaviorGraphAgent.
4. Keep EnemyMovementController enabled so the healer follows the normal goal route.
*/
