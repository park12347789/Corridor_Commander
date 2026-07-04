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

        public void GetAvailableRewards(int roomIndex, int count, List<TreasureRewardEntry> results)
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

            int targetCount = Mathf.Min(count, rewards.Count);
            TreasureRewardGrantType[] preferredTypes =
            {
                TreasureRewardGrantType.Money,
                TreasureRewardGrantType.KillProgress,
                TreasureRewardGrantType.Artifact
            };

            for (int i = 0; i < preferredTypes.Length && results.Count < targetCount; i++)
            {
                AddFirstMatchingReward(preferredTypes[i], roomIndex + i, results);
            }

            int startIndex = Mathf.Abs(roomIndex) % rewards.Count;
            for (int i = 0; i < rewards.Count && results.Count < targetCount; i++)
            {
                AddRewardIfUsable(rewards[(startIndex + i) % rewards.Count], roomIndex + i, results);
            }
        }

        private void AddFirstMatchingReward(TreasureRewardGrantType grantType, int lineupIndex, List<TreasureRewardEntry> results)
        {
            int startIndex = Mathf.Abs(lineupIndex) % rewards.Count;
            for (int i = 0; i < rewards.Count; i++)
            {
                TreasureRewardEntry reward = rewards[(startIndex + i) % rewards.Count];
                if (reward != null && reward.GrantType == grantType && AddRewardIfUsable(reward, lineupIndex, results))
                {
                    return;
                }
            }
        }

        private bool AddRewardIfUsable(TreasureRewardEntry reward, int lineupIndex, List<TreasureRewardEntry> results)
        {
            if (reward == null || ContainsReward(results, reward))
            {
                return false;
            }

            TreasureRewardEntry resolvedReward = ResolveReward(reward, lineupIndex);
            if (resolvedReward == null || ContainsReward(results, resolvedReward))
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
            }

            return false;
        }

        private TreasureRewardEntry ResolveReward(TreasureRewardEntry reward, int lineupIndex)
        {
            if (reward == null
                || reward.GrantType != TreasureRewardGrantType.Artifact
                || artifactLineup == null
                || artifactLineup == this)
            {
                return reward;
            }

            return artifactLineup.TryGetArtifactReward(lineupIndex, out TreasureRewardEntry lineupReward)
                ? lineupReward
                : reward;
        }

        private bool TryGetArtifactReward(int lineupIndex, out TreasureRewardEntry reward)
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
                    && candidate.ArtifactDefinition != null)
                {
                    artifactCount++;
                }
            }

            if (artifactCount == 0)
            {
                return false;
            }

            int targetIndex = Mathf.Abs(lineupIndex) % artifactCount;
            int currentIndex = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                TreasureRewardEntry candidate = rewards[i];
                if (candidate == null
                    || candidate.GrantType != TreasureRewardGrantType.Artifact
                    || candidate.ArtifactDefinition == null)
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
    }
}
