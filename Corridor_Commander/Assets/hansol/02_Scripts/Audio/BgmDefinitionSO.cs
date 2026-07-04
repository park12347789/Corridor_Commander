using UnityEngine;

namespace CorridorCommander.Audio
{
    [CreateAssetMenu(
        fileName = "BGM_New",
        menuName = "Corridor Commander/Audio/BGM Definition")]
    public sealed class BgmDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "BGM";

        [Header("Clip")]
        [SerializeField] private AudioClip clip;
        [SerializeField] private bool loop = true;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.75f;
        [SerializeField, Min(0f)] private float fadeInTime = 1f;
        [SerializeField, Min(0f)] private float fadeOutTime = 1f;

        public string DisplayName => displayName;
        public AudioClip Clip => clip;
        public bool Loop => loop;
        public float Volume => volume;
        public float FadeInTime => fadeInTime;
        public float FadeOutTime => fadeOutTime;
    }
}

/*
Unity setup outline:
1. Create assets from Create > Corridor Commander > Audio > BGM Definition.
2. Store BGM assets under Assets/junhee/10_ScriptableObjects/sound or a BGM subfolder.
3. Assign one AudioClip per BGM asset and tune Volume/Fade values in the Inspector.
*/
