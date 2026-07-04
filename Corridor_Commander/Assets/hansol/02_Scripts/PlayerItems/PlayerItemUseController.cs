using System;
using UnityEngine;

namespace CorridorCommander.PlayerItems
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemUseController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerItemInventory itemInventory;
        [SerializeField] private Health health;

        public event Action<ItemDefinitionSO> ItemUsed;

        private void Awake()
        {
            ResolveReferences();
        }

        public bool TryUseItem(
            PlayerItemRuntimeEntry itemEntry,
            GameObject user,
            out string statusMessage)
        {
            ResolveReferences();

            if (itemEntry == null || itemEntry.ItemDefinition == null)
            {
                statusMessage = "No usable item";
                return false;
            }

            if (itemInventory == null)
            {
                statusMessage = "No item inventory";
                return false;
            }

            if (!itemEntry.IsAvailable)
            {
                statusMessage = itemEntry.ItemDefinition.displayName + " unavailable";
                return false;
            }

            ItemDefinitionSO definition = itemEntry.ItemDefinition;

            if (definition.useType == PlayerItemUseType.Passive)
            {
                statusMessage = definition.displayName + " is kept in inventory";
                return false;
            }

            switch (definition.useType)
            {
                case PlayerItemUseType.Heal:
                    if (!CanUseHeal(definition, out statusMessage))
                    {
                        return false;
                    }

                    if (!itemInventory.TryConsume(itemEntry))
                    {
                        statusMessage = definition.displayName + " unavailable";
                        return false;
                    }

                    statusMessage = UseHeal(definition);
                    ItemUsed?.Invoke(definition);
                    return true;

                case PlayerItemUseType.Grenade:
                    statusMessage = definition.displayName + " requires throwable aim";
                    Debug.LogError("[PlayerItemUseController] Grenade items must be used through PlayerThrowableItemController.", this);
                    return false;

                default:
                    statusMessage = "Unknown item: " + definition.displayName;
                    return false;
            }
        }

        private bool CanUseHeal(ItemDefinitionSO definition, out string statusMessage)
        {
            if (health == null)
            {
                statusMessage = definition.displayName + ": no Health";
                Debug.LogError("[PlayerItemUseController] Health is not connected.", this);
                return false;
            }

            statusMessage = string.Empty;
            return true;
        }

        private string UseHeal(ItemDefinitionSO definition)
        {
            if (health == null)
            {
                return definition.displayName + ": no Health";
            }

            health.Restore(definition.value);
            ItemAudioUtility.PlayUseAudio(definition, transform.position);
            return definition.displayName + " +" + definition.value.ToString("0");
        }

        private void ResolveReferences()
        {
            if (itemInventory == null)
            {
                itemInventory = GetComponentInParent<PlayerItemInventory>();
            }

            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }
        }
    }
}
