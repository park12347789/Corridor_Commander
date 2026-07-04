using System;
using UnityEngine;
using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerControl;

namespace CorridorCommander.PlayerItems
{
    [DisallowMultipleComponent]
    public sealed class PlayerThrowableItemController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerItemInventory itemInventory;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform throwOrigin;
        [SerializeField] private PlayerFacingController facingController;

        [Header("Aim")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float aimDistance = 80f;
        [SerializeField] private float fallbackThrowDistance = 12f;
        [SerializeField] private bool ignoreOwnerWhenResolvingAimPoint = true;
        [SerializeField] private LayerMask aimIgnoredLayers = 0;
        [SerializeField] [Min(0f)] private float cameraAimIgnoreNearHitDistance = 0.75f;
        [SerializeField] private bool ignoreNearGroundAimHits = true;
        [SerializeField] [Range(0f, 1f)] private float nearGroundNormalThreshold = 0.55f;
        [SerializeField] [Min(0f)] private float minimumTargetDistanceFromThrowOrigin = 1.6f;
        [SerializeField] [Min(0.01f)] private float closeThrowArcDistance = 6f;
        [SerializeField] [Range(0.1f, 1f)] private float closeThrowArcHeightMultiplier = 0.45f;
        [SerializeField] private float indicatorHeightOffset = 0.05f;

        [Header("Spawn Safety")]
        [SerializeField] [Min(0f)] private float spawnForwardOffset = 0.45f;
        [SerializeField] [Min(0f)] private float spawnUpOffset = 0.15f;

        [Header("Trajectory Preview")]
        [SerializeField] private LineRenderer trajectoryLine;
        [SerializeField] private Material trajectoryMaterial;
        [SerializeField] [Range(4, 64)] private int trajectoryPointCount = 36;
        [SerializeField] [Min(0.02f)] private float trajectoryTimeStep = 0.06f;
        [SerializeField] [Min(0.005f)] private float trajectoryLineWidth = 0.03f;
        [SerializeField] private Color trajectoryColor = new Color(0f, 0.9f, 1f, 0.82f);
        [SerializeField] private LayerMask trajectoryCollisionLayers = ~0;
        [SerializeField] [Min(0f)] private float trajectoryCollisionRadius = 0.1f;

        private PlayerItemRuntimeEntry aimingItem;
        private GameObject aimingUser;
        private GameObject activeIndicator;
        private Vector3 currentTargetPoint;
        private Vector3 predictedLandingPoint;
        private bool isAiming;
        private bool hasPredictedLandingPoint;
        private bool ownsTrajectoryLine;

        public bool IsAiming => isAiming;
        public PlayerItemRuntimeEntry AimingItem => aimingItem;

        public event Action<ItemDefinitionSO> ThrowAimStarted;
        public event Action<ItemDefinitionSO, Vector3> ThrowAimUpdated;
        public event Action<ItemDefinitionSO> ThrowCanceled;
        public event Action<ItemDefinitionSO> ThrowCommitted;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (!isAiming)
            {
                return;
            }

            UpdateAim();
        }

        public bool CanAimItem(PlayerItemRuntimeEntry itemEntry)
        {
            return itemEntry != null
                && itemEntry.IsAvailable
                && itemEntry.ItemDefinition != null
                && itemEntry.ItemDefinition.useType == PlayerItemUseType.Grenade;
        }

        public bool BeginAim(
            PlayerItemRuntimeEntry itemEntry,
            GameObject user,
            out string statusMessage)
        {
            ResolveReferences();

            if (!CanAimItem(itemEntry))
            {
                statusMessage = "No throwable item";
                return false;
            }

            if (itemInventory == null)
            {
                statusMessage = "No item inventory";
                return false;
            }

            if (ResolveCamera() == null)
            {
                statusMessage = "No aim camera";
                Debug.LogError("[PlayerThrowableItemController] Aim Camera is not connected.", this);
                return false;
            }

            aimingItem = itemEntry;
            aimingUser = ResolveAimingUser(user);
            isAiming = true;
            facingController?.SetThrowableAimHeld(true);

            CreateIndicator(itemEntry.ItemDefinition);
            UpdateAim();
            ThrowAimStarted?.Invoke(itemEntry.ItemDefinition);
            Debug.Log("[PlayerThrowableItemController] Throw aim started: " + itemEntry.ItemDefinition.displayName, this);

            statusMessage = "Aiming: " + itemEntry.ItemDefinition.displayName;
            return true;
        }

