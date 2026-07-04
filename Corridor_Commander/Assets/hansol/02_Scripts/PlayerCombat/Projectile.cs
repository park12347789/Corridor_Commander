using System;
using UnityEngine;
using CorridorCommander;

namespace CorridorCommander.PlayerCombat
{
    public sealed class Projectile : MonoBehaviour
    {
        private ProjectileDefinitionSO definition;
        private GameObject owner;
        private Vector3 velocity;
        private Vector3 bezierStartPoint;
        private Vector3 bezierControlPoint;
        private Vector3 bezierEndPoint;
        private float remainingLifeTime;
        private float bezierElapsedTime;
        private float bezierTravelDuration;
        private float damageMultiplier = 1f;
        private bool isInitialized;
        private Action<Projectile> releaseCallback;

        public ProjectileDefinitionSO Definition => definition;

        public void Initialize(
            ProjectileDefinitionSO projectileDefinition,
            GameObject projectileOwner,
            Vector3 direction,
            Action<Projectile> onRelease = null,
            float projectileDamageMultiplier = 1f,
            Vector3? projectileTargetPoint = null)
        {
            definition = projectileDefinition;
            owner = projectileOwner;
            releaseCallback = onRelease;
            damageMultiplier = Mathf.Max(0f, projectileDamageMultiplier);

            Vector3 normalizedDirection = direction.normalized;

            velocity = normalizedDirection * definition.speed;
            ConfigureBallisticArc(normalizedDirection, projectileTargetPoint);
            remainingLifeTime = definition.lifeTime;
            ConfigureBezierArc(normalizedDirection, projectileTargetPoint);
            isInitialized = true;

            if (normalizedDirection.sqrMagnitude > 0.0001f)
            {
                ApplyVisualRotation(normalizedDirection);
            }
        }

        private void Update()
        {
            if (!isInitialized || definition == null)
            {
                return;
            }

            if (!TickLifeTime())
            {
                return;
            }

            MoveProjectile();
        }

        private bool TickLifeTime()
        {
            remainingLifeTime -= Time.deltaTime;

            if (remainingLifeTime <= 0f)
            {
                Release();
                return false;
            }

            return isInitialized;
        }

        private void MoveProjectile()
        {
            Vector3 previousPosition = transform.position;

            if (definition.moveType == ProjectileMoveType.BezierArc)
            {
                MoveBezierArc(previousPosition);
                return;
            }

            if (definition.moveType == ProjectileMoveType.GravityArc
                || definition.moveType == ProjectileMoveType.BallisticArc)
            {
                velocity.y += definition.gravity * Time.deltaTime;
            }

            Vector3 nextPosition = previousPosition + velocity * Time.deltaTime;
            Vector3 moveDelta = nextPosition - previousPosition;
            float moveDistance = moveDelta.magnitude;

            if (moveDistance <= 0.0001f)
            {
                return;
            }

            Vector3 moveDirection = moveDelta / moveDistance;

            if (Physics.Raycast(
                    previousPosition,
                    moveDirection,
                    out RaycastHit hit,
                    moveDistance,
                    definition.hitLayers))
            {
                if (IsOwnerOrOwnerChild(hit.collider.transform))
                {
                    transform.position = nextPosition;
                    return;
                }

                transform.position = hit.point;
                HandleHit(hit);
                return;
            }

            transform.position = nextPosition;

            if (velocity.sqrMagnitude > 0.0001f)
            {
                ApplyVisualRotation(velocity.normalized);
            }
        }

        private void ConfigureBallisticArc(Vector3 normalizedDirection, Vector3? projectileTargetPoint)
        {
            if (definition.moveType != ProjectileMoveType.BallisticArc)
            {
                return;
            }

            Vector3 targetPoint = projectileTargetPoint.HasValue
                ? projectileTargetPoint.Value
                : transform.position + normalizedDirection * Mathf.Max(0.01f, definition.ballisticFallbackDistance);

            velocity = BallisticTrajectoryUtility.CalculateVelocityByArcHeight(
                transform.position,
                targetPoint,
                definition.gravity,
                definition.ballisticArcHeight,
                normalizedDirection,
                definition.speed);
        }

        private void ConfigureBezierArc(Vector3 normalizedDirection, Vector3? projectileTargetPoint)
        {
            if (definition.moveType != ProjectileMoveType.BezierArc)
            {
                return;
            }

            bezierStartPoint = transform.position;
            bezierEndPoint = projectileTargetPoint.HasValue
                ? projectileTargetPoint.Value
                : bezierStartPoint + normalizedDirection * Mathf.Max(0.01f, definition.bezierFallbackDistance);

            float distance = Vector3.Distance(bezierStartPoint, bezierEndPoint);
            if (distance <= 0.01f)
            {
                bezierEndPoint = bezierStartPoint + normalizedDirection * Mathf.Max(0.01f, definition.bezierFallbackDistance);
                distance = Vector3.Distance(bezierStartPoint, bezierEndPoint);
            }

            bezierControlPoint = Vector3.Lerp(bezierStartPoint, bezierEndPoint, 0.5f)
                + Vector3.up * Mathf.Max(0f, definition.bezierArcHeight);
            bezierTravelDuration = definition.bezierTravelDuration > 0f
                ? definition.bezierTravelDuration
                : distance / Mathf.Max(0.01f, definition.speed);
            bezierElapsedTime = 0f;
        }

