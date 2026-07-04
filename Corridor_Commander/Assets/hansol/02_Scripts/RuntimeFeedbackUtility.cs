using UnityEngine;

namespace CorridorCommander
{
    internal static class RuntimeFeedbackUtility
    {
        public static void SpawnVfx(GameObject prefab, Vector3 position, float fallbackLifetime, float localScale = 1f)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity);
            instance.transform.localScale *= Mathf.Max(0.01f, localScale);
            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            float maxLifetime = Mathf.Max(0.05f, fallbackLifetime);

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle == null)
                {
                    continue;
                }

                particle.Play(true);
                ParticleSystem.MainModule main = particle.main;
                maxLifetime = Mathf.Max(maxLifetime, main.duration + main.startLifetime.constantMax);
            }

            DestroyRuntimeObject(instance, maxLifetime + 0.25f);
        }

        public static void PlayRandomClip(AudioClip[] clips, Vector3 position, float volume, string objectName)
        {
            if (!TryGetRandomClip(clips, out AudioClip clip))
            {
                return;
            }

            PlayClip(clip, position, volume, objectName);
        }

        public static void PlayClip(AudioClip clip, Vector3 position, float volume, string objectName)
        {
            if (clip == null)
            {
                return;
            }

            GameObject audioObject = new GameObject(string.IsNullOrWhiteSpace(objectName) ? "RuntimeFeedbackSfx" : objectName);
            audioObject.transform.position = position;
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.volume = Mathf.Clamp01(volume) * GameplayOptionsController.CurrentSfxVolume;
            audioSource.clip = clip;
            audioSource.Play();
            DestroyRuntimeObject(audioObject, Mathf.Max(clip.length, 0.05f));
        }

        private static bool TryGetRandomClip(AudioClip[] clips, out AudioClip clip)
        {
            clip = null;
            if (clips == null || clips.Length == 0)
            {
                return false;
            }

            int startIndex = Random.Range(0, clips.Length);
            for (int offset = 0; offset < clips.Length; offset++)
            {
                AudioClip candidate = clips[(startIndex + offset) % clips.Length];
                if (candidate != null)
                {
                    clip = candidate;
                    return true;
                }
            }

            return false;
        }

        private static void DestroyRuntimeObject(Object target, float delay)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target, delay);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
