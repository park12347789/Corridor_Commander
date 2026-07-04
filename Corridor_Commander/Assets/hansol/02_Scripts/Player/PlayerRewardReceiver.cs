using UnityEngine;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerRewardReceiver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCurrencyWallet currencyWallet;
        [SerializeField] private PlayerLevelProgression levelProgression;

        private void Awake()
        {
            ResolveReferences();
        }

        public void ReceiveKillReward(int moneyAmount, int killProgressAmount)
        {
            if (moneyAmount > 0)
            {
                currencyWallet?.AddMoney(moneyAmount);
            }

            if (killProgressAmount > 0)
            {
                levelProgression?.AddKillProgress(killProgressAmount);
            }

            Debug.Log($"[PlayerRewardReceiver] Kill Reward Received: Money +{Mathf.Max(0, moneyAmount)}, Kill Progress +{Mathf.Max(0, killProgressAmount)}");
        }

        public void ReceiveMoney(int moneyAmount)
        {
            if (moneyAmount <= 0)
            {
                return;
            }

            currencyWallet?.AddMoney(moneyAmount);
        }

        public void ReceiveKillProgress(int killProgressAmount)
        {
            if (killProgressAmount <= 0)
            {
                return;
            }

            levelProgression?.AddKillProgress(killProgressAmount);
        }

        private void ResolveReferences()
        {
            if (currencyWallet == null)
            {
                currencyWallet = GetComponent<PlayerCurrencyWallet>();
            }

            if (currencyWallet == null)
            {
                currencyWallet = GetComponentInParent<PlayerCurrencyWallet>();
            }

            if (currencyWallet == null)
            {
                currencyWallet = GetComponentInChildren<PlayerCurrencyWallet>(true);
            }

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
        }
    }
}

/*
Unity setup:
1. Add PlayerRewardReceiver to the player root or PlayerSystems object.
2. Add PlayerCurrencyWallet and PlayerLevelProgression to the same player hierarchy.
3. EnemyRewardEmitter should reference this component or find it automatically.
4. Future enemy reward tables should call ReceiveKillReward() with their configured money and kill progress values.
*/
