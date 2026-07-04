using CorridorCommander.PlayerCombat;
using UnityEngine;
using UnityEngine.AI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class AlliedSquadMemberAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private AlliedSquadMemberCombat combat;
        [SerializeField] private Health health;

        [Header("Locomotion")]
        [SerializeField] private float speedForNormalization = 5f;
        [SerializeField] private bool forceGrounded = true;

        [Header("Animator Parameters")]
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string groundedParameter = "IsGrounded";
        [SerializeField] private string verticalVelocityParameter = "VerticalVelocity";
        [SerializeField] private string weaponTypeParameter = "WeaponType";
        [SerializeField] private string aimingParameter = "IsAiming";
        [SerializeField] private string fireTrigger = "Fire";
        [SerializeField] private string reloadTrigger = "Reload";
        [SerializeField] private string deathTrigger = "Die";

        private int moveSpeedHash;
        private int groundedHash;
        private int verticalVelocityHash;
        private int weaponTypeHash;
        private int aimingHash;
        private int fireHash;
        private int reloadHash;
        private int deathHash;

        private Vector3 previousPosition;
        private bool hasPreviousPosition;

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
            RefreshCombatState();
            previousPosition = transform.position;
            hasPreviousPosition = true;
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            hasPreviousPosition = false;
        }

        private void Update()
        {
            UpdateLocomotionParameters();
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (combat != null)
            {
                combat.Fired += HandleFired;
                combat.AimingStarted += HandleAimingStarted;
                combat.AimingStopped += HandleAimingStopped;
                combat.ReloadStarted += HandleReloadStarted;
                combat.WeaponChanged += HandleWeaponChanged;
            }

            if (health != null)
            {
                health.Died += HandleDied;
            }
        }

        private void UnsubscribeEvents()
        {
            if (combat != null)
            {
                combat.Fired -= HandleFired;
                combat.AimingStarted -= HandleAimingStarted;
                combat.AimingStopped -= HandleAimingStopped;
                combat.ReloadStarted -= HandleReloadStarted;
                combat.WeaponChanged -= HandleWeaponChanged;
            }

            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void HandleFired()
        {
            SetTrigger(fireHash);
        }

        private void HandleAimingStarted()
        {
            SetAiming(true);
        }

        private void HandleAimingStopped()
        {
            SetAiming(false);
        }

        private void HandleReloadStarted()
        {
            SetTrigger(reloadHash);
        }

        private void HandleWeaponChanged(WeaponItemDefinitionSO weaponDefinition)
        {
            SetWeaponType(ResolveWeaponAnimationType(weaponDefinition));
        }

        private void HandleDied(Health deadHealth)
        {
            SetTrigger(deathHash);
        }

        private void UpdateLocomotionParameters()
        {
            if (animator == null)
            {
                return;
            }

            float horizontalSpeed = ResolveHorizontalSpeed();
            float normalizedSpeed = speedForNormalization > 0f
                ? Mathf.Clamp01(horizontalSpeed / speedForNormalization)
                : 0f;

            if (moveSpeedHash != 0)
            {
                animator.SetFloat(moveSpeedHash, normalizedSpeed, 0.12f, Time.deltaTime);
            }

            if (groundedHash != 0)
            {
                animator.SetBool(groundedHash, forceGrounded);
            }

            if (verticalVelocityHash != 0)
            {
                animator.SetFloat(verticalVelocityHash, 0f);
            }
        }

        private float ResolveHorizontalSpeed()
        {
            if (navMeshAgent != null)
            {
                Vector3 agentVelocity = navMeshAgent.velocity;
                agentVelocity.y = 0f;
                return agentVelocity.magnitude;
            }

            if (!hasPreviousPosition || Time.deltaTime <= 0f)
            {
                previousPosition = transform.position;
                hasPreviousPosition = true;
                return 0f;
            }

            Vector3 delta = transform.position - previousPosition;
            previousPosition = transform.position;
            delta.y = 0f;
            return delta.magnitude / Time.deltaTime;
        }

        private void RefreshWeaponType()
        {
            SetWeaponType(ResolveWeaponAnimationType(combat != null ? combat.WeaponDefinition : null));
        }

        private void RefreshCombatState()
        {
            SetAiming(combat != null && combat.IsAiming);
        }

        private void SetWeaponType(WeaponAnimationType animationType)
        {
            if (animator == null || weaponTypeHash == 0)
            {
                return;
            }

            animator.SetInteger(weaponTypeHash, (int)animationType);
        }

        private void SetAiming(bool value)
        {
            if (animator == null || aimingHash == 0)
            {
                return;
            }

            animator.SetBool(aimingHash, value);
        }

        private static WeaponAnimationType ResolveWeaponAnimationType(WeaponItemDefinitionSO weaponDefinition)
        {
            if (weaponDefinition == null)
            {
                return WeaponAnimationType.None;
            }

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

        private void CacheAnimatorHashes()
        {
            moveSpeedHash = GetHash(moveSpeedParameter);
            groundedHash = GetHash(groundedParameter);
            verticalVelocityHash = GetHash(verticalVelocityParameter);
            weaponTypeHash = GetHash(weaponTypeParameter);
            aimingHash = GetHash(aimingParameter);
            fireHash = GetHash(fireTrigger);
            reloadHash = GetHash(reloadTrigger);
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

            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }

            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponentInParent<NavMeshAgent>();
            }

            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponentInChildren<NavMeshAgent>(true);
            }

            if (combat == null)
            {
                combat = GetComponent<AlliedSquadMemberCombat>();
            }

            if (combat == null)
            {
                combat = GetComponentInParent<AlliedSquadMemberCombat>();
            }

            if (combat == null)
            {
                combat = GetComponentInChildren<AlliedSquadMemberCombat>(true);
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (health == null)
            {
                health = GetComponentInChildren<Health>(true);
            }
        }
    }
}

/*
Unity setup outline:
1. Add AlliedSquadMemberAnimationController to the allied squad member prefab root.
2. Assign the Animator on the character model, or leave it empty for auto-binding.
3. Keep AC_Player on the Animator so MoveSpeed, WeaponType, Fire, and Die are recognized.
4. Assign AlliedSquadMemberCombat and NavMeshAgent, or leave them empty for auto-binding.
5. Add Health to the allied member if Die animation should be triggered from the shared death event.
*/
