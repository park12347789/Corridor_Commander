using UnityEngine;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerLocomotionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera moveReferenceCamera;
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerStatModifier statModifier;
        [SerializeField] private PlayerStaminaController staminaController;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.4f;
        [SerializeField] private float runSpeed = 5.2f;
        [SerializeField] private float acceleration = 12f;

        [Header("Jump / Gravity")]
        [SerializeField] private float jumpHeight = 1.35f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float groundedStickVelocity = -1f;

        [Header("Animator Parameters")]
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string groundedParameter = "IsGrounded";
        [SerializeField] private string verticalVelocityParameter = "VerticalVelocity";
        [SerializeField] private string jumpTriggerParameter = "Jump";

        private CharacterController characterController;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        private Vector2 moveInput;
        private bool runHeld;
        private bool jumpRequested;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (moveReferenceCamera == null)
            {
                moveReferenceCamera = Camera.main;
            }

            ResolveOptionalReferences();
        }

        private void Update()
        {
            Vector3 moveDirection = ConvertToCameraDirection(moveInput);

            Move(moveDirection, runHeld, jumpRequested);

            jumpRequested = false;
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = input;

            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }
        }

        public void SetRunHeld(bool value)
        {
            runHeld = value;
        }

        public void RequestJump()
        {
            jumpRequested = true;
        }

        public void ClearMoveInput()
        {
            moveInput = Vector2.zero;
            runHeld = false;
            jumpRequested = false;
        }

        private Vector3 ConvertToCameraDirection(Vector2 input)
        {
            Vector3 inputDirection = new Vector3(input.x, 0f, input.y);

            if (moveReferenceCamera == null)
            {
                return inputDirection;
            }

            Vector3 cameraForward = moveReferenceCamera.transform.forward;
            Vector3 cameraRight = moveReferenceCamera.transform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            if (cameraForward.sqrMagnitude > 0.0001f)
            {
                cameraForward.Normalize();
            }

            if (cameraRight.sqrMagnitude > 0.0001f)
            {
                cameraRight.Normalize();
            }

            return cameraForward * inputDirection.z + cameraRight * inputDirection.x;
        }

        private void Move(Vector3 direction, bool wantsRun, bool wantsJump)
        {
            Vector3 flattenedDirection = new Vector3(direction.x, 0f, direction.z);

            if (flattenedDirection.sqrMagnitude > 1f)
            {
                flattenedDirection.Normalize();
            }

            bool isGrounded = characterController.isGrounded;
            bool hasMoveInput = flattenedDirection.sqrMagnitude > 0.001f;
            bool canRun = staminaController == null || staminaController.CanRun;
            bool isRunning = wantsRun && hasMoveInput && canRun;

            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickVelocity;
            }

            if (isGrounded && wantsJump && TryConsumeJumpStamina())
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                TriggerJumpAnimation();
            }

            verticalVelocity += gravity * Time.deltaTime;

            float targetSpeed = isRunning ? GetFinalRunSpeed() : GetFinalWalkSpeed();
            Vector3 targetHorizontalVelocity = flattenedDirection * targetSpeed;

            horizontalVelocity = Vector3.Lerp(
                horizontalVelocity,
                targetHorizontalVelocity,
                acceleration * Time.deltaTime
            );

            Vector3 finalVelocity = horizontalVelocity;
            finalVelocity.y = verticalVelocity;

            characterController.Move(finalVelocity * Time.deltaTime);
            staminaController?.TickStamina(isRunning);

            float normalizedMoveSpeed = GetNormalizedMoveSpeed(flattenedDirection, isRunning);
            UpdateAnimator(characterController.isGrounded, normalizedMoveSpeed);
        }

        private bool TryConsumeJumpStamina()
        {
            return staminaController == null || staminaController.TryConsumeJumpStamina();
        }

        private void TriggerJumpAnimation()
        {
            if (animator == null || string.IsNullOrEmpty(jumpTriggerParameter))
            {
                return;
            }

            animator.SetTrigger(jumpTriggerParameter);
        }

        private float GetFinalWalkSpeed()
        {
            return Mathf.Max(
                0f,
                ArtifactStatManager.Apply(ArtifactTarget.Player, ArtifactStat.MoveSpeed, walkSpeed + GetMoveSpeedBonus()));
        }

        private float GetFinalRunSpeed()
        {
            return Mathf.Max(
                0f,
                ArtifactStatManager.Apply(ArtifactTarget.Player, ArtifactStat.MoveSpeed, runSpeed + GetMoveSpeedBonus()));
        }

        private float GetMoveSpeedBonus()
        {
            return statModifier != null ? statModifier.MoveSpeedBonus : 0f;
        }

        private float GetNormalizedMoveSpeed(Vector3 direction, bool wantsRun)
        {
            if (direction.sqrMagnitude <= 0.001f)
            {
                return 0f;
            }

            return wantsRun ? 1f : 0.5f;
        }

        private void UpdateAnimator(bool isGrounded, float normalizedMoveSpeed)
        {
            if (animator == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(moveSpeedParameter))
            {
                animator.SetFloat(moveSpeedParameter, normalizedMoveSpeed, 0.12f, Time.deltaTime);
            }

            if (!string.IsNullOrEmpty(groundedParameter))
            {
                animator.SetBool(groundedParameter, isGrounded);
            }

            if (!string.IsNullOrEmpty(verticalVelocityParameter))
            {
                animator.SetFloat(verticalVelocityParameter, verticalVelocity);
            }
        }

        private void ResolveOptionalReferences()
        {
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

            if (staminaController == null)
            {
                staminaController = GetComponent<PlayerStaminaController>();
            }

            if (staminaController == null)
            {
                staminaController = GetComponentInParent<PlayerStaminaController>();
            }

            if (staminaController == null)
            {
                staminaController = GetComponentInChildren<PlayerStaminaController>(true);
            }
        }
    }
}
