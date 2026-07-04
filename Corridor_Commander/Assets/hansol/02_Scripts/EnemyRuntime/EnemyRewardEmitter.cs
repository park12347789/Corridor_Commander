using UnityEngine;
using CorridorCommander.PlayerControl;

namespace CorridorCommander.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyRewardEmitter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private PlayerRewardReceiver rewardReceiver;

        [Header("Reward")]
        [SerializeField] private int moneyReward = 0;
        [SerializeField] private int killProgressReward = 1;

        private bool rewardGranted;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            rewardGranted = false;

            if (health != null)
            {
                health.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void HandleDied(Health deadHealth)
        {
            if (rewardGranted)
            {
                return;
            }

            rewardGranted = true;

            if (rewardReceiver == null)
            {
                ResolveRewardReceiver();
            }

            if (rewardReceiver == null)
            {
                Debug.LogWarning("[EnemyRewardEmitter] PlayerRewardReceiver is not connected.");
                return;
            }

            rewardReceiver.ReceiveKillReward(moneyReward, killProgressReward);
            Debug.Log($"[EnemyRewardEmitter] Reward Granted: Money +{Mathf.Max(0, moneyReward)}, Kill Progress +{Mathf.Max(0, killProgressReward)}");
        }

        private void ResolveReferences()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (rewardReceiver == null)
            {
                ResolveRewardReceiver();
            }
        }

        private void ResolveRewardReceiver()
        {
            rewardReceiver = FindFirstObjectByType<PlayerRewardReceiver>(FindObjectsInactive.Exclude);
        }
    }
}

/*
Unity setup:
1. Add EnemyRewardEmitter to enemy prefabs that have Health.
2. Set Money Reward when Hansol's enemy reward scale is ready.
3. Keep Kill Progress Reward at 1 if each enemy counts as one kill toward level up.
4. Assign PlayerRewardReceiver manually for strict scene setup, or leave it empty to auto-find the player reward receiver at runtime.
*/
