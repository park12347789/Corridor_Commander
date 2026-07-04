using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class WaveRewardController : MonoBehaviour
    {
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private TreasureChestRewardTable rewardTable;
        [SerializeField] private TreasureRewardMenuPresenter rewardPresenter;
        [SerializeField] private RewardGrantService rewardGrantService;
        [SerializeField] private ArtifactInventory artifactInventory;
        [SerializeField, Min(1)] private int rewardEveryNWave = 3;
        [SerializeField] private int firstRewardWaveIndex = 2;
        [SerializeField, Min(0f)] private float rewardOfferDelay = 0.8f;

        private readonly List<TreasureRewardEntry> offeredRewards = new List<TreasureRewardEntry>(TreasureRewardMenuPresenter.MaxChoiceCount);
        private int activeWaveIndex = -1;
        private Coroutine rewardOfferRoutine;

        private void OnEnable()
        {
            ResolveRuntimeReferences();
            if (!ValidateReferences())
            {
                return;
            }

            waveDirector.WaveCleared -= HandleWaveCleared;
            waveDirector.WaveCleared += HandleWaveCleared;
        }

        private void OnDisable()
        {
            if (waveDirector != null)
            {
                waveDirector.WaveCleared -= HandleWaveCleared;
            }

            if (rewardOfferRoutine != null)
            {
                StopCoroutine(rewardOfferRoutine);
                rewardOfferRoutine = null;
            }
        }

        private void HandleWaveCleared(int waveIndex, EnemyWaveDefinition wave)
        {
            if (!ShouldOfferReward(waveIndex))
            {
                return;
            }

            waveDirector.HoldNextWave();
            if (rewardOfferRoutine != null)
            {
                StopCoroutine(rewardOfferRoutine);
            }

            rewardOfferRoutine = StartCoroutine(OfferRewardAfterDelay(waveIndex));
        }

        private IEnumerator OfferRewardAfterDelay(int waveIndex)
        {
            float delay = Mathf.Max(0f, rewardOfferDelay);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            rewardOfferRoutine = null;
            activeWaveIndex = waveIndex;
            BuildRewardChoices(waveIndex);
            if (offeredRewards.Count == 0)
            {
                Debug.LogWarning($"[WaveRewardController] No rewards available for wave {waveIndex + 1}.", this);
                waveDirector.ContinueAfterWaveHold();
                yield break;
            }

            if (!UiInputCoordinator.Instance.TryBeginPausedContext(this, UiInputContext.TreasureRewardMenu, true))
            {
                Debug.LogError("[WaveRewardController] Could not enter reward UI context.", this);
                waveDirector.ContinueAfterWaveHold();
                yield break;
            }

            rewardPresenter.ShowRewards(this, offeredRewards, null, ClaimReward);
            Debug.Log($"[WaveRewardController] Wave reward offered: wave={waveIndex + 1}", this);
        }

        private bool ShouldOfferReward(int waveIndex)
        {
            if (rewardEveryNWave <= 0 || waveIndex < firstRewardWaveIndex)
            {
                return false;
            }

            return (waveIndex - firstRewardWaveIndex) % rewardEveryNWave == 0;
        }

        private void BuildRewardChoices(int waveIndex)
        {
            offeredRewards.Clear();
            if (rewardTable == null)
            {
                Debug.LogError("[WaveRewardController] Reward table is not assigned.", this);
                return;
            }

            List<TreasureRewardEntry> candidates = new List<TreasureRewardEntry>();
            rewardTable.GetAvailableRewards(waveIndex, rewardTable.Rewards.Count, candidates);
            for (int i = 0; i < candidates.Count && offeredRewards.Count < TreasureRewardMenuPresenter.MaxChoiceCount; i++)
            {
                TreasureRewardEntry reward = candidates[i];
                if (reward == null)
                {
                    continue;
                }

                offeredRewards.Add(reward);
            }
        }

        private void ClaimReward(int index)
        {
            if (index < 0 || index >= offeredRewards.Count)
            {
                return;
            }

            TreasureRewardEntry reward = offeredRewards[index];
            if (!rewardGrantService.TryGrant(reward, out string message))
            {
                Debug.LogWarning($"[WaveRewardController] Reward grant failed: {message}", this);
                rewardPresenter.ShowSelected(this, message);
                ContinueWaves();
                return;
            }

            rewardPresenter.ShowSelected(this, message);
            Debug.Log($"[WaveRewardController] Wave reward selected: wave={activeWaveIndex + 1}, reward={reward.DisplayName}", this);
            ContinueWaves();
        }

        private void ContinueWaves()
        {
            activeWaveIndex = -1;
            UiInputCoordinator.EndContextIfActive(this);
            waveDirector.ContinueAfterWaveHold();
        }

        private bool ValidateReferences()
        {
            bool valid = true;
            if (waveDirector == null)
            {
                Debug.LogError("[WaveRewardController] WaveDirector is not assigned.", this);
                valid = false;
            }

            if (rewardTable == null)
            {
                Debug.LogError("[WaveRewardController] RewardTable is not assigned.", this);
                valid = false;
            }

            if (rewardPresenter == null)
            {
                Debug.LogError("[WaveRewardController] RewardPresenter is not assigned.", this);
                valid = false;
            }
            else if (!rewardPresenter.HasRewardPanel)
            {
                Debug.LogError("[WaveRewardController] RewardPresenter has no reward panel assigned.", rewardPresenter);
                valid = false;
            }

            if (rewardGrantService == null)
            {
                Debug.LogError("[WaveRewardController] RewardGrantService is not assigned.", this);
                valid = false;
            }

            if (artifactInventory == null)
            {
                Debug.LogError("[WaveRewardController] ArtifactInventory is not assigned.", this);
                valid = false;
            }

            return valid;
        }

        private void ResolveRuntimeReferences()
        {
            if (rewardPresenter == null || !rewardPresenter.HasRewardPanel)
            {
                rewardPresenter = FindRewardPresenterWithPanel();
            }

            if (rewardGrantService == null)
            {
                rewardGrantService = RewardGrantService.Current;
            }

            if (artifactInventory == null)
            {
                artifactInventory = FindFirstObjectByType<ArtifactInventory>(FindObjectsInactive.Include);
            }
        }

        private static TreasureRewardMenuPresenter FindRewardPresenterWithPanel()
        {
            TreasureRewardMenuPresenter[] presenters = FindObjectsByType<TreasureRewardMenuPresenter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < presenters.Length; i++)
            {
                if (presenters[i] != null && presenters[i].HasRewardPanel)
                {
                    return presenters[i];
                }
            }

            return null;
        }
    }
}
