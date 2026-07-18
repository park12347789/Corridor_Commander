using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(menuName = "Corridor Commander/Rewards/Treasure Chest Reward Table")]
    public sealed class TreasureChestRewardTable : ScriptableObject
    {
        [SerializeField] private List<TreasureRewardEntry> rewards = new List<TreasureRewardEntry>();
        [SerializeField] private TreasureChestRewardTable artifactLineup;

        public IReadOnlyList<TreasureRewardEntry> Rewards => rewards;

        public void GetAvailableRewards(int offerSeed, int count, List<TreasureRewardEntry> results)
        {
            GetAvailableRewards(offerSeed, count, null, results);
        }

        public void GetAvailableRewards(
            int offerSeed,
            int count,
            ArtifactInventory artifactInventory,
            List<TreasureRewardEntry> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            if (rewards.Count == 0)
            {
                return;
            }

            int targetCount = Mathf.Min(Mathf.Max(0, count), rewards.Count);
            bool preferStatPoint = (offerSeed & 1) != 0;
            TreasureRewardGrantType[] preferredTypes =
            {
                TreasureRewardGrantType.Money,
                preferStatPoint ? TreasureRewardGrantType.StatPoint : TreasureRewardGrantType.KillProgress,
                TreasureRewardGrantType.Artifact,
                preferStatPoint ? TreasureRewardGrantType.KillProgress : TreasureRewardGrantType.StatPoint,
                TreasureRewardGrantType.Item
            };

            for (int i = 0; i < preferredTypes.Length && results.Count < targetCount; i++)
            {
                AddFirstMatchingReward(preferredTypes[i], offerSeed + i, artifactInventory, results);
            }

            int startIndex = PositiveIndex(offerSeed, rewards.Count);
            for (int i = 0; i < rewards.Count && results.Count < targetCount; i++)
            {
                AddRewardIfUsable(
                    rewards[(startIndex + i) % rewards.Count],
                    offerSeed + i,
                    artifactInventory,
                    results);
            }
        }

        private void AddFirstMatchingReward(
            TreasureRewardGrantType grantType,
            int lineupIndex,
            ArtifactInventory artifactInventory,
            List<TreasureRewardEntry> results)
        {
            int startIndex = PositiveIndex(lineupIndex, rewards.Count);
            for (int i = 0; i < rewards.Count; i++)
            {
                TreasureRewardEntry reward = rewards[(startIndex + i) % rewards.Count];
                if (reward != null
                    && reward.GrantType == grantType
                    && AddRewardIfUsable(reward, lineupIndex, artifactInventory, results))
                {
                    return;
                }
            }
        }

        private bool AddRewardIfUsable(
            TreasureRewardEntry reward,
            int lineupIndex,
            ArtifactInventory artifactInventory,
            List<TreasureRewardEntry> results)
        {
            if (reward == null || ContainsReward(results, reward))
            {
                return false;
            }

            TreasureRewardEntry resolvedReward = ResolveReward(reward, lineupIndex, artifactInventory);
            if (resolvedReward == null
                || ContainsReward(results, resolvedReward)
                || IsOwnedArtifact(resolvedReward, artifactInventory))
            {
                return false;
            }

            results.Add(resolvedReward);
            return true;
        }

        private static bool ContainsReward(List<TreasureRewardEntry> results, TreasureRewardEntry reward)
        {
            for (int i = 0; i < results.Count; i++)
            {
                TreasureRewardEntry candidate = results[i];
                if (candidate == reward)
                {
                    return true;
                }

                if (candidate != null
                    && reward != null
                    && !string.IsNullOrWhiteSpace(candidate.RewardId)
                    && candidate.RewardId == reward.RewardId)
                {
                    return true;
                }

                if (candidate != null
                    && reward != null
                    && candidate.ArtifactDefinition != null
                    && candidate.ArtifactDefinition == reward.ArtifactDefinition)
                {
                    return true;
                }
            }

            return false;
        }

        private TreasureRewardEntry ResolveReward(
            TreasureRewardEntry reward,
            int lineupIndex,
            ArtifactInventory artifactInventory)
        {
            if (reward == null
                || reward.GrantType != TreasureRewardGrantType.Artifact
                || artifactLineup == null
                || artifactLineup == this)
            {
                return reward;
            }

            return artifactLineup.TryGetArtifactReward(lineupIndex, artifactInventory, out TreasureRewardEntry lineupReward)
                ? lineupReward
                : reward;
        }

        private bool TryGetArtifactReward(
            int lineupIndex,
            ArtifactInventory artifactInventory,
            out TreasureRewardEntry reward)
        {
            reward = null;
            if (rewards == null || rewards.Count == 0)
            {
                return false;
            }

            int artifactCount = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                TreasureRewardEntry candidate = rewards[i];
                if (candidate != null
                    && candidate.GrantType == TreasureRewardGrantType.Artifact
                    && candidate.ArtifactDefinition != null
                    && !IsOwnedArtifact(candidate, artifactInventory))
                {
                    artifactCount++;
                }
            }

            if (artifactCount == 0)
            {
                return false;
            }

            int targetIndex = PositiveIndex(lineupIndex, artifactCount);
            int currentIndex = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                TreasureRewardEntry candidate = rewards[i];
                if (candidate == null
                    || candidate.GrantType != TreasureRewardGrantType.Artifact
                    || candidate.ArtifactDefinition == null
                    || IsOwnedArtifact(candidate, artifactInventory))
                {
                    continue;
                }

                if (currentIndex == targetIndex)
                {
                    reward = candidate;
                    return true;
                }

                currentIndex++;
            }

            return false;
        }

        private static bool IsOwnedArtifact(TreasureRewardEntry reward, ArtifactInventory artifactInventory)
        {
            return artifactInventory != null
                && reward != null
                && reward.GrantType == TreasureRewardGrantType.Artifact
                && reward.ArtifactDefinition != null
                && artifactInventory.HasArtifact(reward.ArtifactDefinition);
        }

        private static int PositiveIndex(int value, int count)
        {
            return count > 0 ? (value & int.MaxValue) % count : 0;
        }
    }
}
