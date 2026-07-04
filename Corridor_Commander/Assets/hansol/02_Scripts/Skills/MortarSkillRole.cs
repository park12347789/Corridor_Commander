using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class MortarSkillRole : MonoBehaviour, IBuildableRole, ISkillProvider
    {
        private const float FlightReferenceDistance = 32f;
        private const float MinFlightTimeMultiplier = 0.7f;
        private const float MaxFlightTimeMultiplier = 3f;

        [SerializeField] private MortarSkillRoleDefinitionSO roleDefinition;
        [SerializeField] private Transform fireOrigin;
        [SerializeField] private MuzzleRecoilFeedback recoilFeedback;

        private BuildableObject owner;
        private float nextReadyTime;
        private bool isRegistered;

        public SkillDefinitionSO SkillDefinition => roleDefinition != null ? roleDefinition.SkillDefinition : null;

        public bool IsReady => SkillDefinition != null && Time.time >= nextReadyTime;

        public float CooldownRemaining => SkillDefinition == null
            ? 0f
            : Mathf.Max(0f, nextReadyTime - Time.time);

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Dispose();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Initialize(BuildableObject buildableOwner, BuildContext context)
        {
            owner = buildableOwner;
            Register();
        }

        public void Dispose()
        {
            if (!isRegistered)
            {
                return;
            }

            InstalledSkillRegistry.Current?.Unregister(this);
            isRegistered = false;
        }

        public bool TryUseSkill(SkillUseContext context)
        {
            SkillDefinitionSO skill = SkillDefinition;
            if (skill == null || !IsReady)
            {
                return false;
            }

            Vector3 origin = fireOrigin != null ? fireOrigin.position : transform.position + Vector3.up;
            Fire(origin, context.TargetPoint, skill, context.User);
            float cooldown = ArtifactStatManager.Apply(ArtifactTarget.Mortar, ArtifactStat.Cooldown, skill.Cooldown);
            nextReadyTime = Time.time + Mathf.Max(0.01f, cooldown);
            return true;
        }

        private void Register()
        {
            if (!Application.isPlaying || isRegistered || SkillDefinition == null)
            {
                return;
            }

            InstalledSkillRegistry.Instance.Register(this);
            isRegistered = true;
        }

        private void Fire(Vector3 origin, Vector3 targetPoint, SkillDefinitionSO skill, GameObject user)
        {
            recoilFeedback?.Play();
            SpawnVfx(
                roleDefinition != null ? roleDefinition.MuzzleVfxPrefab : null,
                origin,
                roleDefinition != null ? roleDefinition.MuzzleVfxScale : 1f);
            if (roleDefinition != null)
            {
                RuntimeFeedbackUtility.PlayRandomClip(
                    roleDefinition.FireAudioClips,
                    origin,
                    roleDefinition.FireAudioVolume,
                    "MortarFireSfx");
            }

            MortarProjectile projectilePrefab = roleDefinition != null ? roleDefinition.ProjectilePrefab : null;
            if (projectilePrefab == null)
            {
                ApplyImpact(targetPoint, skill, user);
                return;
            }

            MortarProjectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectile.Initialize(
                owner != null ? owner.gameObject : gameObject,
                user,
                targetPoint,
                skill,
                ResolveFlightTime(origin, targetPoint, roleDefinition.FlightTime),
                roleDefinition.ArcHeight,
                roleDefinition.ProjectileVfxPrefab,
                roleDefinition.ImpactVfxPrefab,
                roleDefinition.ImpactAudioClips,
                roleDefinition.ImpactAudioVolume);
        }

        private static float ResolveFlightTime(Vector3 origin, Vector3 targetPoint, float baseFlightTime)
        {
            Vector3 offset = targetPoint - origin;
            offset.y = 0f;
            float distance = offset.magnitude;
            float multiplier = Mathf.Clamp(
                distance / FlightReferenceDistance,
                MinFlightTimeMultiplier,
                MaxFlightTimeMultiplier);
            return Mathf.Max(0.1f, baseFlightTime * multiplier);
        }

        private void ApplyImpact(Vector3 targetPoint, SkillDefinitionSO skill, GameObject user)
        {
            SpawnVfx(roleDefinition != null ? roleDefinition.ImpactVfxPrefab : null, targetPoint);
            if (roleDefinition != null)
            {
                RuntimeFeedbackUtility.PlayRandomClip(
                    roleDefinition.ImpactAudioClips,
                    targetPoint,
                    roleDefinition.ImpactAudioVolume,
                    "MortarImpactSfx");
            }

            float radius = ArtifactStatManager.Apply(ArtifactTarget.Mortar, ArtifactStat.Range, skill.Radius);
            float damage = ArtifactStatManager.Apply(ArtifactTarget.Mortar, ArtifactStat.Damage, skill.Damage);

            Collider[] colliders = Physics.OverlapSphere(
                targetPoint,
                radius,
                skill.TargetLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null || ShouldSkipDamage(colliders[i].transform, user))
                {
                    continue;
                }

                IDamageable damageable = colliders[i].GetComponentInParent<IDamageable>();
                if (damageable == null)
                {
                    continue;
                }

                Vector3 hitPoint = colliders[i].ClosestPoint(targetPoint);
                damageable.TakeDamage(new DamageInfo(damage, gameObject, hitPoint));
                StatusEffectUtility.ApplyToTarget(damageable, skill.HitEffects, gameObject, hitPoint);
            }
        }

        private bool ShouldSkipDamage(Transform target, GameObject user)
        {
            if (target == null)
            {
                return true;
            }

            if (owner != null && target.IsChildOf(owner.transform))
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

        private static void SpawnVfx(GameObject prefab, Vector3 position, float localScale = 1f)
        {
            if (prefab == null)
            {
                return;
            }

            RuntimeFeedbackUtility.SpawnVfx(prefab, position, 0.85f, localScale);
        }
    }
}
