using CorridorCommander.PlayerCombat;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class AlliedWeaponAudioController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AlliedSquadMemberCombat combat;
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioSource beamChargeSource;
        [SerializeField] private AudioSource beamLoopSource;
        [SerializeField] private AudioSource beamStopSource;

        [Header("3D Audio")]
        [SerializeField, Range(0f, 1f)] private float volumeMultiplier = 0.75f;
        [SerializeField, Min(0.01f)] private float minDistance = 2f;
        [SerializeField, Min(0.01f)] private float maxDistance = 25f;

        [Header("Debug")]
        [SerializeField] private bool logAudioEvents;

        private WeaponAudioDefinitionSO currentAudioDefinition;
        private bool beamAudioActive;

        public float VolumeMultiplier
        {
            get => volumeMultiplier;
            private set => volumeMultiplier = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            ResolveReferences();
            RefreshAudioDefinition();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeEvents();
            RefreshAudioDefinition();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            StopBeamAudio(false);
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (combat == null)
            {
                return;
            }

            combat.Fired += HandleFired;
            combat.ReloadStarted += HandleReloadStarted;
            combat.WeaponChanged += HandleWeaponChanged;
            combat.ContinuousFireStarted += HandleContinuousFireStarted;
            combat.ContinuousFireStopped += HandleContinuousFireStopped;
        }

        private void UnsubscribeEvents()
        {
            if (combat == null)
            {
                return;
            }

            combat.Fired -= HandleFired;
            combat.ReloadStarted -= HandleReloadStarted;
            combat.WeaponChanged -= HandleWeaponChanged;
            combat.ContinuousFireStarted -= HandleContinuousFireStarted;
            combat.ContinuousFireStopped -= HandleContinuousFireStopped;
        }

        private void HandleFired()
        {
            if (currentAudioDefinition == null
                || currentAudioDefinition.PlaybackMode == WeaponAudioPlaybackMode.ContinuousBeam)
            {
                return;
            }

            AudioClip clip = currentAudioDefinition.GetRandomFireClip();
            PlayOneShot(clip);
            LogAudioEvent("Fire", clip);
        }

        private void HandleReloadStarted()
        {
            WeaponItemDefinitionSO weapon = combat != null ? combat.WeaponDefinition : null;
            if (weapon == null || currentAudioDefinition == null)
            {
                return;
            }

            AudioClip clip = currentAudioDefinition.GetReloadClip(weapon.AnimationType);
            PlayOneShot(clip);
            LogAudioEvent("Reload", clip);
        }

        private void HandleWeaponChanged(WeaponItemDefinitionSO weapon)
        {
            StopBeamAudio(true);
            currentAudioDefinition = weapon != null ? weapon.audioDefinition : null;
            ConfigureSources();
        }

        private void HandleContinuousFireStarted()
        {
            if (currentAudioDefinition == null
                || currentAudioDefinition.PlaybackMode != WeaponAudioPlaybackMode.ContinuousBeam
                || beamAudioActive)
            {
                return;
            }

            beamAudioActive = true;
            PlayBeamCharge();
            PlayBeamLoop();
        }

        private void HandleContinuousFireStopped()
        {
            StopBeamAudio(true);
        }

        private void PlayBeamCharge()
        {
            AudioClip clip = currentAudioDefinition.BeamChargeClip;
            if (clip == null)
            {
                return;
            }

            beamChargeSource.clip = clip;
            beamChargeSource.pitch = currentAudioDefinition.GetRandomPitch();
            beamChargeSource.volume = ResolveVolume(currentAudioDefinition.BeamChargeVolumeMultiplier);
            beamChargeSource.Play();
            LogAudioEvent("Beam Charge", clip);
        }

        private void PlayBeamLoop()
        {
            AudioClip clip = currentAudioDefinition.BeamLoopClip;
            if (clip == null)
            {
                return;
            }

            beamLoopSource.clip = clip;
            beamLoopSource.pitch = currentAudioDefinition.GetRandomPitch();
            beamLoopSource.volume = ResolveVolume(currentAudioDefinition.BeamLoopVolumeMultiplier);
            beamLoopSource.loop = true;

            float delay = currentAudioDefinition.BeamLoopStartExtraDelay;
            if (currentAudioDefinition.WaitForBeamChargeBeforeLoop
                && currentAudioDefinition.BeamChargeClip != null)
            {
                delay += currentAudioDefinition.BeamChargeClip.length;
            }

            if (delay > 0f)
            {
                beamLoopSource.PlayDelayed(delay);
            }
            else
            {
                beamLoopSource.Play();
            }

            LogAudioEvent("Beam Loop Start", clip);
        }

        private void StopBeamAudio(bool playStopSound)
        {
            if (!beamAudioActive)
            {
                return;
            }

            beamAudioActive = false;
            beamChargeSource.Stop();
            beamChargeSource.clip = null;
            beamLoopSource.Stop();
            beamLoopSource.clip = null;

            AudioClip stopClip = currentAudioDefinition != null
                ? currentAudioDefinition.BeamStopClip
                : null;

            if (!playStopSound || stopClip == null)
            {
                return;
            }

            beamStopSource.clip = stopClip;
            beamStopSource.pitch = currentAudioDefinition.GetRandomPitch();
            beamStopSource.volume = ResolveVolume(currentAudioDefinition.BeamStopVolumeMultiplier);
            beamStopSource.PlayDelayed(currentAudioDefinition.BeamStopDelay);
            LogAudioEvent("Beam Stop", stopClip);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || currentAudioDefinition == null)
            {
                return;
            }

            oneShotSource.pitch = currentAudioDefinition.GetRandomPitch();
            oneShotSource.PlayOneShot(clip, ResolveVolume(1f));
        }

        private float ResolveVolume(float eventMultiplier)
        {
            return currentAudioDefinition != null
                ? currentAudioDefinition.Volume * volumeMultiplier * eventMultiplier
                : 0f;
        }

        private void RefreshAudioDefinition()
        {
            currentAudioDefinition = combat != null && combat.WeaponDefinition != null
                ? combat.WeaponDefinition.audioDefinition
                : null;
            ConfigureSources();
        }

        private void ResolveReferences()
        {
            if (combat == null)
            {
                combat = GetComponent<AlliedSquadMemberCombat>();
            }

            if (combat == null)
            {
                combat = GetComponentInParent<AlliedSquadMemberCombat>();
            }

            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            oneShotSource = EnsureDedicatedSource(oneShotSource, false);
            beamChargeSource = EnsureDedicatedSource(beamChargeSource, false);
            beamLoopSource = EnsureDedicatedSource(beamLoopSource, true);
            beamStopSource = EnsureDedicatedSource(beamStopSource, false);
        }

        private AudioSource EnsureDedicatedSource(AudioSource source, bool loop)
        {
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = Mathf.Max(0.01f, minDistance);
            source.maxDistance = Mathf.Max(source.minDistance, maxDistance);
            return source;
        }

        private void ConfigureSources()
        {
            ConfigureSource(oneShotSource, false);
            ConfigureSource(beamChargeSource, false);
            ConfigureSource(beamLoopSource, true);
            ConfigureSource(beamStopSource, false);
        }

        private void ConfigureSource(AudioSource source, bool loop)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = Mathf.Max(0.01f, minDistance);
            source.maxDistance = Mathf.Max(source.minDistance, maxDistance);
        }

        private void LogAudioEvent(string eventName, AudioClip clip)
        {
            if (logAudioEvents)
            {
                Debug.Log($"[AlliedWeaponAudioController] {eventName}: {(clip != null ? clip.name : "None")}", this);
            }
        }
    }
}

/*
Unity setup outline:
1. Add AlliedWeaponAudioController to the same GameObject as AlliedSquadMemberCombat.
2. Leave AudioSource fields empty to create four dedicated 3D sources automatically at runtime.
3. Keep WeaponAudioDefinitionSO assigned through WeaponItemDefinitionSO.audioDefinition.
4. Adjust Volume Multiplier per squad member prefab when quieter allied weapon audio is needed.
5. Enable Log Audio Events temporarily to verify fire, reload, and continuous beam transitions.
*/
