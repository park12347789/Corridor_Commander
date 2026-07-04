using UnityEngine;

namespace CorridorCommander.Enemy
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealerAnimationController : MonoBehaviour
    {
        [SerializeField] private EnemyHealerController healerController;
        [SerializeField] private Animator animator;
        [SerializeField] private string healingTriggerParameter = "Healing";

        private int healingTriggerHash;

        private void Awake()
        {
            ResolveReferences();
            healingTriggerHash = Animator.StringToHash(healingTriggerParameter);
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (healerController != null)
            {
                healerController.HealCastStarted -= HandleHealCastStarted;
                healerController.HealCastStarted += HandleHealCastStarted;
            }
        }

        private void OnDisable()
        {
            if (healerController != null)
            {
                healerController.HealCastStarted -= HandleHealCastStarted;
            }
        }

        private void HandleHealCastStarted()
        {
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(healingTriggerHash);
            animator.SetTrigger(healingTriggerHash);
        }

        private void ResolveReferences()
        {
            if (healerController == null)
            {
                healerController = GetComponent<EnemyHealerController>();
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
1. Add this component beside EnemyHealerController.
2. Use an Animator Controller containing a Healing trigger and Healing state.
3. Leave Animator empty to auto-find the child Animator.
*/
