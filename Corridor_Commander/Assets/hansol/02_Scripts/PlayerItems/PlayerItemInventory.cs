using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander.PlayerItems
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemInventory : MonoBehaviour
    {
        [Header("Starting Items")]
        [SerializeField] private PlayerItemRuntimeEntry[] startingItems;

        private readonly List<PlayerItemRuntimeEntry> items = new();

        public int ItemCount => items.Count;
        public IReadOnlyList<PlayerItemRuntimeEntry> Items => items;

        public event Action ItemListChanged;

        private void Awake()
        {
            InitializeStartingItems();
        }

        private void InitializeStartingItems()
        {
            items.Clear();

            if (startingItems == null)
            {
                return;
            }

            for (int i = 0; i < startingItems.Length; i++)
            {
                PlayerItemRuntimeEntry entry = startingItems[i];

                if (entry == null || entry.ItemDefinition == null || entry.Count <= 0)
                {
                    continue;
                }

                AddItem(entry.ItemDefinition, entry.Count);
            }
        }

        public PlayerItemRuntimeEntry GetItemAt(int index)
        {
            if (index < 0 || index >= items.Count)
            {
                return null;
            }

            return items[index];
        }

        public PlayerItemRuntimeEntry GetFirstAvailableItem()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].IsAvailable)
                {
                    return items[i];
                }
            }

            return null;
        }

        public PlayerItemRuntimeEntry GetFirstAvailableItemByType(PlayerItemUseType useType)
        {
            for (int i = 0; i < items.Count; i++)
            {
                PlayerItemRuntimeEntry entry = items[i];

                if (entry == null || !entry.IsAvailable)
                {
                    continue;
                }

                if (entry.ItemDefinition != null && entry.ItemDefinition.useType == useType)
                {
                    return entry;
                }
            }

            return null;
        }

        public void AddItem(ItemDefinitionSO itemDefinition, int amount)
        {
            if (itemDefinition == null || amount <= 0)
            {
                return;
            }

            PlayerItemRuntimeEntry existingEntry = FindEntry(itemDefinition);

            if (existingEntry != null)
            {
                existingEntry.AddCount(amount);
            }
            else
            {
                items.Add(new PlayerItemRuntimeEntry(itemDefinition, amount));
            }

            ItemListChanged?.Invoke();
        }

        public bool TryConsume(PlayerItemRuntimeEntry entry)
        {
            if (entry == null || !entry.TryConsumeOne())
            {
                return false;
            }

            ItemListChanged?.Invoke();
            return true;
        }

        private PlayerItemRuntimeEntry FindEntry(ItemDefinitionSO itemDefinition)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].IsSameItem(itemDefinition))
                {
                    return items[i];
                }
            }

            return null;
        }
    }
}