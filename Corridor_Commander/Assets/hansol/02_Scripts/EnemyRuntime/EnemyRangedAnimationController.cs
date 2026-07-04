using UnityEngine;

namespace CorridorCommander.Enemy
{
    [DisallowMultipleComponent]
    public sealed class EnemyRangedAnimationController : MonoBehaviour
    {
        [SerializeField] private EnemyRangedAttackController rangedAttackController;
        [SerializeField] private Animator animator;
        [SerializeField] private string throwTriggerParameter = "Throw";

        private int throwTriggerHash;

        private void Awake()
        {
            ResolveReferences();
            throwTriggerHash = Animator.StringToHash(throwTriggerParameter);
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (rangedAttackController != null)
            {
                rangedAttackController.AttackWindupStarted -= HandleAttackWindupStarted;
                rangedAttackController.AttackWindupStarted += HandleAttackWindupStarted;
            }
        }

        private void OnDisable()
        {
            if (rangedAttackController != null)
            {
                rangedAttackController.AttackWindupStarted -= HandleAttackWindupStarted;
            }
        }

        private void HandleAttackWindupStarted()
        {
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(throwTriggerHash);
            animator.SetTrigger(throwTriggerHash);
        }

        private void ResolveReferences()
        {
            if (rangedAttackController == null)
            {
                rangedAttackController = GetComponent<EnemyRangedAttackController>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }
    }
}

/*
Unity setup:
1. Add this component beside EnemyRangedAttackController.
2. Assign the ranged Animator Controller containing a Throw trigger.
3. Leave Animator empty to auto-find the child Animator at runtime.
*/
