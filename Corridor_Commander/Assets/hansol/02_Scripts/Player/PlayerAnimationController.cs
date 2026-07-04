using UnityEngine;
using CorridorCommander;
using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerItems;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerFacingController facingController;
        [SerializeField] private PlayerWeaponInventory weaponInventory;
        [SerializeField] private PlayerWeaponRuntime weaponRuntime;
        [SerializeField] private PlayerProjectileLauncher projectileLauncher;
        [SerializeField] private PlayerItemUseController itemUseController;
        [SerializeField] private PlayerThrowableItemController throwableItemController;
        [SerializeField] private Health health;

        [Header("Locomotion")]
        [SerializeField] private bool driveLocomotionParameters = false;
        [SerializeField] private float runSpeedForNormalization = 5.2f;

        [Header("Animator Parameters")]
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string groundedParameter = "IsGrounded";
        [SerializeField] private string verticalVelocityParameter = "VerticalVelocity";
        [SerializeField] private string weaponTypeParameter = "WeaponType";
        [SerializeField] private string aimingParameter = "IsAiming";
        [SerializeField] private string fireTrigger = "Fire";
        [SerializeField] private string reloadTrigger = "Reload";
        [SerializeField] private string hitTrigger = "Hit";
        [SerializeField] private string useItemTrigger = "UseItem";
        [SerializeField] private string throwTrigger = "Throw";
        [SerializeField] private string throwAimingParameter = "IsThrowAiming";
        [SerializeField] private string cancelThrowTrigger = "CancelThrow";
        [SerializeField] private string deathTrigger = "Death";

        [Header("Upper Body State Names")]
        [SerializeField] private string upperBodyLayerName = "UpperBody";
        [SerializeField] private string ranged1HEmptyStateName = "Ranged1H.Ranged1HEmpty";
        [SerializeField] private string ranged1HAimingStateName = "Ranged1H.Ranged_1H_Aiming";
        [SerializeField] private string ranged2HEmptyStateName = "Ranged2H.Ranged2HEmpty";
        [SerializeField] private string ranged2HAimingStateName = "Ranged2H.Ranged_2H_Aiming";
        [SerializeField] private float automaticFireStopCrossFadeDuration = 0.05f;

        private int moveSpeedHash;
        private int groundedHash;
        private int verticalVelocityHash;
        private int weaponTypeHash;
        private int aimingHash;
        private int fireHash;
        private int reloadHash;
        private int hitHash;
        private int useItemHash;
        private int throwHash;
        private int throwAimingHash;
        private int cancelThrowHash;
        private int deathHash;

        private void Awake()
        {
            ResolveReferences();
            CacheAnimatorHashes();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeEvents();
            RefreshWeaponType();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            UpdateAimingParameter();

            if (driveLocomotionParameters)
            {
                UpdateLocomotionParameters();
            }
        }

        public void TriggerUseItem()
        {
            SetTrigger(useItemHash);
        }

        public void TriggerThrow()
        {
            SetTrigger(throwHash);
        }

        public void TriggerHit()
        {
            SetTrigger(hitHash);
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (weaponInventory != null)
            {
                weaponInventory.CurrentWeaponChanged += HandleCurrentWeaponChanged;
            }

            if (weaponRuntime != null)
            {
                weaponRuntime.ReloadStarted += HandleReloadStarted;
            }

            if (projectileLauncher != null)
            {
                projectileLauncher.FireAnimationRequested += HandleFired;
                projectileLauncher.AutomaticFireStopped += HandleAutomaticFireStopped;
            }

            if (itemUseController != null)
            {
                itemUseController.ItemUsed += HandleItemUsed;
            }

            if (throwableItemController != null)
            {
                throwableItemController.ThrowAimStarted += HandleThrowAimStarted;
                throwableItemController.ThrowCanceled += HandleThrowCanceled;
                throwableItemController.ThrowCommitted += HandleThrowCommitted;
            }

            if (health != null)
            {
                health.Damaged += HandleDamaged;
                health.Died += HandleDied;
            }
        }

        private void UnsubscribeEvents()
        {
            if (weaponInventory != null)
            {
                weaponInventory.CurrentWeaponChanged -= HandleCurrentWeaponChanged;
            }

            if (weaponRuntime != null)
            {
                weaponRuntime.ReloadStarted -= HandleReloadStarted;
            }

            if (projectileLauncher != null)
            {
                projectileLauncher.FireAnimationRequested -= HandleFired;
                projectileLauncher.AutomaticFireStopped -= HandleAutomaticFireStopped;
            }

            if (itemUseController != null)
            {
                itemUseController.ItemUsed -= HandleItemUsed;
            }

            if (throwableItemController != null)
            {
                throwableItemController.ThrowAimStarted -= HandleThrowAimStarted;
                throwableItemController.ThrowCanceled -= HandleThrowCanceled;
                throwableItemController.ThrowCommitted -= HandleThrowCommitted;
            }

            if (health != null)
            {
                health.Damaged -= HandleDamaged;
                health.Died -= HandleDied;
            }
        }

        private void HandleCurrentWeaponChanged(WeaponRuntimeState weaponState)
        {
            SetWeaponType(ResolveWeaponAnimationType(weaponState));
        }

        private void HandleReloadStarted(WeaponRuntimeState weaponState)
        {
            SetTrigger(reloadHash);
        }

        private void HandleFired()
        {
            SetTrigger(fireHash);
        }

        private void HandleAutomaticFireStopped()
        {
            if (animator == null)
            {
                return;
            }

            if (fireHash != 0)
            {
                animator.ResetTrigger(fireHash);
            }

            int layerIndex = animator.GetLayerIndex(upperBodyLayerName);

            if (layerIndex < 0)
            {
                return;
            }

            string stateName = ResolveAutomaticFireStopStateName();

            if (string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            animator.CrossFadeInFixedTime(
                stateName,
                Mathf.Max(0f, automaticFireStopCrossFadeDuration),
                layerIndex);
        }

        private void HandleItemUsed(ItemDefinitionSO itemDefinition)
        {
            if (itemDefinition == null)
            {
                return;
            }

            if (itemDefinition.useType == PlayerItemUseType.Grenade)
            {
                SetTrigger(throwHash);
                return;
            }

            SetTrigger(useItemHash);
        }

        private void HandleThrowAimStarted(ItemDefinitionSO itemDefinition)
        {
            SetBool(throwAimingHash, true);
        }

        private void HandleThrowCanceled(ItemDefinitionSO itemDefinition)
        {
            SetBool(throwAimingHash, false);
            SetTrigger(cancelThrowHash);
        }

        private void HandleThrowCommitted(ItemDefinitionSO itemDefinition)
        {
            SetBool(throwAimingHash, false);
            SetTrigger(throwHash);
        }

        private void HandleDamaged(Health damagedHealth, float damageAmount)
        {
            if (damagedHealth == null || damagedHealth.CurrentHitPoints <= 0f)
            {
                return;
            }

            SetTrigger(hitHash);
        }

        private void HandleDied(Health deadHealth)
        {
            SetTrigger(deathHash);
        }

        private string ResolveAutomaticFireStopStateName()
        {
            WeaponAnimationType animationType = ResolveWeaponAnimationType(
                weaponRuntime != null ? weaponRuntime.CurrentWeaponState : null);
            bool isAiming = facingController != null && facingController.IsAimHeld;

            switch (animationType)
            {
                case WeaponAnimationType.Ranged1H:
                    return isAiming ? ranged1HAimingStateName : ranged1HEmptyStateName;

                case WeaponAnimationType.Ranged2H:
                    return isAiming ? ranged2HAimingStateName : ranged2HEmptyStateName;

                default:
                    return string.Empty;
            }
        }

        private void UpdateAimingParameter()
        {
            if (animator == null || aimingHash == 0)
            {
                return;
            }

            bool isAiming = facingController != null && facingController.IsAimHeld;
            animator.SetBool(aimingHash, isAiming);
        }

        private void UpdateLocomotionParameters()
        {
            if (animator == null || characterController == null)
            {
                return;
            }

            Vector3 horizontalVelocity = characterController.velocity;
            horizontalVelocity.y = 0f;

            float normalizedSpeed = runSpeedForNormalization > 0f
                ? Mathf.Clamp01(horizontalVelocity.magnitude / runSpeedForNormalization)
                : 0f;

            if (moveSpeedHash != 0)
            {
                animator.SetFloat(moveSpeedHash, normalizedSpeed, 0.12f, Time.deltaTime);
            }

            if (groundedHash != 0)
            {
                animator.SetBool(groundedHash, characterController.isGrounded);
            }

            if (verticalVelocityHash != 0)
            {
                animator.SetFloat(verticalVelocityHash, characterController.velocity.y);
            }
        }

        private void RefreshWeaponType()
        {
            WeaponRuntimeState currentState = weaponInventory != null
                ? weaponInventory.CurrentWeaponState
                : null;

            SetWeaponType(ResolveWeaponAnimationType(currentState));
        }

        private void SetWeaponType(WeaponAnimationType animationType)
        {
            if (animator == null || weaponTypeHash == 0)
            {
                return;
            }

            animator.SetInteger(weaponTypeHash, (int)animationType);
        }

        private WeaponAnimationType ResolveWeaponAnimationType(WeaponRuntimeState weaponState)
        {
            if (weaponState == null || weaponState.WeaponDefinition == null)
            {
                return WeaponAnimationType.None;
            }

            WeaponItemDefinitionSO weaponDefinition = weaponState.WeaponDefinition;

            if (weaponDefinition.AnimationType != WeaponAnimationType.None)
            {
                return weaponDefinition.AnimationType;
            }

            string weaponId = weaponDefinition.weaponId != null
                ? weaponDefinition.weaponId.ToLowerInvariant()
                : string.Empty;

            string displayName = weaponDefinition.displayName != null
                ? weaponDefinition.displayName.ToLowerInvariant()
                : string.Empty;

            if (weaponId.Contains("pistol") || displayName.Contains("pistol")
                || weaponId.Contains("laser") || displayName.Contains("laser"))
            {
                return WeaponAnimationType.Ranged1H;
            }

            return WeaponAnimationType.Ranged2H;
        }

        private void SetTrigger(int triggerHash)
        {
            if (animator == null || triggerHash == 0)
            {
                return;
            }

            animator.SetTrigger(triggerHash);
        }

        private void SetBool(int parameterHash, bool value)
        {
            if (animator == null || parameterHash == 0)
            {
                return;
            }

            animator.SetBool(parameterHash, value);
        }

        private void CacheAnimatorHashes()
        {
            moveSpeedHash = GetHash(moveSpeedParameter);
            groundedHash = GetHash(groundedParameter);
            verticalVelocityHash = GetHash(verticalVelocityParameter);
            weaponTypeHash = GetHash(weaponTypeParameter);
            aimingHash = GetHash(aimingParameter);
            fireHash = GetHash(fireTrigger);
            reloadHash = GetHash(reloadTrigger);
            hitHash = GetHash(hitTrigger);
            useItemHash = GetHash(useItemTrigger);
            throwHash = GetHash(throwTrigger);
            throwAimingHash = GetHash(throwAimingParameter);
            cancelThrowHash = GetHash(cancelThrowTrigger);
            deathHash = GetHash(deathTrigger);
        }

        private static int GetHash(string parameterName)
        {
            return string.IsNullOrWhiteSpace(parameterName)
                ? 0
                : Animator.StringToHash(parameterName);
        }

        private void ResolveReferences()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (characterController == null)
            {
                characterController = GetComponentInParent<CharacterController>();
            }

            if (facingController == null)
            {
                facingController = GetComponent<PlayerFacingController>();
            }

            if (facingController == null)
            {
                facingController = GetComponentInParent<PlayerFacingController>();
            }

            if (weaponInventory == null)
            {
                weaponInventory = GetComponentInChildren<PlayerWeaponInventory>(true);
            }

            if (weaponRuntime == null)
            {
                weaponRuntime = GetComponentInChildren<PlayerWeaponRuntime>(true);
            }

            if (projectileLauncher == null)
            {
                projectileLauncher = GetComponentInChildren<PlayerProjectileLauncher>(true);
            }

            if (itemUseController == null)
            {
                itemUseController = GetComponentInChildren<PlayerItemUseController>(true);
            }

            if (throwableItemController == null)
            {
                throwableItemController = GetComponentInChildren<PlayerThrowableItemController>(true);
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }
        }
    }
}

/*
Unity setup outline:
1. Add PlayerAnimationController to the Player root.
2. Assign the Animator used by the KayKit character visual.
3. Keep Drive Locomotion Parameters off if PlayerLocomotionController already writes movement parameters.
4. Set WeaponItemDefinitionSO Animation Type to Ranged1H for Pistol and Laser Gun, and Ranged2H for other guns.
5. Clear the Animator field on PlayerHealthController if both scripts trigger Hit/Death at the same time.
*/
