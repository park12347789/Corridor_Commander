using CorridorCommander.PlayerCombat;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class AlliedSquadMemberCombat : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private WeaponItemDefinitionSO weaponDefinition;
        [SerializeField] private Transform muzzle;
        [SerializeField] private ProjectilePool projectilePool;

        [Header("Targeting")]
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private float searchRadius = 16f;
        [SerializeField] private float attackRange = 10f;
        [SerializeField] private float targetRefreshInterval = 0.25f;
        [SerializeField] private float targetLoseDistanceMultiplier = 1.25f;
        [SerializeField] private bool requireEnemyMovementController = true;
        [SerializeField] private bool damageEnemiesOnly = true;
        [SerializeField] private bool requireLineOfSight = false;

        [Header("Fire Timing")]
        [SerializeField] private float fireIntervalMultiplier = 1.5f;
        [SerializeField] private float minimumFireInterval = 0.25f;
        [SerializeField] private float aimReadyDelay = 0.2f;
        [SerializeField] private bool requireAimToleranceBeforeFire = true;
        [SerializeField] private float aimAngleTolerance = 12f;

        [Header("Magazine")]
        [SerializeField] private bool useMagazine = true;
        [SerializeField] private int magazineSizeOverride = 0;
        [SerializeField] private float reloadTimeMultiplier = 1f;

        [Header("Continuous Beam VFX")]
        [SerializeField] private bool ensureContinuousBeamLineRenderer = true;
        [SerializeField] [Min(0.01f)] private float continuousBeamLineWidth = 0.2f;
        [SerializeField] [Min(0.01f)] private float continuousBeamVisualLengthMultiplier = 1f;
        [SerializeField] [Min(0f)] private float continuousBeamEndPadding = 0.1f;

        [Header("Rotation")]
        [SerializeField] private bool rotateTowardTarget = true;
        [SerializeField] private float rotationSpeed = 540f;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRays;
        [SerializeField] private bool logContinuousBeamDebug;

        private Collider currentTargetCollider;
        private IDamageable currentTarget;
        private Health currentTargetHealth;
        private float nextSearchTime;
        private float nextFireTime;
        private float aimStartTime;
        private float reloadEndTime;
        private int currentMagazineAmmo;
        private bool isAiming;
        private bool isReloading;
        private GameObject activeContinuousBeamObject;
        private ContinuousBeamVfxRuntime activeContinuousBeamRuntime;
        private HitscanDefinitionSO activeContinuousHitscanDefinition;

        public WeaponItemDefinitionSO WeaponDefinition => weaponDefinition;
        public Transform Muzzle => muzzle;
        public bool IsAiming => isAiming;
        public bool IsReloading => isReloading;
        public int CurrentMagazineAmmo => currentMagazineAmmo;

        public event System.Action Fired;
        public event System.Action AimingStarted;
        public event System.Action AimingStopped;
        public event System.Action ReloadStarted;
        public event System.Action ReloadCompleted;
        public event System.Action<WeaponItemDefinitionSO> WeaponChanged;
        public event System.Action ContinuousFireStarted;
        public event System.Action ContinuousFireStopped;

        private void Awake()
        {
            ResolveReferences();
            ResetMagazine();
        }

        private void OnEnable()
        {
            if (currentMagazineAmmo <= 0)
            {
                ResetMagazine();
            }
        }

        private void OnDisable()
        {
            StopContinuousBeamVfx();
        }

        private void ResolveReferences()
        {
            if (projectilePool == null)
            {
                projectilePool = FindFirstObjectByType<ProjectilePool>(FindObjectsInactive.Include);
            }
        }

        public void SetWeapon(WeaponItemDefinitionSO nextWeaponDefinition)
        {
            StopContinuousBeamVfx();
            weaponDefinition = nextWeaponDefinition;
            currentTargetCollider = null;
            currentTarget = null;
            currentTargetHealth = null;
            nextFireTime = 0f;
            isReloading = false;
            reloadEndTime = 0f;
            SetAiming(false);
            ResetMagazine();
            WeaponChanged?.Invoke(weaponDefinition);
        }

        public void SetMuzzle(Transform nextMuzzle)
        {
            muzzle = nextMuzzle;
        }

        private void Update()
        {
            if (!TryGetFireDefinition(out WeaponFireDefinitionSO fireDefinition))
            {
                StopContinuousBeamVfx();
                SetAiming(false);
                return;
            }

            TickReload();

            float attackRange = ResolveAttackRange(fireDefinition);
            RefreshTargetIfNeeded(attackRange);

            if (!HasValidTarget(attackRange))
            {
                StopContinuousBeamVfx();
                SetAiming(false);
                return;
            }

            SetAiming(true);

            Vector3 aimPoint = ResolveTargetAimPoint();
            Vector3 fireOrigin = ResolveMuzzlePosition();
            Vector3 direction = aimPoint - fireOrigin;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                StopContinuousBeamVfx();
                return;
            }

            direction.Normalize();

            if (rotateTowardTarget)
            {
                RotateToward(direction);
            }

            if (isReloading || !CanFireAfterAiming(direction))
            {
                StopContinuousBeamVfx();
                return;
            }

            UpdateContinuousBeamVfx(fireDefinition, direction);

            if (Time.time < nextFireTime)
            {
                return;
            }

            if (useMagazine && currentMagazineAmmo <= 0)
            {
                StopContinuousBeamVfx();
                TryStartReload();
                return;
            }

            FireByPattern(fireDefinition, direction);
            ConsumeMagazineRound();
            Fired?.Invoke();
            nextFireTime = Time.time + ResolveFireInterval(fireDefinition);
        }

        private bool TryGetFireDefinition(out WeaponFireDefinitionSO fireDefinition)
        {
            fireDefinition = weaponDefinition != null ? weaponDefinition.fireDefinition : null;
            return fireDefinition != null;
        }

        private void RefreshTargetIfNeeded(float resolvedAttackRange)
        {
            if (Time.time < nextSearchTime && HasValidTarget(resolvedAttackRange))
            {
                return;
            }

            nextSearchTime = Time.time + Mathf.Max(0.05f, targetRefreshInterval);
            AcquireTarget(resolvedAttackRange);
        }

        private void AcquireTarget(float resolvedAttackRange)
        {
            currentTargetCollider = null;
            currentTarget = null;
            currentTargetHealth = null;

            float radius = Mathf.Max(0f, searchRadius);
            float effectiveAttackRange = Mathf.Max(0f, Mathf.Min(radius, resolvedAttackRange));

            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                radius,
                targetLayers,
                QueryTriggerInteraction.Ignore);

            float bestDistanceSqr = float.PositiveInfinity;
            float attackRangeSqr = effectiveAttackRange * effectiveAttackRange;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidateCollider = colliders[i];

                if (candidateCollider == null || candidateCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (requireEnemyMovementController
                    && candidateCollider.GetComponentInParent<EnemyMovementController>() == null)
                {
                    continue;
                }

                IDamageable damageable = candidateCollider.GetComponentInParent<IDamageable>();

                if (damageable == null)
                {
                    continue;
                }

                Health health = candidateCollider.GetComponentInParent<Health>();

                if (health != null && !health.IsAlive)
                {
                    continue;
                }

                Vector3 aimPoint = ResolveColliderAimPoint(candidateCollider);

                if (requireLineOfSight && !HasLineOfSight(aimPoint, candidateCollider))
                {
                    continue;
                }

                Vector3 offset = aimPoint - transform.position;
                offset.y = 0f;

                float distanceSqr = offset.sqrMagnitude;

                // 탐색 반경 안에 있어도, 실제 무기 사거리 밖이면 공격 대상으로 잡지 않음
                if (distanceSqr > attackRangeSqr)
                {
                    continue;
                }

                if (distanceSqr >= bestDistanceSqr)
                {
                    continue;
                }

                bestDistanceSqr = distanceSqr;
                currentTargetCollider = candidateCollider;
                currentTarget = damageable;
                currentTargetHealth = health;
            }
        }

        private bool HasValidTarget(float resolvedAttackRange)
        {
            if (currentTarget == null || currentTargetCollider == null)
            {
                return false;
            }

            if (!currentTargetCollider.enabled || !currentTargetCollider.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (currentTargetHealth != null && !currentTargetHealth.IsAlive)
            {
                return false;
            }

            Vector3 offset = ResolveTargetAimPoint() - transform.position;
            offset.y = 0f;

            float baseLoseDistance = Mathf.Min(searchRadius, resolvedAttackRange);
            float loseDistance = baseLoseDistance * Mathf.Max(1f, targetLoseDistanceMultiplier);

            return offset.sqrMagnitude <= loseDistance * loseDistance;
        }

        private float ResolveAttackRange(WeaponFireDefinitionSO fireDefinition)
        {
            float configuredAttackRange = Mathf.Max(0f, attackRange);
            float resolvedRange;
            if (fireDefinition == null)
            {
                resolvedRange = configuredAttackRange;
                return ArtifactStatManager.Apply(ArtifactTarget.Squad, ArtifactStat.Range, resolvedRange);
            }

            switch (fireDefinition.resolveType)
            {
                case WeaponFireResolveType.Hitscan:
                    float hitscanRange = fireDefinition.hitscanDefinition != null
                        ? fireDefinition.hitscanDefinition.range
                        : configuredAttackRange;
                    resolvedRange = Mathf.Min(configuredAttackRange, Mathf.Max(0f, hitscanRange));
                    break;

                case WeaponFireResolveType.Projectile:
                    resolvedRange = configuredAttackRange;
                    break;

                default:
                    resolvedRange = configuredAttackRange;
                    break;
            }

            return ArtifactStatManager.Apply(ArtifactTarget.Squad, ArtifactStat.Range, resolvedRange);
        }

        private float ResolveFireInterval(WeaponFireDefinitionSO fireDefinition)
        {
            float baseInterval = fireDefinition != null
                ? fireDefinition.triggerMode == WeaponTriggerMode.Continuous
                    ? fireDefinition.damageTickInterval
                    : fireDefinition.fireInterval
                : minimumFireInterval;

            return Mathf.Max(
                minimumFireInterval,
                baseInterval * Mathf.Max(0.01f, fireIntervalMultiplier));
        }

        private void SetAiming(bool value)
        {
            if (isAiming == value)
            {
                return;
            }

            isAiming = value;
            aimStartTime = value ? Time.time : 0f;

            if (value)
            {
                AimingStarted?.Invoke();
                return;
            }

            AimingStopped?.Invoke();
        }

        private bool CanFireAfterAiming(Vector3 direction)
        {
            if (!isAiming)
            {
                return false;
            }

            if (Time.time < aimStartTime + Mathf.Max(0f, aimReadyDelay))
            {
                return false;
            }

            if (!requireAimToleranceBeforeFire)
            {
                return true;
            }

            Vector3 flatForward = transform.forward;
            Vector3 flatDirection = direction;
            flatForward.y = 0f;
            flatDirection.y = 0f;

            if (flatForward.sqrMagnitude <= 0.0001f || flatDirection.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            float angle = Vector3.Angle(flatForward.normalized, flatDirection.normalized);
            return angle <= Mathf.Max(0f, aimAngleTolerance);
        }

        private void ConsumeMagazineRound()
        {
            if (!useMagazine)
            {
                return;
            }

            currentMagazineAmmo = Mathf.Max(0, currentMagazineAmmo - 1);

            if (currentMagazineAmmo <= 0)
            {
                TryStartReload();
            }
        }

        private bool TryStartReload()
        {
            if (!useMagazine || isReloading || weaponDefinition == null)
            {
                return false;
            }

            isReloading = true;
            reloadEndTime = Time.time + ResolveReloadTime();
            nextFireTime = reloadEndTime;
            ReloadStarted?.Invoke();
            return true;
        }

        private void TickReload()
        {
            if (!isReloading || Time.time < reloadEndTime)
            {
                return;
            }

            isReloading = false;
            ResetMagazine();
            ReloadCompleted?.Invoke();
        }

        private void ResetMagazine()
        {
            currentMagazineAmmo = ResolveMagazineSize();
        }

        private int ResolveMagazineSize()
        {
            if (magazineSizeOverride > 0)
            {
                return magazineSizeOverride;
            }

            return weaponDefinition != null
                ? Mathf.Max(1, weaponDefinition.magazineSize)
                : 1;
        }

        private float ResolveReloadTime()
        {
            float baseReloadTime = weaponDefinition != null
                ? weaponDefinition.reloadTime
                : 1f;

            return Mathf.Max(0.01f, baseReloadTime * Mathf.Max(0.01f, reloadTimeMultiplier));
        }

        private Vector3 ResolveTargetAimPoint()
        {
            return currentTargetCollider != null
                ? ResolveColliderAimPoint(currentTargetCollider)
                : transform.position + transform.forward;
        }

        private static Vector3 ResolveColliderAimPoint(Collider targetCollider)
        {
            Bounds bounds = targetCollider.bounds;
            return new Vector3(
                bounds.center.x,
                Mathf.Lerp(bounds.min.y, bounds.max.y, 0.65f),
                bounds.center.z);
        }

        private Vector3 ResolveMuzzlePosition()
        {
            return muzzle != null
                ? muzzle.position
                : transform.position + Vector3.up * 1.35f + transform.forward * 0.35f;
        }

        private Quaternion ResolveMuzzleRotation(Vector3 direction)
        {
            return direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : transform.rotation;
        }

        private bool HasLineOfSight(Vector3 aimPoint, Collider targetCollider)
        {
            Vector3 origin = ResolveMuzzlePosition();
            Vector3 direction = aimPoint - origin;
            float distance = direction.magnitude;
            if (distance <= 0.0001f)
            {
                return false;
            }

            direction /= distance;
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, distance, targetLayers, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.collider == targetCollider || hit.collider.transform.IsChildOf(targetCollider.transform);
        }

        private void RotateToward(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        private void FireByPattern(WeaponFireDefinitionSO fireDefinition, Vector3 baseDirection)
        {
            switch (fireDefinition.firePattern)
            {
                case WeaponFirePattern.ForwardSpread:
                    FireForwardSpread(fireDefinition, baseDirection);
                    break;

                case WeaponFirePattern.RandomCone:
                    FireRandomCone(fireDefinition, baseDirection);
                    break;

                default:
                    FireSingle(fireDefinition, baseDirection);
                    break;
            }
        }

        private void FireSingle(WeaponFireDefinitionSO fireDefinition, Vector3 baseDirection)
        {
            int count = Mathf.Max(1, fireDefinition.projectileCount);
            for (int i = 0; i < count; i++)
            {
                ResolveFire(fireDefinition, baseDirection);
            }
        }

        private void FireForwardSpread(WeaponFireDefinitionSO fireDefinition, Vector3 baseDirection)
        {
            int count = Mathf.Max(1, fireDefinition.projectileCount);
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = GetForwardSpreadDirection(fireDefinition, baseDirection, i, count);
                ResolveFire(fireDefinition, direction);
            }
        }

        private Vector3 GetForwardSpreadDirection(
            WeaponFireDefinitionSO fireDefinition,
            Vector3 baseDirection,
            int index,
            int count)
        {
            float spreadAngle = fireDefinition.horizontalSpreadAngle;
            if (spreadAngle <= 0f)
            {
                return baseDirection;
            }

            float yawOffset;
            if (fireDefinition.useRandomHorizontalSpread)
            {
                yawOffset = Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f);
            }
            else if (count <= 1)
            {
                yawOffset = 0f;
            }
            else
            {
                float t = index / (float)(count - 1);
                yawOffset = Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, t);
            }

            Quaternion yawRotation = Quaternion.AngleAxis(yawOffset, Vector3.up);
            return (yawRotation * baseDirection).normalized;
        }

        private void FireRandomCone(WeaponFireDefinitionSO fireDefinition, Vector3 baseDirection)
        {
            int count = Mathf.Max(1, fireDefinition.projectileCount);
            for (int i = 0; i < count; i++)
            {
                ResolveFire(
                    fireDefinition,
                    GetRandomConeDirection(baseDirection, fireDefinition.coneSpreadAngle));
            }
        }

        private static Vector3 GetRandomConeDirection(Vector3 baseDirection, float coneSpreadAngle)
        {
            if (coneSpreadAngle <= 0f)
            {
                return baseDirection;
            }

            Quaternion baseRotation = Quaternion.LookRotation(baseDirection, Vector3.up);
            Vector2 randomPoint = Random.insideUnitCircle * (coneSpreadAngle * 0.5f);
            Quaternion randomRotation = Quaternion.Euler(-randomPoint.y, randomPoint.x, 0f);
            return (baseRotation * randomRotation * Vector3.forward).normalized;
        }

        private void ResolveFire(WeaponFireDefinitionSO fireDefinition, Vector3 direction)
        {
            switch (fireDefinition.resolveType)
            {
                case WeaponFireResolveType.Projectile:
                    SpawnProjectile(
                        fireDefinition.projectileDefinition,
                        ApplyProjectileLaunchPitch(direction, fireDefinition.projectileLaunchPitchOffset));
                    break;

                case WeaponFireResolveType.Hitscan:
                    FireHitscan(
                        fireDefinition.hitscanDefinition,
                        direction,
                        fireDefinition.triggerMode == WeaponTriggerMode.Continuous);
                    break;
            }
        }

        private static Vector3 ApplyProjectileLaunchPitch(Vector3 direction, float pitchOffset)
        {
            if (Mathf.Approximately(pitchOffset, 0f) || direction.sqrMagnitude <= 0.0001f)
            {
                return direction;
            }

            Vector3 normalizedDirection = direction.normalized;
            Vector3 pitchAxis = Vector3.Cross(Vector3.up, normalizedDirection);

            if (pitchAxis.sqrMagnitude <= 0.0001f)
            {
                return normalizedDirection;
            }

            return (Quaternion.AngleAxis(-pitchOffset, pitchAxis.normalized) * normalizedDirection).normalized;
        }

        private void SpawnProjectile(ProjectileDefinitionSO projectileDefinition, Vector3 direction)
        {
            if (projectileDefinition == null || projectileDefinition.projectilePrefab == null)
            {
                return;
            }

            ResolveReferences();

            Vector3 origin = ResolveMuzzlePosition();
            Quaternion rotation = ResolveMuzzleRotation(direction);
            CorridorCommander.PlayerCombat.Projectile projectile = null;

            if (projectilePool != null)
            {
                projectile = projectilePool.Get(projectileDefinition, origin, rotation);
            }
            else
            {
                GameObject projectileObject = Instantiate(projectileDefinition.projectilePrefab, origin, rotation);
                projectileObject.TryGetComponent(out projectile);
            }

            if (projectile == null)
            {
                return;
            }

            if (drawDebugRays)
            {
                Debug.DrawRay(origin, direction * 2f, Color.red, 0.15f);
            }

            projectile.Initialize(
                projectileDefinition,
                gameObject,
                direction,
                projectilePool != null ? projectilePool.Release : null,
                ArtifactStatManager.Apply(ArtifactTarget.Squad, ArtifactStat.Damage, 1f));
        }

        private void FireHitscan(
            HitscanDefinitionSO hitscanDefinition,
            Vector3 direction,
            bool useContinuousBeamVfx)
        {
            if (hitscanDefinition == null)
            {
                return;
            }

            Vector3 origin = ResolveMuzzlePosition();
            float range = Mathf.Min(hitscanDefinition.range, ResolveAttackRange(weaponDefinition.fireDefinition));
            Vector3 endPoint = origin + direction * range;

            bool hasHit = TryGetHitscanHit(hitscanDefinition, origin, direction, range, out RaycastHit hit);

            if (hasHit)
            {
                endPoint = hit.point;
                ApplyHitscanDamage(hitscanDefinition, hit);
                SpawnHitscanHitVfx(hitscanDefinition, hit);
            }

            if (drawDebugRays || hitscanDefinition.drawDebugRay)
            {
                Debug.DrawLine(origin, endPoint, Color.cyan, 0.1f);
            }

            if (useContinuousBeamVfx)
            {
                SetContinuousBeamVfx(hitscanDefinition, origin, endPoint);
            }
            else
            {
                SpawnHitscanBeamVfx(hitscanDefinition, origin, endPoint);
            }
        }

        private void ApplyHitscanDamage(HitscanDefinitionSO hitscanDefinition, RaycastHit hit)
        {
            if (hitscanDefinition.useSplashDamage)
            {
                ApplyHitscanSplashDamage(hitscanDefinition, hit.point);
                return;
            }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageEnemiesOnly && hit.collider.GetComponentInParent<EnemyMovementController>() == null)
            {
                return;
            }

            float damage = ArtifactStatManager.Apply(
                ArtifactTarget.Squad,
                ArtifactStat.Damage,
                hitscanDefinition.damage);
            damageable?.TakeDamage(new DamageInfo(damage, gameObject, hit.point));
        }

        private void ApplyHitscanSplashDamage(HitscanDefinitionSO hitscanDefinition, Vector3 center)
        {
            Collider[] colliders = Physics.OverlapSphere(
                center,
                hitscanDefinition.splashRadius,
                hitscanDefinition.hitLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];
                if (targetCollider == null || targetCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                IDamageable damageable = targetCollider.GetComponentInParent<IDamageable>();
                if (damageable == null)
                {
                    continue;
                }

                if (damageEnemiesOnly && targetCollider.GetComponentInParent<EnemyMovementController>() == null)
                {
                    continue;
                }

                damageable.TakeDamage(new DamageInfo(
                    ArtifactStatManager.Apply(
                        ArtifactTarget.Squad,
                        ArtifactStat.Damage,
                        hitscanDefinition.splashDamage),
                    gameObject,
                    targetCollider.ClosestPoint(center)));
            }
        }

        private static void SpawnHitscanBeamVfx(
            HitscanDefinitionSO hitscanDefinition,
            Vector3 startPoint,
            Vector3 endPoint)
        {
            if (hitscanDefinition.beamVfxPrefab == null)
            {
                return;
            }

            GameObject beamObject = Instantiate(
                hitscanDefinition.beamVfxPrefab,
                startPoint,
                Quaternion.identity);

            ContinuousBeamVfxRuntime beamRuntime = beamObject.AddComponent<ContinuousBeamVfxRuntime>();
            beamRuntime.Initialize(hitscanDefinition);
            beamRuntime.SetSegment(startPoint, endPoint);

            Destroy(beamObject, hitscanDefinition.beamVisibleTime);
        }

        private void UpdateContinuousBeamVfx(
            WeaponFireDefinitionSO fireDefinition,
            Vector3 direction)
        {
            if (fireDefinition == null
                || fireDefinition.triggerMode != WeaponTriggerMode.Continuous
                || fireDefinition.resolveType != WeaponFireResolveType.Hitscan
                || fireDefinition.hitscanDefinition == null)
            {
                StopContinuousBeamVfx();
                return;
            }

            HitscanDefinitionSO hitscanDefinition = fireDefinition.hitscanDefinition;
            Vector3 origin = ResolveMuzzlePosition();
            float range = Mathf.Min(hitscanDefinition.range, ResolveAttackRange(fireDefinition));
            Vector3 endPoint = ResolveHitscanEndPoint(hitscanDefinition, origin, direction, range);
            SetContinuousBeamVfx(hitscanDefinition, origin, endPoint);
        }

        private void SetContinuousBeamVfx(
            HitscanDefinitionSO hitscanDefinition,
            Vector3 startPoint,
            Vector3 endPoint)
        {
            if (hitscanDefinition.beamVfxPrefab == null)
            {
                StopContinuousBeamVfx();
                return;
            }

            if (activeContinuousBeamObject == null
                || activeContinuousHitscanDefinition != hitscanDefinition)
            {
                StopContinuousBeamVfx();

                activeContinuousBeamObject = Instantiate(
                    hitscanDefinition.beamVfxPrefab,
                    startPoint,
                    Quaternion.identity);
                activeContinuousBeamRuntime = activeContinuousBeamObject.AddComponent<ContinuousBeamVfxRuntime>();
                activeContinuousBeamRuntime.Initialize(
                    hitscanDefinition,
                    ensureContinuousBeamLineRenderer,
                    continuousBeamLineWidth,
                    continuousBeamVisualLengthMultiplier,
                    continuousBeamEndPadding);
                activeContinuousHitscanDefinition = hitscanDefinition;
                ContinuousFireStarted?.Invoke();

                if (logContinuousBeamDebug)
                {
                    float beamDistance = Vector3.Distance(startPoint, endPoint);
                    Debug.Log(
                        $"[AlliedSquadMemberCombat] Continuous beam VFX started. Distance: {beamDistance:0.00}",
                        this);
                }
            }

            activeContinuousBeamRuntime.SetSegment(startPoint, endPoint);
        }

        private Vector3 ResolveHitscanEndPoint(
            HitscanDefinitionSO hitscanDefinition,
            Vector3 origin,
            Vector3 direction,
            float range)
        {
            if (TryGetHitscanHit(hitscanDefinition, origin, direction, range, out RaycastHit hit))
            {
                return hit.point;
            }

            return origin + direction * range;
        }

        private bool TryGetHitscanHit(
            HitscanDefinitionSO hitscanDefinition,
            Vector3 origin,
            Vector3 direction,
            float range,
            out RaycastHit selectedHit)
        {
            selectedHit = default;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                range,
                hitscanDefinition.hitLayers,
                QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            System.Array.Sort(hits, CompareHitsByDistance);

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit candidate = hits[i];
                if (candidate.collider == null || candidate.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (candidate.collider.GetComponentInParent<AlliedSquadMemberCombat>() != null)
                {
                    continue;
                }

                IDamageable damageable = candidate.collider.GetComponentInParent<IDamageable>();
                if (damageEnemiesOnly
                    && damageable != null
                    && candidate.collider.GetComponentInParent<EnemyMovementController>() == null)
                {
                    continue;
                }

                selectedHit = candidate;
                return true;
            }

            return false;
        }

        private static int CompareHitsByDistance(RaycastHit left, RaycastHit right)
        {
            return left.distance.CompareTo(right.distance);
        }

        private void StopContinuousBeamVfx()
        {
            bool wasActive = activeContinuousBeamObject != null;

            if (activeContinuousBeamObject != null)
            {
                Destroy(activeContinuousBeamObject);
            }

            activeContinuousBeamObject = null;
            activeContinuousBeamRuntime = null;
            activeContinuousHitscanDefinition = null;

            if (wasActive)
            {
                ContinuousFireStopped?.Invoke();
            }
        }

        private static void SpawnHitscanHitVfx(HitscanDefinitionSO hitscanDefinition, RaycastHit hit)
        {
            if (hitscanDefinition.hitVfxPrefab == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
            GameObject hitObject = Instantiate(hitscanDefinition.hitVfxPrefab, hit.point, rotation);
            Destroy(hitObject, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            // 적 탐색 반경: 빨간색
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, searchRadius);

            // 현재 무기 공격 가능 거리: 주황색
            if (weaponDefinition != null && weaponDefinition.fireDefinition != null)
            {
                float attackRange = ResolveAttackRange(weaponDefinition.fireDefinition);
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.85f);
                Gizmos.DrawWireSphere(transform.position, attackRange);
            }

            // 현재 타겟 방향: 마젠타
            if (currentTargetCollider != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, ResolveTargetAimPoint());
            }
        }
    }
}




