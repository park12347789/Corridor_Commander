using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Skills/Mortar Role Definition",
        fileName = "MortarRoleDefinition")]
    public sealed class MortarSkillRoleDefinitionSO : BuildableRoleDefinitionSO
    {
        [SerializeField] private SkillDefinitionSO skillDefinition;
        [SerializeField] private MortarProjectile projectilePrefab;
        [SerializeField] private GameObject muzzleVfxPrefab;
        [SerializeField] [Min(0.01f)] private float muzzleVfxScale = 1f;
        [SerializeField] private GameObject projectileVfxPrefab;
        [SerializeField] private GameObject impactVfxPrefab;
        [SerializeField] private AudioClip[] fireAudioClips;
        [SerializeField] private AudioClip[] impactAudioClips;
        [SerializeField, Range(0f, 1f)] private float fireAudioVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float impactAudioVolume = 0.8f;
        [SerializeField] [Min(0.1f)] private float flightTime = 1.2f;
        [SerializeField] [Min(0f)] private float arcHeight = 6f;

        public SkillDefinitionSO SkillDefinition => skillDefinition;
        public MortarProjectile ProjectilePrefab => projectilePrefab;
        public GameObject MuzzleVfxPrefab => muzzleVfxPrefab;
        public float MuzzleVfxScale => muzzleVfxScale;
        public GameObject ProjectileVfxPrefab => projectileVfxPrefab;
        public GameObject ImpactVfxPrefab => impactVfxPrefab;
        public AudioClip[] FireAudioClips => fireAudioClips;
        public AudioClip[] ImpactAudioClips => impactAudioClips;
        public float FireAudioVolume => fireAudioVolume;
        public float ImpactAudioVolume => impactAudioVolume;
        public float FlightTime => flightTime;
        public float ArcHeight => arcHeight;
    }
}
