using System;
using UnityEngine;
using UnityEngine.Events;

namespace CorridorCommander.PlayerControl
{
    public enum PlayerStatUpgradeType
    {
        Health,
        Damage,
        MoveSpeed,
        Stamina
    }

    [DisallowMultipleComponent]
    public sealed class PlayerStatUpgradeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerLevelProgression levelProgression;
        [SerializeField] private PlayerStatModifier statModifier;

        [Header("Upgrade Cost")]
        [SerializeField] private int statPointCostPerUpgrade = 1;

        [Header("Events")]
        [SerializeField] private UnityEvent<PlayerStatUpgradeType> upgraded;

        private int healthUpgradeLevel;
        private int damageUpgradeLevel;
        private int moveSpeedUpgradeLevel;
        private int staminaUpgradeLevel;

        public int HealthUpgradeLevel => healthUpgradeLevel;
        public int DamageUpgradeLevel => damageUpgradeLevel;
        public int MoveSpeedUpgradeLevel => moveSpeedUpgradeLevel;
        public int StaminaUpgradeLevel => staminaUpgradeLevel;

        public event Action<PlayerStatUpgradeType> Upgraded;

        private void Awake()
        {
            ResolveReferences();
            ApplyUpgradeLevels();
        }

        public bool TryUpgradeHealth()
        {
            return TryUpgrade(PlayerStatUpgradeType.Health);
        }

        public bool TryUpgradeDamage()
        {
            return TryUpgrade(PlayerStatUpgradeType.Damage);
        }

        public bool TryUpgradeMoveSpeed()
        {
            return TryUpgrade(PlayerStatUpgradeType.MoveSpeed);
        }

        public bool TryUpgradeStamina()
        {
            return TryUpgrade(PlayerStatUpgradeType.Stamina);
        }

        public bool TryUpgrade(PlayerStatUpgradeType upgradeType)
        {
            ResolveReferences();

            if (levelProgression == null)
            {
                Debug.LogWarning("[PlayerStatUpgradeController] PlayerLevelProgression is not connected.");
                return false;
            }

            int cost = Mathf.Max(1, statPointCostPerUpgrade);

            if (!levelProgression.TrySpendStatPoint(cost))
            {
                Debug.Log($"[PlayerStatUpgradeController] Not Enough Stat Points: Need {cost}");
                return false;
            }

            IncreaseUpgradeLevel(upgradeType);
            ApplyUpgradeLevels();

            Debug.Log($"[PlayerStatUpgradeController] Upgraded: {upgradeType}");

            Upgraded?.Invoke(upgradeType);
            upgraded?.Invoke(upgradeType);

            return true;
        }

        private void IncreaseUpgradeLevel(PlayerStatUpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case PlayerStatUpgradeType.Health:
                    healthUpgradeLevel++;
                    break;

                case PlayerStatUpgradeType.Damage:
                    damageUpgradeLevel++;
                    break;

                case PlayerStatUpgradeType.MoveSpeed:
                    moveSpeedUpgradeLevel++;
                    break;

                case PlayerStatUpgradeType.Stamina:
                    staminaUpgradeLevel++;
                    break;
            }
        }

        private void ApplyUpgradeLevels()
        {
            if (statModifier == null)
            {
                return;
            }

            statModifier.SetUpgradeLevels(
                healthUpgradeLevel,
                damageUpgradeLevel,
                moveSpeedUpgradeLevel,
                staminaUpgradeLevel);
        }

        private void ResolveReferences()
        {
            if (levelProgression == null)
            {
                levelProgression = GetComponent<PlayerLevelProgression>();
            }

            if (levelProgression == null)
            {
                levelProgression = GetComponentInParent<PlayerLevelProgression>();
            }

            if (levelProgression == null)
            {
                levelProgression = GetComponentInChildren<PlayerLevelProgression>(true);
            }

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

/*
Unity setup:
1. Add PlayerStatUpgradeController to the same player hierarchy as PlayerLevelProgression.
2. Connect PlayerLevelProgression and PlayerStatModifier, or leave them empty for auto-binding.
3. Stat shop buttons should call TryUpgradeHealth, TryUpgradeDamage, TryUpgradeMoveSpeed, or TryUpgradeStamina.
4. Each successful upgrade spends one stat point by default.
*/
