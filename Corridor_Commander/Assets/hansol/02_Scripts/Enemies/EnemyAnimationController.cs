using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class EnemyAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private RuntimeAnimatorController fallbackAnimatorController;
        [SerializeField] private EnemyMeleeAttackController meleeAttackController;
        [SerializeField] private Health health;
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string attackTriggerParameter = "Attack";
        [SerializeField] private string deathTriggerParameter = "Death";
        [SerializeField] private string defaultStateName = "Move";
        [SerializeField] private float fullWalkSpeed = 2.6f;
        [SerializeField] private float dampTime = 0.12f;
        [SerializeField] private float destroyDelay = 1.35f;
        [SerializeField] private bool playDefaultStateOnEnable = true;
        [SerializeField] private bool runUpdateLoop = true;
        [SerializeField] private bool destroyAfterDeath = true;

        private int moveSpeedHash;
        private int attackTriggerHash;
        private int deathTriggerHash;
        private Vector3 lastPosition;
        private bool isDead;
        private bool destroyScheduled;
        private bool missingControllerWarningLogged;

        private void Awake()
        {
            ResolveComponents();
            CacheHashes();
        }

        private void OnEnable()
        {
            lastPosition = transform.position;
            isDead = health != null && !health.IsAlive;
            destroyScheduled = false;
            ResolveComponents();
            Subscribe();

            if (HasPlayableAnimator() && playDefaultStateOnEnable && !string.IsNullOrWhiteSpace(defaultStateName))
            {
                animator.Play(defaultStateName, 0, 0f);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (runUpdateLoop)
            {
                TickAnimation();
            }
        }

        public void SetUpdateLoopEnabled(bool enabled)
        {
            runUpdateLoop = enabled;
        }

        public void TickAnimation()
        {
            if (!HasPlayableAnimator())
            {
                lastPosition = transform.position;
                return;
            }

            float normalizedSpeed = isDead ? 0f : CalculateNormalizedSpeed();
            animator.SetFloat(moveSpeedHash, normalizedSpeed, dampTime, Time.deltaTime);
            lastPosition = transform.position;
        }

        private float CalculateNormalizedSpeed()
        {
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;

            if (Time.deltaTime <= 0f || fullWalkSpeed <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(delta.magnitude / Time.deltaTime / fullWalkSpeed);
        }

        private void HandleAttackPerformed()
        {
            if (isDead || !HasPlayableAnimator())
            {
                return;
            }

            animator.ResetTrigger(attackTriggerHash);
            animator.SetTrigger(attackTriggerHash);
        }

        private void HandleDied(Health deadHealth)
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            TickAnimation();

            if (HasPlayableAnimator())
            {
                animator.ResetTrigger(attackTriggerHash);
                animator.SetTrigger(deathTriggerHash);
            }

            if (destroyAfterDeath && !destroyScheduled && (deadHealth == null || !deadHealth.DestroyOnDeath))
            {
                destroyScheduled = true;
                Destroy(gameObject, destroyDelay);
            }
        }

        private void ResolveComponents()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (meleeAttackController == null)
            {
                meleeAttackController = GetComponent<EnemyMeleeAttackController>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        private bool HasPlayableAnimator()
        {
            if (animator == null)
            {
                ResolveComponents();
            }

            if (animator == null)
            {
                return false;
            }

            if (animator.runtimeAnimatorController == null && fallbackAnimatorController != null)
            {
                animator.runtimeAnimatorController = fallbackAnimatorController;
            }

            if (animator.runtimeAnimatorController != null)
            {
                return true;
            }

            if (!missingControllerWarningLogged)
            {
                Debug.LogWarning("[EnemyAnimationController] AnimatorController is not assigned.", this);
                missingControllerWarningLogged = true;
            }

            return false;
        }

        private void CacheHashes()
        {
            moveSpeedHash = Animator.StringToHash(moveSpeedParameter);
            attackTriggerHash = Animator.StringToHash(attackTriggerParameter);
            deathTriggerHash = Animator.StringToHash(deathTriggerParameter);
        }

        private void Subscribe()
        {
            if (meleeAttackController != null)
            {
                meleeAttackController.AttackPerformed -= HandleAttackPerformed;
                meleeAttackController.AttackPerformed += HandleAttackPerformed;
            }

            if (health != null)
            {
                health.Died -= HandleDied;
                health.Died += HandleDied;
            }
        }

        private void Unsubscribe()
        {
            if (meleeAttackController != null)
            {
                meleeAttackController.AttackPerformed -= HandleAttackPerformed;
            }

            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }
    }
}
