using System;
using UnityEngine;
using UnityEngine.Events;

namespace CorridorCommander
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHitPoints = 30f;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private UnityEvent died;

        private float currentHitPoints;
        private bool isDead;

        public float CurrentHitPoints => currentHitPoints;
        public float MaxHitPoints => maxHitPoints;
        public bool IsAlive => !isDead;

        public event Action<Health> Died;

        private void Awake()
        {
            currentHitPoints = maxHitPoints;
            isDead = false;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (isDead || damageInfo.Amount <= 0f)
            {
                return;
            }

            currentHitPoints = Mathf.Max(0f, currentHitPoints - damageInfo.Amount);
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
            Died?.Invoke(this);
            died?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}
