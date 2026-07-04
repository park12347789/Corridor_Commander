using System;
using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerItems;
using UnityEngine;

namespace CorridorCommander
{
    [Serializable]
    public sealed class SupportTruckShopOfferEntry
    {
        [SerializeField] private string offerId = "offer";
        [SerializeField] private string displayName = "Offer";
        [SerializeField] [TextArea] private string description = "Offer description";
        [SerializeField] private Sprite icon;
        [SerializeField] [Min(0)] private int cost = 100;
        [SerializeField] private SupportTruckShopOfferAction action;
        [SerializeField] private SupportTruckShopItemGrant itemGrant;
        [SerializeField] private SupportTruckShopUnlockKey unlockKey;
        [SerializeField] private GameObject squadMemberPrefab;
        [SerializeField] private ItemDefinitionSO itemDefinition;
        [SerializeField] [Min(1)] private int itemAmount = 1;
        [SerializeField] private WeaponItemDefinitionSO weaponDefinition;
        [SerializeField] private bool fillWeaponMagazine = true;

        public string OfferId => offerId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon != null
            ? icon
            : weaponDefinition != null && weaponDefinition.icon != null
                ? weaponDefinition.icon
                : itemDefinition != null
                    ? itemDefinition.icon
                    : null;
        public int Cost => cost;
        public SupportTruckShopOfferAction Action => action;
        public SupportTruckShopItemGrant ItemGrant => itemGrant;
        public SupportTruckShopUnlockKey UnlockKey => unlockKey;
        public GameObject SquadMemberPrefab => squadMemberPrefab;
        public ItemDefinitionSO ItemDefinition => itemDefinition;
        public int ItemAmount => itemAmount;
        public WeaponItemDefinitionSO WeaponDefinition => weaponDefinition;
        public bool FillWeaponMagazine => fillWeaponMagazine;
    }
}
