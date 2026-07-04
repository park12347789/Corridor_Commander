using System;
using UnityEngine;
using UnityEngine.Events;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerCurrencyWallet : MonoBehaviour
    {
        [Header("Currency")]
        [SerializeField] private int startingMoney = 0;

        [Header("Events")]
        [SerializeField] private UnityEvent<int> moneyChanged;
        [SerializeField] private UnityEvent<int> moneyAdded;
        [SerializeField] private UnityEvent<int> moneySpent;

        private int currentMoney;

        public int CurrentMoney => currentMoney;

        public event Action<int> MoneyChanged;
        public event Action<int> MoneyAdded;
        public event Action<int> MoneySpent;

        private void Awake()
        {
            currentMoney = Mathf.Max(0, startingMoney);
            NotifyMoneyChanged();
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentMoney += amount;

            Debug.Log($"[PlayerCurrencyWallet] Money Added: +{amount}, Current: {currentMoney}");

            MoneyAdded?.Invoke(amount);
            moneyAdded?.Invoke(amount);
            NotifyMoneyChanged();
        }

        public bool CanSpend(int amount)
        {
            return amount >= 0 && currentMoney >= amount;
        }

        public bool TrySpendMoney(int amount)
        {
            if (amount < 0)
            {
                return false;
            }

            if (!CanSpend(amount))
            {
                Debug.Log($"[PlayerCurrencyWallet] Not Enough Money: Need {amount}, Current: {currentMoney}");
                return false;
            }

            currentMoney -= amount;

            Debug.Log($"[PlayerCurrencyWallet] Money Spent: -{amount}, Current: {currentMoney}");

            MoneySpent?.Invoke(amount);
            moneySpent?.Invoke(amount);
            NotifyMoneyChanged();

            return true;
        }

        private void NotifyMoneyChanged()
        {
            MoneyChanged?.Invoke(currentMoney);
            moneyChanged?.Invoke(currentMoney);
        }
    }
}

/*
Unity setup:
1. Add PlayerCurrencyWallet to the player root or PlayerSystems object.
2. Set Starting Money in the Inspector.
3. Shop, turret, weapon, and squad purchase systems should call TrySpendMoney().
4. Enemy reward systems should not edit money directly; call PlayerRewardReceiver instead.
*/
