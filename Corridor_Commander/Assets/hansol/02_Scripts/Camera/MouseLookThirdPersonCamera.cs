using UnityEngine;
using UnityEngine.InputSystem;
using CorridorCommander;
using CorridorCommander.PlayerControl;

namespace CorridorCommander.PlayerCamera
{
    public sealed class MouseLookThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Distance")]
        [SerializeField] private float distance = 4.5f;
        [SerializeField] private float heightOffset = 0.55f;

        [Header("Zoom")]
        [SerializeField] private bool allowRuntimeZoom = true;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 6f;
        [SerializeField] private float minHeightOffset = 0f;
        [SerializeField] private float maxHeightOffset = 0.4f;
        [SerializeField] private float zoomStep = 0.2f;

        [Header("First Person Zoom")]
        [SerializeField] private bool enableFirstPersonOnSustainedZoomIn = true;
        [SerializeField] [Min(0f)] private float sustainedZoomInTime = 0.35f;
        [SerializeField] [Min(0.01f)] private float sustainedZoomInInputGrace = 0.18f;
        [SerializeField] [Min(0.01f)] private float zoomInInputCredit = 0.12f;
        [SerializeField] [Min(0.01f)] private float firstPersonDistance = 0.02f;
        [SerializeField] private float firstPersonHeightOffset = 0.55f;
        [SerializeField] private bool hideTargetRenderersInFirstPerson = true;
        [SerializeField] private Transform firstPersonVisibleRoot;
        [SerializeField] private Renderer[] firstPersonHiddenRenderers;

        [Header("Shoulder View")]
        [SerializeField] private float shoulderOffset = 0.65f;
        [SerializeField] private float lookAtSideOffset = 0f;
        [SerializeField] private PlayerFacingController facingController;
        [SerializeField] private float aimShoulderOffset = 0.9f;
        [SerializeField] private float aimLookAtSideOffset = -0.38f;
        [SerializeField] [Min(0f)] private float shoulderSmoothTime = 0.06f;

        [Header("Camera Collision")]
        [SerializeField] private bool preventCameraWallClip = true;
        [SerializeField] private LayerMask cameraCollisionLayers = ~0;
        [SerializeField] [Min(0.01f)] private float cameraCollisionRadius = 0.25f;
        [SerializeField] [Min(0f)] private float cameraCollisionSkin = 0.08f;
        [SerializeField] private QueryTriggerInteraction cameraCollisionTriggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 0.15f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 60f;

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.02f;

        private float yaw;
        private float pitch = 20f;
        private float zoom01;
        private float currentShoulderOffset;
        private float currentLookAtSideOffset;
        private float shoulderOffsetVelocity;
        private float lookAtSideOffsetVelocity;
        private Vector3 positionVelocity;
        private bool isFirstPerson;
        private float sustainedZoomInTimer;
        private float lastSustainedZoomInTime = -1f;
        private bool firstPersonRenderersHidden;
        private bool[] firstPersonRendererOriginalStates;
        private readonly RaycastHit[] cameraCollisionHits = new RaycastHit[16];

        public float CurrentDistance => distance;
        public float CurrentZoom01 => zoom01;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public bool IsFirstPerson => isFirstPerson;

        private void Start()
        {
            Vector3 currentEuler = transform.eulerAngles;
            yaw = currentEuler.y;
            pitch = NormalizePitch(currentEuler.x);
            zoom01 = Mathf.InverseLerp(minDistance, maxDistance, distance);
            ApplyZoom01(zoom01);
            currentShoulderOffset = shoulderOffset;
            currentLookAtSideOffset = lookAtSideOffset;
            ResolveFacingController();
            ResolveFirstPersonVisibleRoot();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            SetFirstPersonRenderersHidden(false);
        }

        private void Update()
        {
            ResetStaleFirstPersonZoomInput();
            ReadMouseInput();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            UpdateCameraPositionAndRotation();
        }

