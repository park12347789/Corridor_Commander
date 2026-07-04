using UnityEngine;
using UnityEngine.InputSystem;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class TEMP_KayKitThirdPersonTestController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPitchPivot;
        [SerializeField] private Animator animator;
        [SerializeField] private float walkSpeed = 2.4f;
        [SerializeField] private float runSpeed = 5.2f;
        [SerializeField] private float jumpHeight = 1.35f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 55f;
        [SerializeField] private bool lockCursorOnPlay = true;

        private CharacterController characterController;
        private float pitch;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
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
                Move(Vector3.zero, false, false);
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

            Vector3 moveDirection = Vector3.zero;
            if (keyboard.wKey.isPressed)
            {
                moveDirection += transform.forward;
            }
            if (keyboard.sKey.isPressed)
            {
                moveDirection -= transform.forward;
            }
            if (keyboard.dKey.isPressed)
            {
                moveDirection += transform.right;
            }
            if (keyboard.aKey.isPressed)
            {
                moveDirection -= transform.right;
            }

            bool wantsJump = keyboard.spaceKey.wasPressedThisFrame;
            Move(moveDirection, keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed, wantsJump);
        }

        private void Move(Vector3 direction, bool wantsRun, bool wantsJump)
        {
            Vector3 flattenedDirection = new Vector3(direction.x, 0f, direction.z);
            if (flattenedDirection.sqrMagnitude > 1f)
            {
                flattenedDirection.Normalize();
            }

            bool isGrounded = characterController.isGrounded;
            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            if (isGrounded && wantsJump)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;

            bool isMoving = flattenedDirection.sqrMagnitude > 0.001f;
            float speed = wantsRun ? runSpeed : walkSpeed;
            Vector3 velocity = flattenedDirection * speed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);

            if (animator != null)
            {
                float normalizedSpeed = isMoving ? (wantsRun ? 1f : 0.5f) : 0f;
                animator.SetFloat("MoveSpeed", normalizedSpeed, 0.12f, Time.deltaTime);
            }
        }
    }
}
