using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BuildableObject))]
    public sealed class BuildableInstallable : MonoBehaviour, IBuildableInstallable
    {
        [SerializeField] private BuildableObject buildableObject;
        [SerializeField] private BuildableKind fallbackKind;
        [Header("Install Feedback")]
        [SerializeField] private GameObject installVfxPrefab;
        [SerializeField] private AudioClip installAudioClip;
        [SerializeField, Range(0f, 1f)] private float installAudioVolume = 0.85f;
        [SerializeField, Min(0.05f)] private float installFeedbackLifetime = 2f;

        public BuildableKind Kind => buildableObject != null && buildableObject.Definition != null
            ? buildableObject.Definition.Kind
            : fallbackKind;

        private void Awake()
        {
            CacheReferences();
        }

        public void OnInstalled(BuildContext context)
        {
            CacheReferences();
            buildableObject?.Initialize(context);
            PlayInstallFeedback(context);
        }

        private void CacheReferences()
        {
            if (buildableObject == null)
            {
                buildableObject = GetComponent<BuildableObject>();
            }
        }

        private void PlayInstallFeedback(BuildContext context)
        {
            Vector3 position = context.BuildAnchor != null ? context.BuildAnchor.position : transform.position;
            RuntimeFeedbackUtility.SpawnVfx(installVfxPrefab, position, installFeedbackLifetime);
            RuntimeFeedbackUtility.PlayClip(installAudioClip, position, installAudioVolume, "BuildableInstallSfx");
        }
    }
}
