using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TurretServiceController))]
    public sealed class TurretInstalledActionProvider : MonoBehaviour, IInstalledObjectActionProvider, IInstalledAimInfoProvider
    {
        [SerializeField] private TurretServiceController serviceController;
        [SerializeField] private TurretTargetingController targetingController;

        public string Prompt => "Turret Upgrade";
        public string Title => "Turret Management";

        private void Awake()
        {
            ResolveReferences();
        }

        public string GetSummary()
        {
            ResolveReferences();
            return serviceController != null ? serviceController.GetSummary() : string.Empty;
        }

        public void CollectActions(IList<InstalledObjectAction> actions)
        {
            ResolveReferences();
            bool hasService = serviceController != null;
            bool canUpgrade = hasService && serviceController.CanUpgrade;
            bool canRepair = hasService && serviceController.CanRepair;
            string upgradeStatus = canUpgrade ? string.Empty : " (Max)";
            string repairStatus = canRepair ? string.Empty : " (Full)";
            int upgradeLevel = hasService ? serviceController.UpgradeLevel : 0;
            int maxUpgradeLevel = hasService ? serviceController.MaxUpgradeLevel : 0;
            int currentHitPoints = hasService ? Mathf.CeilToInt(serviceController.CurrentHitPoints) : 0;
            int maxHitPoints = hasService ? Mathf.CeilToInt(serviceController.MaxHitPoints) : 0;
            int refundAmount = hasService ? serviceController.DismantleRefundAmount : 0;
            string upgradeCost = hasService ? serviceController.UpgradeCost.ToString() : string.Empty;
            string repairCost = hasService ? serviceController.RepairCost.ToString() : string.Empty;
            string dismantleRefund = hasService ? refundAmount.ToString() : string.Empty;

            actions.Add(new InstalledObjectAction(
                $"Upgrade{upgradeStatus}",
                hasService && canUpgrade,
                false,
                $"{upgradeLevel}/{maxUpgradeLevel}",
                false,
                upgradeCost,
                true,
                false,
                upgradeLevel,
                maxUpgradeLevel,
                0f));
            actions.Add(new InstalledObjectAction(
                $"Repair{repairStatus}",
                hasService && canRepair,
                false,
                $"{currentHitPoints}/{maxHitPoints}",
                false,
                repairCost,
                false,
                true,
                currentHitPoints,
                maxHitPoints,
                hasService ? serviceController.HealthFillAmount : 0f));
            actions.Add(new InstalledObjectAction(
                "Dismantle",
                hasService,
                true,
                string.Empty,
                false,
                dismantleRefund,
                false,
                false,
                0,
                0,
                0f));
        }

        public bool TryGetAimInfo(out InstalledAimInfo info)
        {
            ResolveReferences();
            if (serviceController == null || targetingController == null)
            {
                info = default;
                return false;
            }

            info = new InstalledAimInfo(
                serviceController.DisplayName,
                $"Lv {serviceController.CurrentUpgradeLevel}/{serviceController.MaxUpgradeLevel}",
                $"Range {targetingController.CurrentRange:0.#}   Damage {targetingController.CurrentDamage:0.#}   Cooldown {targetingController.CurrentFireInterval:0.##}s",
                $"HP {Mathf.CeilToInt(serviceController.CurrentHitPoints)}/{Mathf.CeilToInt(serviceController.MaxHitPoints)}",
                serviceController.HealthFillAmount,
                true,
                targetingController.CurrentRange);
            return true;
        }

        public bool ExecuteAction(int actionIndex, Transform player, out string statusMessage)
        {
            ResolveReferences();
            if (serviceController == null)
            {
                statusMessage = "No turret target";
                return false;
            }

            switch (actionIndex)
            {
                case 0:
                    return serviceController.TryUpgrade(player, out statusMessage);
                case 1:
                    return serviceController.TryRepair(player, out statusMessage);
                case 2:
                    return serviceController.TryDismantle(player, out statusMessage);
                default:
                    statusMessage = "Unknown command";
                    return false;
            }
        }

        private void ResolveReferences()
        {
            if (serviceController == null)
            {
                serviceController = GetComponent<TurretServiceController>();
            }

            if (targetingController == null)
            {
                targetingController = GetComponent<TurretTargetingController>();
            }
        }

    }
}
