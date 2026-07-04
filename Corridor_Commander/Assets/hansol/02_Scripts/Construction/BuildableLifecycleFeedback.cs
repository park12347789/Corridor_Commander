using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class BuildableLifecycleFeedback : MonoBehaviour
    {
        [Header("Upgrade Feedback")]
        [SerializeField] private GameObject upgradeVfxPrefab;
        [SerializeField] private AudioClip upgradeAudioClip;
        [SerializeField, Range(0f, 1f)] private float upgradeAudioVolume = 0.85f;
        [SerializeField, Min(0.05f)] private float upgradeFeedbackLifetime = 1.5f;

        [Header("Repair Feedback")]
        [SerializeField] private GameObject repairVfxPrefab;
        [SerializeField] private AudioClip repairAudioClip;
        [SerializeField, Range(0f, 1f)] private float repairAudioVolume = 0.8f;
        [SerializeField, Min(0.05f)] private float repairFeedbackLifetime = 1.25f;

        [Header("Dismantle Feedback")]
        [SerializeField] private GameObject dismantleVfxPrefab;
        [SerializeField] private AudioClip dismantleAudioClip;
        [SerializeField, Range(0f, 1f)] private float dismantleAudioVolume = 0.9f;
        [SerializeField, Min(0.05f)] private float dismantleFeedbackLifetime = 1.75f;

        private bool missingUpgradeVfxLogged;
        private bool missingUpgradeAudioLogged;
        private bool missingRepairVfxLogged;
        private bool missingRepairAudioLogged;
        private bool missingDismantleVfxLogged;
        private bool missingDismantleAudioLogged;

        public void PlayUpgrade()
        {
            PlayFeedback(
                upgradeVfxPrefab,
                upgradeAudioClip,
                upgradeAudioVolume,
                upgradeFeedbackLifetime,
                "upgrade",
                "BuildableUpgradeSfx",
                ref missingUpgradeVfxLogged,
                ref missingUpgradeAudioLogged);
        }

        public void PlayRepair()
        {
            PlayFeedback(
                repairVfxPrefab,
                repairAudioClip,
                repairAudioVolume,
                repairFeedbackLifetime,
                "repair",
                "BuildableRepairSfx",
                ref missingRepairVfxLogged,
                ref missingRepairAudioLogged);
        }

        public void PlayDismantle()
        {
            PlayFeedback(
                dismantleVfxPrefab,
                dismantleAudioClip,
                dismantleAudioVolume,
                dismantleFeedbackLifetime,
                "dismantle",
                "BuildableDismantleSfx",
                ref missingDismantleVfxLogged,
                ref missingDismantleAudioLogged);
        }

        private void PlayFeedback(
            GameObject vfxPrefab,
            AudioClip audioClip,
            float audioVolume,
            float feedbackLifetime,
            string actionName,
            string audioObjectName,
            ref bool missingVfxLogged,
            ref bool missingAudioLogged)
        {
            Vector3 position = transform.position;
            if (vfxPrefab == null)
            {
                LogMissingOnce(actionName, "VFX prefab", ref missingVfxLogged);
            }
            else
            {
                RuntimeFeedbackUtility.SpawnVfx(vfxPrefab, position, feedbackLifetime);
            }

            if (audioClip == null)
            {
                LogMissingOnce(actionName, "audio clip", ref missingAudioLogged);
                return;
            }

            RuntimeFeedbackUtility.PlayClip(audioClip, position, audioVolume, audioObjectName);
        }

        private void LogMissingOnce(string actionName, string targetName, ref bool logged)
        {
            if (logged)
            {
                return;
            }

            logged = true;
            Debug.LogError($"[BuildableLifecycleFeedback] {actionName} {targetName} is not assigned.", this);
        }
    }
}
