using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class SawTrapTurretController : MonoBehaviour
    {
        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private float attackInterval = 0.35f;
        [SerializeField] private float damage = 8f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private Transform sawBlade;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float rotationSpeed = 720f;
        [Header("Attack Feedback")]
        [SerializeField] private GameObject attackVfxPrefab;
        [SerializeField] private AudioClip spinLoopClip;
        [SerializeField] private AudioClip[] hitAudioClips;
        [SerializeField, Range(0f, 1f)] private float spinLoopVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float hitAudioVolume = 0.65f;
        [SerializeField, Min(0.05f)] private float attackVfxLifetime = 1f;
        [SerializeField] private int maxTargets = 24;
        [SerializeField] private bool runUpdateLoop = true;

        private readonly List<DamageTarget> damageTargets = new List<DamageTarget>(16);
        private readonly List<IDamageable> collectedDamageables = new List<IDamageable>(16);
        private Collider[] hitBuffer;
        private AudioSource spinLoopSource;
        private float nextAttackTime;
        private bool hasEnemyInRange;

        public float AttackRange => attackRange;
        public float AttackInterval => attackInterval;
        public float Damage => damage;
        public bool HasEnemyInRange => hasEnemyInRange;

        private void Reset()
        {
            ResolveSawBladeReference();
        }

        private void Awake()
        {
            ResolveSawBladeReference();
            EnsureHitBuffer();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            attackRange = Mathf.Max(0f, attackRange);
            attackInterval = Mathf.Max(0.01f, attackInterval);
            damage = Mathf.Max(0f, damage);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            maxTargets = Mathf.Clamp(maxTargets, 1, 128);
            ResolveSawBladeReference();
            EnsureHitBuffer();
        }
#endif

        private void Update()
        {
            if (runUpdateLoop)
            {
                TickTrap();
            }
        }

        public void TickTrap()
        {
            CollectTargets();
            hasEnemyInRange = damageTargets.Count > 0;

            if (!hasEnemyInRange)
            {
                StopSpinLoop();
                return;
            }

            RotateSawBlade();
            PlaySpinLoop();

            if (Time.time < nextAttackTime)
            {
                return;
            }

            ApplyDamage();
            nextAttackTime = Time.time + attackInterval;
        }

        public void SetUpdateLoopEnabled(bool enabled)
        {
            runUpdateLoop = enabled;
        }

        public void Configure(float configuredRange, float configuredAttackInterval, float configuredDamage)
        {
            attackRange = Mathf.Max(0f, configuredRange);
            attackInterval = Mathf.Max(0.01f, configuredAttackInterval);
            damage = Mathf.Max(0f, configuredDamage);
        }

        private void CollectTargets()
        {
            EnsureHitBuffer();
            damageTargets.Clear();
            collectedDamageables.Clear();

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                attackRange,
                hitBuffer,
                targetLayers.value,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                TryCollectTarget(hitBuffer[i]);
                hitBuffer[i] = null;
            }
        }

        private void TryCollectTarget(Collider targetCollider)
        {
            if (targetCollider == null || targetCollider.transform.root == transform.root)
            {
                return;
            }

            if (targetCollider.GetComponentInParent<EnemyMovementController>() == null)
            {
                return;
            }

            IDamageable damageable = targetCollider.GetComponentInParent<IDamageable>();
            if (damageable == null || collectedDamageables.Contains(damageable))
            {
                return;
            }

            Health health = targetCollider.GetComponentInParent<Health>();
            if (health != null && !health.IsAlive)
            {
                return;
            }

            collectedDamageables.Add(damageable);
            damageTargets.Add(new DamageTarget(damageable, targetCollider.ClosestPoint(transform.position)));
        }

        private void ApplyDamage()
        {
            Vector3 feedbackPosition = sawBlade != null ? sawBlade.position : transform.position;
            RuntimeFeedbackUtility.SpawnVfx(attackVfxPrefab, feedbackPosition, attackVfxLifetime);
            RuntimeFeedbackUtility.PlayRandomClip(hitAudioClips, feedbackPosition, hitAudioVolume, "SawTrapHitSfx");

            for (int i = 0; i < damageTargets.Count; i++)
            {
                DamageTarget target = damageTargets[i];
                target.Damageable.TakeDamage(new DamageInfo(damage, gameObject, target.HitPoint));
            }
        }

        private void PlaySpinLoop()
        {
            if (spinLoopClip == null)
            {
                return;
            }

            if (spinLoopSource == null)
            {
                spinLoopSource = gameObject.AddComponent<AudioSource>();
                spinLoopSource.spatialBlend = 1f;
                spinLoopSource.loop = true;
                spinLoopSource.playOnAwake = false;
            }

            spinLoopSource.clip = spinLoopClip;
            spinLoopSource.volume = spinLoopVolume;
            if (!spinLoopSource.isPlaying)
            {
                spinLoopSource.Play();
            }
        }

        private void StopSpinLoop()
        {
            if (spinLoopSource != null && spinLoopSource.isPlaying)
            {
                spinLoopSource.Stop();
            }
        }

        private void RotateSawBlade()
        {
            if (sawBlade == null || rotationSpeed <= 0f)
            {
                return;
            }

            Vector3 axis = rotationAxis.sqrMagnitude > 0.0001f ? rotationAxis.normalized : Vector3.up;
            sawBlade.Rotate(axis, rotationSpeed * Time.deltaTime, Space.Self);
        }

        private void ResolveSawBladeReference()
        {
            if (sawBlade != null)
            {
                return;
            }

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name.Contains("sawblade"))
                {
                    sawBlade = children[i];
                    return;
                }
            }
        }

        private void EnsureHitBuffer()
        {
            int bufferSize = Mathf.Clamp(maxTargets, 1, 128);
            if (hitBuffer == null || hitBuffer.Length != bufferSize)
            {
                hitBuffer = new Collider[bufferSize];
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        private readonly struct DamageTarget
        {
            public DamageTarget(IDamageable damageable, Vector3 hitPoint)
            {
                Damageable = damageable;
                HitPoint = hitPoint;
            }

            public IDamageable Damageable { get; }
            public Vector3 HitPoint { get; }
        }
    }
}
