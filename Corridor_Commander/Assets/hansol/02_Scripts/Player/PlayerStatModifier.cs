using System;
using UnityEngine;
using UnityEngine.Events;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerStatModifier : MonoBehaviour
    {
        [Header("Bonus Per Upgrade")]
        [SerializeField] private float healthBonusPerLevel = 10f;
        [SerializeField] private float damagePercentBonusPerLevel = 0.1f;
        [SerializeField] private float moveSpeedBonusPerLevel = 0.15f;
        [SerializeField] private float maxStaminaBonusPerLevel = 10f;

        [Header("Events")]
        [SerializeField] private UnityEvent statsChanged;

        private int healthUpgradeLevel;
        private int damageUpgradeLevel;
        private int moveSpeedUpgradeLevel;
        private int staminaUpgradeLevel;

        public int HealthUpgradeLevel => healthUpgradeLevel;
        public int DamageUpgradeLevel => damageUpgradeLevel;
        public int MoveSpeedUpgradeLevel => moveSpeedUpgradeLevel;
        public int StaminaUpgradeLevel => staminaUpgradeLevel;

        public float HealthBonus => healthUpgradeLevel * Mathf.Max(0f, healthBonusPerLevel);
        public float DamageMultiplier => ArtifactStatManager.Apply(
            ArtifactTarget.Player,
            ArtifactStat.Damage,
            1f + (damageUpgradeLevel * Mathf.Max(0f, damagePercentBonusPerLevel)));
        public float MoveSpeedBonus => moveSpeedUpgradeLevel * Mathf.Max(0f, moveSpeedBonusPerLevel);
        public float MaxStaminaBonus => staminaUpgradeLevel * Mathf.Max(0f, maxStaminaBonusPerLevel);

        public event Action StatsChanged;

        public void SetUpgradeLevels(
            int healthLevel,
            int damageLevel,
            int moveSpeedLevel,
            int staminaLevel)
        {
            healthUpgradeLevel = Mathf.Max(0, healthLevel);
            damageUpgradeLevel = Mathf.Max(0, damageLevel);
            moveSpeedUpgradeLevel = Mathf.Max(0, moveSpeedLevel);
            staminaUpgradeLevel = Mathf.Max(0, staminaLevel);

            NotifyStatsChanged();
        }

        private void NotifyStatsChanged()
        {
            StatsChanged?.Invoke();
            statsChanged?.Invoke();
        }
    }
}

/*
Unity setup:
1. Add PlayerStatModifier to the player root or PlayerSystems object.
2. Set bonus values per upgrade in the Inspector.
3. PlayerStatUpgradeController should be connected to this component.
4. Runtime systems read this component for final player-only stat bonuses.
*/
