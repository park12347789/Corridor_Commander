using UnityEngine;

namespace CorridorCommander.PlayerControl
{
    public sealed class PlayerFacingController : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float minimumMoveDistance = 0.001f;

        [Header("Aim")]
        [SerializeField] private bool faceCameraWhileAimHeld = true;
        [SerializeField] private bool faceCameraWhileFireHeld = true;

        private Vector3 lastPosition;
        private bool aimHeld;
        private bool fireHeld;
        private bool throwableAimHeld;

        public bool IsAimHeld => aimHeld;
        public bool IsFireHeld => fireHeld;
        public bool IsThrowableAimHeld => throwableAimHeld;

        private void Start()
        {
            lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (ShouldFaceCameraDirection())
            {
                FaceCameraDirection();
            }
            else
            {
                FaceMovementDirection();
            }

            lastPosition = transform.position;
        }

        public void SetAimHeld(bool value)
        {
            aimHeld = value;
        }

        public void SetFireHeld(bool value)
        {
            fireHeld = value;
        }

        public void SetThrowableAimHeld(bool value)
        {
            throwableAimHeld = value;
        }

        public void ClearCombatInput()
        {
            aimHeld = false;
            fireHeld = false;
            throwableAimHeld = false;
        }

        private bool ShouldFaceCameraDirection()
        {
            bool isAiming = faceCameraWhileAimHeld && aimHeld;
            bool isFiring = faceCameraWhileFireHeld && fireHeld;

            return isAiming || isFiring || throwableAimHeld;
        }

        private void FaceCameraDirection()
        {
            if (aimCamera == null)
            {
                return;
            }

            Vector3 cameraForward = aimCamera.transform.forward;
            cameraForward.y = 0f;

            if (cameraForward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            RotateToward(cameraForward.normalized);
        }

        private void FaceMovementDirection()
        {
            Vector3 movementDelta = transform.position - lastPosition;
            movementDelta.y = 0f;

            if (movementDelta.sqrMagnitude <= minimumMoveDistance * minimumMoveDistance)
            {
                return;
            }

            RotateToward(movementDelta.normalized);
        }

        private void RotateToward(Vector3 direction)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
