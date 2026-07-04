using System;
using UnityEngine;

namespace CorridorCommander.PlayerItems
{
    [Serializable]
    public sealed class PlayerItemRuntimeEntry
    {
        [SerializeField] private ItemDefinitionSO itemDefinition;
        [SerializeField][Min(0)] private int count;

        public PlayerItemRuntimeEntry(ItemDefinitionSO itemDefinition, int count)
        {
            this.itemDefinition = itemDefinition;
            this.count = Mathf.Max(0, count);
        }

        public ItemDefinitionSO ItemDefinition => itemDefinition;
        public int Count => count;
        public bool IsAvailable => itemDefinition != null && count > 0;

        public bool IsSameItem(ItemDefinitionSO otherDefinition)
        {
            return itemDefinition == otherDefinition;
        }

        public bool TryConsumeOne()
        {
            if (count <= 0)
            {
                return false;
            }

            count--;
            return true;
        }

        public void AddCount(int amount)
        {
            count += Mathf.Max(0, amount);
        }

        public void SetCount(int value)
        {
            count = Mathf.Max(0, value);
        }
    }
}