using UnityEngine;

namespace CorridorCommander.Enemy
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Enemies/Ranged Attack Definition",
        fileName = "EnemyRangedAttackDefinition")]
    public sealed class EnemyRangedAttackDefinitionSO : ScriptableObject
    {
        [Header("Targeting")]
        [SerializeField, Min(0.1f)] private float detectionRange = 12f;
        [SerializeField, Min(0.1f)] private float attackRange = 10f;
        [SerializeField, Min(0.05f)] private float targetRefreshInterval = 0.25f;
        [SerializeField] private bool requireLineOfSight = true;

        [Header("Attack Timing")]
        [SerializeField, Min(0f)] private float windupDuration = 0.48f;
        [SerializeField, Min(0.05f)] private float attackInterval = 2.2f;

        [Header("Projectile")]
        [SerializeField] private EnemyRangedProjectile projectilePrefab;
        [SerializeField] private GameObject projectileVisualPrefab;
        [SerializeField] private Vector3 projectileVisualScale = Vector3.one;
        [SerializeField, Min(0f)] private float damage = 12f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 11f;
        [SerializeField] private bool useBallisticArc = true;
        [SerializeField, Min(0.1f)] private float ballisticArcHeight = 2.5f;
        [SerializeField] private float projectileGravity = -1.5f;
        [SerializeField, Min(0.1f)] private float projectileLifetime = 4f;
        [SerializeField, Min(0f)] private float targetHeightOffset = 1f;

        public float DetectionRange => detectionRange;
        public float AttackRange => Mathf.Min(attackRange, detectionRange);
        public float TargetRefreshInterval => targetRefreshInterval;
        public bool RequireLineOfSight => requireLineOfSight;
        public float WindupDuration => windupDuration;
        public float AttackInterval => attackInterval;
        public EnemyRangedProjectile ProjectilePrefab => projectilePrefab;
        public GameObject ProjectileVisualPrefab => projectileVisualPrefab;
        public Vector3 ProjectileVisualScale => projectileVisualScale;
        public float Damage => damage;
        public float ProjectileSpeed => projectileSpeed;
        public bool UseBallisticArc => useBallisticArc;
        public float BallisticArcHeight => ballisticArcHeight;
        public float ProjectileGravity => projectileGravity;
        public float ProjectileLifetime => projectileLifetime;
        public float TargetHeightOffset => targetHeightOffset;
    }
}

/*
Unity setup:
1. Create an asset from Corridor Commander/Enemies/Ranged Attack Definition.
2. Assign an EnemyRangedProjectile prefab and tune range, timing, and damage.
3. Assign the asset to EnemyRangedAttackController on a ranged enemy prefab.
*/
