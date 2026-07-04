using CorridorCommander.PlayerCombat;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class AlliedWeaponVisualController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AlliedSquadMemberCombat combat;
        [SerializeField] private Transform weaponRoot;

        [Header("Auto Binding")]
        [SerializeField] private bool autoFindWeaponRoot = true;
        [SerializeField] private string weaponRootName = "WeaponSocket";
        [SerializeField] private string muzzleName = "Muzzle";

        [Header("Spawn")]
        [SerializeField] private bool showWeaponVisual = true;
        [SerializeField] private bool clearRootChildrenOnEquip = false;
        [SerializeField] private bool overrideWeaponScale = false;
        [SerializeField] private Vector3 weaponScaleOverride = Vector3.one;

        [Header("Fallback")]
        [SerializeField] private Transform fallbackMuzzle;

        private GameObject currentWeaponVisual;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeEvents();
            EquipCurrentWeaponVisual();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (combat != null)
            {
                combat.WeaponChanged += HandleWeaponChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (combat != null)
            {
                combat.WeaponChanged -= HandleWeaponChanged;
            }
        }

        private void HandleWeaponChanged(WeaponItemDefinitionSO weaponDefinition)
        {
            EquipWeaponVisual(weaponDefinition);
        }

        private void EquipCurrentWeaponVisual()
        {
            EquipWeaponVisual(combat != null ? combat.WeaponDefinition : null);
        }

        private void EquipWeaponVisual(WeaponItemDefinitionSO weaponDefinition)
        {
            ClearCurrentWeaponVisual();

            if (weaponRoot == null)
            {
                Debug.LogWarning("[AlliedWeaponVisualController] Weapon Root is not connected.", this);
                combat?.SetMuzzle(fallbackMuzzle);
                return;
            }

            if (clearRootChildrenOnEquip)
            {
                ClearWeaponRootChildren();
            }

            if (!showWeaponVisual)
            {
                Transform handMuzzle = FindChildByName(weaponRoot, muzzleName);
                combat?.SetMuzzle(handMuzzle != null ? handMuzzle : fallbackMuzzle);
                return;
            }

            if (weaponDefinition == null || weaponDefinition.weaponPrefab == null)
            {
                combat?.SetMuzzle(fallbackMuzzle);
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

            Transform resolvedMuzzle = FindChildByName(currentWeaponVisual.transform, muzzleName);

            if (resolvedMuzzle != null)
            {
                combat?.SetMuzzle(resolvedMuzzle);
                Debug.Log($"[AlliedWeaponVisualController] Equipped Visual: {weaponDefinition.displayName}", this);
                return;
            }

            combat?.SetMuzzle(fallbackMuzzle);
            Debug.LogWarning("[AlliedWeaponVisualController] Muzzle was not found on the equipped weapon.", this);
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

        private void ClearWeaponRootChildren()
        {
            for (int i = weaponRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(weaponRoot.GetChild(i).gameObject);
            }
        }

        private void ResolveReferences()
        {
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

            if (weaponRoot == null && autoFindWeaponRoot)
            {
                weaponRoot = FindChildByName(transform, weaponRootName);
            }
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildByName(root.GetChild(i), childName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}

/*
Unity setup outline:
1. Add AlliedWeaponVisualController to the allied squad member prefab root.
2. Assign AlliedSquadMemberCombat, or leave it empty for auto-binding.
3. Assign Weapon Root to the character's WeaponSocket, or keep Auto Find Weapon Root on.
4. Make sure each weapon prefab has a child named Muzzle.
5. Set Override Weapon Scale only when the squad member needs a custom weapon scale.
*/
