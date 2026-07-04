using UnityEngine;
using System.Collections;
using CorridorCommander;
using CorridorCommander.PlayerControl;

namespace CorridorCommander.PlayerCombat
{
    public sealed class PlayerProjectileLauncher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private GameObject owner;
        [SerializeField] private Transform muzzle;
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private Animator animator;

        [Header("Muzzle Auto Binding")]
        [SerializeField] private Transform weaponVisualRoot;
        [SerializeField] private string muzzleName = "Muzzle";
        [SerializeField] private bool autoFindMuzzleOnAwake = true;
        [SerializeField] private bool logMuzzleBinding = true;

        [Header("Weapon Runtime")]
        [SerializeField] private PlayerWeaponRuntime weaponRuntime;
        [SerializeField] private PlayerStatModifier statModifier;

        [Header("Aim")]
        [SerializeField] private bool ignoreOwnerWhenResolvingAimPoint = true;
        [SerializeField] private QueryTriggerInteraction aimTriggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("First Shot Animation Delay")]
        [SerializeField] private bool delayUnaimedFirstShot = true;
        [SerializeField] private string upperBodyLayerName = "UpperBody";
        [SerializeField] private string ranged1HAimingStateName = "Ranged_1H_Aiming";
        [SerializeField] private string ranged1HShootingStateName = "Ranged_1H_Shooting";
        [SerializeField] private string ranged1HFireRecoverStateName = "Ranged_1H_FireRecover";
        [SerializeField] private string ranged2HAimingStateName = "Ranged_2H_Aiming";
        [SerializeField] private string ranged2HShootingStateName = "Ranged_2H_Shooting";
        [SerializeField] private string ranged2HFireRecoverStateName = "Ranged_2H_FireRecover";

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay = true;

        private float lastFireTime;
        private float lastContinuousTickTime;
        private float delayedFireTime;
        private WeaponFireDefinitionSO delayedFireDefinition;

        private bool firePressed;
        private bool fireHeld;
        private bool hasDelayedFire;
        private GameObject activeContinuousBeamObject;
        private ContinuousBeamVfxRuntime activeContinuousBeamRuntime;
        private HitscanDefinitionSO activeContinuousHitscanDefinition;

        public event System.Action FireAnimationRequested;
        public event System.Action AutomaticFireStopped;
        public event System.Action Fired;

        private void Awake()
        {
            ResolveOptionalReferences();

            if (autoFindMuzzleOnAwake)
            {
                RefreshMuzzleReference();
            }
        }

        private void Update()
        {
            ProcessDelayedFire();
            HandleFireInput();
            UpdateContinuousBeamVisual();

            firePressed = false;
        }

        private void OnDisable()
        {
            StopContinuousBeamVfx();
        }

        public void RequestFirePressed()
        {
            firePressed = true;
        }

        public void SetFireHeld(bool value)
        {
            if (fireHeld && !value)
            {
                StopAutomaticFire();
            }

            fireHeld = value;
        }

        public void ClearFireInput()
        {
            firePressed = false;

            if (fireHeld)
            {
                StopAutomaticFire();
            }

            fireHeld = false;
        }

        public void SetMuzzle(Transform nextMuzzle)
        {
            if (nextMuzzle == null)
            {
                return;
            }

            muzzle = nextMuzzle;

            if (logMuzzleBinding)
            {
                Debug.Log($"[PlayerProjectileLauncher] Muzzle Bound: {muzzle.name}");
            }
        }

        public bool SetWeaponVisualRoot(Transform nextWeaponVisualRoot)
        {
            weaponVisualRoot = nextWeaponVisualRoot;
            return RefreshMuzzleReference();
        }

        public void SetMuzzleName(string nextMuzzleName)
        {
            if (string.IsNullOrWhiteSpace(nextMuzzleName))
            {
                return;
            }

            muzzleName = nextMuzzleName;
        }

        public bool RefreshMuzzleReference()
        {
            Transform searchRoot = weaponVisualRoot != null ? weaponVisualRoot : transform;
            Transform resolvedMuzzle = FindChildByName(searchRoot, muzzleName);

            if (resolvedMuzzle == null)
            {
                return false;
            }

            SetMuzzle(resolvedMuzzle);
            return true;
        }

        private void HandleFireInput()
        {
            if (!firePressed && !fireHeld)
            {
                return;
            }

            WeaponFireDefinitionSO fireDefinition = GetFireDefinition();

            if (fireDefinition == null)
            {
                return;
            }

            switch (fireDefinition.triggerMode)
            {
                case WeaponTriggerMode.SemiAuto:
                    HandleSemiAutoInput(fireDefinition);
                    break;

                case WeaponTriggerMode.FullAuto:
                    HandleFullAutoInput(fireDefinition);
                    break;

                case WeaponTriggerMode.Continuous:
                    HandleContinuousInput(fireDefinition);
                    break;
            }
        }

        private void HandleSemiAutoInput(WeaponFireDefinitionSO fireDefinition)
        {
            if (!firePressed)
            {
                return;
            }

            if (Time.time < lastFireTime + fireDefinition.fireInterval)
            {
                return;
            }

            if (!TryFire(fireDefinition))
            {
                return;
            }

            lastFireTime = Time.time;
        }

        private void HandleFullAutoInput(WeaponFireDefinitionSO fireDefinition)
        {
            if (!fireHeld)
            {
                return;
            }

            if (Time.time < lastFireTime + fireDefinition.fireInterval)
            {
                return;
            }

            if (!TryFire(fireDefinition))
            {
                return;
            }

            lastFireTime = Time.time;
        }

        private void HandleContinuousInput(WeaponFireDefinitionSO fireDefinition)
        {
            if (!fireHeld)
            {
                return;
            }

            float tickInterval = Mathf.Max(0.01f, fireDefinition.damageTickInterval);

            if (Time.time < lastContinuousTickTime + tickInterval)
            {
                return;
            }

            if (!TryFire(fireDefinition))
            {
                StopContinuousBeamVfx();
                return;
            }

            lastContinuousTickTime = Time.time;
        }

        private WeaponFireDefinitionSO GetFireDefinition()
        {
            if (weaponRuntime == null)
            {
                Debug.LogWarning("[PlayerProjectileLauncher] Weapon Runtime이 연결되지 않았습니다.");
                return null;
            }

            if (weaponRuntime.CurrentWeapon == null)
            {
                Debug.LogWarning("[PlayerProjectileLauncher] 현재 장착 무기가 없습니다.");
                return null;
            }

            if (weaponRuntime.CurrentFireDefinition == null)
            {
                Debug.LogWarning("[PlayerProjectileLauncher] 현재 무기에 Fire Definition이 없습니다.");
                return null;
            }

            return weaponRuntime.CurrentFireDefinition;
        }

        private bool TryFire(WeaponFireDefinitionSO fireDefinition)
        {
            if (!TryValidateFireDefinition(fireDefinition))
            {
                return false;
            }

            if (hasDelayedFire)
            {
                return false;
            }

            if (TryScheduleUnaimedFirstShot(fireDefinition))
            {
                return true;
            }

            return ExecuteFire(fireDefinition, true);
        }

        private bool ExecuteFire(
            WeaponFireDefinitionSO fireDefinition,
            bool requestFireAnimation)
        {
            float aimDistance = GetAimDistance(fireDefinition);
            LayerMask hitLayers = GetAimHitLayers(fireDefinition);

            Vector3 aimPoint = GetCameraAimPoint(aimDistance, hitLayers);
            Vector3 baseDirection = aimPoint - muzzle.position;

            if (baseDirection.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            baseDirection.Normalize();

            if (weaponRuntime == null || !weaponRuntime.TryConsumeOneRound())
            {
                return false;
            }

            if (requestFireAnimation)
            {
                FireAnimationRequested?.Invoke();
            }

            FireByPattern(fireDefinition, baseDirection);
            Fired?.Invoke();

            return true;
        }

        private bool TryScheduleUnaimedFirstShot(WeaponFireDefinitionSO fireDefinition)
        {
            if (!delayUnaimedFirstShot || hasDelayedFire)
            {
                return false;
            }

            WeaponItemDefinitionSO weaponDefinition = weaponRuntime != null
                ? weaponRuntime.CurrentWeapon
                : null;

            if (weaponDefinition == null)
            {
                return false;
            }

            float delay = Mathf.Max(0f, weaponDefinition.UnaimedFirstShotDelay);

            if (delay <= 0f || IsCurrentWeaponUpperBodyReady(weaponDefinition.AnimationType))
            {
                return false;
            }

            FireAnimationRequested?.Invoke();
            delayedFireDefinition = fireDefinition;
            delayedFireTime = Time.time + delay;
            hasDelayedFire = true;

            Debug.Log(
                $"[PlayerProjectileLauncher] Delayed unaimed first shot: {weaponDefinition.displayName}, Delay: {delay:0.###}s",
                this);

            return true;
        }

        private void StopAutomaticFire()
        {
            WeaponFireDefinitionSO fireDefinition = GetFireDefinition();

            if (fireDefinition == null || !IsAutomaticTriggerMode(fireDefinition.triggerMode))
            {
                return;
            }

            if (hasDelayedFire)
            {
                hasDelayedFire = false;
                delayedFireDefinition = null;
            }

            StopContinuousBeamVfx();
            AutomaticFireStopped?.Invoke();
        }

        private static bool IsAutomaticTriggerMode(WeaponTriggerMode triggerMode)
        {
            return triggerMode == WeaponTriggerMode.FullAuto
                || triggerMode == WeaponTriggerMode.Continuous;
        }

        private void ProcessDelayedFire()
        {
            if (!hasDelayedFire || Time.time < delayedFireTime)
            {
                return;
            }

            WeaponFireDefinitionSO fireDefinition = delayedFireDefinition;
            hasDelayedFire = false;
            delayedFireDefinition = null;

            if (fireDefinition == null)
            {
                return;
            }

            ExecuteFire(fireDefinition, false);
            lastFireTime = Time.time;
        }

        private bool IsCurrentWeaponUpperBodyReady(WeaponAnimationType animationType)
        {
            if (animator == null)
            {
                return false;
            }

            int layerIndex = animator.GetLayerIndex(upperBodyLayerName);

            if (layerIndex < 0)
            {
                return false;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            AnimatorStateInfo nextStateInfo = animator.GetNextAnimatorStateInfo(layerIndex);

            return IsReadyState(stateInfo, animationType) || IsReadyState(nextStateInfo, animationType);
        }

        private bool IsReadyState(AnimatorStateInfo stateInfo, WeaponAnimationType animationType)
        {
            switch (animationType)
            {
                case WeaponAnimationType.Ranged1H:
                    return stateInfo.IsName(ranged1HAimingStateName)
                        || stateInfo.IsName(ranged1HShootingStateName)
                        || stateInfo.IsName(ranged1HFireRecoverStateName);

                case WeaponAnimationType.Ranged2H:
                    return stateInfo.IsName(ranged2HAimingStateName)
                        || stateInfo.IsName(ranged2HShootingStateName)
                        || stateInfo.IsName(ranged2HFireRecoverStateName);

                default:
                    return true;
            }
        }

        private bool TryValidateFireDefinition(WeaponFireDefinitionSO fireDefinition)
        {
            if (aimCamera == null)
            {
                Debug.LogWarning("[PlayerProjectileLauncher] Aim Camera가 연결되지 않았습니다.");
                return false;
            }

            if (muzzle == null)
            {
                Debug.LogWarning("[PlayerProjectileLauncher] Muzzle이 연결되지 않았습니다.");
                return false;
            }

            if (fireDefinition == null)
            {
                Debug.LogWarning("[PlayerProjectileLauncher] Fire Definition이 없습니다.");
                return false;
            }

            switch (fireDefinition.resolveType)
            {
                case WeaponFireResolveType.Projectile:
                    return ValidateProjectileResolve(fireDefinition);

                case WeaponFireResolveType.Hitscan:
                    return ValidateHitscanResolve(fireDefinition);

                default:
                    Debug.LogWarning("[PlayerProjectileLauncher] 알 수 없는 Resolve Type입니다.");
                    return false;
            }
        }

        private bool ValidateProjectileResolve(WeaponFireDefinitionSO fireDefinition)
        {
            if (fireDefinition.projectileDefinition == null)
            {
                Debug.LogWarning("[PlayerProjectileLauncher] Projectile Definition이 없습니다.");
                return false;
            }

            if (fireDefinition.projectileDefinition.projectilePrefab == null)
            {
                Debug.LogWarning("[PlayerProjectileLauncher] Projectile Prefab이 연결되지 않았습니다.");
                return false;
            }

            return true;
        }

        private bool ValidateHitscanResolve(WeaponFireDefinitionSO fireDefinition)
        {
            if (fireDefinition.hitscanDefinition == null)
            {
                Debug.LogWarning("[PlayerProjectileLauncher] Hitscan Definition이 없습니다.");
                return false;
            }

            return true;
        }

        private float GetAimDistance(WeaponFireDefinitionSO fireDefinition)
        {
            if (fireDefinition.resolveType == WeaponFireResolveType.Hitscan)
            {
                return fireDefinition.hitscanDefinition.range;
            }

            ProjectileDefinitionSO projectileDefinition = fireDefinition.projectileDefinition;
            return projectileDefinition.speed * projectileDefinition.lifeTime;
        }

        private LayerMask GetAimHitLayers(WeaponFireDefinitionSO fireDefinition)
        {
            if (fireDefinition.resolveType == WeaponFireResolveType.Hitscan)
            {
                return fireDefinition.hitscanDefinition.hitLayers;
            }

            return fireDefinition.projectileDefinition.hitLayers;
        }

        private Vector3 GetCameraAimPoint(float aimDistance, LayerMask hitLayers)
        {
            Ray cameraRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (drawDebugRay)
            {
                Debug.DrawRay(
                    cameraRay.origin,
                    cameraRay.direction * aimDistance,
                    Color.blue,
                    0.5f
                );
            }

            if (TryGetCameraAimHit(cameraRay, aimDistance, hitLayers, out RaycastHit hit))
            {
                return hit.point;
            }

            return cameraRay.origin + cameraRay.direction * aimDistance;
        }

        private bool TryGetCameraAimHit(
            Ray cameraRay,
            float aimDistance,
            LayerMask hitLayers,
            out RaycastHit selectedHit)
        {
            selectedHit = default;

            if (!ignoreOwnerWhenResolvingAimPoint)
            {
                return Physics.Raycast(
                    cameraRay,
                    out selectedHit,
                    aimDistance,
                    hitLayers,
                    aimTriggerInteraction);
            }

            RaycastHit[] hits = Physics.RaycastAll(
                cameraRay,
                aimDistance,
                hitLayers,
                aimTriggerInteraction);

            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            System.Array.Sort(hits, CompareHitsByDistance);

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit candidate = hits[i];

                if (candidate.collider == null)
                {
                    continue;
                }

                if (IsOwnerOrOwnerChild(candidate.collider.transform))
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

        private void FireByPattern(
            WeaponFireDefinitionSO fireDefinition,
            Vector3 baseDirection)
        {
            switch (fireDefinition.firePattern)
            {
                case WeaponFirePattern.Single:
                    FireSingle(fireDefinition, baseDirection);
                    break;

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

        private void FireSingle(
            WeaponFireDefinitionSO fireDefinition,
            Vector3 baseDirection)
        {
            int count = Mathf.Max(1, fireDefinition.projectileCount);

            for (int i = 0; i < count; i++)
            {
                ResolveFire(fireDefinition, baseDirection);
            }
        }

        private void FireForwardSpread(
            WeaponFireDefinitionSO fireDefinition,
            Vector3 baseDirection)
        {
            int count = Mathf.Max(1, fireDefinition.projectileCount);

            for (int i = 0; i < count; i++)
            {
                Vector3 direction = GetForwardSpreadDirection(
                    baseDirection,
                    fireDefinition,
                    i,
                    count
                );

                ResolveFire(fireDefinition, direction);
            }
        }

        private Vector3 GetForwardSpreadDirection(
            Vector3 baseDirection,
            WeaponFireDefinitionSO fireDefinition,
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
            else
            {
                if (count <= 1)
                {
                    yawOffset = 0f;
                }
                else
                {
                    float t = index / (float)(count - 1);
                    yawOffset = Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, t);
                }
            }

            Quaternion yawRotation = Quaternion.AngleAxis(yawOffset, Vector3.up);
            return (yawRotation * baseDirection).normalized;
        }

        private void FireRandomCone(
            WeaponFireDefinitionSO fireDefinition,
            Vector3 baseDirection)
        {
            int count = Mathf.Max(1, fireDefinition.projectileCount);

            for (int i = 0; i < count; i++)
            {
                Vector3 direction = GetRandomConeDirection(
                    baseDirection,
                    fireDefinition.coneSpreadAngle
                );

                ResolveFire(fireDefinition, direction);
            }
        }

        private Vector3 GetRandomConeDirection(Vector3 baseDirection, float coneSpreadAngle)
        {
            if (coneSpreadAngle <= 0f)
            {
                return baseDirection;
            }

            Quaternion baseRotation = Quaternion.LookRotation(baseDirection, Vector3.up);

            float radius = coneSpreadAngle * 0.5f;
            Vector2 randomPoint = Random.insideUnitCircle * radius;

            Quaternion randomRotation = Quaternion.Euler(
                -randomPoint.y,
                randomPoint.x,
                0f
            );

            Vector3 direction = baseRotation * randomRotation * Vector3.forward;
            return direction.normalized;
        }

        private void ResolveFire(
            WeaponFireDefinitionSO fireDefinition,
            Vector3 direction)
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

        private void SpawnProjectile(
            ProjectileDefinitionSO projectileDefinition,
            Vector3 direction)
        {
            if (projectilePool == null)
            {
                Debug.LogError("[PlayerProjectileLauncher] ProjectilePool is not connected.", this);
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

            Projectile projectile = projectilePool.Get(
                projectileDefinition,
                muzzle.position,
                rotation
            );

            if (projectile == null)
            {
                return;
            }

            if (drawDebugRay)
            {
                Debug.DrawRay(muzzle.position, direction * 2f, Color.red, 0.5f);
            }

            projectile.Initialize(
                projectileDefinition,
                owner != null ? owner : gameObject,
                direction,
                projectilePool.Release,
                GetDamageMultiplier()
            );
        }

        private void FireHitscan(
            HitscanDefinitionSO hitscanDefinition,
            Vector3 direction,
            bool useContinuousBeamVfx)
        {
            Vector3 origin = muzzle.position;
            float range = hitscanDefinition.range;
            Vector3 endPoint = origin + direction * range;

            RaycastHit hitInfo = default;

            bool hasHit = Physics.Raycast(
                origin,
                direction,
                out hitInfo,
                range,
                hitscanDefinition.hitLayers
            );

            if (hasHit && IsOwnerOrOwnerChild(hitInfo.collider.transform))
            {
                hasHit = false;
            }

            if (hasHit)
            {
                endPoint = hitInfo.point;
                ApplyHitscanDamage(hitscanDefinition, hitInfo);
            }

            if (drawDebugRay || hitscanDefinition.drawDebugRay)
            {
                Debug.DrawLine(origin, endPoint, Color.cyan, 0.05f);
            }

            float hitVfxDelay = 0f;
            if (useContinuousBeamVfx)
            {
                SetContinuousBeamVfx(hitscanDefinition, origin, endPoint);
            }
            else
            {
                hitVfxDelay = SpawnHitscanBeamVfx(hitscanDefinition, origin, endPoint);
            }

            if (hasHit)
            {
                SpawnHitscanHitVfx(hitscanDefinition, hitInfo, hitVfxDelay);
            }
        }

        private void ApplyHitscanDamage(
            HitscanDefinitionSO hitscanDefinition,
            RaycastHit hit)
        {
            if (hitscanDefinition.useSplashDamage)
            {
                ApplyHitscanSplashDamage(hitscanDefinition, hit.point);
            }
            else
            {
                ApplyHitscanDirectDamage(hitscanDefinition, hit);
            }
        }

        private void ApplyHitscanDirectDamage(
            HitscanDefinitionSO hitscanDefinition,
            RaycastHit hit)
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                return;
            }

            DamageInfo damageInfo = new DamageInfo(
                ApplyDamageMultiplier(hitscanDefinition.damage),
                owner != null ? owner : gameObject,
                hit.point
            );

            damageable.TakeDamage(damageInfo);
        }

        private void ApplyHitscanSplashDamage(
            HitscanDefinitionSO hitscanDefinition,
            Vector3 center)
        {
            Collider[] colliders = Physics.OverlapSphere(
                center,
                hitscanDefinition.splashRadius,
                hitscanDefinition.hitLayers
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
                    ApplyDamageMultiplier(hitscanDefinition.splashDamage),
                    owner != null ? owner : gameObject,
                    targetCollider.ClosestPoint(center)
                );

                damageable.TakeDamage(damageInfo);
            }
        }

        private float SpawnHitscanBeamVfx(
            HitscanDefinitionSO hitscanDefinition,
            Vector3 startPoint,
            Vector3 endPoint)
        {
            if (hitscanDefinition.beamVfxPrefab == null)
            {
                return 0f;
            }

            GameObject beamObject = Instantiate(
                hitscanDefinition.beamVfxPrefab,
                startPoint,
                Quaternion.identity
            );

            ContinuousBeamVfxRuntime beamRuntime = beamObject.AddComponent<ContinuousBeamVfxRuntime>();
            beamRuntime.Initialize(
                hitscanDefinition,
                hitscanDefinition.MoveBeamVfxToHitPoint,
                hitscanDefinition.BeamVfxMovingLineWidth);

            float visibleTime = hitscanDefinition.beamVisibleTime;
            float impactDelay = 0f;
            if (hitscanDefinition.MoveBeamVfxToHitPoint)
            {
                impactDelay = beamRuntime.SetMovingSegment(startPoint, endPoint);
                visibleTime = Mathf.Max(visibleTime, impactDelay + 0.015f);
            }
            else
            {
                beamRuntime.SetSegment(startPoint, endPoint);
            }

            Destroy(beamObject, visibleTime);
            return impactDelay;
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
                activeContinuousBeamRuntime.Initialize(hitscanDefinition);
                activeContinuousHitscanDefinition = hitscanDefinition;
            }

            activeContinuousBeamRuntime.SetSegment(startPoint, endPoint);
        }

        private void UpdateContinuousBeamVisual()
        {
            if (activeContinuousBeamRuntime == null)
            {
                return;
            }

            WeaponFireDefinitionSO fireDefinition = weaponRuntime != null
                ? weaponRuntime.CurrentFireDefinition
                : null;

            if (!fireHeld
                || fireDefinition == null
                || fireDefinition.triggerMode != WeaponTriggerMode.Continuous
                || fireDefinition.resolveType != WeaponFireResolveType.Hitscan
                || fireDefinition.hitscanDefinition != activeContinuousHitscanDefinition
                || aimCamera == null
                || muzzle == null)
            {
                StopContinuousBeamVfx();
                return;
            }

            HitscanDefinitionSO hitscanDefinition = fireDefinition.hitscanDefinition;
            Vector3 aimPoint = GetCameraAimPoint(hitscanDefinition.range, hitscanDefinition.hitLayers);
            Vector3 direction = aimPoint - muzzle.position;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            direction.Normalize();
            Vector3 endPoint = ResolveHitscanEndPoint(hitscanDefinition, muzzle.position, direction);
            activeContinuousBeamRuntime.SetSegment(muzzle.position, endPoint);
        }

        private Vector3 ResolveHitscanEndPoint(
            HitscanDefinitionSO hitscanDefinition,
            Vector3 origin,
            Vector3 direction)
        {
            if (Physics.Raycast(
                    origin,
                    direction,
                    out RaycastHit hit,
                    hitscanDefinition.range,
                    hitscanDefinition.hitLayers)
                && !IsOwnerOrOwnerChild(hit.collider.transform))
            {
                return hit.point;
            }

            return origin + direction * hitscanDefinition.range;
        }

        private void StopContinuousBeamVfx()
        {
            if (activeContinuousBeamObject != null)
            {
                Destroy(activeContinuousBeamObject);
            }

            activeContinuousBeamObject = null;
            activeContinuousBeamRuntime = null;
            activeContinuousHitscanDefinition = null;
        }

        private void SpawnHitscanHitVfx(
            HitscanDefinitionSO hitscanDefinition,
            RaycastHit hit,
            float delay)
        {
            if (hitscanDefinition.hitVfxPrefab == null)
            {
                return;
            }

            if (delay > 0f)
            {
                StartCoroutine(SpawnHitscanHitVfxDelayed(hitscanDefinition, hit.point, hit.normal, delay));
                return;
            }

            SpawnHitscanHitVfxNow(hitscanDefinition, hit.point, hit.normal);
        }

        private IEnumerator SpawnHitscanHitVfxDelayed(
            HitscanDefinitionSO hitscanDefinition,
            Vector3 hitPoint,
            Vector3 hitNormal,
            float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnHitscanHitVfxNow(hitscanDefinition, hitPoint, hitNormal);
        }

        private static void SpawnHitscanHitVfxNow(
            HitscanDefinitionSO hitscanDefinition,
            Vector3 hitPoint,
            Vector3 hitNormal)
        {
            if (hitscanDefinition == null || hitscanDefinition.hitVfxPrefab == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(hitNormal, Vector3.up);

            GameObject hitObject = Instantiate(
                hitscanDefinition.hitVfxPrefab,
                hitPoint,
                rotation
            );

            Destroy(hitObject, 1f);
        }

        private bool IsOwnerOrOwnerChild(Transform target)
        {
            if (owner == null || target == null)
            {
                return false;
            }

            return target == owner.transform || target.IsChildOf(owner.transform);
        }

        private Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildByName(root.GetChild(i), childName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
        private float ApplyDamageMultiplier(float baseDamage)
        {
            return baseDamage * GetDamageMultiplier();
        }

        private float GetDamageMultiplier()
        {
            return statModifier != null ? statModifier.DamageMultiplier : 1f;
        }

        private void ResolveOptionalReferences()
        {
            if (projectilePool == null)
            {
                projectilePool = FindFirstObjectByType<ProjectilePool>(FindObjectsInactive.Include);
            }

            if (statModifier == null)
            {
                statModifier = GetComponent<PlayerStatModifier>();
            }

            if (statModifier == null)
            {
                statModifier = GetComponentInParent<PlayerStatModifier>();
            }

            if (statModifier == null)
            {
                statModifier = GetComponentInChildren<PlayerStatModifier>(true);
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                animator = GetComponentInParent<Animator>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }
    }
}



