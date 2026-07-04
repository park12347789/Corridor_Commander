using UnityEngine;

namespace CorridorCommander.PlayerCombat
{
    public enum WeaponAudioPlaybackMode
    {
        OneShot,
        ContinuousBeam
    }

    [CreateAssetMenu(
        menuName = "Corridor Commander/Combat/Weapon Audio Definition",
        fileName = "WeaponAudioDefinition"
    )]
    public sealed class WeaponAudioDefinitionSO : ScriptableObject
    {
        [Header("Playback")]
        [SerializeField] private WeaponAudioPlaybackMode playbackMode = WeaponAudioPlaybackMode.OneShot;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.96f, 1.04f);
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;

        [Header("Weapon Fire")]
        [SerializeField] private AudioClip[] fireClips;
        [SerializeField] private AudioClip[] dryFireClips;

        [Header("Continuous Beam")]
        [SerializeField] private AudioClip beamChargeClip;
        [SerializeField] private AudioClip beamLoopClip;
        [SerializeField] private AudioClip beamStopClip;
        [SerializeField] private bool waitForBeamChargeBeforeLoop = true;
        [SerializeField, Min(0f)] private float beamLoopStartExtraDelay = 0f;
        [SerializeField, Range(0f, 2f)] private float beamChargeVolumeMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float beamLoopVolumeMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float beamStopVolumeMultiplier = 1f;
        [SerializeField, Min(0f)] private float beamStopDelay = 0f;

        [Header("Reload")]
        [SerializeField] private AudioClip ranged1HReloadClip;
        [SerializeField] private AudioClip ranged2HReloadClip;

        [Header("Impact")]
        [SerializeField] private bool useRicochetSounds = true;
        [SerializeField] private AudioClip[] ricochetClips;
        [SerializeField] private AudioClip[] explosionClips;

        [Header("Equip")]
        [SerializeField] private AudioClip equipClip;
        [SerializeField] private AudioClip unequipClip;

        public WeaponAudioPlaybackMode PlaybackMode => playbackMode;
        public float Volume => volume;
        public Vector2 PitchRange => pitchRange;
        public float SpatialBlend => spatialBlend;
        public AudioClip BeamChargeClip => beamChargeClip;
        public AudioClip BeamLoopClip => beamLoopClip;
        public AudioClip BeamStopClip => beamStopClip;
        public bool WaitForBeamChargeBeforeLoop => waitForBeamChargeBeforeLoop;
        public float BeamLoopStartExtraDelay => beamLoopStartExtraDelay;
        public float BeamChargeVolumeMultiplier => beamChargeVolumeMultiplier;
        public float BeamLoopVolumeMultiplier => beamLoopVolumeMultiplier;
        public float BeamStopVolumeMultiplier => beamStopVolumeMultiplier;
        public float BeamStopDelay => beamStopDelay;
        public AudioClip Ranged1HReloadClip => ranged1HReloadClip;
        public AudioClip Ranged2HReloadClip => ranged2HReloadClip;
        public bool UseRicochetSounds => useRicochetSounds;
        public AudioClip EquipClip => equipClip;
        public AudioClip UnequipClip => unequipClip;

        public AudioClip GetRandomFireClip()
        {
            return GetRandomClip(fireClips);
        }

        public AudioClip GetRandomDryFireClip()
        {
            return GetRandomClip(dryFireClips);
        }

        public AudioClip GetReloadClip(WeaponAnimationType animationType)
        {
            switch (animationType)
            {
                case WeaponAnimationType.Ranged1H:
                    return ranged1HReloadClip;

                case WeaponAnimationType.Ranged2H:
                    return ranged2HReloadClip;

                default:
                    return ranged2HReloadClip != null ? ranged2HReloadClip : ranged1HReloadClip;
            }
        }

        public AudioClip GetRandomRicochetClip()
        {
            return useRicochetSounds ? GetRandomClip(ricochetClips) : null;
        }

        public AudioClip GetRandomExplosionClip()
        {
            return GetRandomClip(explosionClips);
        }

        public float GetRandomPitch()
        {
            float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);
            return Random.Range(minPitch, maxPitch);
        }

        private static AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int index = Random.Range(0, clips.Length);
            return clips[index];
        }
    }
}

/*
Unity setup outline:
1. Create assets from Create > Corridor Commander > Combat > Weapon Audio Definition.
2. Store weapon audio assets under Assets/junhee/10_ScriptableObjects/sound.
3. Assign Fire Clips per weapon, shared Ricochet Clips for bullet weapons, and disable Use Ricochet Sounds for laser weapons.
4. Use Continuous Beam clips for laser cannon charge, loop, and stop sounds.
5. Assign the resulting asset to WeaponItemDefinitionSO.audioDefinition.
*/
