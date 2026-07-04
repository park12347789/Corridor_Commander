using System;
using UnityEngine;
using UnityEngine.Events;

namespace CorridorCommander
{
    public sealed class Health : MonoBehaviour, IDamageTarget
    {
        [SerializeField] private float maxHitPoints = 30f;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private UnityEvent died;

        private float currentHitPoints;
        private bool isDead;

        public float CurrentHitPoints => currentHitPoints;
        public float MaxHitPoints => maxHitPoints;
        public bool IsAlive => !isDead;
        public bool DestroyOnDeath => destroyOnDeath;
        public Transform Transform => transform;

        public event Action<Health, float> Damaged;
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

            float damageAmount = damageInfo.Amount;
            currentHitPoints = Mathf.Max(0f, currentHitPoints - damageAmount);
            Damaged?.Invoke(this, damageAmount);

            if (currentHitPoints <= 0f)
            {
                Die();
            }
        }

        public void Configure(float configuredMaxHitPoints, bool configuredDestroyOnDeath)
        {
            maxHitPoints = Mathf.Max(1f, configuredMaxHitPoints);
            destroyOnDeath = configuredDestroyOnDeath;
            currentHitPoints = maxHitPoints;
            isDead = false;
        }

        public void Kill(GameObject source, Vector3 hitPoint)
        {
            if (isDead)
            {
                return;
            }

            TakeDamage(new DamageInfo(Mathf.Max(1f, currentHitPoints), source, hitPoint));
        }

        public void ScaleMaxHitPoints(float multiplier)
        {
            if (multiplier <= 0f)
            {
                return;
            }

            maxHitPoints = Mathf.Max(1f, maxHitPoints * multiplier);
            currentHitPoints = maxHitPoints;
            isDead = false;
        }

        public void Restore(float amount)
        {
            if (isDead || amount <= 0f)
            {
                return;
            }

            currentHitPoints = Mathf.Min(maxHitPoints, currentHitPoints + amount);
        }

        public bool Repair(float amount)
        {
            if (isDead || amount <= 0f || currentHitPoints >= maxHitPoints)
            {
                return false;
            }

            float previousHitPoints = currentHitPoints;
            currentHitPoints = Mathf.Min(maxHitPoints, currentHitPoints + amount);
            return currentHitPoints > previousHitPoints;
        }

        public bool RestoreToFull()
        {
            if (isDead || currentHitPoints >= maxHitPoints)
            {
                return false;
            }

            currentHitPoints = maxHitPoints;
            return true;
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
