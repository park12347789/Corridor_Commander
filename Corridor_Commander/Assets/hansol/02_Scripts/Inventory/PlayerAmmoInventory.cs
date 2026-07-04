using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander.PlayerCombat
{
    public sealed class PlayerAmmoInventory : MonoBehaviour
    {
        [Serializable]
        private sealed class AmmoStack
        {
            public AmmoDefinitionSO ammoDefinition;
            public int amount = 0;
        }

        [SerializeField] private AmmoStack[] startingAmmos;

        private readonly Dictionary<AmmoDefinitionSO, int> ammoAmounts = new();

        private void Awake()
        {
            InitializeStartingAmmos();
        }

        private void InitializeStartingAmmos()
        {
            ammoAmounts.Clear();

            for (int i = 0; i < startingAmmos.Length; i++)
            {
                AmmoStack stack = startingAmmos[i];

                if (stack == null || stack.ammoDefinition == null)
                {
                    continue;
                }

                int clampedAmount = Mathf.Clamp(
                    stack.amount,
                    0,
                    stack.ammoDefinition.maxCarryAmount
                );

                ammoAmounts[stack.ammoDefinition] = clampedAmount;
            }
        }

        public int GetAmmoAmount(AmmoDefinitionSO ammoDefinition)
        {
            if (ammoDefinition == null)
            {
                return 0;
            }

            return ammoAmounts.TryGetValue(ammoDefinition, out int amount)
                ? amount
                : 0;
        }

        public int ConsumeAmmo(AmmoDefinitionSO ammoDefinition, int requestedAmount)
        {
            if (ammoDefinition == null || requestedAmount <= 0)
            {
                return 0;
            }

            int currentAmount = GetAmmoAmount(ammoDefinition);
            int consumeAmount = Mathf.Min(currentAmount, requestedAmount);

            ammoAmounts[ammoDefinition] = currentAmount - consumeAmount;

            return consumeAmount;
        }

        public void AddAmmo(AmmoDefinitionSO ammoDefinition, int amount)
        {
            if (ammoDefinition == null || amount <= 0)
            {
                return;
            }

            int currentAmount = GetAmmoAmount(ammoDefinition);
            int nextAmount = Mathf.Clamp(
                currentAmount + amount,
                0,
                ammoDefinition.maxCarryAmount
            );

            ammoAmounts[ammoDefinition] = nextAmount;
        }
    }
}