using UnityEngine;

namespace CorridorCommander.Enemy
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Enemies/Healer Definition",
        fileName = "EnemyHealerDefinition")]
    public sealed class EnemyHealerDefinitionSO : ScriptableObject
    {
        [Header("Targeting")]
        [SerializeField, Min(0.1f)] private float detectionRadius = 12f;
        [SerializeField, Min(0.1f)] private float healingRange = 10f;
        [SerializeField, Min(0.05f)] private float targetRefreshInterval = 0.25f;
        [SerializeField] private bool requireLineOfSight;

        [Header("Healing")]
        [SerializeField, Min(0.01f)] private float healingAmount = 8f;
        [SerializeField, Min(0.05f)] private float healingInterval = 1.5f;
        [SerializeField, Min(0f)] private float castDelay = 0.45f;

        public float DetectionRadius => detectionRadius;
        public float HealingRange => Mathf.Min(healingRange, detectionRadius);
        public float TargetRefreshInterval => targetRefreshInterval;
        public bool RequireLineOfSight => requireLineOfSight;
        public float HealingAmount => healingAmount;
        public float HealingInterval => healingInterval;
        public float CastDelay => castDelay;
    }
}

/*
Unity setup:
1. Create an asset from Corridor Commander/Enemies/Healer Definition.
2. Tune Healing Amount and Healing Interval in the Inspector.
3. Assign the asset to EnemyHealerController on a healer enemy prefab.
*/
