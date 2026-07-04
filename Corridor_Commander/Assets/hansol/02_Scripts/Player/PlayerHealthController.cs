using UnityEngine;
using CorridorCommander.PlayerCombat;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerHealthController : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Health health;
        [SerializeField] private float maxHitPoints = 100f;
        [SerializeField] private bool destroyPlayerOnDeath = false;
        [SerializeField] private PlayerStatModifier statModifier;

        [Header("Disable On Death")]
        [SerializeField] private PlayerCentralInputController inputController;
        [SerializeField] private PlayerLocomotionController locomotionController;
        [SerializeField] private PlayerFacingController facingController;
        [SerializeField] private PlayerProjectileLauncher weaponLauncher;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string deathTriggerName = "Die";
        [SerializeField] private string hitTriggerName = "Hit";

        private float previousHitPoints;

        public Health Health => health;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            ResolveOptionalReferences();
            ConfigureHealthToFull();
            previousHitPoints = health.CurrentHitPoints;
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDied;
            }

            if (statModifier != null)
            {
                statModifier.StatsChanged += HandleStatsChanged;
            }

            if (ArtifactStatManager.Current != null)
            {
                ArtifactStatManager.Current.StatsChanged += HandleStatsChanged;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }

            if (statModifier != null)
            {
                statModifier.StatsChanged -= HandleStatsChanged;
            }

            if (ArtifactStatManager.Current != null)
            {
                ArtifactStatManager.Current.StatsChanged -= HandleStatsChanged;
            }
        }

        private void Update()
        {
            DetectHitPointChanged();
        }

        private void DetectHitPointChanged()
        {
            if (health == null || !health.IsAlive)
            {
                return;
            }

            float currentHitPoints = health.CurrentHitPoints;

            if (currentHitPoints < previousHitPoints)
            {
                HandleDamaged(previousHitPoints - currentHitPoints);
            }

            previousHitPoints = currentHitPoints;
        }

        private void HandleDamaged(float damageAmount)
        {
            Debug.Log($"[PlayerHealthController] Player Damaged: {damageAmount}, HP: {health.CurrentHitPoints}/{health.MaxHitPoints}");

            if (animator != null && !string.IsNullOrEmpty(hitTriggerName))
            {
                animator.SetTrigger(hitTriggerName);
            }
        }

        private void HandleStatsChanged()
        {
            ConfigureHealthPreservingMissingHitPoints();
            previousHitPoints = health != null ? health.CurrentHitPoints : 0f;
        }

        private void ConfigureHealthToFull()
        {
            if (health == null)
            {
                return;
            }

            float finalMaxHitPoints = Mathf.Max(
                1f,
                ArtifactStatManager.Apply(ArtifactTarget.Player, ArtifactStat.Health, maxHitPoints + GetHealthBonus()));
            health.Configure(finalMaxHitPoints, destroyPlayerOnDeath);
            Debug.Log($"[PlayerHealthController] Max HP Applied: {health.MaxHitPoints}");
        }

        private void ConfigureHealthPreservingMissingHitPoints()
        {
            if (health == null)
            {
                return;
            }

            float missingHitPoints = Mathf.Max(0f, health.MaxHitPoints - health.CurrentHitPoints);
            float finalMaxHitPoints = Mathf.Max(
                1f,
                ArtifactStatManager.Apply(ArtifactTarget.Player, ArtifactStat.Health, maxHitPoints + GetHealthBonus()));

            health.Configure(finalMaxHitPoints, destroyPlayerOnDeath);

            if (missingHitPoints > 0f && missingHitPoints < health.CurrentHitPoints)
            {
                health.TakeDamage(new DamageInfo(missingHitPoints, gameObject, transform.position));
            }

            Debug.Log($"[PlayerHealthController] Max HP Applied: {health.MaxHitPoints}");
        }

        private float GetHealthBonus()
        {
            return statModifier != null ? statModifier.HealthBonus : 0f;
        }

        private void HandleDied(Health deadHealth)
        {
            Debug.Log("[PlayerHealthController] Player Died");

            if (inputController != null)
            {
                inputController.enabled = false;
            }

            if (locomotionController != null)
            {
                locomotionController.enabled = false;
            }

            if (facingController != null)
            {
                facingController.enabled = false;
            }

            if (weaponLauncher != null)
            {
                weaponLauncher.enabled = false;
            }

            if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
            {
                animator.SetTrigger(deathTriggerName);
            }
        }

        private void ResolveOptionalReferences()
        {
            if (statModifier == null)
            {
                statModifier = GetComponent<PlayerStatModifier>();
            }

            if (statModifier == null)
            {
                statModifier = GetComponentInParent<PlayerStatModifier>();
            }

            if (statModifier == null)
            {
                statModifier = GetComponentInChildren<PlayerStatModifier>(true);
            }
        }
    }
}


