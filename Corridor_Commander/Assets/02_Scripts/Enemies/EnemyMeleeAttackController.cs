using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class EnemyMeleeAttackController : MonoBehaviour
    {
        [SerializeField] private float attackRange = 2.25f;
        [SerializeField] private float attackInterval = 0.6f;
        [SerializeField] private float damage = 20f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private bool runUpdateLoop = true;

        private EnemyMovementController movementController;
        private Health currentTarget;
        private float nextAttackTime;

        private void Awake()
        {
            movementController = GetComponent<EnemyMovementController>();
        }

        private void Update()
        {
            if (runUpdateLoop)
            {
                TickMeleeAttack();
            }
        }

        public void TickMeleeAttack()
        {
            if (!IsTargetValid(currentTarget))
            {
                currentTarget = FindClosestAttackTarget();
            }

            bool hasTarget = currentTarget != null;
            if (movementController != null)
            {
                movementController.SetPaused(hasTarget);
            }

            if (!hasTarget)
            {
                return;
            }

            FaceTarget(currentTarget.transform.position);

            if (Time.time >= nextAttackTime)
            {
                currentTarget.TakeDamage(new DamageInfo(damage, gameObject, currentTarget.transform.position));
                nextAttackTime = Time.time + attackInterval;
            }
        }

        public void SetUpdateLoopEnabled(bool enabled)
        {
            runUpdateLoop = enabled;
        }

        private Health FindClosestAttackTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, targetLayers, QueryTriggerInteraction.Ignore);
            Health closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (Collider hit in hits)
            {
                Health health = hit.GetComponentInParent<Health>();
                if (!IsTargetValid(health))
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(health.transform.position - transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = health;
                }
            }

            return closestTarget;
        }

        private bool IsTargetValid(Health health)
        {
            if (health == null || !health.IsAlive || health.transform.root == transform.root)
            {
                return false;
            }

            if (health.GetComponentInParent<EnemyMovementController>() != null)
            {
                return false;
            }

            return Vector3.Distance(transform.position, health.transform.position) <= attackRange;
        }

        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
