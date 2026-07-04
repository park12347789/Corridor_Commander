using UnityEngine;

namespace CorridorCommander.PlayerCombat
{
    [DisallowMultipleComponent]
    public sealed class WeaponView : MonoBehaviour, IWeaponView
    {
        [SerializeField] private Transform muzzle;

        public Transform Muzzle => muzzle;

        private void Awake()
        {
            if (muzzle == null)
            {
                Debug.LogError("[WeaponView] Muzzle is not assigned.", this);
            }
        }
    }
}
