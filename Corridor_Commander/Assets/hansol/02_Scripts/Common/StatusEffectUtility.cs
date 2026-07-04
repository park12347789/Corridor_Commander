using UnityEngine;

namespace CorridorCommander
{
    public static class StatusEffectUtility
    {
        public static void ApplyToTarget(
            IDamageable target,
            StatusEffectDefinitionSO[] effects,
            GameObject source,
            Vector3 hitPoint)
        {
            if (target == null || effects == null || effects.Length == 0)
            {
                return;
            }

            IStatusEffectReceiver receiver = ResolveReceiver(target);
            if (receiver == null)
            {
                return;
            }

            for (int i = 0; i < effects.Length; i++)
            {
                receiver.ApplyStatusEffect(effects[i], source, hitPoint);
            }
        }

        private static IStatusEffectReceiver ResolveReceiver(IDamageable target)
        {
            if (target is not Component component)
            {
                return null;
            }

            IStatusEffectReceiver existingReceiver = FindReceiver(component);
            if (existingReceiver != null)
            {
                return existingReceiver;
            }

            EnemyMovementController enemyMovement = component.GetComponentInParent<EnemyMovementController>();
            if (enemyMovement == null)
            {
                return null;
            }

            StatusEffectReceiver receiver = enemyMovement.GetComponent<StatusEffectReceiver>();
            if (receiver == null)
            {
                receiver = enemyMovement.gameObject.AddComponent<StatusEffectReceiver>();
            }

            return receiver;
        }

        private static IStatusEffectReceiver FindReceiver(Component component)
        {
            MonoBehaviour[] behaviours = component.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IStatusEffectReceiver receiver)
                {
                    return receiver;
                }
            }

            return null;
        }
    }
}
