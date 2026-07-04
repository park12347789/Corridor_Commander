using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class BarricadeInstalledActionProvider : MonoBehaviour, IInstalledObjectActionProvider, IInstalledUpgradeLevelProvider, IInstalledAimInfoProvider
    {
        [SerializeField] private Health health;
        [SerializeField] private InstalledBuildableState installedState;
        [SerializeField] private BuildableLifecycleFeedback lifecycleFeedback;
        [SerializeField] private GameObject upgradedPrefab;
        [SerializeField] private int barricadeLevel = 1;
        [SerializeField] private int maxBarricadeLevel = 4;
        [SerializeField] [Min(1f)] private float healthMultiplierPerUpgrade = 2f;
        [SerializeField] private float repairAmount = 999f;

        public string Prompt => "\uBC29\uBCBD \uC5C5\uADF8\uB808\uC774\uB4DC";
        public string Title => "\uBC29\uBCBD \uAD00\uB9AC";
        public int CurrentUpgradeLevel => Mathf.Max(0, barricadeLevel - 1);
        public int MaxUpgradeLevel => Mathf.Max(0, maxBarricadeLevel - 1);
        public int VisibleUpgradeStars => Mathf.Clamp(CurrentUpgradeLevel, 0, 3);

        private void Awake()
        {
            ResolveReferences();
        }

        public string GetSummary()
        {
            ResolveReferences();
            if (health == null)
            {
                return string.Empty;
            }

            return $"\uBC29\uBCBD Lv {barricadeLevel}/{Mathf.Max(1, maxBarricadeLevel)}\n"
                + $"\uCCB4\uB825 {Mathf.CeilToInt(health.CurrentHitPoints)}/{Mathf.CeilToInt(health.MaxHitPoints)}";
        }

        public void CollectActions(IList<InstalledObjectAction> actions)
        {
            ResolveReferences();
            bool hasHealth = health != null && health.IsAlive;
            bool canRepair = hasHealth && health.CurrentHitPoints < health.MaxHitPoints;
            bool canUpgrade = CanUpgrade();
            string upgradeLabel = ResolveUpgradeLabel();
            int currentHitPoints = hasHealth ? Mathf.CeilToInt(health.CurrentHitPoints) : 0;
            int maxHitPoints = hasHealth ? Mathf.CeilToInt(health.MaxHitPoints) : 0;
            float healthFill = maxHitPoints > 0 ? Mathf.Clamp01(health.CurrentHitPoints / health.MaxHitPoints) : 0f;

            actions.Add(new InstalledObjectAction(
                upgradeLabel,
                canUpgrade,
                upgradedPrefab != null,
                $"{CurrentUpgradeLevel}/{MaxUpgradeLevel}",
                false,
                string.Empty,
                true,
                false,
                CurrentUpgradeLevel,
                MaxUpgradeLevel,
                0f));
            actions.Add(new InstalledObjectAction(
                canRepair ? "\uC218\uB9AC" : "\uC218\uB9AC (\uD544\uC694 \uC5C6\uC74C)",
                canRepair,
                false,
                $"{currentHitPoints}/{maxHitPoints}",
                false,
                string.Empty,
                false,
                true,
                currentHitPoints,
                maxHitPoints,
                healthFill));
            actions.Add(new InstalledObjectAction("\uCCA0\uAC70", hasHealth, true));
        }

        public bool TryGetAimInfo(out InstalledAimInfo info)
        {
            ResolveReferences();
            if (health == null)
            {
                info = default;
                return false;
            }

            info = new InstalledAimInfo(
                "\uBC29\uBCBD",
                $"Lv {barricadeLevel}/{Mathf.Max(1, maxBarricadeLevel)}",
                CanUpgrade() ? "\uC5C5\uADF8\uB808\uC774\uB4DC \uAC00\uB2A5" : "\uC5C5\uADF8\uB808\uC774\uB4DC \uBD88\uAC00",
                $"\uCCB4\uB825 {Mathf.CeilToInt(health.CurrentHitPoints)}/{Mathf.CeilToInt(health.MaxHitPoints)}",
                health.MaxHitPoints > 0f ? Mathf.Clamp01(health.CurrentHitPoints / health.MaxHitPoints) : 0f,
                false,
                0f);
            return true;
        }

        public bool ExecuteAction(int actionIndex, Transform player, out string statusMessage)
        {
            ResolveReferences();
            switch (actionIndex)
            {
                case 0:
                    return TryUpgrade(player, out statusMessage);
                case 1:
                    return TryRepair(out statusMessage);
                case 2:
                    return TryDismantle(out statusMessage);
                default:
                    statusMessage = "\uC54C \uC218 \uC5C6\uB294 \uBA85\uB839";
                    return false;
            }
        }

        private bool TryUpgrade(Transform player, out string statusMessage)
        {
            ResolveReferences();
            if (!CanUpgrade())
            {
                statusMessage = ResolveUpgradeFailureMessage();
                return false;
            }

            int nextLevel = Mathf.Min(Mathf.Max(1, maxBarricadeLevel), barricadeLevel + 1);
            if (upgradedPrefab == null)
            {
                barricadeLevel = nextLevel;
                health.ScaleMaxHitPoints(healthMultiplierPerUpgrade);
                statusMessage = $"\uBC29\uBCBD {barricadeLevel}\uB2E8\uACC4 \uC5C5\uADF8\uB808\uC774\uB4DC";
                lifecycleFeedback?.PlayUpgrade();
                return true;
            }

            BuildContext context = installedState.InstallContext;
            GameObject upgraded = Instantiate(upgradedPrefab, transform.position, transform.rotation);
            upgraded.name = name + "_Lv" + nextLevel;

            InstalledBuildableState upgradedState = upgraded.GetComponent<InstalledBuildableState>();
            if (upgradedState == null)
            {
                upgradedState = upgraded.AddComponent<InstalledBuildableState>();
            }

            upgradedState.Initialize(context);
            IBuildableInstallable installable = FindInstallable(upgraded);
            installable?.OnInstalled(context);

            if (!installedState.TransferPlacement(upgraded))
            {
                Destroy(upgraded);
                statusMessage = "\uC5C5\uADF8\uB808\uC774\uB4DC \uC704\uCE58 \uC804\uD658 \uC2E4\uD328";
                return false;
            }

            statusMessage = $"\uBC29\uBCBD {nextLevel}\uB2E8\uACC4 \uC5C5\uADF8\uB808\uC774\uB4DC";
            TransferOpenPanelToReplacement(upgraded, player);
            lifecycleFeedback?.PlayUpgrade();
            Destroy(gameObject);
            return true;
        }

        private bool TryRepair(out string statusMessage)
        {
            if (health == null)
            {
                statusMessage = "\uC218\uB9AC \uB300\uC0C1 \uC5C6\uC74C";
                return false;
            }

            bool repaired = repairAmount >= health.MaxHitPoints
                ? health.RestoreToFull()
                : health.Repair(repairAmount);

            statusMessage = repaired
                ? "\uC218\uB9AC \uC644\uB8CC"
                : "\uC218\uB9AC \uD544\uC694 \uC5C6\uC74C";
            if (repaired)
            {
                lifecycleFeedback?.PlayRepair();
            }

            return repaired;
        }

        private bool TryDismantle(out string statusMessage)
        {
            installedState?.ReleasePlacement();
            statusMessage = "\uCCA0\uAC70 \uC644\uB8CC";
            lifecycleFeedback?.PlayDismantle();
            Destroy(gameObject);
            return true;
        }

        private bool CanUpgrade()
        {
            return health != null
                && health.IsAlive
                && CurrentUpgradeLevel < MaxUpgradeLevel
                && installedState != null
                && installedState.HasInstallContext;
        }

        private string ResolveUpgradeLabel()
        {
            if (CurrentUpgradeLevel >= MaxUpgradeLevel)
            {
                return "\uC5C5\uADF8\uB808\uC774\uB4DC (\uCD5C\uB300)";
            }

            return "\uC5C5\uADF8\uB808\uC774\uB4DC";
        }

        private string ResolveUpgradeFailureMessage()
        {
            if (CurrentUpgradeLevel >= MaxUpgradeLevel)
            {
                return "\uC5C5\uADF8\uB808\uC774\uB4DC \uCD5C\uB300";
            }

            return "\uC5C5\uADF8\uB808\uC774\uB4DC \uC2E4\uD328";
        }

        private static IBuildableInstallable FindInstallable(GameObject root)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IBuildableInstallable installable)
                {
                    return installable;
                }
            }

            return null;
        }

        private void TransferOpenPanelToReplacement(GameObject replacement, Transform player)
        {
            InstalledObjectInteraction currentInteraction = GetComponent<InstalledObjectInteraction>();
            InstalledObjectInteraction replacementInteraction = replacement != null
                ? replacement.GetComponentInChildren<InstalledObjectInteraction>(true)
                : null;

            currentInteraction?.TransferOpenPanelTo(replacementInteraction, player);
        }

        private void ResolveReferences()
        {
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
    }
}
