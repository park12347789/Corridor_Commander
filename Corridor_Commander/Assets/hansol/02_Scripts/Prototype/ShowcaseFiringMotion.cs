using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class ShowcaseFiringMotion : MonoBehaviour
    {
        [SerializeField] private Transform recoilTarget;
        [SerializeField] private Transform spinTarget;
        [SerializeField] private ParticleSystem fireEffect;
        [SerializeField] private Light pulseLight;
        [SerializeField] private Vector3 recoilOffset = new Vector3(0f, 0f, -0.2f);
        [SerializeField] private Vector3 recoilEulerOffset;
        [SerializeField] private Vector3 spinAxis = Vector3.forward;
        [SerializeField] private float spinSpeed = 360f;
        [SerializeField] private float cycleDuration = 1.2f;
        [SerializeField] private float recoilDuration = 0.18f;
        [SerializeField] private float pulseIntensity = 2.5f;
        [SerializeField] private float phaseOffset;
        [SerializeField] private bool previewInEditMode;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation = Quaternion.identity;
        private float baseLightIntensity;
        private float previousTime;
        private int previousCycle = -1;
        private bool initialized;

        private void OnEnable()
        {
            CaptureBasePose();
            previousTime = ResolveTime();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            cycleDuration = Mathf.Max(0.05f, cycleDuration);
            recoilDuration = Mathf.Clamp(recoilDuration, 0.01f, cycleDuration);
            spinSpeed = Mathf.Max(0f, spinSpeed);
            pulseIntensity = Mathf.Max(0f, pulseIntensity);

            if (!Application.isPlaying)
            {
                CaptureBasePose();
            }
        }
#endif

        private void Update()
        {
            if (!ShouldTick())
            {
                return;
            }

            if (!initialized)
            {
                CaptureBasePose();
            }

            float time = ResolveTime();
            float deltaTime = Mathf.Max(0f, time - previousTime);
            previousTime = time;

            RotateSpinTarget(deltaTime);
            ApplyRecoilAndPulse(time);
        }

        public void Configure(
            Transform configuredRecoilTarget,
            Transform configuredSpinTarget,
            ParticleSystem configuredFireEffect,
            Light configuredPulseLight,
            Vector3 configuredRecoilOffset,
            Vector3 configuredRecoilEulerOffset,
            Vector3 configuredSpinAxis,
            float configuredSpinSpeed,
            float configuredCycleDuration,
            float configuredRecoilDuration,
            float configuredPulseIntensity,
            float configuredPhaseOffset,
            bool configuredPreviewInEditMode)
        {
            recoilTarget = configuredRecoilTarget;
            spinTarget = configuredSpinTarget;
            fireEffect = configuredFireEffect;
            pulseLight = configuredPulseLight;
            recoilOffset = configuredRecoilOffset;
            recoilEulerOffset = configuredRecoilEulerOffset;
            spinAxis = configuredSpinAxis;
            spinSpeed = Mathf.Max(0f, configuredSpinSpeed);
            cycleDuration = Mathf.Max(0.05f, configuredCycleDuration);
            recoilDuration = Mathf.Clamp(configuredRecoilDuration, 0.01f, cycleDuration);
            pulseIntensity = Mathf.Max(0f, configuredPulseIntensity);
            phaseOffset = configuredPhaseOffset;
            previewInEditMode = configuredPreviewInEditMode;
            CaptureBasePose();
        }

        private void CaptureBasePose()
        {
            if (recoilTarget != null)
            {
                baseLocalPosition = recoilTarget.localPosition;
                baseLocalRotation = recoilTarget.localRotation;
            }

            if (pulseLight != null)
            {
                baseLightIntensity = pulseLight.intensity;
            }

            initialized = true;
        }

        private bool ShouldTick()
        {
            if (Application.isPlaying)
            {
                return true;
            }

            return previewInEditMode
                && gameObject.scene.IsValid()
                && !string.IsNullOrEmpty(gameObject.scene.path);
        }

        private float ResolveTime()
        {
            return Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        }

        private void RotateSpinTarget(float deltaTime)
        {
            if (spinTarget == null || spinSpeed <= 0f)
            {
                return;
            }

            Vector3 axis = spinAxis.sqrMagnitude > 0.0001f ? spinAxis.normalized : Vector3.forward;
            spinTarget.Rotate(axis, spinSpeed * deltaTime, Space.Self);
        }

        private void ApplyRecoilAndPulse(float time)
        {
            float cycleTime = time + phaseOffset;
            int cycle = Mathf.FloorToInt(cycleTime / cycleDuration);
            float phase = Mathf.Repeat(cycleTime, cycleDuration);

            if (cycle != previousCycle)
            {
                previousCycle = cycle;
                PlayFireEffect();
            }

            float impulse = phase <= recoilDuration
                ? 1f - Mathf.SmoothStep(0f, 1f, phase / recoilDuration)
                : 0f;

            if (recoilTarget != null)
            {
                recoilTarget.localPosition = baseLocalPosition + recoilOffset * impulse;
                recoilTarget.localRotation = baseLocalRotation * Quaternion.Euler(recoilEulerOffset * impulse);
            }

            if (pulseLight != null)
            {
                pulseLight.intensity = baseLightIntensity + pulseIntensity * impulse;
                pulseLight.enabled = impulse > 0.01f;
            }
        }

        private void PlayFireEffect()
        {
            if (fireEffect == null)
            {
                return;
            }

            fireEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fireEffect.Play(true);
        }
    }
}
