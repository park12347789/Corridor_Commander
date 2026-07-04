using System;
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
        private IDamageTarget currentTarget;
        private float nextAttackTime;

        public event Action AttackPerformed;

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

            FaceTarget(currentTarget.Transform.position);

            if (Time.time >= nextAttackTime)
            {
                currentTarget.TakeDamage(new DamageInfo(damage, gameObject, currentTarget.Transform.position));
                AttackPerformed?.Invoke();
                nextAttackTime = Time.time + attackInterval;
            }
        }

        public void SetUpdateLoopEnabled(bool enabled)
        {
            runUpdateLoop = enabled;
        }

        private IDamageTarget FindClosestAttackTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, targetLayers, QueryTriggerInteraction.Ignore);
            IDamageTarget closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (Collider hit in hits)
            {
                IDamageTarget target = ResolveDamageTarget(hit);
                if (!IsTargetValid(target))
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(target.Transform.position - transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        private static IDamageTarget ResolveDamageTarget(Collider hit)
        {
            if (hit == null)
            {
                return null;
            }

            IDamageTarget target = ResolveDamageTarget(hit.GetComponentsInParent<MonoBehaviour>());
            if (target != null)
            {
                return target;
            }

            return ResolveDamageTarget(hit.transform.root.GetComponentsInChildren<MonoBehaviour>());
        }

        private static IDamageTarget ResolveDamageTarget(MonoBehaviour[] behaviours)
        {
            if (behaviours == null)
            {
                return null;
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IDamageTarget target)
                {
                    return target;
                }
            }

            return null;
        }

        private bool IsTargetValid(IDamageTarget target)
        {
            if (target == null || !target.IsAlive || target.Transform.root == transform.root)
            {
                return false;
            }

            if (target.Transform.GetComponentInParent<EnemyMovementController>() != null)
            {
                return false;
            }

            if (target.Transform.GetComponentInParent<TurretTargetingController>() != null)
            {
                return false;
            }

            return Vector3.Distance(transform.position, target.Transform.position) <= attackRange;
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
