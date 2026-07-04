using UnityEngine;
using CorridorCommander.PlayerControl;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SawTrapTurretController))]
    [RequireComponent(typeof(Health))]
    public sealed class SawTrapServiceController : MonoBehaviour, IInstalledUpgradeLevelProvider
    {
        [SerializeField] private SawTrapTurretController sawTrapController;
        [SerializeField] private Health health;
        [SerializeField] private InstalledBuildableState installedState;
        [SerializeField] private BuildableLifecycleFeedback lifecycleFeedback;
        [SerializeField] private int maxUpgradeLevel = 3;
        [SerializeField] private float baseRange = 2.5f;
        [SerializeField] private float baseAttackInterval = 0.35f;
        [SerializeField] private float baseDamage = 8f;
        [SerializeField] private float rangePerLevel = 0.35f;
        [SerializeField] private float damagePerLevel = 3f;
        [SerializeField] private float attackIntervalMultiplierPerLevel = 0.9f;
        [SerializeField] [Min(0)] private int upgradeCost = 2;
        [SerializeField] [Range(0f, 1f)] private float sellRefundRate = 1f;

        private int upgradeLevel;

        public string DisplayName => "Saw Trap Turret";
        public int UpgradeLevel => upgradeLevel;
        public int CurrentUpgradeLevel => upgradeLevel;
        public int MaxUpgradeLevel => Mathf.Max(0, maxUpgradeLevel);
        public int VisibleUpgradeStars => Mathf.Clamp(upgradeLevel, 0, 3);
        public bool CanUpgrade => upgradeLevel < MaxUpgradeLevel;
        public bool CanRepair => false;
        public float CurrentHitPoints => health != null ? health.CurrentHitPoints : 0f;
        public float MaxHitPoints => health != null ? health.MaxHitPoints : 0f;
        public float HealthFillAmount => MaxHitPoints > 0f ? Mathf.Clamp01(CurrentHitPoints / MaxHitPoints) : 0f;
        public float CurrentRange => sawTrapController != null ? sawTrapController.AttackRange : baseRange;
        public float CurrentDamage => sawTrapController != null ? sawTrapController.Damage : baseDamage;
        public float CurrentAttackInterval => sawTrapController != null ? sawTrapController.AttackInterval : baseAttackInterval;
        public int UpgradeCost => Mathf.Max(0, upgradeCost);
        public int SellRefundAmount => ResolveSellRefundAmount();

        private void Awake()
        {
            ResolveReferences();
            ApplyStats();
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

        public void Initialize(BuildContext context)
        {
            ResolveReferences();
            upgradeLevel = 0;
            ApplyStats();
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

        public bool TrySell(Transform player, out string statusMessage)
        {
            installedState?.ReleasePlacement();
            int refundAmount = ResolveSellRefundAmount();
            if (refundAmount > 0)
            {
                PlayerCurrencyWallet wallet = ResolveCurrencyWallet(player);
                if (wallet != null)
                {
                    wallet.AddMoney(refundAmount);
                }
                else
                {
                    Debug.LogWarning("[SawTrapServiceController] PlayerCurrencyWallet is not connected for sell refund.", this);
                }
            }

            statusMessage = refundAmount > 0
                ? $"Sold +{refundAmount}"
                : "Sold";
            lifecycleFeedback?.PlayDismantle();
            Destroy(gameObject);
            return true;
        }

        public string GetSummary()
        {
            return $"Saw Trap Turret Lv {upgradeLevel}/{MaxUpgradeLevel}\n"
                + $"HP {Mathf.CeilToInt(CurrentHitPoints)}/{Mathf.CeilToInt(MaxHitPoints)}";
        }

        private void ApplyStats()
        {
            ResolveReferences();
            if (sawTrapController == null)
            {
                return;
            }

            float range = baseRange + rangePerLevel * upgradeLevel;
            float interval = baseAttackInterval * Mathf.Pow(Mathf.Clamp(attackIntervalMultiplierPerLevel, 0.05f, 1f), upgradeLevel);
            float damage = baseDamage + damagePerLevel * upgradeLevel;
            range = ArtifactStatManager.Apply(ArtifactTarget.Turret, ArtifactStat.Range, range);
            interval = ArtifactStatManager.Apply(ArtifactTarget.Turret, ArtifactStat.AttackInterval, interval);
            damage = ArtifactStatManager.Apply(ArtifactTarget.Turret, ArtifactStat.Damage, damage);
            sawTrapController.Configure(range, interval, damage);
        }

        private void ResolveReferences()
        {
            if (sawTrapController == null)
            {
                sawTrapController = GetComponent<SawTrapTurretController>();
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

        private int ResolveSellRefundAmount()
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

            return Mathf.Max(0, Mathf.RoundToInt(definition.Price * Mathf.Clamp01(sellRefundRate)));
        }

        private bool TrySpendCost(Transform player, int cost, string actionName, out string statusMessage)
        {
            if (cost <= 0)
            {
                statusMessage = string.Empty;
                return true;
            }

            PlayerCurrencyWallet wallet = ResolveCurrencyWallet(player);
            if (wallet == null)
            {
                Debug.LogWarning($"[SawTrapServiceController] PlayerCurrencyWallet is not connected for {actionName} cost.", this);
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

    }
}