        private void ReadMouseInput()
        {
            if (Mouse.current == null || !UiInputCoordinator.CanLook)
            {
                return;
            }

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            yaw += mouseDelta.x * mouseSensitivity;
            pitch -= mouseDelta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private void UpdateCameraPositionAndRotation()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 cameraRight = rotation * Vector3.right;
            ResolveShoulderOffsets(out float resolvedShoulderOffset, out float resolvedLookAtSideOffset);

            if (isFirstPerson)
            {
                resolvedShoulderOffset = 0f;
                resolvedLookAtSideOffset = 0f;
            }

            Vector3 lookPoint =
                target.position
                + Vector3.up * heightOffset
                + cameraRight * resolvedLookAtSideOffset;

            Vector3 desiredPosition =
                lookPoint
                - rotation * Vector3.forward * distance
                + cameraRight * resolvedShoulderOffset;

            desiredPosition = ResolveCameraCollision(lookPoint, desiredPosition, out bool collisionCorrected);

            if (positionSmoothTime <= 0f || collisionCorrected)
            {
                if (collisionCorrected)
                {
                    positionVelocity = Vector3.zero;
                }

                transform.SetPositionAndRotation(desiredPosition, rotation);
                return;
            }

            Vector3 smoothedPosition = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime
            );

            transform.SetPositionAndRotation(smoothedPosition, rotation);
        }

        private void ResolveShoulderOffsets(out float resolvedShoulderOffset, out float resolvedLookAtSideOffset)
        {
            ResolveFacingController();

            bool isAiming = facingController != null
                && (facingController.IsAimHeld
                    || facingController.IsFireHeld
                    || facingController.IsThrowableAimHeld);

            float targetShoulderOffset = isAiming ? aimShoulderOffset : shoulderOffset;
            float targetLookAtSideOffset = isAiming ? aimLookAtSideOffset : lookAtSideOffset;

            if (shoulderSmoothTime <= 0f || !Application.isPlaying)
            {
                currentShoulderOffset = targetShoulderOffset;
                currentLookAtSideOffset = targetLookAtSideOffset;
            }
            else
            {
                currentShoulderOffset = Mathf.SmoothDamp(
                    currentShoulderOffset,
                    targetShoulderOffset,
                    ref shoulderOffsetVelocity,
                    shoulderSmoothTime);
                currentLookAtSideOffset = Mathf.SmoothDamp(
                    currentLookAtSideOffset,
                    targetLookAtSideOffset,
                    ref lookAtSideOffsetVelocity,
                    shoulderSmoothTime);
            }

            resolvedShoulderOffset = currentShoulderOffset;
            resolvedLookAtSideOffset = currentLookAtSideOffset;
        }

        private void ResolveFacingController()
        {
            if (facingController != null)
            {
                return;
            }

            if (target != null)
            {
                facingController = target.GetComponentInParent<PlayerFacingController>();
            }

            if (facingController == null)
            {
                facingController = FindFirstObjectByType<PlayerFacingController>(FindObjectsInactive.Include);
            }
        }

        private Vector3 ResolveCameraCollision(Vector3 lookPoint, Vector3 desiredPosition, out bool collisionCorrected)
        {
            collisionCorrected = false;
            if (!preventCameraWallClip)
            {
                return desiredPosition;
            }

            Vector3 cameraOffset = desiredPosition - lookPoint;
            float desiredDistance = cameraOffset.magnitude;

            if (desiredDistance <= 0.0001f)
            {
                return desiredPosition;
            }

            Vector3 cameraDirection = cameraOffset / desiredDistance;
            if (!TryResolveNearestCameraHit(
                    lookPoint,
                    cameraDirection,
                    desiredDistance,
                    out RaycastHit hit))
            {
                return desiredPosition;
            }

            float correctedDistance = Mathf.Max(0.05f, hit.distance - cameraCollisionSkin);
            collisionCorrected = true;
            return lookPoint + cameraDirection * correctedDistance;
        }

        private bool TryResolveNearestCameraHit(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            out RaycastHit nearestHit)
        {
            nearestHit = default;
            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                cameraCollisionRadius,
                direction,
                cameraCollisionHits,
                maxDistance,
                cameraCollisionLayers,
                cameraCollisionTriggerInteraction);

            Transform ignoredRoot = target != null ? target.root : null;
            bool foundHit = false;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = cameraCollisionHits[i];
                Collider hitCollider = hit.collider;
                if (hitCollider == null || IsIgnoredCameraCollision(hitCollider.transform, ignoredRoot))
                {
                    continue;
                }

                if (hit.distance < nearestDistance)
                {
                    nearestHit = hit;
                    nearestDistance = hit.distance;
                    foundHit = true;
                }
            }

            return foundHit;
        }

        private static bool IsIgnoredCameraCollision(Transform hitTransform, Transform ignoredRoot)
        {
            if (hitTransform == null || ignoredRoot == null)
            {
                return false;
            }

            return hitTransform == ignoredRoot || hitTransform.IsChildOf(ignoredRoot);
        }

        public void AdjustZoom(float scrollDirection)
        {
            if (!allowRuntimeZoom || Mathf.Approximately(scrollDirection, 0f))
            {
                return;
            }

            if (isFirstPerson)
            {
                if (scrollDirection < 0f)
                {
                    ExitFirstPerson();
                }

                return;
            }

            float direction = scrollDirection > 0f ? -1f : 1f;
            zoom01 = Mathf.Clamp01(zoom01 + direction * Mathf.Max(0.01f, zoomStep));
            ApplyZoom01(zoom01);

            UpdateFirstPersonZoomInput(scrollDirection);
        }

        public void SetZoom01(float value)
        {
            isFirstPerson = false;
            sustainedZoomInTimer = 0f;
            lastSustainedZoomInTime = -1f;
            zoom01 = Mathf.Clamp01(value);
            ApplyZoom01(zoom01);
        }

        private void ApplyZoom01(float value)
        {
            float clampedValue = Mathf.Clamp01(value);
            distance = Mathf.Lerp(minDistance, maxDistance, clampedValue);
            heightOffset = EvaluateHeightOffset(distance);
        }

        private void UpdateFirstPersonZoomInput(float scrollDirection)
        {
            if (!enableFirstPersonOnSustainedZoomIn)
            {
                sustainedZoomInTimer = 0f;
                lastSustainedZoomInTime = -1f;
                return;
            }

            if (scrollDirection <= 0f || zoom01 > 0.001f)
            {
                sustainedZoomInTimer = 0f;
                lastSustainedZoomInTime = -1f;
                return;
            }

            float now = Time.unscaledTime;
            if (lastSustainedZoomInTime >= 0f
                && now - lastSustainedZoomInTime > sustainedZoomInInputGrace)
            {
                sustainedZoomInTimer = 0f;
            }

            lastSustainedZoomInTime = now;
            sustainedZoomInTimer += Mathf.Max(Time.unscaledDeltaTime, zoomInInputCredit);
            if (sustainedZoomInTimer >= sustainedZoomInTime)
            {
                EnterFirstPerson();
            }
        }

        private void ResetStaleFirstPersonZoomInput()
        {
            if (sustainedZoomInTimer <= 0f || lastSustainedZoomInTime < 0f)
            {
                return;
            }

            if (Time.unscaledTime - lastSustainedZoomInTime > sustainedZoomInInputGrace)
            {
                sustainedZoomInTimer = 0f;
                lastSustainedZoomInTime = -1f;
            }
        }

        private void EnterFirstPerson()
        {
            isFirstPerson = true;
            sustainedZoomInTimer = 0f;
            lastSustainedZoomInTime = -1f;
            distance = Mathf.Max(0.01f, firstPersonDistance);
            heightOffset = firstPersonHeightOffset;
            currentShoulderOffset = 0f;
            currentLookAtSideOffset = 0f;
            shoulderOffsetVelocity = 0f;
            lookAtSideOffsetVelocity = 0f;
            SetFirstPersonRenderersHidden(true);
        }

        private void ExitFirstPerson()
        {
            isFirstPerson = false;
            sustainedZoomInTimer = 0f;
            lastSustainedZoomInTime = -1f;
            zoom01 = Mathf.Max(zoom01, Mathf.Max(0.01f, zoomStep));
            ApplyZoom01(zoom01);
            SetFirstPersonRenderersHidden(false);
        }

        private void SetFirstPersonRenderersHidden(bool hidden)
        {
            if (!hideTargetRenderersInFirstPerson)
            {
                hidden = false;
            }

            if (firstPersonRenderersHidden == hidden)
            {
                return;
            }

            Renderer[] renderers = ResolveFirstPersonHiddenRenderers();
            if (renderers.Length == 0)
            {
                firstPersonRenderersHidden = hidden;
                return;
            }

            if (hidden)
            {
                firstPersonRendererOriginalStates = new bool[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    firstPersonRendererOriginalStates[i] = renderer.enabled;
                    renderer.enabled = false;
                }
            }
            else
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    bool originalState = firstPersonRendererOriginalStates != null
                        && i < firstPersonRendererOriginalStates.Length
                        && firstPersonRendererOriginalStates[i];
                    renderer.enabled = originalState;
                }

                firstPersonRendererOriginalStates = null;
            }

            firstPersonRenderersHidden = hidden;
        }

        private Renderer[] ResolveFirstPersonHiddenRenderers()
        {
            if (firstPersonHiddenRenderers != null && firstPersonHiddenRenderers.Length > 0)
            {
                return firstPersonHiddenRenderers;
            }

            if (target == null)
            {
                return System.Array.Empty<Renderer>();
            }

            Transform root = target.root;
            if (root == null)
            {
                return System.Array.Empty<Renderer>();
            }

            ResolveFirstPersonVisibleRoot();

            Renderer[] allRenderers = root.GetComponentsInChildren<Renderer>(true);
            System.Collections.Generic.List<Renderer> hiddenRenderers = new System.Collections.Generic.List<Renderer>(allRenderers.Length);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer renderer = allRenderers[i];
                if (renderer == null || ShouldKeepRendererVisibleInFirstPerson(renderer.transform))
                {
                    continue;
                }

                hiddenRenderers.Add(renderer);
            }

            firstPersonHiddenRenderers = hiddenRenderers.ToArray();
            return firstPersonHiddenRenderers;
        }

        private bool ShouldKeepRendererVisibleInFirstPerson(Transform rendererTransform)
        {
            if (rendererTransform == null)
            {
                return false;
            }

            if (rendererTransform == transform || rendererTransform.IsChildOf(transform))
            {
                return true;
            }

            return firstPersonVisibleRoot != null
                && (rendererTransform == firstPersonVisibleRoot || rendererTransform.IsChildOf(firstPersonVisibleRoot));
        }

        private void ResolveFirstPersonVisibleRoot()
        {
            if (firstPersonVisibleRoot != null || target == null)
            {
                return;
            }

            PlayerWeaponVisualController weaponVisualController =
                target.root.GetComponentInChildren<PlayerWeaponVisualController>(true);
            if (weaponVisualController != null)
            {
                firstPersonVisibleRoot = weaponVisualController.WeaponRoot;
            }
        }

        private float EvaluateHeightOffset(float currentDistance)
        {
            if (currentDistance <= 2f)
            {
                return Mathf.Lerp(minHeightOffset, 0.1f, Mathf.InverseLerp(1f, 2f, currentDistance));
            }

            if (currentDistance <= 4f)
            {
                return Mathf.Lerp(0.1f, 0.2f, Mathf.InverseLerp(2f, 4f, currentDistance));
            }

            return Mathf.Lerp(0.2f, maxHeightOffset, Mathf.InverseLerp(4f, 6f, currentDistance));
        }

        private float NormalizePitch(float value)
        {
            if (value > 180f)
            {
                value -= 360f;
            }

            return value;
        }
    }
}

/*
Unity setup outline:
1. Add MouseLookThirdPersonCamera to the gameplay camera.
2. Assign Target to the player camera target.
3. Enable Allow Runtime Zoom to let PlayerCentralInputController adjust Distance and Height Offset.
4. Tune Min/Max Distance and Zoom Step in the Inspector.
5. Set Camera Collision Layers to world geometry layers and exclude the Player layer.
*/
