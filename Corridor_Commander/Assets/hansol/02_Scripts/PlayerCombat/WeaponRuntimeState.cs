using System;
using UnityEngine;

namespace CorridorCommander.PlayerCombat
{
    [Serializable]
    public sealed class WeaponRuntimeState
    {
        [SerializeField] private WeaponItemDefinitionSO weaponDefinition;
        [SerializeField] private int currentMagazineAmmo;
        [SerializeField] private bool isReloading;

        public WeaponItemDefinitionSO WeaponDefinition => weaponDefinition;
        public WeaponFireDefinitionSO FireDefinition => weaponDefinition != null ? weaponDefinition.fireDefinition : null;
        public AmmoDefinitionSO AmmoDefinition => weaponDefinition != null ? weaponDefinition.ammoDefinition : null;

        public int CurrentMagazineAmmo => currentMagazineAmmo;
        public bool IsReloading => isReloading;

        public int MagazineSize => weaponDefinition != null ? weaponDefinition.magazineSize : 0;
        public float ReloadTime => weaponDefinition != null ? weaponDefinition.reloadTime : 0f;

        public WeaponRuntimeState(WeaponItemDefinitionSO weaponDefinition, bool fillMagazine)
        {
            this.weaponDefinition = weaponDefinition;
            currentMagazineAmmo = fillMagazine && weaponDefinition != null
                ? weaponDefinition.magazineSize
                : 0;
            isReloading = false;
        }

        public bool TryConsumeOneRound()
        {
            if (weaponDefinition == null)
            {
                return false;
            }

            if (isReloading)
            {
                return false;
            }

            if (currentMagazineAmmo <= 0)
            {
                return false;
            }

            currentMagazineAmmo--;
            return true;
        }

        public int GetMissingAmmo()
        {
            if (weaponDefinition == null)
            {
                return 0;
            }

            return Mathf.Max(0, weaponDefinition.magazineSize - currentMagazineAmmo);
        }

        public void AddMagazineAmmo(int amount)
        {
            if (weaponDefinition == null || amount <= 0)
            {
                return;
            }

            currentMagazineAmmo = Mathf.Min(
                currentMagazineAmmo + amount,
                weaponDefinition.magazineSize
            );
        }

        public void SetReloading(bool value)
        {
            isReloading = value;
        }
    }
}