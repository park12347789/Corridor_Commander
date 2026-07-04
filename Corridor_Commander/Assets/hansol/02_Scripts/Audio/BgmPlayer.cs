using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class BgmPlayer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SceneBgmTableSO sceneBgmTable;
        [SerializeField] private AudioSource audioSource;

        [Header("Options")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool playCurrentSceneOnStart = true;
        [SerializeField] private bool logBgmEvents = false;

        private static BgmPlayer instance;
        private Coroutine transitionRoutine;
        private BgmDefinitionSO currentBgm;
        private BgmDefinitionSO overrideBgm;

        public static BgmPlayer Instance => instance;
        public BgmDefinitionSO CurrentBgm => currentBgm;

        private void Awake()
        {
            ResolveReferences();

            if (dontDestroyOnLoad)
            {
                if (instance != null && instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                instance = this;
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (playCurrentSceneOnStart)
            {
                PlayForScene(SceneManager.GetActiveScene().name);
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void PlayForScene(string sceneName)
        {
            if (overrideBgm != null)
            {
                PlayBgm(overrideBgm);
                return;
            }

            BgmDefinitionSO nextBgm = sceneBgmTable != null
                ? sceneBgmTable.GetBgmForScene(sceneName)
                : null;
            PlayBgm(nextBgm);
        }

        public void PlayOverride(BgmDefinitionSO bgm)
        {
            overrideBgm = bgm;
            PlayBgm(overrideBgm);
        }

        public void ClearOverrideAndResumeScene()
        {
            overrideBgm = null;
            PlayForScene(SceneManager.GetActiveScene().name);
        }

        public void StopBgm()
        {
            StartTransition(null);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayForScene(scene.name);
        }

        private void PlayBgm(BgmDefinitionSO nextBgm)
        {
            if (currentBgm == nextBgm)
            {
                return;
            }

            StartTransition(nextBgm);
        }

        private void StartTransition(BgmDefinitionSO nextBgm)
        {
            ResolveReferences();

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(TransitionRoutine(nextBgm));
        }

        private IEnumerator TransitionRoutine(BgmDefinitionSO nextBgm)
        {
            float fadeOutTime = currentBgm != null ? currentBgm.FadeOutTime : 0f;
            yield return FadeVolume(audioSource.volume, 0f, fadeOutTime);

            currentBgm = nextBgm;

            if (currentBgm == null || currentBgm.Clip == null)
            {
                audioSource.Stop();
                audioSource.clip = null;
                LogBgm("Stopped");
                transitionRoutine = null;
                yield break;
            }

            audioSource.clip = currentBgm.Clip;
            audioSource.loop = currentBgm.Loop;
            audioSource.volume = 0f;
            audioSource.Play();
            LogBgm("Playing " + currentBgm.DisplayName);

            yield return FadeVolume(0f, currentBgm.Volume, currentBgm.FadeInTime);
            transitionRoutine = null;
        }

        private IEnumerator FadeVolume(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                audioSource.volume = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                audioSource.volume = Mathf.Lerp(from, to, t);
                yield return null;
            }

            audioSource.volume = to;
        }

        private void ResolveReferences()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void LogBgm(string message)
        {
            if (!logBgmEvents)
            {
                return;
            }

            Debug.Log("[BgmPlayer] " + message, this);
        }
    }
}

/*
Unity setup outline:
1. Create a BgmPlayer GameObject in the first loaded scene, usually MainMenu or a bootstrap scene.
2. Add an AudioSource and assign a SceneBgmTableSO.
3. Keep Dont Destroy On Load enabled so one BGM player survives scene changes.
4. Use PlayOverride(gameOverBgm) from a game-over flow, then ClearOverrideAndResumeScene() when returning to normal scene music.
*/
