using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerItems;
using System;
using UnityEngine;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerSupportTruckItemReceiver : MonoBehaviour, ISupportTruckItemReceiver, ISupportTruckWeaponReceiver, ISupportTruckPlayerItemReceiver
    {
        [Serializable]
        private sealed class WeaponAmmoGrantEntry
        {
            public WeaponItemDefinitionSO weaponDefinition;
            [Min(0)] public int reserveAmmoAmount = 200;
        }

        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private PlayerWeaponInventory weaponInventory;
        [SerializeField] private PlayerAmmoInventory ammoInventory;
        [SerializeField] private PlayerItemInventory itemInventory;

        [Header("Weapon Purchase Ammo")]
        [SerializeField] private bool grantReserveAmmoOnWeaponPurchase = true;
        [SerializeField] [Min(0)] private int defaultWeaponPurchaseReserveAmmoAmount = 0;
        [SerializeField] private WeaponAmmoGrantEntry[] weaponAmmoGrants;

        [Header("Temporary Gun")]
        [SerializeField] private WeaponItemDefinitionSO temporaryGunDefinition;
        [SerializeField] private bool fillTemporaryGunMagazine = true;

        [Header("Heal")]
        [SerializeField] private float healAmountPerGrant = 35f;

        [Header("Grenade")]
        [SerializeField] private AmmoDefinitionSO grenadeAmmoDefinition;
        [SerializeField] private int grenadeAmmoPerGrant = 1;

        private void Awake()
        {
            ResolveReferences();
        }

        public bool TryReceiveSupportTruckItem(
            SupportTruckShopItemGrant itemGrant,
            int amount,
            out string statusMessage)
        {
            ResolveReferences();

            int grantAmount = Mathf.Max(1, amount);
            switch (itemGrant)
            {
                case SupportTruckShopItemGrant.TemporaryGun:
                    return TryReceiveTemporaryGun(out statusMessage);

                case SupportTruckShopItemGrant.Heal:
                    return TryReceiveHeal(grantAmount, out statusMessage);

                case SupportTruckShopItemGrant.Grenade:
                    return TryReceiveGrenade(grantAmount, out statusMessage);

                default:
                    statusMessage = "Support truck item grant is not configured.";
                    return false;
            }
        }

        public bool TryReceiveSupportTruckPlayerItem(
            ItemDefinitionSO itemDefinition,
            int amount,
            out string statusMessage)
        {
            ResolveReferences();

            if (itemInventory == null)
            {
                statusMessage = "Player item inventory is not connected.";
                return false;
            }

            if (itemDefinition == null)
            {
                statusMessage = "Item definition is not configured.";
                return false;
            }

            int grantAmount = Mathf.Max(1, amount);
            itemInventory.AddItem(itemDefinition, grantAmount);
            statusMessage = $"Item granted: {itemDefinition.displayName} x{grantAmount}";
            return true;
        }

        public bool TryReceiveSupportTruckWeapon(
            WeaponItemDefinitionSO weaponDefinition,
            bool fillMagazine,
            out string statusMessage)
        {
            ResolveReferences();

            if (weaponInventory == null)
            {
                statusMessage = "Weapon inventory is not connected.";
                return false;
            }

            if (weaponDefinition == null)
            {
                statusMessage = "Weapon definition is not configured.";
                return false;
            }

            bool alreadyOwned = weaponInventory.HasWeapon(weaponDefinition);
            if (alreadyOwned)
            {
                statusMessage = $"Weapon already owned: {weaponDefinition.displayName}";
                return false;
            }

            WeaponRuntimeState state = weaponInventory.AddWeapon(
                weaponDefinition,
                fillMagazine,
                autoEquipIfFirst: true);

            if (state == null)
            {
                statusMessage = "Weapon grant failed.";
                return false;
            }

            int addedReserveAmmo = TryGrantWeaponReserveAmmo(weaponDefinition);
            statusMessage = $"Weapon granted: {weaponDefinition.displayName}, Ammo +{addedReserveAmmo}";
            return true;
        }

        private int TryGrantWeaponReserveAmmo(WeaponItemDefinitionSO weaponDefinition)
        {
            int reserveAmmoAmount = ResolveWeaponPurchaseReserveAmmoAmount(weaponDefinition);
            if (!grantReserveAmmoOnWeaponPurchase
                || reserveAmmoAmount <= 0
                || ammoInventory == null
                || weaponDefinition == null
                || weaponDefinition.ammoDefinition == null)
            {
                return 0;
            }

            int previousAmount = ammoInventory.GetAmmoAmount(weaponDefinition.ammoDefinition);
            ammoInventory.AddAmmo(weaponDefinition.ammoDefinition, reserveAmmoAmount);
            int currentAmount = ammoInventory.GetAmmoAmount(weaponDefinition.ammoDefinition);
            return Mathf.Max(0, currentAmount - previousAmount);
        }

        private int ResolveWeaponPurchaseReserveAmmoAmount(WeaponItemDefinitionSO weaponDefinition)
        {
            if (weaponDefinition == null || weaponAmmoGrants == null)
            {
                return defaultWeaponPurchaseReserveAmmoAmount;
            }

            for (int i = 0; i < weaponAmmoGrants.Length; i++)
            {
                WeaponAmmoGrantEntry entry = weaponAmmoGrants[i];
                if (entry == null || entry.weaponDefinition == null)
                {
                    continue;
                }

                bool sameReference = entry.weaponDefinition == weaponDefinition;
                bool sameId = !string.IsNullOrWhiteSpace(entry.weaponDefinition.weaponId)
                    && entry.weaponDefinition.weaponId == weaponDefinition.weaponId;
                if (sameReference || sameId)
                {
                    return Mathf.Max(0, entry.reserveAmmoAmount);
                }
            }

            return defaultWeaponPurchaseReserveAmmoAmount;
        }

        private bool TryReceiveTemporaryGun(out string statusMessage)
        {
            if (weaponInventory == null)
            {
                statusMessage = "Weapon inventory is not connected.";
                return false;
            }

            if (temporaryGunDefinition == null)
            {
                statusMessage = "Temporary gun definition is not configured.";
                return false;
            }

            return TryReceiveSupportTruckWeapon(
                temporaryGunDefinition,
                fillTemporaryGunMagazine,
                out statusMessage);
        }

        private bool TryReceiveHeal(int grantAmount, out string statusMessage)
        {
            if (health == null)
            {
                statusMessage = "Player health is not connected.";
                return false;
            }

            if (!health.IsAlive)
            {
                statusMessage = "Player is dead and cannot be healed.";
                return false;
            }

            float healAmount = Mathf.Max(0f, healAmountPerGrant) * grantAmount;
            if (healAmount <= 0f)
            {
                statusMessage = "Heal amount is zero.";
                return false;
            }

            float previousHitPoints = health.CurrentHitPoints;
            health.Restore(healAmount);
            float restoredAmount = health.CurrentHitPoints - previousHitPoints;

            statusMessage = $"Heal granted: +{restoredAmount:0} HP";
            return restoredAmount > 0f;
        }

        private bool TryReceiveGrenade(int grantAmount, out string statusMessage)
        {
            if (ammoInventory == null)
            {
                statusMessage = "Ammo inventory is not connected.";
                return false;
            }

            if (grenadeAmmoDefinition == null)
            {
                statusMessage = "Grenade ammo definition is not configured.";
                return false;
            }

            int ammoAmount = Mathf.Max(1, grenadeAmmoPerGrant) * grantAmount;
            int previousAmount = ammoInventory.GetAmmoAmount(grenadeAmmoDefinition);
            ammoInventory.AddAmmo(grenadeAmmoDefinition, ammoAmount);
            int addedAmount = ammoInventory.GetAmmoAmount(grenadeAmmoDefinition) - previousAmount;

            statusMessage = $"Grenade ammo granted: +{addedAmount}";
            return addedAmount > 0;
        }

        private void ResolveReferences()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (health == null)
            {
                health = FindFirstObjectByType<Health>(FindObjectsInactive.Exclude);
            }

            if (weaponInventory == null)
            {
                weaponInventory = GetComponentInParent<PlayerWeaponInventory>();
            }

            if (weaponInventory == null)
            {
                weaponInventory = GetComponentInChildren<PlayerWeaponInventory>(true);
            }

            if (weaponInventory == null)
            {
                weaponInventory = FindFirstObjectByType<PlayerWeaponInventory>(FindObjectsInactive.Exclude);
            }

            if (ammoInventory == null)
            {
                ammoInventory = GetComponentInParent<PlayerAmmoInventory>();
            }

            if (ammoInventory == null)
            {
                ammoInventory = GetComponentInChildren<PlayerAmmoInventory>(true);
            }

            if (ammoInventory == null)
            {
                ammoInventory = FindFirstObjectByType<PlayerAmmoInventory>(FindObjectsInactive.Exclude);
            }

            if (itemInventory == null)
            {
                itemInventory = GetComponentInParent<PlayerItemInventory>();
            }

            if (itemInventory == null)
            {
                itemInventory = GetComponentInChildren<PlayerItemInventory>(true);
            }

            if (itemInventory == null)
            {
                itemInventory = FindFirstObjectByType<PlayerItemInventory>(FindObjectsInactive.Exclude);
            }
        }
    }
}
