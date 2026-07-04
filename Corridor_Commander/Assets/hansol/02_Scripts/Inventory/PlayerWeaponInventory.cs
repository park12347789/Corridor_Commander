using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander.PlayerCombat
{

    public enum DuplicateWeaponPolicy
    {
        AllowDuplicate,
        IgnoreDuplicate,
        RefillMagazineOnDuplicate
    }
    public sealed class PlayerWeaponInventory : MonoBehaviour
    {
        [Serializable]
        private sealed class StartingWeaponEntry
        {
            public WeaponItemDefinitionSO weaponDefinition;
            public bool fillMagazine = true;
        }

        [SerializeField] private StartingWeaponEntry[] startingWeapons;
        [SerializeField] private int startingWeaponIndex = 0;
        [SerializeField] private DuplicateWeaponPolicy duplicatePolicy = DuplicateWeaponPolicy.IgnoreDuplicate;

        private readonly List<WeaponRuntimeState> ownedWeapons = new List<WeaponRuntimeState>();
        private int currentWeaponIndex = -1;

        public WeaponRuntimeState CurrentWeaponState
        {
            get
            {
                if (currentWeaponIndex < 0 || currentWeaponIndex >= ownedWeapons.Count)
                {
                    return null;
                }

                return ownedWeapons[currentWeaponIndex];
            }
        }

        public int CurrentWeaponIndex => currentWeaponIndex;
        public int WeaponCount => ownedWeapons.Count;

        public event Action<WeaponRuntimeState> CurrentWeaponChanged;
        public event Action WeaponListChanged;

        private void Awake()
        {
            InitializeStartingWeapons();
        }

        private void InitializeStartingWeapons()
        {
            ownedWeapons.Clear();
            currentWeaponIndex = -1;

            for (int i = 0; i < startingWeapons.Length; i++)
            {
                StartingWeaponEntry entry = startingWeapons[i];

                if (entry == null || entry.weaponDefinition == null)
                {
                    continue;
                }

                AddWeapon(entry.weaponDefinition, entry.fillMagazine, autoEquipIfFirst: false);
            }

            if (ownedWeapons.Count > 0)
            {
                int clampedIndex = Mathf.Clamp(startingWeaponIndex, 0, ownedWeapons.Count - 1);
                EquipWeaponAt(clampedIndex);
            }
        }

        public WeaponRuntimeState AddWeapon(
            WeaponItemDefinitionSO weaponDefinition,
            bool fillMagazine,
            bool autoEquipIfFirst = true)
        {
            if (weaponDefinition == null)
            {
                Debug.LogWarning("[PlayerWeaponInventory] 추가할 WeaponDefinition이 없습니다.");
                return null;
            }

            WeaponRuntimeState existingState = FindWeaponState(weaponDefinition);

            if (existingState != null && duplicatePolicy != DuplicateWeaponPolicy.AllowDuplicate)
            {
                if (duplicatePolicy == DuplicateWeaponPolicy.RefillMagazineOnDuplicate)
                {
                    existingState.AddMagazineAmmo(existingState.GetMissingAmmo());
                    Debug.Log($"[PlayerWeaponInventory] Duplicate Weapon Refilled: {weaponDefinition.displayName}");
                }
                else
                {
                    Debug.Log($"[PlayerWeaponInventory] Duplicate Weapon Ignored: {weaponDefinition.displayName}");
                }

                return existingState;
            }

            WeaponRuntimeState weaponState = new WeaponRuntimeState(
                weaponDefinition,
                fillMagazine
            );

            ownedWeapons.Add(weaponState);
            WeaponListChanged?.Invoke();

            Debug.Log($"[PlayerWeaponInventory] Weapon Added: {weaponDefinition.displayName}");

            if (autoEquipIfFirst && currentWeaponIndex < 0)
            {
                EquipWeaponAt(ownedWeapons.Count - 1);
            }

            return weaponState;
        }

        public bool EquipWeaponAt(int index)
        {
            if (index < 0 || index >= ownedWeapons.Count)
            {
                return false;
            }

            currentWeaponIndex = index;
            CurrentWeaponChanged?.Invoke(CurrentWeaponState);

            WeaponItemDefinitionSO weapon = CurrentWeaponState.WeaponDefinition;
            Debug.Log($"[PlayerWeaponInventory] Equipped: {weapon.displayName}");

            return true;
        }

        public bool EquipNextWeapon()
        {
            if (ownedWeapons.Count <= 0)
            {
                return false;
            }

            int nextIndex = currentWeaponIndex + 1;

            if (nextIndex >= ownedWeapons.Count)
            {
                nextIndex = 0;
            }

            return EquipWeaponAt(nextIndex);
        }

        public bool EquipPreviousWeapon()
        {
            if (ownedWeapons.Count <= 0)
            {
                return false;
            }

            int previousIndex = currentWeaponIndex - 1;

            if (previousIndex < 0)
            {
                previousIndex = ownedWeapons.Count - 1;
            }

            return EquipWeaponAt(previousIndex);
        }

        public WeaponRuntimeState GetWeaponStateAt(int index)
        {
            if (index < 0 || index >= ownedWeapons.Count)
            {
                return null;
            }

            return ownedWeapons[index];
        }

        public bool HasWeapon(WeaponItemDefinitionSO weaponDefinition)
        {
            return FindWeaponState(weaponDefinition) != null;
        }

        private WeaponRuntimeState FindWeaponState(WeaponItemDefinitionSO weaponDefinition)
        {
            if (weaponDefinition == null)
            {
                return null;
            }

            for (int i = 0; i < ownedWeapons.Count; i++)
            {
                WeaponRuntimeState state = ownedWeapons[i];

                if (state == null || state.WeaponDefinition == null)
                {
                    continue;
                }

                if (state.WeaponDefinition.weaponId == weaponDefinition.weaponId)
                {
                    return state;
                }
            }

            return null;
        }
    }
}