using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Skills/Skill Definition",
        fileName = "SkillDefinition")]
    public sealed class SkillDefinitionSO : ScriptableObject
    {
        [SerializeField] private string skillId = "skill";
        [SerializeField] private string displayName = "Skill";
        [SerializeField] private Sprite icon;
        [SerializeField] [Min(0.01f)] private float cooldown = 5f;
        [SerializeField] [Min(0f)] private float range = 20f;
        [SerializeField] [Min(0f)] private float damage = 25f;
        [SerializeField] [Min(0f)] private float radius = 3f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private StatusEffectDefinitionSO[] hitEffects;

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public float Cooldown => cooldown;
        public float Range => range;
        public float Damage => damage;
        public float Radius => radius;
        public LayerMask TargetLayers => targetLayers;
        public StatusEffectDefinitionSO[] HitEffects => hitEffects;
    }
}
