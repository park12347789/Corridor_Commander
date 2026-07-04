using UnityEngine;
using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerItems;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponVisualController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWeaponInventory weaponInventory;
        [SerializeField] private PlayerProjectileLauncher projectileLauncher;
        [SerializeField] private PlayerThrowableItemController throwableItemController;
        [SerializeField] private Transform weaponRoot;

        [Header("Spawn")]
        [SerializeField] private bool clearRootChildrenOnEquip = false;
        [SerializeField] private bool overrideWeaponScale = false;
        [SerializeField] private Vector3 weaponScaleOverride = Vector3.one;

        [Header("Throwable Visibility")]
        [SerializeField] private bool hideWeaponDuringThrowableAim = true;
        [SerializeField] private float showWeaponDelayAfterThrow = 0.45f;

        private GameObject currentWeaponVisual;
        private Coroutine restoreWeaponRoutine;
        private bool weaponHiddenByThrowable;

        public Transform WeaponRoot => weaponRoot;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (weaponInventory != null)
            {
                weaponInventory.CurrentWeaponChanged += HandleCurrentWeaponChanged;
            }

            if (throwableItemController != null)
            {
                throwableItemController.ThrowAimStarted += HandleThrowAimStarted;
                throwableItemController.ThrowCanceled += HandleThrowCanceled;
                throwableItemController.ThrowCommitted += HandleThrowCommitted;
            }

            EquipCurrentWeaponVisual();
        }

        private void OnDisable()
        {
            if (weaponInventory != null)
            {
                weaponInventory.CurrentWeaponChanged -= HandleCurrentWeaponChanged;
            }

            if (throwableItemController != null)
            {
                throwableItemController.ThrowAimStarted -= HandleThrowAimStarted;
                throwableItemController.ThrowCanceled -= HandleThrowCanceled;
                throwableItemController.ThrowCommitted -= HandleThrowCommitted;
            }

            StopRestoreWeaponRoutine();
            SetCurrentWeaponVisualVisible(true);
        }

        private void HandleCurrentWeaponChanged(WeaponRuntimeState currentWeaponState)
        {
            EquipWeaponVisual(currentWeaponState);
        }

        private void EquipCurrentWeaponVisual()
        {
            EquipWeaponVisual(weaponInventory != null ? weaponInventory.CurrentWeaponState : null);
        }

        private void EquipWeaponVisual(WeaponRuntimeState weaponState)
        {
            ClearCurrentWeaponVisual();

            if (weaponRoot == null)
            {
                Debug.LogError("[PlayerWeaponVisualController] Weapon Root is not connected.", this);
                return;
            }

            if (clearRootChildrenOnEquip)
            {
                ClearWeaponRootChildren();
            }

            WeaponItemDefinitionSO weaponDefinition = weaponState != null
                ? weaponState.WeaponDefinition
                : null;

            if (weaponDefinition == null || weaponDefinition.weaponPrefab == null)
            {
                if (weaponDefinition != null)
                {
                    Debug.LogError("[PlayerWeaponVisualController] Weapon Prefab is not connected: " + weaponDefinition.displayName, this);
                }

                return;
            }

            currentWeaponVisual = Instantiate(
                weaponDefinition.weaponPrefab,
                weaponRoot.position,
                weaponRoot.rotation,
                weaponRoot);

            currentWeaponVisual.transform.localPosition = Vector3.zero;
            currentWeaponVisual.transform.localRotation = Quaternion.identity;

            if (overrideWeaponScale)
            {
                currentWeaponVisual.transform.localScale = weaponScaleOverride;
            }

            if (weaponHiddenByThrowable)
            {
                SetCurrentWeaponVisualVisible(false);
            }

            if (projectileLauncher != null)
            {
                IWeaponView weaponView = currentWeaponVisual.GetComponentInChildren<IWeaponView>(true);
                if (weaponView == null || weaponView.Muzzle == null)
                {
                    Debug.LogError("[PlayerWeaponVisualController] WeaponView or Muzzle is not configured on " + weaponDefinition.weaponPrefab.name, currentWeaponVisual);
                }
                else
                {
                    projectileLauncher.SetMuzzle(weaponView.Muzzle);
                }
            }

            Debug.Log($"[PlayerWeaponVisualController] Equipped Visual: {weaponDefinition.displayName}");
        }

        private void ClearCurrentWeaponVisual()
        {
            if (currentWeaponVisual == null)
            {
                return;
            }

            Destroy(currentWeaponVisual);
            currentWeaponVisual = null;
        }

        private void HandleThrowAimStarted(ItemDefinitionSO itemDefinition)
        {
            if (!hideWeaponDuringThrowableAim)
            {
                return;
            }

            StopRestoreWeaponRoutine();
            weaponHiddenByThrowable = true;
            SetCurrentWeaponVisualVisible(false);
            Debug.Log("[PlayerWeaponVisualController] Weapon visual hidden for throwable aim.", this);
        }

        private void HandleThrowCanceled(ItemDefinitionSO itemDefinition)
        {
            RestoreWeaponVisual();
        }

        private void HandleThrowCommitted(ItemDefinitionSO itemDefinition)
        {
            StopRestoreWeaponRoutine();
            restoreWeaponRoutine = StartCoroutine(RestoreWeaponVisualAfterDelay());
        }

        private System.Collections.IEnumerator RestoreWeaponVisualAfterDelay()
        {
            float delay = Mathf.Max(0f, showWeaponDelayAfterThrow);

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            restoreWeaponRoutine = null;
            RestoreWeaponVisual();
        }

        private void RestoreWeaponVisual()
        {
            StopRestoreWeaponRoutine();
            weaponHiddenByThrowable = false;
            SetCurrentWeaponVisualVisible(true);
            Debug.Log("[PlayerWeaponVisualController] Weapon visual restored after throwable action.", this);
        }

        private void SetCurrentWeaponVisualVisible(bool isVisible)
        {
            if (currentWeaponVisual == null)
            {
                return;
            }

            currentWeaponVisual.SetActive(isVisible);
        }

        private void StopRestoreWeaponRoutine()
        {
            if (restoreWeaponRoutine == null)
            {
                return;
            }

            StopCoroutine(restoreWeaponRoutine);
            restoreWeaponRoutine = null;
        }

        private void ClearWeaponRootChildren()
        {
            for (int i = weaponRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(weaponRoot.GetChild(i).gameObject);
            }
        }

        private void ResolveReferences()
        {
            if (weaponInventory == null)
            {
                weaponInventory = GetComponent<PlayerWeaponInventory>();
            }

            if (weaponInventory == null)
            {
                weaponInventory = GetComponentInParent<PlayerWeaponInventory>();
            }

            if (weaponInventory == null)
            {
                weaponInventory = GetComponentInChildren<PlayerWeaponInventory>(true);
            }

            if (projectileLauncher == null)
            {
                projectileLauncher = GetComponent<PlayerProjectileLauncher>();
            }

            if (projectileLauncher == null)
            {
                projectileLauncher = GetComponentInParent<PlayerProjectileLauncher>();
            }

            if (projectileLauncher == null)
            {
                projectileLauncher = GetComponentInChildren<PlayerProjectileLauncher>(true);
            }

            if (throwableItemController == null)
            {
                throwableItemController = GetComponent<PlayerThrowableItemController>();
            }

            if (throwableItemController == null)
            {
                throwableItemController = GetComponentInParent<PlayerThrowableItemController>();
            }

            if (throwableItemController == null)
            {
                throwableItemController = GetComponentInChildren<PlayerThrowableItemController>(true);
            }

            if (throwableItemController == null)
            {
                throwableItemController = FindFirstObjectByType<PlayerThrowableItemController>(FindObjectsInactive.Include);
            }
        }
    }
}

/*
Unity setup:
1. Add PlayerWeaponVisualController to the player root or PlayerSystems object.
2. Assign PlayerWeaponInventory and PlayerProjectileLauncher, or leave them empty for auto-binding.
3. Assign Weapon Root to the transform where the weapon model should be attached.
4. Put a child named Muzzle inside each weapon prefab, or assign Fallback Muzzle.
5. Set WeaponItemDefinitionSO.weaponPrefab for every weapon that should appear in the player's hands.
6. Assign PlayerThrowableItemController to hide the weapon while aiming or throwing grenades.
*/
