using UnityEngine;

namespace CorridorCommander.PlayerCombat
{
    public enum WeaponTriggerMode
    {
        SemiAuto,
        FullAuto,
        Continuous
    }

    public enum WeaponFirePattern
    {
        Single,
        ForwardSpread,
        RandomCone
    }

    public enum WeaponFireResolveType
    {
        Projectile,
        Hitscan
    }

    [CreateAssetMenu(
        menuName = "Corridor Commander/Combat/Weapon Fire Definition",
        fileName = "WeaponFireDefinition"
    )]
    public sealed class WeaponFireDefinitionSO : ScriptableObject
    {
        [Header("Resolve Type")]
        public WeaponFireResolveType resolveType = WeaponFireResolveType.Projectile;

        [Header("Projectile Resolve")]
        public ProjectileDefinitionSO projectileDefinition;

        [Header("Projectile Trajectory")]
        [Tooltip("Projectile resolve only. Positive values raise the launch angle to compensate for gravity.")]
        public float projectileLaunchPitchOffset = 0f;

        [Header("Hitscan Resolve")]
        public HitscanDefinitionSO hitscanDefinition;

        [Header("Fire Timing")]
        public float fireInterval = 0.12f;

        [Header("Fire Pattern")]
        public WeaponFirePattern firePattern = WeaponFirePattern.Single;

        [Header("Projectile Count")]
        [Min(1)]
        public int projectileCount = 1;

        [Header("Forward Spread")]
        [Tooltip("Total horizontal spread angle around the forward direction.")]
        public float horizontalSpreadAngle = 0f;

        [Tooltip("Use random horizontal spread instead of evenly spaced forward spread.")]
        public bool useRandomHorizontalSpread = false;

        [Header("Random Cone")]
        [Tooltip("Random cone angle around the aim direction.")]
        public float coneSpreadAngle = 0f;

        [Header("Trigger")]
        public WeaponTriggerMode triggerMode = WeaponTriggerMode.FullAuto;

        [Tooltip("Damage tick interval for continuous weapons.")]
        public float damageTickInterval = 0.1f;
    }
}
