using UnityEngine;

namespace CorridorCommander
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMovementMotor : MonoBehaviour, IMovementMotor
    {
        [SerializeField] private MovementStats stats = new MovementStats();
        [SerializeField] private float gravity = -18f;

        private CharacterController characterController;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
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
            Vector3 velocity = flattenedDirection * stats.moveSpeed;
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
    }
}
