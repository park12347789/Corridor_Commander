using CorridorCommander.PlayerCamera;
using CorridorCommander.PlayerControl;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class FieldInteractionAimVisibilityController : MonoBehaviour
    {
        [Header("State Sources")]
        [SerializeField] private PlayerFacingController facingController;
        [SerializeField] private MouseLookThirdPersonCamera thirdPersonCamera;

        [Header("Suppression")]
        [SerializeField] private bool suppressWhileAimHeld = true;
        [SerializeField] private bool suppressWhileThrowableAim = true;
        [SerializeField] private bool suppressWhenZoomedIn = true;
        [SerializeField] [Min(0.1f)] private float zoomSuppressDistance = 3f;
        [SerializeField] [Min(0f)] private float zoomReleasePadding = 0.15f;

        private bool isZoomSuppressed;

        public static bool IsSuppressed { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            RefreshSuppression();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshSuppression();
        }

        private void Update()
        {
            ResolveReferences();
            RefreshSuppression();
        }

        private void OnDisable()
        {
            if (IsSuppressed)
            {
                IsSuppressed = false;
            }

            isZoomSuppressed = false;
        }

        private void RefreshSuppression()
        {
            bool aimSuppressed = suppressWhileAimHeld
                && facingController != null
                && facingController.IsAimHeld;

            bool throwableSuppressed = suppressWhileThrowableAim
                && facingController != null
                && facingController.IsThrowableAimHeld;

            bool zoomSuppressed = ResolveZoomSuppressed();
            IsSuppressed = aimSuppressed || throwableSuppressed || zoomSuppressed;
        }

        private bool ResolveZoomSuppressed()
        {
            if (!suppressWhenZoomedIn || thirdPersonCamera == null)
            {
                isZoomSuppressed = false;
                return false;
            }

            float currentDistance = thirdPersonCamera.CurrentDistance;
            if (currentDistance <= zoomSuppressDistance)
            {
                isZoomSuppressed = true;
            }
            else if (currentDistance > zoomSuppressDistance + zoomReleasePadding)
            {
                isZoomSuppressed = false;
            }

            return isZoomSuppressed;
        }

        private void ResolveReferences()
        {
            if (facingController == null)
            {
                facingController = FindFirstObjectByType<PlayerFacingController>();
            }

            if (thirdPersonCamera == null)
            {
                thirdPersonCamera = FindFirstObjectByType<MouseLookThirdPersonCamera>();
            }
        }
    }
}
