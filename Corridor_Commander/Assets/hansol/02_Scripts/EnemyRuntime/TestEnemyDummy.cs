using UnityEngine;
using CorridorCommander;

namespace CorridorCommander.TestEnemy
{
    public sealed class TestEnemyDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHitPoints = 30f;
        [SerializeField] private bool destroyOnDeath = true;

        private float currentHitPoints;
        private bool isDead;

        private void Awake()
        {
            currentHitPoints = maxHitPoints;
            isDead = false;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (isDead)
            {
                return;
            }

            if (damageInfo.Amount <= 0f)
            {
                return;
            }

            currentHitPoints = Mathf.Max(0f, currentHitPoints - damageInfo.Amount);

            Debug.Log(
                $"[TestEnemyDummy] Hit! Damage: {damageInfo.Amount}, HP: {currentHitPoints}/{maxHitPoints}"
            );

            if (currentHitPoints <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;

            Debug.Log("[TestEnemyDummy] Dead");

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}