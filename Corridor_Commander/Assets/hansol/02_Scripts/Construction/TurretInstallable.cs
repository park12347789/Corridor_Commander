using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TurretTargetingController))]
    public sealed class TurretInstallable : MonoBehaviour, IBuildableInstallable
    {
        [SerializeField] private TurretTargetingController targetingController;
        [SerializeField] private TurretServiceController serviceController;
        [SerializeField] private TurretRangeIndicator rangeIndicator;
        [SerializeField] private TurretAttackDefinitionSO attackDefinition;
        [SerializeField] private float range = 7f;
        [SerializeField] private float fireInterval = 0.75f;
        [SerializeField] private float damage = 6f;
        [Header("Install Feedback")]
        [SerializeField] private GameObject installVfxPrefab;
        [SerializeField] private AudioClip installAudioClip;
        [SerializeField, Min(0f)] private float installAudioVolume = 0.85f;
        [SerializeField, Min(0.05f)] private float installFeedbackLifetime = 2f;

        public BuildableKind Kind => BuildableKind.Turret;
        public TurretAttackDefinitionSO AttackDefinition => attackDefinition;

        private void Awake()
        {
            if (targetingController == null)
            {
                targetingController = GetComponent<TurretTargetingController>();
            }

            if (serviceController == null)
            {
                serviceController = GetComponent<TurretServiceController>();
            }

            if (rangeIndicator == null)
            {
                rangeIndicator = GetComponent<TurretRangeIndicator>();
            }
        }

        public void OnInstalled(BuildContext context)
        {
            if (targetingController == null)
            {
                targetingController = GetComponent<TurretTargetingController>();
            }

            if (serviceController == null)
            {
                serviceController = GetComponent<TurretServiceController>();
            }

            if (rangeIndicator == null)
            {
                rangeIndicator = GetComponent<TurretRangeIndicator>();
            }

            ApplyPlacementOrientation(context);
            PlayInstallFeedback(context);

            if (serviceController != null)
            {
                serviceController.Initialize(context, attackDefinition, range, fireInterval, damage);
                return;
            }

            if (attackDefinition != null)
            {
                targetingController.Configure(attackDefinition, 0);
                ShowInstalledRange();
                return;
            }

            targetingController.Configure(range, fireInterval, damage);
            ShowInstalledRange();
        }

        private void PlayInstallFeedback(BuildContext context)
        {
            Vector3 position = context.BuildAnchor != null ? context.BuildAnchor.position : transform.position;

            if (installVfxPrefab == null)
            {
                Debug.LogError("[TurretInstallable] Install VFX prefab is not assigned.", this);
            }
            else
            {
                GameObject vfx = Instantiate(installVfxPrefab, position, Quaternion.identity);
                Destroy(vfx, installFeedbackLifetime);
            }

            if (installAudioClip == null)
            {
                Debug.LogError("[TurretInstallable] Install audio clip is not assigned.", this);
                return;
            }

            GameObject audioObject = new GameObject("TurretInstallSfx");
            audioObject.transform.position = position;
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.volume = installAudioVolume;
            audioSource.clip = installAudioClip;
            audioSource.Play();
            Destroy(audioObject, Mathf.Max(installAudioClip.length, installFeedbackLifetime));
        }

        private void ShowInstalledRange()
        {
            rangeIndicator?.SetRange(targetingController.CurrentRange);
        }

        private void ApplyPlacementOrientation(BuildContext context)
        {
            if (targetingController == null || context.PlacementPoint is not WallPlacementPoint)
            {
                return;
            }

            targetingController.SetAimUpAxis(transform.up);
        }
    }
}
