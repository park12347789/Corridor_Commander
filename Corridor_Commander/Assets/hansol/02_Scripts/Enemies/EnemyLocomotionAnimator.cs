using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class EnemyLocomotionAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string defaultStateName = "Move";
        [SerializeField] private float fullWalkSpeed = 2.6f;
        [SerializeField] private float dampTime = 0.12f;
        [SerializeField] private bool playDefaultStateOnEnable = true;

        private int moveSpeedHash;
        private Vector3 lastPosition;

        private void Awake()
        {
            ResolveAnimator();
            moveSpeedHash = Animator.StringToHash(moveSpeedParameter);
        }

        private void OnEnable()
        {
            lastPosition = transform.position;
            ResolveAnimator();

            if (animator != null && playDefaultStateOnEnable && !string.IsNullOrWhiteSpace(defaultStateName))
            {
                animator.Play(defaultStateName, 0, 0f);
            }
        }

        private void LateUpdate()
        {
            if (animator == null)
            {
                ResolveAnimator();
                if (animator == null)
                {
                    lastPosition = transform.position;
                    return;
                }
            }

            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;

            float normalizedSpeed = 0f;
            if (Time.deltaTime > 0f && fullWalkSpeed > 0f)
            {
                normalizedSpeed = Mathf.Clamp01(delta.magnitude / Time.deltaTime / fullWalkSpeed);
            }

            animator.SetFloat(moveSpeedHash, normalizedSpeed, dampTime, Time.deltaTime);
            lastPosition = transform.position;
        }

        private void ResolveAnimator()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }
    }
}
