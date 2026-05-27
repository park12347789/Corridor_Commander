using UnityEngine;

namespace CorridorCommander
{
    [RequireComponent(typeof(Collider))]
    public sealed class EnemyGoalZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            EnemyMovementController enemy = other.GetComponentInParent<EnemyMovementController>();
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
    }
}
