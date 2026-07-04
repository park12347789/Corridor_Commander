using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class MortarProjectile : MonoBehaviour
    {
        private GameObject source;
        private GameObject user;
        private SkillDefinitionSO skillDefinition;
        private GameObject projectileVfxPrefab;
        private GameObject impactVfxPrefab;
        private GameObject projectileVfxInstance;
        private AudioClip[] impactAudioClips;
        private float impactAudioVolume;
        private Vector3 startPoint;
        private Vector3 targetPoint;
        private float flightTime;
        private float arcHeight;
        private float elapsedTime;
        private bool isInitialized;

        public void Initialize(
            GameObject projectileSource,
            GameObject projectileUser,
            Vector3 impactPoint,
            SkillDefinitionSO skill,
            float configuredFlightTime,
            float configuredArcHeight,
            GameObject configuredProjectileVfxPrefab,
            GameObject configuredImpactVfxPrefab,
            AudioClip[] configuredImpactAudioClips,
            float configuredImpactAudioVolume)
        {
            source = projectileSource;
            user = projectileUser;
            skillDefinition = skill;
            projectileVfxPrefab = configuredProjectileVfxPrefab;
            impactVfxPrefab = configuredImpactVfxPrefab;
            impactAudioClips = configuredImpactAudioClips;
            impactAudioVolume = Mathf.Clamp01(configuredImpactAudioVolume);
            startPoint = transform.position;
            targetPoint = impactPoint;
            flightTime = Mathf.Max(0.1f, configuredFlightTime);
            arcHeight = Mathf.Max(0f, configuredArcHeight);
            elapsedTime = 0f;
            isInitialized = true;
            AttachProjectileVfx();
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / flightTime);
            Vector3 previousPosition = transform.position;
            Vector3 nextPosition = Vector3.Lerp(startPoint, targetPoint, t);
            nextPosition.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            transform.position = nextPosition;
            Vector3 direction = nextPosition - previousPosition;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            if (t >= 1f)
            {
                Impact();
            }
        }

        private void Impact()
        {
            isInitialized = false;

            SpawnVfx(impactVfxPrefab, targetPoint);
            RuntimeFeedbackUtility.PlayRandomClip(impactAudioClips, targetPoint, impactAudioVolume, "MortarImpactSfx");

            if (skillDefinition != null)
            {
                ApplyDamage();
            }

            DestroyRuntimeObject(gameObject);
        }

        private void AttachProjectileVfx()
        {
            if (projectileVfxPrefab == null)
            {
                Debug.LogError("[MortarProjectile] Projectile VFX prefab is not assigned.", this);
                return;
            }

            projectileVfxInstance = Instantiate(projectileVfxPrefab, transform);
            projectileVfxInstance.transform.localPosition = Vector3.zero;
            projectileVfxInstance.transform.localRotation = Quaternion.identity;
            projectileVfxInstance.transform.localScale = Vector3.one;
            ParticleSystem[] particles = projectileVfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Play(true);
            }
        }

        private void ApplyDamage()
        {
            float radius = ArtifactStatManager.Apply(ArtifactTarget.Mortar, ArtifactStat.Range, skillDefinition.Radius);
            float damage = ArtifactStatManager.Apply(ArtifactTarget.Mortar, ArtifactStat.Damage, skillDefinition.Damage);

            Collider[] colliders = Physics.OverlapSphere(
                targetPoint,
                radius,
                skillDefinition.TargetLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];
                if (targetCollider == null || ShouldSkipDamage(targetCollider.transform))
                {
                    continue;
                }

                IDamageable damageable = targetCollider.GetComponentInParent<IDamageable>();
                if (damageable == null)
                {
                    continue;
                }

                Vector3 hitPoint = targetCollider.ClosestPoint(targetPoint);
                damageable.TakeDamage(new DamageInfo(
                    damage,
                    source,
                    hitPoint));
                StatusEffectUtility.ApplyToTarget(damageable, skillDefinition.HitEffects, source, hitPoint);
            }
        }

        private bool ShouldSkipDamage(Transform target)
        {
            if (target == null)
            {
                return true;
            }

            if (source != null && target.IsChildOf(source.transform))
            {
                return true;
            }

            if (user != null && target.IsChildOf(user.transform))
            {
                return true;
            }

            return target.GetComponentInParent<BuildableObject>() != null
                || HasBuildableInstallable(target);
        }

        private static bool HasBuildableInstallable(Transform target)
        {
            MonoBehaviour[] behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IBuildableInstallable)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SpawnVfx(GameObject prefab, Vector3 position)
        {
            if (prefab == null)
            {
                return;
            }

            RuntimeFeedbackUtility.SpawnVfx(prefab, position, 0.85f);
        }

        private static void DestroyRuntimeObject(GameObject target, float delay = 0f)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target, delay);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
