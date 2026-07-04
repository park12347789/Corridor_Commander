using UnityEngine;

namespace CorridorCommander
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DirectCharacterMovementMotor : MonoBehaviour, IMovementMotor, IMoveSpeedMultiplierReceiver
    {
        [SerializeField] private MovementStats stats = new MovementStats();
        [SerializeField] private float gravity = -18f;
        [SerializeField] private bool rotateTowardMovement = true;

        private CharacterController characterController;
        private IStatusEffectReceiver statusEffectReceiver;
        private float moveSpeedMultiplier = 1f;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            ResolveStatusEffectReceiver();
        }

        public void Move(Vector3 direction)
        {
            if (characterController == null)
            {
                return;
            }

            Vector3 flattenedDirection = new Vector3(direction.x, 0f, direction.z);
            if (flattenedDirection.sqrMagnitude > 1f)
            {
                flattenedDirection.Normalize();
            }

            ApplyGravity();
            Vector3 velocity = flattenedDirection * GetMoveSpeed();
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);

            if (rotateTowardMovement)
            {
                RotateToward(flattenedDirection);
            }
        }

        public void MoveTo(Vector3 worldPosition)
        {
            Vector3 direction = worldPosition - transform.position;
            direction.y = 0f;

            if (direction.magnitude <= stats.stoppingDistance)
            {
                Stop();
                return;
            }

            Move(direction.normalized);
        }

        public void Stop()
        {
            Move(Vector3.zero);
        }

        public void SetMoveSpeedMultiplier(float multiplier)
        {
            moveSpeedMultiplier = Mathf.Max(0.01f, multiplier);
        }

        private void ApplyGravity()
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * Time.deltaTime;
        }

        private void RotateToward(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, stats.rotationSpeed * Time.deltaTime);
        }

        private float GetMoveSpeed()
        {
            ResolveStatusEffectReceiver();
            float statusMultiplier = statusEffectReceiver != null ? statusEffectReceiver.MoveSpeedMultiplier : 1f;
            return stats.moveSpeed * moveSpeedMultiplier * statusMultiplier;
        }

        private void ResolveStatusEffectReceiver()
        {
            if (statusEffectReceiver != null)
            {
                return;
            }

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IStatusEffectReceiver receiver)
                {
                    statusEffectReceiver = receiver;
                    return;
                }
            }
        }
    }
}
