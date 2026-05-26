using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class EnemyMovementController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float refreshInterval = 0.25f;
        [SerializeField] private bool runUpdateLoop = true;

        private IMovementMotor movementMotor;
        private float nextRefreshTime;
        private bool isPaused;

        private void Awake()
        {
            movementMotor = GetComponent<IMovementMotor>();
        }

        private void Update()
        {
            if (runUpdateLoop)
            {
                TickMovement();
            }
        }

        public void TickMovement()
        {
            if (isPaused)
            {
                movementMotor?.Stop();
                return;
            }

            if (target == null || movementMotor == null || Time.time < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.time + refreshInterval;
            movementMotor.MoveTo(target.position);
        }

        public void SetUpdateLoopEnabled(bool enabled)
        {
            runUpdateLoop = enabled;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            nextRefreshTime = 0f;
        }

        public void SetPaused(bool paused)
        {
            if (isPaused == paused)
            {
                return;
            }

            isPaused = paused;
            nextRefreshTime = 0f;

            if (isPaused)
            {
                movementMotor?.Stop();
            }
        }
    }
}
