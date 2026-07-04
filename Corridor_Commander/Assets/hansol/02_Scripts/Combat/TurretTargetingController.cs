using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TurretTargetingController : MonoBehaviour
    {
        [SerializeField] private float range = 7f;
        [SerializeField] private float fireInterval = 0.75f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private TurretAttackMode attackMode = TurretAttackMode.PulseHitscan;
        [SerializeField, Min(0f)] private float attackWindupTime;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private ProjectileFirePoint firePoint;
        [SerializeField] private StatusEffectDefinitionSO[] hitEffects;
        [SerializeField] private bool runUpdateLoop = true;

        private Vector3 aimUpAxis = Vector3.up;
        private Health currentTarget;
        private Health sustainedBeamTarget;
        private float nextFireTime;

        public float CurrentRange => range;
        public float CurrentFireInterval => fireInterval;
        public float CurrentDamage => damage;

        private void Reset()
        {
            ResolveFirePointReference();
        }

        private void Awake()
        {
            ResolveFirePointReference();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveFirePointReference();
        }
#endif

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
                sustainedBeamTarget = null;
                currentTarget = FindClosestTarget();
            }

            if (currentTarget == null)
            {
                sustainedBeamTarget = null;
                return;
            }

            AimAt(currentTarget.transform.position);

            if (attackMode == TurretAttackMode.SustainedBeam)
            {
                TickSustainedBeam(currentTarget);
                return;
            }

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

        public void SetAimUpAxis(Vector3 upAxis)
        {
            aimUpAxis = upAxis.sqrMagnitude > 0.0001f
                ? upAxis.normalized
                : Vector3.up;
        }

        public void Configure(float configuredRange, float configuredFireInterval, float configuredDamage)
        {
            Configure(configuredRange, configuredFireInterval, configuredDamage, null);
        }

        public void Configure(
            float configuredRange,
            float configuredFireInterval,
            float configuredDamage,
            StatusEffectDefinitionSO[] configuredHitEffects)
        {
            Configure(
                configuredRange,
                configuredFireInterval,
                configuredDamage,
                configuredHitEffects,
                TurretAttackMode.PulseHitscan,
                0f);
        }

        public void Configure(
            float configuredRange,
            float configuredFireInterval,
            float configuredDamage,
            StatusEffectDefinitionSO[] configuredHitEffects,
            TurretAttackMode configuredAttackMode,
            float configuredAttackWindupTime)
        {
            range = Mathf.Max(0f, configuredRange);
            fireInterval = Mathf.Max(0.01f, configuredFireInterval);
            damage = Mathf.Max(0f, configuredDamage);
            hitEffects = configuredHitEffects;
            attackMode = configuredAttackMode;
            attackWindupTime = Mathf.Max(0f, configuredAttackWindupTime);
            currentTarget = null;
            sustainedBeamTarget = null;
            nextFireTime = Time.time + (attackMode == TurretAttackMode.SustainedBeam
                ? attackWindupTime
                : fireInterval);
        }

        public void Configure(TurretAttackDefinitionSO attackDefinition, int upgradeLevel)
        {
            if (attackDefinition == null)
            {
                return;
            }

            Configure(
                attackDefinition.GetRange(upgradeLevel),
                attackDefinition.GetFireInterval(upgradeLevel),
                attackDefinition.GetDamage(upgradeLevel),
                attackDefinition.HitEffects,
                attackDefinition.AttackMode,
                attackDefinition.AttackWindupTime);
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
            Vector3 upAxis = ResolveAimUpAxis();
            Vector3 direction = Vector3.ProjectOnPlane(targetPosition - transform.position, upAxis);

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, upAxis);
        }

        private void FireAt(Health target)
        {
            if (firePoint == null)
            {
                return;
            }

            Vector3 aimPoint = target.transform.position + Vector3.up * 0.75f;
            Vector3 direction = (aimPoint - firePoint.Position).normalized;
            Vector3 hitPoint = ResolveHitPoint(target, firePoint.Position, aimPoint);
            firePoint.FireHitscan(target, direction, hitPoint, damage, gameObject, hitEffects);
        }

        private void TickSustainedBeam(Health target)
        {
            if (target != sustainedBeamTarget)
            {
                sustainedBeamTarget = target;
                nextFireTime = Time.time + attackWindupTime;
                firePoint?.PlayChargeAudio();
            }

            if (Time.time < nextFireTime)
            {
                return;
            }

            FireAt(target);
            nextFireTime = Time.time + fireInterval;
        }

        private void ResolveFirePointReference()
        {
            if (firePoint != null)
            {
                return;
            }

            firePoint = GetComponentInChildren<ProjectileFirePoint>(true);
        }

        private static Vector3 ResolveHitPoint(Health target, Vector3 origin, Vector3 fallback)
        {
            Collider targetCollider = target.GetComponentInChildren<Collider>();
            return targetCollider != null ? targetCollider.ClosestPoint(origin) : fallback;
        }

        private Vector3 ResolveAimUpAxis()
        {
            return aimUpAxis.sqrMagnitude > 0.0001f
                ? aimUpAxis.normalized
                : Vector3.up;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