        private void MoveBezierArc(Vector3 previousPosition)
        {
            bezierElapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(bezierElapsedTime / Mathf.Max(0.01f, bezierTravelDuration));
            Vector3 nextPosition = EvaluateQuadraticBezier(bezierStartPoint, bezierControlPoint, bezierEndPoint, t);
            Vector3 moveDelta = nextPosition - previousPosition;
            float moveDistance = moveDelta.magnitude;

            if (moveDistance > 0.0001f)
            {
                Vector3 moveDirection = moveDelta / moveDistance;

                if (Physics.Raycast(
                        previousPosition,
                        moveDirection,
                        out RaycastHit hit,
                        moveDistance,
                        definition.hitLayers))
                {
                    if (IsOwnerOrOwnerChild(hit.collider.transform))
                    {
                        transform.position = nextPosition;
                        return;
                    }

                    transform.position = hit.point;
                    HandleHit(hit);
                    return;
                }

                transform.position = nextPosition;
                ApplyVisualRotation(moveDirection);
            }

            if (t >= 1f)
            {
                Release();
            }
        }

        private static Vector3 EvaluateQuadraticBezier(
            Vector3 startPoint,
            Vector3 controlPoint,
            Vector3 endPoint,
            float t)
        {
            float inverseT = 1f - t;
            return inverseT * inverseT * startPoint
                + 2f * inverseT * t * controlPoint
                + t * t * endPoint;
        }

        private void ApplyVisualRotation(Vector3 forwardDirection)
        {
            if (forwardDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion forwardRotation = Quaternion.LookRotation(forwardDirection.normalized, Vector3.up);
            Quaternion visualOffset = definition != null
                ? Quaternion.Euler(definition.visualRotationOffset)
                : Quaternion.identity;
            transform.rotation = forwardRotation * visualOffset;
        }

        private void HandleHit(RaycastHit hit)
        {
            if (definition.useSplashDamage)
            {
                ApplySplashDamage(hit.point);
            }
            else
            {
                ApplyDirectDamage(hit);
            }

            SpawnHitVfx(hit);
            SpawnHitAudio(hit.point);
            Release();
        }

        private void SpawnHitVfx(RaycastHit hit)
        {
            if (definition == null || definition.hitVfxPrefab == null)
            {
                return;
            }

            Quaternion rotation = hit.normal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(hit.normal, Vector3.up)
                : Quaternion.identity;

            GameObject hitVfx = Instantiate(definition.hitVfxPrefab, hit.point, rotation);
            Destroy(hitVfx, Mathf.Max(0.05f, definition.hitVfxLifeTime));
        }

        private void SpawnHitAudio(Vector3 position)
        {
            if (definition == null || definition.hitAudioClips == null || definition.hitAudioClips.Length == 0)
            {
                return;
            }

            AudioClip clip = GetRandomHitAudioClip();
            if (clip == null)
            {
                return;
            }

            GameObject audioObject = new GameObject($"{name}_HitAudio");
            audioObject.transform.position = position;

            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.clip = clip;
            audioSource.volume = definition.hitAudioVolume;
            audioSource.pitch = GetRandomHitAudioPitch();
            audioSource.spatialBlend = definition.hitAudioSpatialBlend;
            audioSource.Play();

            float clipDuration = clip.length / Mathf.Max(0.01f, Mathf.Abs(audioSource.pitch));
            float lifeTime = definition.hitAudioLifeTime > 0f
                ? definition.hitAudioLifeTime
                : clipDuration;
            Destroy(audioObject, Mathf.Max(0.05f, lifeTime));
        }

        private AudioClip GetRandomHitAudioClip()
        {
            int index = UnityEngine.Random.Range(0, definition.hitAudioClips.Length);
            return definition.hitAudioClips[index];
        }

        private float GetRandomHitAudioPitch()
        {
            float minPitch = Mathf.Min(definition.hitAudioPitchRange.x, definition.hitAudioPitchRange.y);
            float maxPitch = Mathf.Max(definition.hitAudioPitchRange.x, definition.hitAudioPitchRange.y);
            return UnityEngine.Random.Range(minPitch, maxPitch);
        }

        private void ApplyDirectDamage(RaycastHit hit)
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                return;
            }

            DamageInfo damageInfo = new DamageInfo(
                ApplyDamageMultiplier(definition.damage),
                owner != null ? owner : gameObject,
                hit.point
            );

            damageable.TakeDamage(damageInfo);
        }

        private void ApplySplashDamage(Vector3 center)
        {
            Collider[] colliders = Physics.OverlapSphere(
                center,
                definition.splashRadius,
                definition.hitLayers
            );

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];

                if (IsOwnerOrOwnerChild(targetCollider.transform))
                {
                    continue;
                }

                IDamageable damageable = targetCollider.GetComponentInParent<IDamageable>();

                if (damageable == null)
                {
                    continue;
                }

                DamageInfo damageInfo = new DamageInfo(
                    ApplyDamageMultiplier(definition.splashDamage),
                    owner != null ? owner : gameObject,
                    targetCollider.ClosestPoint(center)
                );

                damageable.TakeDamage(damageInfo);
            }
        }

        private bool IsOwnerOrOwnerChild(Transform target)
        {
            if (owner == null || target == null)
            {
                return false;
            }

            return target == owner.transform || target.IsChildOf(owner.transform);
        }

        private float ApplyDamageMultiplier(float baseDamage)
        {
            return baseDamage * damageMultiplier;
        }

        private void Release()
        {
            isInitialized = false;

            if (releaseCallback != null)
            {
                releaseCallback.Invoke(this);
                return;
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            if (definition == null || !definition.useSplashDamage)
            {
                return;
            }

            Gizmos.DrawWireSphere(transform.position, definition.splashRadius);
        }
    }
}
