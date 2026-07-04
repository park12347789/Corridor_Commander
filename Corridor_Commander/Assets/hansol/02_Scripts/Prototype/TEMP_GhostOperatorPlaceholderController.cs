using UnityEngine;
using UnityEngine.InputSystem;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TEMP_GhostOperatorPlaceholderController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPitchPivot;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 55f;
        [SerializeField] private bool lockCursorOnPlay = true;

        private IMovementMotor movementMotor;
        private float pitch;

        private void Awake()
        {
            movementMotor = GetComponent<IMovementMotor>();
        }

        private void OnEnable()
        {
            if (lockCursorOnPlay && Application.isPlaying)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Update()
        {
            Keyboard keyboard = KeyboardInputMessenger.CurrentKeyboard;
            Mouse mouse = Mouse.current;

            if (keyboard == null)
            {
                movementMotor?.Stop();
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (mouse != null && !TEMP_CommandInputState.BlocksLookInput)
            {
                Vector2 lookDelta = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, lookDelta.x, 0f);
                pitch = Mathf.Clamp(pitch - lookDelta.y, minPitch, maxPitch);
                if (cameraPitchPivot != null)
                {
                    cameraPitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                }
            }

            Vector3 move = Vector3.zero;
            if (keyboard.wKey.isPressed)
            {
                move += transform.forward;
            }
            if (keyboard.sKey.isPressed)
            {
                move -= transform.forward;
            }
            if (keyboard.dKey.isPressed)
            {
                move += transform.right;
            }
            if (keyboard.aKey.isPressed)
            {
                move -= transform.right;
            }

            movementMotor?.Move(move);
        }
    }
}
