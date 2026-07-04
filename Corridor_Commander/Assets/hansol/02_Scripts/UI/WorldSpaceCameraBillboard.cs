using UnityEngine;

namespace CorridorCommander
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class WorldSpaceCameraBillboard : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool yawOnly = true;
        [SerializeField] private bool faceCameraForward;
        [SerializeField] private bool lockWorldY;

        private bool hasLockedWorldY;
        private float lockedWorldY;

        private void OnEnable()
        {
            CaptureWorldY();
            FaceCamera();
        }

        private void LateUpdate()
        {
            LockWorldY();
            FaceCamera();
        }

        private void CaptureWorldY()
        {
            if (!lockWorldY)
            {
                hasLockedWorldY = false;
                return;
            }

            lockedWorldY = transform.position.y;
            hasLockedWorldY = true;
        }

        private void LockWorldY()
        {
            if (!lockWorldY)
            {
                return;
            }

            if (!hasLockedWorldY)
            {
                CaptureWorldY();
                return;
            }

            Vector3 position = transform.position;
            if (Mathf.Approximately(position.y, lockedWorldY))
            {
                return;
            }

            position.y = lockedWorldY;
            transform.position = position;
        }

        private void FaceCamera()
        {
            Camera cameraToFace = ResolveCamera();
            if (cameraToFace == null)
            {
                return;
            }

            Vector3 direction = faceCameraForward
                ? cameraToFace.transform.forward
                : transform.position - cameraToFace.transform.position;

            if (yawOnly)
            {
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null && targetCamera.isActiveAndEnabled)
            {
                return targetCamera;
            }

            Camera mainCamera = Camera.main;
            if (Application.isPlaying)
            {
                targetCamera = mainCamera;
            }

            return mainCamera;
        }
    }
}
