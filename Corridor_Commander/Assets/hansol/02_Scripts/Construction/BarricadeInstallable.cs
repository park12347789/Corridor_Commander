using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(MapObstacle))]
    public sealed class BarricadeInstallable : MonoBehaviour, IBuildableInstallable
    {
        [SerializeField] private Health health;
        [SerializeField] private MapObstacle obstacle;
        [SerializeField] private float maxHitPoints = 40f;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private MapObstacleKind obstacleKind = MapObstacleKind.Breakable;
        [Header("Install Feedback")]
        [SerializeField] private GameObject installVfxPrefab;
        [SerializeField] private AudioClip installAudioClip;
        [SerializeField, Range(0f, 1f)] private float installAudioVolume = 0.85f;
        [SerializeField, Min(0.05f)] private float installFeedbackLifetime = 1.45f;

        private bool missingInstallVfxLogged;
        private bool missingInstallAudioLogged;

        public BuildableKind Kind => BuildableKind.Barricade;

        private void Awake()
        {
            CacheReferences();
        }

        public void OnInstalled(BuildContext context)
        {
            CacheReferences();

            if (health != null)
            {
                health.Configure(maxHitPoints, destroyOnDeath);
            }

            if (obstacle != null)
            {
                obstacle.Configure(obstacleKind);
            }

            PlayInstallFeedback(context);
        }

        private void CacheReferences()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (obstacle == null)
            {
                obstacle = GetComponent<MapObstacle>();
            }
        }

        private void PlayInstallFeedback(BuildContext context)
        {
            Vector3 position = context.BuildAnchor != null ? context.BuildAnchor.position : transform.position;
            if (installVfxPrefab == null)
            {
                LogMissingOnce("Install VFX prefab", ref missingInstallVfxLogged);
            }
            else
            {
                RuntimeFeedbackUtility.SpawnVfx(installVfxPrefab, position, installFeedbackLifetime);
            }

            if (installAudioClip == null)
            {
                LogMissingOnce("Install audio clip", ref missingInstallAudioLogged);
                return;
            }

            RuntimeFeedbackUtility.PlayClip(installAudioClip, position, installAudioVolume, "BarricadeInstallSfx");
        }

        private void LogMissingOnce(string targetName, ref bool logged)
        {
            if (logged)
            {
                return;
            }

            logged = true;
            Debug.LogError("[BarricadeInstallable] " + targetName + " is not assigned.", this);
        }
    }
}
