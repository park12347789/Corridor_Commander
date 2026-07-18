using CorridorCommander.PlayerControl;
using CorridorCommander.PlayerItems;
using UnityEngine;

namespace CorridorCommander
{
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class RewardGrantService : MonoBehaviour
    {
        private static RewardGrantService current;

        [SerializeField] private PlayerCurrencyWallet currencyWallet;
        [SerializeField] private PlayerItemInventory itemInventory;
        [SerializeField] private PlayerLevelProgression levelProgression;
        [SerializeField] private ArtifactInventory artifactInventory;

        public static RewardGrantService Current => current;
        public ArtifactInventory ArtifactInventory => artifactInventory;

        public void Configure(
            PlayerCurrencyWallet configuredCurrencyWallet,
            PlayerItemInventory configuredItemInventory,
            PlayerLevelProgression configuredLevelProgression,
            ArtifactInventory configuredArtifactInventory)
        {
            currencyWallet = configuredCurrencyWallet;
            itemInventory = configuredItemInventory;
            levelProgression = configuredLevelProgression;
            artifactInventory = configuredArtifactInventory;
        }

        private void Awake()
        {
            if (current != null && current != this)
            {
                Debug.LogError("[RewardGrantService] Duplicate service exists.", this);
                enabled = false;
                return;
            }

            current = this;
        }

        private void OnDestroy()
        {
            if (current == this)
            {
                current = null;
            }
        }

        public bool TryGrant(TreasureRewardEntry reward, out string message)
        {
            if (reward == null)
            {
                message = "Reward data missing";
                return false;
            }

            int amount = Mathf.Max(1, reward.Amount);
            switch (reward.GrantType)
            {
                case TreasureRewardGrantType.Money:
                    if (currencyWallet == null)
                    {
                        message = "No player wallet";
                        Debug.LogError("[RewardGrantService] PlayerCurrencyWallet is not assigned.", this);
                        return false;
                    }

                    currencyWallet.AddMoney(amount);
                    message = $"Received: {reward.DisplayName} x{amount}";
                    return true;

                case TreasureRewardGrantType.Item:
                    if (itemInventory == null)
                    {
                        message = "No player inventory";
                        Debug.LogError("[RewardGrantService] PlayerItemInventory is not assigned.", this);
                        return false;
                    }

                    if (reward.ItemDefinition == null)
                    {
                        message = $"Item data missing: {reward.DisplayName}";
                        return false;
                    }

                    itemInventory.AddItem(reward.ItemDefinition, amount);
                    message = $"Received: {reward.DisplayName} x{amount}";
                    return true;

                case TreasureRewardGrantType.KillProgress:
                    if (levelProgression == null)
                    {
                        message = "No player level data";
                        Debug.LogError("[RewardGrantService] PlayerLevelProgression is not assigned.", this);
                        return false;
                    }

                    levelProgression.AddKillProgress(amount);
                    message = $"Received: {reward.DisplayName} x{amount}";
                    return true;

                case TreasureRewardGrantType.StatPoint:
                    if (levelProgression == null)
                    {
                        message = "No player level data";
                        Debug.LogError("[RewardGrantService] PlayerLevelProgression is not assigned.", this);
                        return false;
                    }

                    levelProgression.AddStatPoints(amount);
                    message = $"Received: {reward.DisplayName} x{amount}";
                    return true;

                case TreasureRewardGrantType.Artifact:
                    if (artifactInventory == null)
                    {
                        message = "No artifact inventory";
                        Debug.LogError("[RewardGrantService] ArtifactInventory is not assigned.", this);
                        return false;
                    }

                    if (reward.ArtifactDefinition == null)
                    {
                        message = $"Artifact data missing: {reward.DisplayName}";
                        return false;
                    }

                    return artifactInventory.TryAddArtifact(reward.ArtifactDefinition, out message);

                default:
                    message = $"Reward grant type missing: {reward.DisplayName}";
                    return false;
            }
        }
    }
}
