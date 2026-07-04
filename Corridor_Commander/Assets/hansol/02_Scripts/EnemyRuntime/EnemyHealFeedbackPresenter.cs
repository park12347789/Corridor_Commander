using UnityEngine;

namespace CorridorCommander.Enemy
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private EnemyHealerController healerController;
        [SerializeField] private EnemyHealWorldFeedback feedbackPrefab;
        [SerializeField, Min(0f)] private float heightOffset = 0.45f;

        private void OnEnable()
        {
            ResolveReferences();
            if (healerController != null)
            {
                healerController.HealApplied -= HandleHealApplied;
                healerController.HealApplied += HandleHealApplied;
            }
        }

        private void OnDisable()
        {
            if (healerController != null)
            {
                healerController.HealApplied -= HandleHealApplied;
            }
        }

        private void HandleHealApplied(Health target, float restoredAmount)
        {
            if (target == null || feedbackPrefab == null)
            {
                return;
            }

            Collider targetCollider = target.GetComponentInChildren<Collider>(true);
            Vector3 position = targetCollider != null
                ? new Vector3(
                    targetCollider.bounds.center.x,
                    targetCollider.bounds.max.y + heightOffset,
                    targetCollider.bounds.center.z)
                : target.transform.position + Vector3.up * (1.5f + heightOffset);

            EnemyHealWorldFeedback feedback = Instantiate(
                feedbackPrefab,
                position,
                Quaternion.identity);
            feedback.Initialize(restoredAmount);
        }

        private void ResolveReferences()
        {
            if (healerController == null)
            {
                healerController = GetComponent<EnemyHealerController>();
            }
        }
    }
}

/*
Unity setup:
1. Add this component beside EnemyHealerController.
2. Assign an EnemyHealWorldFeedback prefab.
3. Adjust Height Offset to place the green feedback above the healed target.
*/
