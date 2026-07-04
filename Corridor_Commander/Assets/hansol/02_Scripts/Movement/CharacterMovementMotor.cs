using UnityEngine;

namespace CorridorCommander
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMovementMotor : MonoBehaviour, IMovementMotor, IMoveSpeedMultiplierReceiver
    {
        [SerializeField] private MovementStats stats = new MovementStats();
        [SerializeField] private float gravity = -18f;

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

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = flattenedDirection * GetMoveSpeed();
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        public void MoveTo(Vector3 worldPosition)
        {
            Vector3 direction = worldPosition - transform.position;
            Move(direction);
        }

        public void Stop()
        {
            Move(Vector3.zero);
        }

        public void SetMoveSpeedMultiplier(float multiplier)
        {
            moveSpeedMultiplier = Mathf.Max(0.01f, multiplier);
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
