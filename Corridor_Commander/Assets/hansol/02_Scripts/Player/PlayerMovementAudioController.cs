using UnityEngine;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovementAudioController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private AudioSource audioSource;

        [Header("Clips")]
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip landClip;

        [Header("Footsteps")]
        [SerializeField] private float minimumHorizontalSpeed = 0.18f;
        [SerializeField] private float walkStepInterval = 0.46f;
        [SerializeField] private float runStepInterval = 0.31f;
        [SerializeField] private float runSpeedThreshold = 3.6f;

        [Header("Playback")]
        [SerializeField] private float volume = 0.72f;
        [SerializeField] private float minPitch = 0.94f;
        [SerializeField] private float maxPitch = 1.06f;

        private float stepTimer;
        private bool wasGrounded;
        private bool setupValid;
        private bool setupErrorLogged;

        private void Awake()
        {
            ValidateSetup();
            wasGrounded = characterController != null && characterController.isGrounded;
        }

        private void OnEnable()
        {
            ValidateSetup();
            stepTimer = 0f;
            wasGrounded = characterController != null && characterController.isGrounded;
        }

        private void Update()
        {
            if (!setupValid)
            {
                return;
            }

            bool isGrounded = characterController.isGrounded;
            float horizontalSpeed = GetHorizontalSpeed(characterController.velocity);

            PlayAirTransitionSounds(isGrounded);
            TickFootsteps(isGrounded, horizontalSpeed);

            wasGrounded = isGrounded;
        }

        private void TickFootsteps(bool isGrounded, float horizontalSpeed)
        {
            if (!isGrounded || horizontalSpeed < minimumHorizontalSpeed)
            {
                stepTimer = 0f;
                return;
            }

            float interval = horizontalSpeed >= runSpeedThreshold ? runStepInterval : walkStepInterval;
            stepTimer -= Time.deltaTime;

            if (stepTimer > 0f)
            {
                return;
            }

            PlayRandomFootstep();
            stepTimer = Mathf.Max(0.05f, interval);
        }

        private void PlayAirTransitionSounds(bool isGrounded)
        {
            if (wasGrounded && !isGrounded && characterController.velocity.y > 0.05f)
            {
                PlayOneShot(jumpClip);
                return;
            }

            if (!wasGrounded && isGrounded)
            {
                PlayOneShot(landClip);
            }
        }

        private void PlayRandomFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0)
            {
                return;
            }

            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            PlayOneShot(clip);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip, volume * CorridorCommander.GameplayOptionsController.CurrentSfxVolume);
        }

        private void ValidateSetup()
        {
            setupValid = characterController != null
                && audioSource != null
                && footstepClips != null
                && footstepClips.Length > 0
                && jumpClip != null
                && landClip != null;

            if (setupValid)
            {
                return;
            }

            if (setupErrorLogged)
            {
                return;
            }

            setupErrorLogged = true;
            Debug.LogError("[PlayerMovementAudioController] Required movement audio references are not connected.", this);
        }

        private static float GetHorizontalSpeed(Vector3 velocity)
        {
            velocity.y = 0f;
            return velocity.magnitude;
        }

        private void OnValidate()
        {
            minimumHorizontalSpeed = Mathf.Max(0f, minimumHorizontalSpeed);
            walkStepInterval = Mathf.Max(0.05f, walkStepInterval);
            runStepInterval = Mathf.Max(0.05f, runStepInterval);
            runSpeedThreshold = Mathf.Max(0f, runSpeedThreshold);
            volume = Mathf.Clamp01(volume);
            minPitch = Mathf.Max(0.01f, minPitch);
            maxPitch = Mathf.Max(minPitch, maxPitch);
        }
    }
}
