using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TurretTargetingController : MonoBehaviour
    {
        [SerializeField] private float range = 7f;
        [SerializeField] private float fireInterval = 0.75f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private bool runUpdateLoop = true;

        private Health currentTarget;
        private float nextFireTime;

        private void Update()
        {
            if (runUpdateLoop)
            {
                TickTargeting();
            }
        }

        public void TickTargeting()
        {
            if (!IsTargetValid(currentTarget))
            {
                currentTarget = FindClosestTarget();
            }

            if (currentTarget == null)
            {
                return;
            }

            AimAt(currentTarget.transform.position);

            if (Time.time >= nextFireTime)
            {
                FireAt(currentTarget);
                nextFireTime = Time.time + fireInterval;
            }
        }

        public void SetUpdateLoopEnabled(bool enabled)
        {
            runUpdateLoop = enabled;
        }

        private Health FindClosestTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, range, targetLayers, QueryTriggerInteraction.Ignore);
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

            if (health.GetComponentInParent<EnemyMovementController>() == null)
            {
                return false;
            }

            float distance = Vector3.Distance(transform.position, health.transform.position);
            return distance <= range;
        }

        private void AimAt(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void FireAt(Health target)
        {
            if (projectilePrefab == null)
            {
                return;
            }

            Vector3 origin = muzzle != null ? muzzle.position : transform.position + transform.forward;
            Vector3 aimPoint = target.transform.position + Vector3.up * 0.75f;
            Vector3 direction = (aimPoint - origin).normalized;

            Projectile projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction, Vector3.up));
            projectile.Launch(direction, damage, gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
