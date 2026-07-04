using UnityEngine;

namespace CorridorCommander.PlayerItems
{
    [CreateAssetMenu(
        fileName = "Item_New",
        menuName = "Corridor Commander/Items/Item Definition")]
    public sealed class ItemDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string itemId = "item";
        public string displayName = "Item";
        [TextArea] public string description = "Usable item.";

        [Header("Use")]
        public PlayerItemUseType useType;

        [Header("Values")]
        [Min(0f)] public float value = 25f;
        [Min(0f)] public float radius = 0f;

        [Header("Throwable")]
        public GameObject projectilePrefab;
        [Min(0f)] public float throwSpeed = 12f;
        [Min(0f)] public float upwardVelocity = 2.5f;
        public bool useBallisticThrowArc = false;
        [Min(0.01f)] public float ballisticThrowArcHeight = 3f;
        public float ballisticThrowGravity = -9.81f;
        [Min(0f)] public float fuseTime = 2f;
        public LayerMask effectLayers = ~0;
        public GameObject aimIndicatorPrefab;

        [Header("Audio")]
        public AudioClip useAudioClip;
        public AudioClip throwAudioClip;
        public AudioClip[] explosionAudioClips;
        [Range(0f, 1f)] public float audioVolume = 1f;
        public Vector2 audioPitchRange = new Vector2(0.96f, 1.04f);
        [Range(0f, 1f)] public float audioSpatialBlend = 1f;
        [Tooltip("If 0, the spawned audio object lives for the selected clip length.")]
        public float audioLifeTime = 0f;

        [Header("Stack")]
        [Min(1)] public int maxStack = 99;

        [Header("UI")]
        public Sprite icon;
    }

    public static class ItemAudioUtility
    {
        public static void PlayUseAudio(ItemDefinitionSO definition, Vector3 position)
        {
            if (definition == null)
            {
                return;
            }

            PlayClip(definition.useAudioClip, definition, position, "UseAudio");
        }

        public static void PlayThrowAudio(ItemDefinitionSO definition, Vector3 position)
        {
            if (definition == null)
            {
                return;
            }

            PlayClip(definition.throwAudioClip, definition, position, "ThrowAudio");
        }

        public static void PlayExplosionAudio(ItemDefinitionSO definition, Vector3 position)
        {
            if (definition == null || definition.explosionAudioClips == null || definition.explosionAudioClips.Length == 0)
            {
                return;
            }

            AudioClip clip = definition.explosionAudioClips[
                Random.Range(0, definition.explosionAudioClips.Length)];
            PlayClip(clip, definition, position, "ExplosionAudio");
        }

        private static void PlayClip(
            AudioClip clip,
            ItemDefinitionSO definition,
            Vector3 position,
            string objectSuffix)
        {
            if (clip == null || definition == null)
            {
                return;
            }

            GameObject audioObject = new GameObject($"{definition.name}_{objectSuffix}");
            audioObject.transform.position = position;

            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.clip = clip;
            audioSource.volume = definition.audioVolume;
            audioSource.pitch = GetRandomPitch(definition.audioPitchRange);
            audioSource.spatialBlend = definition.audioSpatialBlend;
            audioSource.Play();

            float clipDuration = clip.length / Mathf.Max(0.01f, Mathf.Abs(audioSource.pitch));
            float lifeTime = definition.audioLifeTime > 0f
                ? definition.audioLifeTime
                : clipDuration;
            if (Application.isPlaying)
            {
                Object.Destroy(audioObject, Mathf.Max(0.05f, lifeTime));
            }
            else
            {
                Object.DestroyImmediate(audioObject);
            }
        }

        private static float GetRandomPitch(Vector2 pitchRange)
        {
            float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);
            return Random.Range(minPitch, maxPitch);
        }
    }
}
