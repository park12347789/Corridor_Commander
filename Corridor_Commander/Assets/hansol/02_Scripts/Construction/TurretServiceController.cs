using UnityEngine;
using CorridorCommander.PlayerControl;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TurretTargetingController))]
    [RequireComponent(typeof(Health))]
    public sealed class TurretServiceController : MonoBehaviour, IInstalledUpgradeLevelProvider
    {
        [SerializeField] private TurretTargetingController targetingController;
        [SerializeField] private TurretRangeIndicator rangeIndicator;
        [SerializeField] private Health health;
        [SerializeField] private InstalledBuildableState installedState;
        [SerializeField] private BuildableLifecycleFeedback lifecycleFeedback;
        [SerializeField] private int maxUpgradeLevel = 3;
        [SerializeField] private float rangePerLevel = 1.25f;
        [SerializeField] private float damagePerLevel = 2f;
        [SerializeField] private float fireIntervalMultiplierPerLevel = 0.9f;
        [SerializeField] private float repairAmount = 999f;
        [SerializeField] [Min(0)] private int upgradeCost = 2;
        [SerializeField] [Min(0)] private int repairCost = 2;
        [SerializeField] [Range(0f, 1f)] private float dismantleRefundRate = 1f;

        private int upgradeLevel;
        private TurretAttackDefinitionSO runtimeAttackDefinition;
        private float baseRange = 7f;
        private float baseFireInterval = 0.75f;
        private float baseDamage = 6f;

        public string DisplayName => runtimeAttackDefinition != null && !string.IsNullOrWhiteSpace(runtimeAttackDefinition.DisplayName)
            ? runtimeAttackDefinition.DisplayName
            : "Turret";
        public int UpgradeLevel => upgradeLevel;
        public int CurrentUpgradeLevel => upgradeLevel;
        public int MaxUpgradeLevel => runtimeAttackDefinition != null
            ? runtimeAttackDefinition.MaxUpgradeLevel
            : Mathf.Max(0, maxUpgradeLevel);
        public int VisibleUpgradeStars => Mathf.Clamp(upgradeLevel, 0, 3);
        public bool CanUpgrade => upgradeLevel < MaxUpgradeLevel;
        public bool CanRepair => health != null && health.IsAlive && health.CurrentHitPoints < health.MaxHitPoints;
        public float CurrentHitPoints => health != null ? health.CurrentHitPoints : 0f;
        public float MaxHitPoints => health != null ? health.MaxHitPoints : 0f;
        public float HealthFillAmount => MaxHitPoints > 0f ? Mathf.Clamp01(CurrentHitPoints / MaxHitPoints) : 0f;
        public int UpgradeCost => Mathf.Max(0, upgradeCost);
        public int RepairCost => Mathf.Max(0, repairCost);
        public int DismantleRefundAmount => CalculateDismantleRefundAmount();

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (ArtifactStatManager.Current != null)
            {
                ArtifactStatManager.Current.StatsChanged += ApplyStats;
            }
        }

        private void OnDisable()
        {
            if (ArtifactStatManager.Current != null)
            {
                ArtifactStatManager.Current.StatsChanged -= ApplyStats;
            }
        }

        public void Initialize(BuildContext context, float range, float fireInterval, float damage)
        {
            Initialize(context, null, range, fireInterval, damage);
        }

        public void Initialize(
            BuildContext context,
            TurretAttackDefinitionSO attackDefinition,
            float range,
            float fireInterval,
            float damage)
        {
            ResolveReferences();

            runtimeAttackDefinition = attackDefinition;
            baseRange = Mathf.Max(0f, range);
            baseFireInterval = Mathf.Max(0.01f, fireInterval);
            baseDamage = Mathf.Max(0f, damage);
            upgradeLevel = 0;

            ApplyStats();
        }

        public bool TryUpgrade(out string statusMessage)
        {
            return TryUpgrade(null, out statusMessage);
        }

        public bool TryUpgrade(Transform player, out string statusMessage)
        {
            if (!CanUpgrade)
            {
                statusMessage = "Upgrade max";
                return false;
            }

            if (!TrySpendCost(player, UpgradeCost, "upgrade", out statusMessage))
            {
                return false;
            }

            upgradeLevel++;
            ApplyStats();
            lifecycleFeedback?.PlayUpgrade();
            statusMessage = $"Upgrade complete Lv {upgradeLevel}/{MaxUpgradeLevel}";
            return true;
        }

        public bool TryRepair(out string statusMessage)
        {
            return TryRepair(null, out statusMessage);
        }

        public bool TryRepair(Transform player, out string statusMessage)
        {
            ResolveReferences();
            if (health == null)
            {
                statusMessage = "No repair target";
                return false;
            }

            if (!CanRepair)
            {
                statusMessage = "Repair not needed";
                return false;
            }

            if (!TrySpendCost(player, RepairCost, "repair", out statusMessage))
            {
                return false;
            }

            bool repaired = repairAmount >= health.MaxHitPoints
                ? health.RestoreToFull()
                : health.Repair(repairAmount);

            statusMessage = repaired
                ? "Repair complete"
                : "Repair not needed";
            if (repaired)
            {
                lifecycleFeedback?.PlayRepair();
            }

            return repaired;
        }

        public bool TryDismantle(out string statusMessage)
        {
            return TryDismantle(null, out statusMessage);
        }

        public bool TryDismantle(Transform player, out string statusMessage)
        {
            installedState?.ReleasePlacement();
            int refundAmount = CalculateDismantleRefundAmount();
            if (refundAmount > 0)
            {
                PlayerCurrencyWallet wallet = ResolveCurrencyWallet(player);
                if (wallet != null)
                {
                    wallet.AddMoney(refundAmount);
                }
                else
                {
                    Debug.LogWarning("[TurretServiceController] PlayerCurrencyWallet is not connected for dismantle refund.", this);
                }
            }

            statusMessage = refundAmount > 0
                ? $"Dismantled +{refundAmount}"
                : "Dismantled";
            lifecycleFeedback?.PlayDismantle();
            Destroy(gameObject);
            return true;
        }

        public string GetSummary()
        {
            return $"{DisplayName} Lv {upgradeLevel}/{MaxUpgradeLevel}\n"
                + $"HP {Mathf.CeilToInt(CurrentHitPoints)}/{Mathf.CeilToInt(MaxHitPoints)}";
        }

        private void ApplyStats()
        {
            ResolveReferences();
            if (targetingController == null)
            {
                return;
            }

            if (runtimeAttackDefinition != null)
            {
                targetingController.Configure(runtimeAttackDefinition, upgradeLevel);
                ShowCurrentRange();
                return;
            }

            float range = baseRange + rangePerLevel * upgradeLevel;
            float fireInterval = baseFireInterval * Mathf.Pow(Mathf.Clamp(fireIntervalMultiplierPerLevel, 0.05f, 1f), upgradeLevel);
            float damage = baseDamage + damagePerLevel * upgradeLevel;
            range = ArtifactStatManager.Apply(ArtifactTarget.Turret, ArtifactStat.Range, range);
            fireInterval = ArtifactStatManager.Apply(ArtifactTarget.Turret, ArtifactStat.AttackInterval, fireInterval);
            damage = ArtifactStatManager.Apply(ArtifactTarget.Turret, ArtifactStat.Damage, damage);
            targetingController.Configure(range, fireInterval, damage);
            ShowCurrentRange();
        }

        private void ResolveReferences()
        {
            if (targetingController == null)
            {
                targetingController = GetComponent<TurretTargetingController>();
            }

            if (rangeIndicator == null)
            {
                rangeIndicator = GetComponent<TurretRangeIndicator>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (installedState == null)
            {
                installedState = GetComponent<InstalledBuildableState>();
            }

            if (lifecycleFeedback == null)
            {
                lifecycleFeedback = GetComponent<BuildableLifecycleFeedback>();
            }
        }

        private int CalculateDismantleRefundAmount()
        {
            ResolveReferences();
            if (installedState == null || !installedState.HasInstallContext)
            {
                return 0;
            }

            BuildableDefinitionSO definition = installedState.InstallContext.Definition;
            if (definition == null)
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.RoundToInt(definition.Price * Mathf.Clamp01(dismantleRefundRate)));
        }

        private bool TrySpendCost(
            Transform player,
            int cost,
            string actionName,
            out string statusMessage)
        {
            if (cost <= 0)
            {
                statusMessage = string.Empty;
                return true;
            }

            PlayerCurrencyWallet wallet = ResolveCurrencyWallet(player);
            if (wallet == null)
            {
                Debug.LogWarning($"[TurretServiceController] PlayerCurrencyWallet is not connected for {actionName} cost.", this);
                statusMessage = "No currency wallet";
                return false;
            }

            if (!wallet.TrySpendMoney(cost))
            {
                statusMessage = "Not enough gold";
                return false;
            }

            statusMessage = string.Empty;
            return true;
        }

        private static PlayerCurrencyWallet ResolveCurrencyWallet(Transform player)
        {
            if (player == null)
            {
                return null;
            }

            PlayerCurrencyWallet wallet = player.GetComponentInParent<PlayerCurrencyWallet>();
            if (wallet != null)
            {
                return wallet;
            }

            return player.GetComponentInChildren<PlayerCurrencyWallet>(true);
        }

        private void ShowCurrentRange()
        {
            rangeIndicator?.SetRange(targetingController.CurrentRange);
        }

    }
}
