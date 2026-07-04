using System;
using CorridorCommander.PlayerItems;
using UnityEngine;

namespace CorridorCommander
{
    public enum TreasureRewardGrantType
    {
        None = 0,
        Money = 1,
        Item = 2,
        KillProgress = 3,
        StatPoint = 4,
        Artifact = 5
    }

    [Serializable]
    public sealed class TreasureRewardEntry
    {
        [SerializeField] private string rewardId = "Reward_01";
        [SerializeField] private string displayName = "Gold";
        [SerializeField] [Min(1)] private int amount = 1;
        [SerializeField] private Sprite icon;
        [SerializeField] private TreasureRewardGrantType grantType = TreasureRewardGrantType.None;
        [SerializeField] private ItemDefinitionSO itemDefinition;
        [SerializeField] private ArtifactDefinitionSO artifactDefinition;

        public string RewardId => rewardId;
        public string DisplayName => displayName;
        public int Amount => amount;
        public Sprite Icon => icon;
        public TreasureRewardGrantType GrantType => grantType;
        public ItemDefinitionSO ItemDefinition => itemDefinition;
        public ArtifactDefinitionSO ArtifactDefinition => artifactDefinition;
    }
}
