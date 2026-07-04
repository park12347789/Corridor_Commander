using UnityEngine;

namespace CorridorCommander
{
    public enum StatusEffectKind
    {
        Slow = 0,
        Custom = 100
    }

    [CreateAssetMenu(
        menuName = "Corridor Commander/Combat/Status Effect Definition",
        fileName = "StatusEffectDefinition")]
    public sealed class StatusEffectDefinitionSO : ScriptableObject
    {
        [SerializeField] private string effectId = "status_effect";
        [SerializeField] private string displayName = "Status Effect";
        [SerializeField] private StatusEffectKind kind = StatusEffectKind.Slow;
        [SerializeField] [Min(0.01f)] private float duration = 2f;
        [SerializeField] [Range(0.05f, 1f)] private float speedMultiplier = 0.6f;
        [SerializeField] private GameObject applyVfxPrefab;

        public string EffectId => effectId;
        public string DisplayName => displayName;
        public StatusEffectKind Kind => kind;
        public float Duration => Mathf.Max(0.01f, duration);
        public float SpeedMultiplier => Mathf.Clamp(speedMultiplier, 0.05f, 1f);
        public GameObject ApplyVfxPrefab => applyVfxPrefab;
        public bool AffectsMoveSpeed => kind == StatusEffectKind.Slow && SpeedMultiplier < 1f;
    }
}
