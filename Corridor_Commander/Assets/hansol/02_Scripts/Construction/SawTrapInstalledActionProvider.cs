using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SawTrapServiceController))]
    public sealed class SawTrapInstalledActionProvider : MonoBehaviour, IInstalledObjectActionProvider, IInstalledAimInfoProvider
    {
        [SerializeField] private SawTrapServiceController serviceController;

        public string Prompt => "Saw Trap Management";
        public string Title => "Saw Trap Turret";

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
            string upgradeStatus = canUpgrade ? string.Empty : " (Max)";
            int upgradeLevel = hasService ? serviceController.UpgradeLevel : 0;
            int maxUpgradeLevel = hasService ? serviceController.MaxUpgradeLevel : 0;
            int currentHitPoints = hasService ? Mathf.CeilToInt(serviceController.CurrentHitPoints) : 0;
            int maxHitPoints = hasService ? Mathf.CeilToInt(serviceController.MaxHitPoints) : 0;
            string upgradeCost = hasService ? serviceController.UpgradeCost.ToString() : string.Empty;
            string sellRefund = hasService ? serviceController.SellRefundAmount.ToString() : string.Empty;

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
                "Repair (Disabled)",
                false,
                false,
                $"{currentHitPoints}/{maxHitPoints}",
                false,
                string.Empty,
                false,
                true,
                currentHitPoints,
                maxHitPoints,
                hasService ? serviceController.HealthFillAmount : 0f));
            actions.Add(new InstalledObjectAction(
                "Sell",
                hasService,
                true,
                string.Empty,
                false,
                sellRefund,
                false,
                false,
                0,
                0,
                0f));
        }

        public bool TryGetAimInfo(out InstalledAimInfo info)
        {
            ResolveReferences();
            if (serviceController == null)
            {
                info = default;
                return false;
            }

            info = new InstalledAimInfo(
                serviceController.DisplayName,
                $"Lv {serviceController.CurrentUpgradeLevel}/{serviceController.MaxUpgradeLevel}",
                $"Range {serviceController.CurrentRange:0.#}   Damage {serviceController.CurrentDamage:0.#}   Cooldown {serviceController.CurrentAttackInterval:0.##}s",
                $"HP {Mathf.CeilToInt(serviceController.CurrentHitPoints)}/{Mathf.CeilToInt(serviceController.MaxHitPoints)}",
                serviceController.HealthFillAmount,
                true,
                serviceController.CurrentRange);
            return true;
        }

        public bool ExecuteAction(int actionIndex, Transform player, out string statusMessage)
        {
            ResolveReferences();
            if (serviceController == null)
            {
                statusMessage = "No saw trap target";
                return false;
            }

            switch (actionIndex)
            {
                case 0:
                    return serviceController.TryUpgrade(player, out statusMessage);
                case 1:
                    statusMessage = "Repair disabled";
                    return false;
                case 2:
                    return serviceController.TrySell(player, out statusMessage);
                default:
                    statusMessage = "Unknown command";
                    return false;
            }
        }

        private void ResolveReferences()
        {
            if (serviceController == null)
            {
                serviceController = GetComponent<SawTrapServiceController>();
            }
        }
    }
}
