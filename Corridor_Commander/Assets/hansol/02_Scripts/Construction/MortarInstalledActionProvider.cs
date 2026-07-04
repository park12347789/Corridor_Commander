using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(InstalledBuildableState))]
    public sealed class MortarInstalledActionProvider : MonoBehaviour, IInstalledObjectActionProvider, IInstalledAimInfoProvider
    {
        [SerializeField] private Health health;
        [SerializeField] private InstalledBuildableState installedState;
        [SerializeField] private MortarSkillRole mortarRole;
        [SerializeField] private BuildableLifecycleFeedback lifecycleFeedback;
        [SerializeField] private float repairAmount = 999f;

        public string Prompt => "Mortar Management";
        public string Title => "Mortar Management";

        private void Awake()
        {
            ResolveReferences();
        }

        public string GetSummary()
        {
            ResolveReferences();

            SkillDefinitionSO skill = mortarRole != null ? mortarRole.SkillDefinition : null;
            string displayName = skill != null ? skill.DisplayName : "Mortar";
            string upgradeSummary = displayName + " Lv 0/0";
            string healthSummary = health != null
                ? $"HP {Mathf.CeilToInt(health.CurrentHitPoints)}/{Mathf.CeilToInt(health.MaxHitPoints)}"
                : string.Empty;

            return string.IsNullOrEmpty(healthSummary)
                ? upgradeSummary
                : upgradeSummary + "\n" + healthSummary;
        }

        public void CollectActions(IList<InstalledObjectAction> actions)
        {
            ResolveReferences();

            bool hasHealth = health != null && health.IsAlive;
            bool canRepair = hasHealth && health.CurrentHitPoints < health.MaxHitPoints;
            int currentHitPoints = health != null ? Mathf.CeilToInt(health.CurrentHitPoints) : 0;
            int maxHitPoints = health != null ? Mathf.CeilToInt(health.MaxHitPoints) : 0;
            string repairLabel = canRepair ? "Repair" : "Repair (Full)";
            string healthInfo = health != null ? $"{currentHitPoints}/{maxHitPoints}" : string.Empty;

            actions.Add(new InstalledObjectAction(
                "Upgrade (Max)",
                false,
                false,
                "0/0",
                false,
                true,
                false,
                0,
                0,
                0f));
            actions.Add(new InstalledObjectAction(
                repairLabel,
                canRepair,
                false,
                healthInfo,
                false,
                false,
                true,
                currentHitPoints,
                maxHitPoints,
                ResolveHealthFillAmount()));
            actions.Add(new InstalledObjectAction("Dismantle", hasHealth, true));
        }

        public bool TryGetAimInfo(out InstalledAimInfo info)
        {
            ResolveReferences();
            SkillDefinitionSO skill = mortarRole != null ? mortarRole.SkillDefinition : null;
            if (health == null || skill == null)
            {
                info = default;
                return false;
            }

            float range = ApplyAimStat(ArtifactStat.Range, skill.Range);
            float radius = ApplyAimStat(ArtifactStat.Range, skill.Radius);
            float damage = ApplyAimStat(ArtifactStat.Damage, skill.Damage);
            float cooldown = ApplyAimStat(ArtifactStat.Cooldown, skill.Cooldown);
            info = new InstalledAimInfo(
                skill.DisplayName,
                "Lv 0/0",
                $"Range {range:0.#}   Radius {radius:0.#}   Damage {damage:0.#}   Cooldown {cooldown:0.##}s",
                $"HP {Mathf.CeilToInt(health.CurrentHitPoints)}/{Mathf.CeilToInt(health.MaxHitPoints)}",
                ResolveHealthFillAmount(),
                true,
                range);
            return true;
        }

        public bool ExecuteAction(int actionIndex, Transform player, out string statusMessage)
        {
            ResolveReferences();
            switch (actionIndex)
            {
                case 0:
                    statusMessage = "Already at max level";
                    return false;
                case 1:
                    return TryRepair(out statusMessage);
                case 2:
                    return TryDismantle(out statusMessage);
                default:
                    statusMessage = "Unknown command";
                    return false;
            }
        }

        private bool TryRepair(out string statusMessage)
        {
            if (health == null)
            {
                statusMessage = "No repair target";
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

        private bool TryDismantle(out string statusMessage)
        {
            installedState?.ReleasePlacement();
            statusMessage = "Dismantled";
            lifecycleFeedback?.PlayDismantle();
            Destroy(gameObject);
            return true;
        }

        private float ResolveHealthFillAmount()
        {
            if (health == null || health.MaxHitPoints <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(health.CurrentHitPoints / health.MaxHitPoints);
        }

        private static float ApplyAimStat(ArtifactStat stat, float baseValue)
        {
            ArtifactStatManager manager = ArtifactStatManager.Current;
            return manager != null
                ? manager.ApplyMultiplier(ArtifactTarget.Mortar, stat, baseValue)
                : baseValue;
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

            if (mortarRole == null)
            {
                mortarRole = GetComponent<MortarSkillRole>();
            }

            if (lifecycleFeedback == null)
            {
                lifecycleFeedback = GetComponent<BuildableLifecycleFeedback>();
            }
        }
    }
}
