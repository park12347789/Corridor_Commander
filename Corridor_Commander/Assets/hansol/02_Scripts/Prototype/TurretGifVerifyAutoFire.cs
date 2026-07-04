using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TurretGifVerifyAutoFire : MonoBehaviour
    {
        [SerializeField] private ProjectileFirePoint firePoint;
        [SerializeField] private Transform targetPoint;
        [SerializeField] private GifVerifyDamageTarget damageTarget;
        [SerializeField, Min(0.05f)] private float fireInterval = 0.8f;
        [SerializeField, Min(0f)] private float preFireDelay;
        [SerializeField, Min(0f)] private float phaseOffset;
        [SerializeField, Min(0f)] private float damage = 1f;

        private float nextFireTime;
        private float delayedFireTime;
        private bool waitingForDelayedFire;

        private void Reset()
        {
            firePoint = GetComponentInChildren<ProjectileFirePoint>(true);
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            nextFireTime = Time.time + phaseOffset;
        }

        private void Update()
        {
            if (firePoint == null || targetPoint == null || damageTarget == null)
            {
                return;
            }

            if (waitingForDelayedFire)
            {
                if (Time.time < delayedFireTime)
                {
                    return;
                }

                FireNow();
                waitingForDelayedFire = false;
                nextFireTime = Time.time + fireInterval;
                return;
            }

            if (Time.time < nextFireTime)
            {
                return;
            }

            if (preFireDelay > 0f)
            {
                waitingForDelayedFire = true;
                delayedFireTime = Time.time + preFireDelay;
                return;
            }

            FireNow();
            nextFireTime = Time.time + fireInterval;
        }

        private void FireNow()
        {
            Vector3 origin = firePoint.Position;
            Vector3 hitPoint = targetPoint.position;
            Vector3 direction = hitPoint - origin;
            if (direction.sqrMagnitude > 0.0001f)
            {
                firePoint.FireHitscan(damageTarget, direction.normalized, hitPoint, damage, gameObject);
            }
        }

        private void ResolveReferences()
        {
            if (firePoint == null)
            {
                firePoint = GetComponentInChildren<ProjectileFirePoint>(true);
            }

            if (damageTarget == null && targetPoint != null)
            {
                damageTarget = targetPoint.GetComponentInParent<GifVerifyDamageTarget>();
            }
        }
    }
}
