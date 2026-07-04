using System;
using UnityEngine;

namespace CorridorCommander
{
    [Serializable]
    public sealed class TEMP_UsableItemEntry
    {
        [SerializeField] private string itemId = "item";
        [SerializeField] private string displayName = "Item";
        [SerializeField] [TextArea] private string description = "Temporary usable item";
        [SerializeField] private TEMP_ItemUseType useType;
        [SerializeField] [Min(0)] private int charges = 1;
        [SerializeField] [Min(0f)] private float value = 25f;
        [SerializeField] [Min(0f)] private float radius = 3f;

        public TEMP_UsableItemEntry(
            string configuredId,
            string configuredDisplayName,
            string configuredDescription,
            TEMP_ItemUseType configuredUseType,
            int configuredCharges,
            float configuredValue,
            float configuredRadius)
        {
            itemId = configuredId;
            displayName = configuredDisplayName;
            description = configuredDescription;
            useType = configuredUseType;
            charges = configuredCharges;
            value = configuredValue;
            radius = configuredRadius;
        }

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public TEMP_ItemUseType UseType => useType;
        public int Charges => charges;
        public float Value => value;
        public float Radius => radius;
        public bool IsAvailable => charges > 0;

        public bool TryConsume()
        {
            if (charges <= 0)
            {
                return false;
            }

            charges--;
            return true;
        }

        public void AddCharges(int amount)
        {
            charges += Mathf.Max(0, amount);
        }
    }
}