        public bool ConfirmThrow(out string statusMessage)
        {
            if (!isAiming || aimingItem == null || aimingItem.ItemDefinition == null)
            {
                statusMessage = "No throwable aim";
                return false;
            }

            ItemDefinitionSO definition = aimingItem.ItemDefinition;

            if (definition.projectilePrefab == null)
            {
                CancelAim();
                statusMessage = definition.displayName + " projectile prefab missing";
                Debug.LogError("[PlayerThrowableItemController] Grenade projectile prefab is not configured.", this);
                return false;
            }

            if (itemInventory == null)
            {
                CancelAim();
                statusMessage = definition.displayName + " unavailable";
                return false;
            }

            if (!CanSpawnThrowable(definition))
            {
                CancelAim();
                statusMessage = definition.displayName + " throw failed";
                return false;
            }

            if (!itemInventory.TryConsume(aimingItem))
            {
                CancelAim();
                statusMessage = definition.displayName + " unavailable";
                return false;
            }

            if (!TrySpawnThrowable(definition))
            {
                statusMessage = definition.displayName + " throw failed";
                return false;
            }

            ThrowCommitted?.Invoke(definition);
            ClearAimState();
            Debug.Log("[PlayerThrowableItemController] Throw committed: " + definition.displayName, this);

            statusMessage = "Thrown: " + definition.displayName;
            return true;
        }

        public void CancelAim()
        {
            if (!isAiming)
            {
                return;
            }

            ItemDefinitionSO definition = aimingItem != null ? aimingItem.ItemDefinition : null;
            ThrowCanceled?.Invoke(definition);
            ClearAimState();
            Debug.Log("[PlayerThrowableItemController] Throw aim canceled.", this);
        }

        private void UpdateAim()
        {
            currentTargetPoint = ResolveAimPoint();
            UpdateTrajectory();
            UpdateIndicator();

            if (aimingItem != null && aimingItem.ItemDefinition != null)
            {
                ThrowAimUpdated?.Invoke(aimingItem.ItemDefinition, currentTargetPoint);
            }
        }

        private Vector3 ResolveAimPoint()
        {
            Camera resolvedCamera = ResolveCamera();

            if (resolvedCamera == null)
            {
                Debug.LogError("[PlayerThrowableItemController] Aim Camera is not connected.", this);
                return currentTargetPoint;
            }

            Ray ray = resolvedCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (TryGetAimHit(ray, out RaycastHit hit))
            {
                return ClampTargetDistanceFromThrowOrigin(hit.point, ray.direction);
            }

            return ProjectAimPoint(ray.direction);
        }

        private bool TryGetAimHit(Ray ray, out RaycastHit selectedHit)
        {
            selectedHit = default;

            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                aimDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, CompareHitsByDistance);

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit candidate = hits[i];

                if (candidate.collider == null)
                {
                    continue;
                }

                if (ignoreOwnerWhenResolvingAimPoint
                    && aimingUser != null
                    && candidate.collider.transform.IsChildOf(aimingUser.transform))
                {
                    continue;
                }

                if (IsAimIgnoredLayer(candidate.collider.gameObject.layer))
                {
                    continue;
                }

                if (ShouldIgnoreAimHit(candidate))
                {
                    continue;
                }

