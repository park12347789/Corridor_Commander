using System;
using System.Collections;
using UnityEngine;

namespace CorridorCommander.PlayerCombat
{
    public sealed class PlayerWeaponRuntime : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponInventory weaponInventory;
        [SerializeField] private PlayerAmmoInventory ammoInventory;
        [SerializeField] private bool infiniteReserveAmmo = true;

        private Coroutine reloadRoutine;
        private WeaponRuntimeState reloadingWeaponState;
        private bool reloadRequested;

        public event Action<WeaponRuntimeState> ReloadStarted;
        public event Action<WeaponRuntimeState> ReloadCompleted;

        public WeaponRuntimeState CurrentWeaponState =>
            weaponInventory != null ? weaponInventory.CurrentWeaponState : null;

        public WeaponItemDefinitionSO CurrentWeapon =>
            CurrentWeaponState != null ? CurrentWeaponState.WeaponDefinition : null;

        public WeaponFireDefinitionSO CurrentFireDefinition =>
            CurrentWeaponState != null ? CurrentWeaponState.FireDefinition : null;

        public int CurrentMagazineAmmo =>
            CurrentWeaponState != null ? CurrentWeaponState.CurrentMagazineAmmo : 0;

        public bool IsReloading =>
            CurrentWeaponState != null && CurrentWeaponState.IsReloading;

        public bool HasInfiniteReserveAmmo => infiniteReserveAmmo;

        public int CurrentReserveAmmo
        {
            get
            {
                WeaponRuntimeState state = CurrentWeaponState;

                if (state == null || state.AmmoDefinition == null)
                {
                    return 0;
                }

                if (infiniteReserveAmmo)
                {
                    return int.MaxValue;
                }

                if (ammoInventory == null)
                {
                    return 0;
                }

                return ammoInventory.GetAmmoAmount(state.AmmoDefinition);
            }
        }

        private void OnEnable()
        {
            if (weaponInventory != null)
            {
                weaponInventory.CurrentWeaponChanged += HandleCurrentWeaponChanged;
            }
        }

        private void OnDisable()
        {
            if (weaponInventory != null)
            {
                weaponInventory.CurrentWeaponChanged -= HandleCurrentWeaponChanged;
            }
        }

        private void Update()
        {
            if (!reloadRequested)
            {
                return;
            }

            reloadRequested = false;
            TryStartReload();
        }

        public void RequestReload()
        {
            reloadRequested = true;
        }

        private void HandleCurrentWeaponChanged(WeaponRuntimeState newWeaponState)
        {
            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
            }

            if (reloadingWeaponState != null)
            {
                reloadingWeaponState.SetReloading(false);
                reloadingWeaponState = null;
            }

            if (newWeaponState != null)
            {
                newWeaponState.SetReloading(false);

                Debug.Log(
                    $"[PlayerWeaponRuntime] Current Weapon: {newWeaponState.WeaponDefinition.displayName}, Ammo: {newWeaponState.CurrentMagazineAmmo}/{CurrentReserveAmmo}"
                );
            }
        }

        public bool TryConsumeOneRound()
        {
            WeaponRuntimeState state = CurrentWeaponState;

            if (state == null)
            {
                Debug.LogWarning("[PlayerWeaponRuntime] 현재 장착 무기가 없습니다.");
                return false;
            }

            if (state.IsReloading)
            {
                return false;
            }

            if (state.CurrentMagazineAmmo <= 0)
            {
                Debug.Log("[PlayerWeaponRuntime] 탄창이 비었습니다.");
                TryStartReload();
                return false;
            }

            bool consumed = state.TryConsumeOneRound();

            if (consumed)
            {
                Debug.Log($"[PlayerWeaponRuntime] Ammo: {state.CurrentMagazineAmmo}/{CurrentReserveAmmo}");
            }

            return consumed;
        }

        public bool TryStartReload()
        {
            WeaponRuntimeState state = CurrentWeaponState;

            if (state == null)
            {
                return false;
            }

            if (state.IsReloading)
            {
                return false;
            }

            if (state.GetMissingAmmo() <= 0)
            {
                return false;
            }

            if (!infiniteReserveAmmo && (ammoInventory == null || CurrentReserveAmmo <= 0))
            {
                Debug.Log("[PlayerWeaponRuntime] 예비 탄약이 없습니다.");
                return false;
            }

            reloadRoutine = StartCoroutine(ReloadRoutine(state));
            return true;
        }

        private IEnumerator ReloadRoutine(WeaponRuntimeState state)
        {
            reloadingWeaponState = state;
            state.SetReloading(true);

            Debug.Log("[PlayerWeaponRuntime] Reload Start");
            ReloadStarted?.Invoke(state);

            yield return new WaitForSeconds(state.ReloadTime);

            if (CurrentWeaponState != state)
            {
                state.SetReloading(false);
                reloadRoutine = null;
                reloadingWeaponState = null;
                yield break;
            }

            int needAmount = state.GetMissingAmmo();
            int loadedAmount = infiniteReserveAmmo
                ? needAmount
                : ammoInventory.ConsumeAmmo(state.AmmoDefinition, needAmount);

            state.AddMagazineAmmo(loadedAmount);
            state.SetReloading(false);

            reloadRoutine = null;
            reloadingWeaponState = null;

            Debug.Log($"[PlayerWeaponRuntime] Reload Complete: {state.CurrentMagazineAmmo}/{CurrentReserveAmmo}");
            ReloadCompleted?.Invoke(state);
        }
    }
}
