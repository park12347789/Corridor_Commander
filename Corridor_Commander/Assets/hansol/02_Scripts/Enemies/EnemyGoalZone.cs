using UnityEngine;

namespace CorridorCommander
{
    [RequireComponent(typeof(Collider))]
    public sealed class EnemyGoalZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            EnemyMovementController enemy = other.GetComponentInParent<EnemyMovementController>();
            if (enemy == null)
            {
                return;
            }

            Health health = enemy.GetComponent<Health>();
            if (health != null)
            {
                DisableRewardEmitters(enemy.gameObject);
                health.Kill(gameObject, other.ClosestPoint(transform.position));
            }
            else
            {
                Destroy(enemy.gameObject);
            }
        }

        private static void DisableRewardEmitters(GameObject enemyRoot)
        {
            if (enemyRoot == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = enemyRoot.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && behaviour.GetType().Name == "EnemyRewardEmitter")
                {
                    behaviour.enabled = false;
                }
            }
        }
    }
}
