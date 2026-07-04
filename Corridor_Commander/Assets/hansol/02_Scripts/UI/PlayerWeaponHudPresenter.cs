using CorridorCommander.PlayerCombat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponHudPresenter : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private PlayerWeaponRuntime weaponRuntime;
        [SerializeField] private PlayerWeaponInventory weaponInventory;

        [Header("Text")]
        [SerializeField] private TMP_Text weaponNameText;
        [SerializeField] private TMP_Text ammoText;

        [Header("Images")]
        [SerializeField] private Image weaponIconImage;

        [Header("Refresh")]
        [SerializeField] private bool refreshEveryFrame = true;

        private WeaponItemDefinitionSO displayedWeapon;
        private int lastMagazineAmmo = -1;
        private int lastReserveAmmo = -1;
        private bool lastReloading;
        private bool missingIconImageWarningLogged;
        private WeaponItemDefinitionSO lastMissingIconWarningWeapon;

        private void Awake()
        {
            ResolveReferences();
            RefreshAll(force: true);
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            RefreshAll(force: true);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            if (refreshEveryFrame)
            {
                RefreshAll(force: false);
            }
        }

        public void RefreshAll()
        {
            RefreshAll(force: false);
        }

        private void Subscribe()
        {
            if (weaponInventory != null)
            {
                weaponInventory.CurrentWeaponChanged -= HandleWeaponChanged;
                weaponInventory.WeaponListChanged -= HandleWeaponListChanged;
                weaponInventory.CurrentWeaponChanged += HandleWeaponChanged;
                weaponInventory.WeaponListChanged += HandleWeaponListChanged;
            }

            if (weaponRuntime != null)
            {
                weaponRuntime.ReloadStarted -= HandleReloadChanged;
                weaponRuntime.ReloadCompleted -= HandleReloadChanged;
                weaponRuntime.ReloadStarted += HandleReloadChanged;
                weaponRuntime.ReloadCompleted += HandleReloadChanged;
            }
        }

        private void Unsubscribe()
        {
            if (weaponInventory != null)
            {
                weaponInventory.CurrentWeaponChanged -= HandleWeaponChanged;
                weaponInventory.WeaponListChanged -= HandleWeaponListChanged;
            }

            if (weaponRuntime != null)
            {
                weaponRuntime.ReloadStarted -= HandleReloadChanged;
                weaponRuntime.ReloadCompleted -= HandleReloadChanged;
            }
        }

        private void RefreshAll(bool force)
        {
            WeaponRuntimeState state = weaponRuntime != null
                ? weaponRuntime.CurrentWeaponState
                : weaponInventory != null
                    ? weaponInventory.CurrentWeaponState
                    : null;

            if (state == null || state.WeaponDefinition == null)
            {
                SetEmpty();
                return;
            }

            WeaponItemDefinitionSO weapon = state.WeaponDefinition;
            int magazineAmmo = state.CurrentMagazineAmmo;
            int reserveAmmo = weaponRuntime != null ? weaponRuntime.CurrentReserveAmmo : 0;
            bool reloading = state.IsReloading;
            bool infiniteReserveAmmo = weaponRuntime != null && weaponRuntime.HasInfiniteReserveAmmo;

            bool changed = force
                || displayedWeapon != weapon
                || lastMagazineAmmo != magazineAmmo
                || lastReserveAmmo != reserveAmmo
                || lastReloading != reloading;

            if (!changed)
            {
                return;
            }

            displayedWeapon = weapon;
            lastMagazineAmmo = magazineAmmo;
            lastReserveAmmo = reserveAmmo;
            lastReloading = reloading;

            if (weaponNameText != null)
            {
                weaponNameText.SetText(string.IsNullOrWhiteSpace(weapon.displayName) ? weapon.name : weapon.displayName);
            }

            if (ammoText != null)
            {
                if (infiniteReserveAmmo)
                {
                    ammoText.SetText("{0} / INF", magazineAmmo);
                }
                else
                {
                    ammoText.SetText("{0} / {1}", magazineAmmo, reserveAmmo);
                }
            }

            RefreshWeaponIcon(weapon);
        }

        private void SetEmpty()
        {
            displayedWeapon = null;
            lastMagazineAmmo = -1;
            lastReserveAmmo = -1;
            lastReloading = false;
            lastMissingIconWarningWeapon = null;

            if (weaponNameText != null)
            {
                weaponNameText.SetText("Ready");
            }

            if (ammoText != null)
            {
                ammoText.SetText("-- / --");
            }

            ClearWeaponIcon();
        }

        private void RefreshWeaponIcon(WeaponItemDefinitionSO weapon)
        {
            if (weaponIconImage == null)
            {
                if (!missingIconImageWarningLogged)
                {
                    missingIconImageWarningLogged = true;
                    Debug.LogWarning("[PlayerWeaponHudPresenter] Weapon icon image is not assigned.", this);
                }

                return;
            }

            if (weapon == null || weapon.icon == null)
            {
                ClearWeaponIcon();

                if (weapon != null && lastMissingIconWarningWeapon != weapon)
                {
                    lastMissingIconWarningWeapon = weapon;
                    Debug.LogWarning($"[PlayerWeaponHudPresenter] Weapon icon is not assigned: {weapon.name}", this);
                }

                return;
            }

            lastMissingIconWarningWeapon = null;
            weaponIconImage.sprite = weapon.icon;
            weaponIconImage.preserveAspect = true;
            weaponIconImage.enabled = true;
        }

        private void ClearWeaponIcon()
        {
            if (weaponIconImage == null)
            {
                return;
            }

            weaponIconImage.sprite = null;
            weaponIconImage.enabled = false;
        }

        private void HandleWeaponChanged(WeaponRuntimeState state)
        {
            RefreshAll(force: true);
        }

        private void HandleWeaponListChanged()
        {
            RefreshAll(force: true);
        }

        private void HandleReloadChanged(WeaponRuntimeState state)
        {
            RefreshAll(force: false);
        }

        private void ResolveReferences()
        {
            if (weaponRuntime == null)
            {
                weaponRuntime = FindFirstObjectByType<PlayerWeaponRuntime>(FindObjectsInactive.Include);
            }

            if (weaponInventory == null)
            {
                weaponInventory = FindFirstObjectByType<PlayerWeaponInventory>(FindObjectsInactive.Include);
            }
        }
    }
}