                selectedHit = candidate;
                return true;
            }

            return false;
        }

        private bool ShouldIgnoreAimHit(RaycastHit hit)
        {
            if (hit.distance > cameraAimIgnoreNearHitDistance)
            {
                return false;
            }

            if (!ignoreNearGroundAimHits)
            {
                return true;
            }

            return hit.normal.y >= nearGroundNormalThreshold;
        }

        private bool IsAimIgnoredLayer(int layer)
        {
            return (aimIgnoredLayers.value & (1 << layer)) != 0;
        }

        private Vector3 ClampTargetDistanceFromThrowOrigin(Vector3 targetPoint, Vector3 fallbackDirection)
        {
            Transform origin = ResolveThrowOrigin();
            Vector3 originPosition = origin != null ? origin.position : transform.position;
            Vector3 offset = targetPoint - originPosition;

            if (offset.magnitude >= minimumTargetDistanceFromThrowOrigin)
            {
                return targetPoint;
            }

            Vector3 flatDirection = new Vector3(offset.x, 0f, offset.z);
            if (flatDirection.sqrMagnitude <= 0.0001f)
            {
                flatDirection = new Vector3(fallbackDirection.x, 0f, fallbackDirection.z);
            }

            if (flatDirection.sqrMagnitude <= 0.0001f)
            {
                flatDirection = transform.forward;
            }

            Vector3 clampedPoint = originPosition
                + flatDirection.normalized * Mathf.Max(0f, minimumTargetDistanceFromThrowOrigin);
            clampedPoint.y = targetPoint.y;
            return clampedPoint;
        }

        private Vector3 ProjectAimPoint(Vector3 cameraDirection)
        {
            Transform origin = ResolveThrowOrigin();
            Vector3 originPosition = origin != null ? origin.position : transform.position;
            Vector3 flatDirection = new Vector3(cameraDirection.x, 0f, cameraDirection.z);

            if (flatDirection.sqrMagnitude <= 0.0001f)
            {
                flatDirection = transform.forward;
            }

            return originPosition + flatDirection.normalized * Mathf.Max(1f, fallbackThrowDistance);
        }

        private bool CanSpawnThrowable(ItemDefinitionSO definition)
        {
            if (definition == null || definition.projectilePrefab == null)
            {
                Debug.LogError("[PlayerThrowableItemController] Grenade projectile prefab is not configured.", this);
                return false;
            }

            bool hasGrenadeProjectile = definition.projectilePrefab.GetComponent<GrenadeProjectile>() != null;
            bool hasRigidbody = definition.projectilePrefab.GetComponent<Rigidbody>() != null;
            if (!hasGrenadeProjectile && !hasRigidbody)
            {
                Debug.LogError("[PlayerThrowableItemController] Throwable prefab requires GrenadeProjectile or Rigidbody: " + definition.projectilePrefab.name, definition.projectilePrefab);
                return false;
            }

            return true;
        }

        private bool TrySpawnThrowable(ItemDefinitionSO definition)
        {
            if (definition.projectilePrefab == null)
            {
                Debug.LogError("[PlayerThrowableItemController] Grenade projectile prefab is not configured.", this);
                return false;
            }

            Transform origin = ResolveThrowOrigin();
            Vector3 originPosition = origin.position;
            Vector3 launchDirection = currentTargetPoint - originPosition;

            if (launchDirection.sqrMagnitude <= 0.0001f)
            {
                launchDirection = origin.forward;
            }

            launchDirection.Normalize();
            Vector3 spawnPosition = originPosition
                + launchDirection * spawnForwardOffset
                + Vector3.up * spawnUpOffset;
            Vector3 velocity = ResolveThrowVelocity(definition, spawnPosition, currentTargetPoint, launchDirection);

            GameObject projectileObject = Instantiate(
                definition.projectilePrefab,
                spawnPosition,
                Quaternion.LookRotation(launchDirection, Vector3.up));
            ItemAudioUtility.PlayThrowAudio(definition, spawnPosition);

            if (projectileObject.TryGetComponent(out GrenadeProjectile grenadeProjectile))
            {
                float? gravityOverride = definition.useBallisticThrowArc
                    ? definition.ballisticThrowGravity
                    : null;
                grenadeProjectile.Launch(definition, aimingUser, velocity, gravityOverride);
                return true;
            }

            if (projectileObject.TryGetComponent(out Rigidbody body))
            {
                body.linearVelocity = velocity;
                return true;
            }

            Debug.LogError("[PlayerThrowableItemController] Throwable prefab requires GrenadeProjectile or Rigidbody: " + definition.projectilePrefab.name, projectileObject);
            Destroy(projectileObject);
            return false;
        }

        private Vector3 ResolveThrowVelocity(
            ItemDefinitionSO definition,
            Vector3 origin,
            Vector3 targetPoint,
            Vector3 fallbackDirection)
        {
            if (definition.useBallisticThrowArc)
            {
                return BallisticTrajectoryUtility.CalculateVelocityByArcHeight(
                    origin,
                    targetPoint,
                    definition.ballisticThrowGravity,
                    ResolveThrowArcHeight(definition, origin, targetPoint),
                    fallbackDirection,
                    definition.throwSpeed);
            }

            return fallbackDirection * Mathf.Max(0f, definition.throwSpeed)
                + Vector3.up * Mathf.Max(0f, definition.upwardVelocity);
        }

        private float ResolveThrowArcHeight(
            ItemDefinitionSO definition,
            Vector3 origin,
            Vector3 targetPoint)
        {
            float baseArcHeight = Mathf.Max(0.01f, definition.ballisticThrowArcHeight);
            Vector3 offset = targetPoint - origin;
            offset.y = 0f;

            float horizontalDistance = offset.magnitude;
            if (horizontalDistance >= closeThrowArcDistance)
            {
                return baseArcHeight;
            }

            float closeArcHeight = baseArcHeight * closeThrowArcHeightMultiplier;
            float distanceRatio = Mathf.Clamp01(horizontalDistance / closeThrowArcDistance);
            return Mathf.Lerp(closeArcHeight, baseArcHeight, distanceRatio);
        }

        private Transform ResolveThrowOrigin()
        {
            return throwOrigin != null ? throwOrigin : transform;
        }

        private GameObject ResolveAimingUser(GameObject user)
        {
            if (user != null && user.transform.root != null)
            {
                return user.transform.root.gameObject;
            }

            if (transform.root != null)
            {
                return transform.root.gameObject;
            }

            return gameObject;
        }

        private void CreateIndicator(ItemDefinitionSO definition)
        {
            DestroyIndicator();

            if (definition.aimIndicatorPrefab != null)
            {
                activeIndicator = Instantiate(definition.aimIndicatorPrefab);
                return;
            }

            Debug.LogError("[PlayerThrowableItemController] Aim Indicator Prefab is not configured: " + definition.displayName, this);
        }

        private void UpdateIndicator()
        {
            if (activeIndicator == null)
            {
                return;
            }

            Vector3 indicatorPoint = hasPredictedLandingPoint
                ? predictedLandingPoint
                : currentTargetPoint;
            activeIndicator.transform.position = indicatorPoint + Vector3.up * indicatorHeightOffset;
        }

        private void UpdateTrajectory()
        {
            hasPredictedLandingPoint = false;

            if (aimingItem == null || aimingItem.ItemDefinition == null)
            {
                HideTrajectoryLine();
                return;
            }

            if (!EnsureTrajectoryLine())
            {
                return;
            }

            ItemDefinitionSO definition = aimingItem.ItemDefinition;
            Transform origin = ResolveThrowOrigin();
            Vector3 originPosition = origin.position;
            Vector3 launchDirection = currentTargetPoint - originPosition;

            if (launchDirection.sqrMagnitude <= 0.0001f)
            {
                launchDirection = origin.forward;
            }

            launchDirection.Normalize();
            Vector3 spawnPosition = originPosition
                + launchDirection * spawnForwardOffset
                + Vector3.up * spawnUpOffset;
            Vector3 velocity = ResolveThrowVelocity(definition, spawnPosition, currentTargetPoint, launchDirection);
            float gravity = definition.useBallisticThrowArc
                ? definition.ballisticThrowGravity
                : Physics.gravity.y;

            trajectoryLine.enabled = true;
            trajectoryLine.positionCount = 0;

            Vector3 previousPoint = spawnPosition;
            int writtenPoints = 1;
            trajectoryLine.positionCount = 1;
            trajectoryLine.SetPosition(0, previousPoint);

            for (int i = 1; i < trajectoryPointCount; i++)
            {
                float time = i * trajectoryTimeStep;
                Vector3 nextPoint = spawnPosition
                    + velocity * time
                    + Vector3.up * (0.5f * gravity * time * time);

                if (TryGetTrajectoryHit(previousPoint, nextPoint, out RaycastHit hit))
                {
                    trajectoryLine.positionCount = writtenPoints + 1;
                    trajectoryLine.SetPosition(writtenPoints, hit.point);
                    predictedLandingPoint = hit.point;
                    hasPredictedLandingPoint = true;
                    return;
                }

                trajectoryLine.positionCount = writtenPoints + 1;
                trajectoryLine.SetPosition(writtenPoints, nextPoint);
                writtenPoints++;
                previousPoint = nextPoint;
            }

            predictedLandingPoint = currentTargetPoint;
            hasPredictedLandingPoint = true;
        }

        private bool TryGetTrajectoryHit(Vector3 from, Vector3 to, out RaycastHit selectedHit)
        {
            selectedHit = default;

            Vector3 delta = to - from;
            float distance = delta.magnitude;

            if (distance <= 0.0001f)
            {
                return false;
            }

            RaycastHit[] hits = trajectoryCollisionRadius > 0f
                ? Physics.SphereCastAll(
                    from,
                    trajectoryCollisionRadius,
                    delta / distance,
                    distance,
                    trajectoryCollisionLayers,
                    QueryTriggerInteraction.Ignore)
                : Physics.RaycastAll(
                    from,
                    delta / distance,
                    distance,
                    trajectoryCollisionLayers,
                    QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, CompareHitsByDistance);

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit candidate = hits[i];

                if (candidate.collider == null)
                {
                    continue;
                }

                if (ignoreOwnerWhenResolvingAimPoint
                    && aimingUser != null
                    && candidate.collider.transform.IsChildOf(aimingUser.transform))
                {
                    continue;
                }

                if (IsAimIgnoredLayer(candidate.collider.gameObject.layer))
                {
                    continue;
                }

                selectedHit = candidate;
                return true;
            }

            return false;
        }

        private bool EnsureTrajectoryLine()
        {
            if (trajectoryLine == null)
            {
                Debug.LogError("[PlayerThrowableItemController] Trajectory Line is not assigned.", this);
                return false;
            }

            if (trajectoryMaterial == null)
            {
                Debug.LogError("[PlayerThrowableItemController] Trajectory material is not assigned.", this);
                return false;
            }

            trajectoryLine.useWorldSpace = true;
            trajectoryLine.loop = false;
            trajectoryLine.widthMultiplier = 1f;
            trajectoryLine.numCapVertices = 4;
            trajectoryLine.numCornerVertices = 2;
            trajectoryLine.startWidth = trajectoryLineWidth;
            trajectoryLine.endWidth = trajectoryLineWidth;
            trajectoryLine.startColor = trajectoryColor;
            trajectoryLine.endColor = trajectoryColor;
            trajectoryLine.material = trajectoryMaterial;
            return true;
        }

        private void HideTrajectoryLine()
        {
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = false;
                trajectoryLine.positionCount = 0;
            }
        }

        private void ClearAimState()
        {
            DestroyIndicator();
            HideTrajectoryLine();
            aimingItem = null;
            aimingUser = null;
            isAiming = false;
            hasPredictedLandingPoint = false;
            facingController?.SetThrowableAimHeld(false);
        }

        private void DestroyIndicator()
        {
            if (activeIndicator == null)
            {
                return;
            }

            DestroyRuntimeObject(activeIndicator);
            activeIndicator = null;

            if (ownsTrajectoryLine && trajectoryLine != null)
            {
                DestroyRuntimeObject(trajectoryLine.gameObject);
                trajectoryLine = null;
                ownsTrajectoryLine = false;
            }
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void ResolveReferences()
        {
            if (itemInventory == null)
            {
                itemInventory = GetComponentInParent<PlayerItemInventory>();
            }

            if (facingController == null)
            {
                facingController = GetComponentInParent<PlayerFacingController>();
            }

            if (facingController == null)
            {
                Debug.LogError("[PlayerThrowableItemController] PlayerFacingController is not connected.", this);
            }

            ResolveCamera();
        }

        private Camera ResolveCamera()
        {
            if (aimCamera != null)
            {
                return aimCamera;
            }

            aimCamera = Camera.main;
            return aimCamera;
        }

        private static int CompareHitsByDistance(RaycastHit left, RaycastHit right)
        {
            return left.distance.CompareTo(right.distance);
        }
    }
}

/*
Unity setup outline:
1. Add PlayerThrowableItemController to the player item system object.
2. Assign PlayerItemInventory, Aim Camera, and Throw Origin.
3. Set Ground Layers to the layer used by walkable ground.
4. Configure grenade ItemDefinitionSO with projectilePrefab, throwSpeed/upwardVelocity or ballistic throw arc, fuseTime, radius, and value.
5. Assign PlayerFacingController to face the camera direction while grenade aim is held.
6. Increase Minimum Target Distance From Throw Origin if grenades can still land too close to the player.
*/
