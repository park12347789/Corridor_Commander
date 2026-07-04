using UnityEngine;

namespace CorridorCommander
{
    public enum TurretAttackMode
    {
        PulseHitscan = 0,
        SustainedBeam = 1
    }

    [CreateAssetMenu(
        menuName = "Corridor Commander/Combat/Turret Attack Definition",
        fileName = "TurretAttackDefinition")]
    public sealed class TurretAttackDefinitionSO : ScriptableObject
    {
        [SerializeField] private string attackId = "turret_attack";
        [SerializeField] private string displayName = "Turret Attack";
        [SerializeField] [Min(0f)] private float range = 10f;
        [SerializeField] [Min(0.01f)] private float fireInterval = 0.75f;
        [SerializeField] [Min(0f)] private float damage = 6f;
        [SerializeField] private TurretAttackMode attackMode = TurretAttackMode.PulseHitscan;
        [SerializeField] [Min(0f)] private float attackWindupTime;
        [SerializeField] [Min(0)] private int maxUpgradeLevel = 3;
        [SerializeField] [Min(0f)] private float rangePerLevel = 1.25f;
        [SerializeField] [Min(0f)] private float damagePerLevel = 2f;
        [SerializeField] [Range(0.05f, 1f)] private float fireIntervalMultiplierPerLevel = 0.9f;
        [SerializeField] private StatusEffectDefinitionSO[] hitEffects;
        [SerializeField] private SkillDefinitionSO linkedSkill;

        public string AttackId => attackId;
        public string DisplayName => displayName;
        public float Range => Mathf.Max(0f, range);
        public float FireInterval => Mathf.Max(0.01f, fireInterval);
        public float Damage => Mathf.Max(0f, damage);
        public TurretAttackMode AttackMode => attackMode;
        public float AttackWindupTime => Mathf.Max(0f, attackWindupTime);
        public int MaxUpgradeLevel => Mathf.Max(0, maxUpgradeLevel);
        public StatusEffectDefinitionSO[] HitEffects => hitEffects;
        public SkillDefinitionSO LinkedSkill => linkedSkill;

        public float GetRange(int upgradeLevel)
        {
            return Range + Mathf.Max(0, upgradeLevel) * Mathf.Max(0f, rangePerLevel);
        }

        public float GetFireInterval(int upgradeLevel)
        {
            float multiplier = Mathf.Pow(
                Mathf.Clamp(fireIntervalMultiplierPerLevel, 0.05f, 1f),
                Mathf.Max(0, upgradeLevel));
            return Mathf.Max(0.01f, FireInterval * multiplier);
        }

        public float GetDamage(int upgradeLevel)
        {
            return Damage + Mathf.Max(0, upgradeLevel) * Mathf.Max(0f, damagePerLevel);
        }
    }
}
