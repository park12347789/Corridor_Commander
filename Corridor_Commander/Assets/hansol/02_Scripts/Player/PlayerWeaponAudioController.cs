using UnityEngine;
using CorridorCommander.PlayerCombat;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponAudioController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWeaponInventory weaponInventory;
        [SerializeField] private PlayerWeaponRuntime weaponRuntime;
        [SerializeField] private PlayerProjectileLauncher projectileLauncher;
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioSource beamChargeSource;
        [SerializeField] private AudioSource beamLoopSource;
        [SerializeField] private AudioSource beamStopSource;

        [Header("Options")]
        [SerializeField] private bool playEquipSound = false;
        [SerializeField] private bool logAudioEvents = false;

        private WeaponAudioDefinitionSO activeBeamAudio;
        private bool beamLoopActive;

        private void Awake()
        {
            ResolveReferences();
            ConfigureSourcesFromCurrentWeapon();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeEvents();
            ConfigureSourcesFromCurrentWeapon();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            StopBeamLoop(false);
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (weaponInventory != null)
            {
                weaponInventory.CurrentWeaponChanged += HandleCurrentWeaponChanged;
            }

            if (weaponRuntime != null)
            {
                weaponRuntime.ReloadStarted += HandleReloadStarted;
            }

            if (projectileLauncher != null)
            {
                projectileLauncher.Fired += HandleFired;
                projectileLauncher.AutomaticFireStopped += HandleAutomaticFireStopped;
            }
        }

        private void UnsubscribeEvents()
        {
            if (weaponInventory != null)
            {
                weaponInventory.CurrentWeaponChanged -= HandleCurrentWeaponChanged;
            }

            if (weaponRuntime != null)
            {
                weaponRuntime.ReloadStarted -= HandleReloadStarted;
            }

            if (projectileLauncher != null)
            {
                projectileLauncher.Fired -= HandleFired;
                projectileLauncher.AutomaticFireStopped -= HandleAutomaticFireStopped;
            }
        }

        private void HandleCurrentWeaponChanged(WeaponRuntimeState weaponState)
        {
            StopBeamLoop(true);
            ConfigureSourcesFromCurrentWeapon();

            WeaponAudioDefinitionSO audioDefinition = ResolveAudioDefinition(weaponState);
            if (playEquipSound && audioDefinition != null)
            {
                PlayOneShot(audioDefinition.EquipClip, audioDefinition);
            }
        }

        private void HandleReloadStarted(WeaponRuntimeState weaponState)
        {
            WeaponAudioDefinitionSO audioDefinition = ResolveAudioDefinition(weaponState);
            if (audioDefinition == null || weaponState == null || weaponState.WeaponDefinition == null)
            {
                return;
            }

            AudioClip reloadClip = audioDefinition.GetReloadClip(weaponState.WeaponDefinition.AnimationType);
            PlayOneShot(reloadClip, audioDefinition);
            LogAudioEvent("Reload", reloadClip);
        }

        private void HandleFired()
        {
            WeaponAudioDefinitionSO audioDefinition = ResolveCurrentAudioDefinition();
            if (audioDefinition == null)
            {
                return;
            }

            ConfigureSources(audioDefinition);

            if (audioDefinition.PlaybackMode == WeaponAudioPlaybackMode.ContinuousBeam)
            {
                StartBeamLoop(audioDefinition);
                return;
            }

            AudioClip fireClip = audioDefinition.GetRandomFireClip();
            PlayOneShot(fireClip, audioDefinition);
            LogAudioEvent("Fire", fireClip);
        }

        private void HandleAutomaticFireStopped()
        {
            StopBeamLoop(true);
        }

        private void StartBeamLoop(WeaponAudioDefinitionSO audioDefinition)
        {
            if (beamLoopActive && activeBeamAudio == audioDefinition)
            {
                return;
            }

            StopBeamLoop(false);
            activeBeamAudio = audioDefinition;
            beamLoopActive = true;

            PlayBeamCharge(audioDefinition);

            if (beamLoopSource == null || audioDefinition.BeamLoopClip == null)
            {
                return;
            }

            beamLoopSource.clip = audioDefinition.BeamLoopClip;
            beamLoopSource.loop = true;
            beamLoopSource.volume = audioDefinition.Volume * audioDefinition.BeamLoopVolumeMultiplier;
            beamLoopSource.pitch = audioDefinition.GetRandomPitch();
            beamLoopSource.spatialBlend = audioDefinition.SpatialBlend;
            float loopDelay = ResolveBeamLoopStartDelay(audioDefinition);
            if (loopDelay > 0f)
            {
                beamLoopSource.PlayDelayed(loopDelay);
            }
            else
            {
                beamLoopSource.Play();
            }

            LogAudioEvent("Beam Loop Start", audioDefinition.BeamLoopClip);
        }

        private void PlayBeamCharge(WeaponAudioDefinitionSO audioDefinition)
        {
            if (beamChargeSource == null || audioDefinition.BeamChargeClip == null)
            {
                PlayOneShot(audioDefinition.BeamChargeClip, audioDefinition);
                return;
            }

            ConfigureSource(beamChargeSource, audioDefinition, false);
            beamChargeSource.clip = audioDefinition.BeamChargeClip;
            beamChargeSource.volume = audioDefinition.Volume * audioDefinition.BeamChargeVolumeMultiplier;
            beamChargeSource.Play();
            LogAudioEvent("Beam Charge", audioDefinition.BeamChargeClip);
        }

        private void StopBeamLoop(bool playStopSound)
        {
            if (!beamLoopActive && activeBeamAudio == null)
            {
                return;
            }

            WeaponAudioDefinitionSO previousAudio = activeBeamAudio;
            beamLoopActive = false;
            activeBeamAudio = null;

            if (beamChargeSource != null && beamChargeSource.isPlaying)
            {
                beamChargeSource.Stop();
                beamChargeSource.clip = null;
            }

            if (beamLoopSource != null)
            {
                beamLoopSource.Stop();
                beamLoopSource.clip = null;
            }

            if (playStopSound && previousAudio != null)
            {
                PlayBeamStop(previousAudio);
                LogAudioEvent("Beam Stop", previousAudio.BeamStopClip);
            }
        }

        private void PlayBeamStop(WeaponAudioDefinitionSO audioDefinition)
        {
            if (beamStopSource == null || audioDefinition.BeamStopClip == null)
            {
                PlayOneShot(audioDefinition.BeamStopClip, audioDefinition);
                return;
            }

            ConfigureSource(beamStopSource, audioDefinition, false);
            beamStopSource.clip = audioDefinition.BeamStopClip;
            beamStopSource.volume = audioDefinition.Volume * audioDefinition.BeamStopVolumeMultiplier;
            beamStopSource.PlayDelayed(audioDefinition.BeamStopDelay);
        }

        private static float ResolveBeamLoopStartDelay(WeaponAudioDefinitionSO audioDefinition)
        {
            if (audioDefinition == null)
            {
                return 0f;
            }

            float delay = audioDefinition.BeamLoopStartExtraDelay;
            if (audioDefinition.WaitForBeamChargeBeforeLoop && audioDefinition.BeamChargeClip != null)
            {
                delay += audioDefinition.BeamChargeClip.length;
            }

            return Mathf.Max(0f, delay);
        }

        private void PlayOneShot(AudioClip clip, WeaponAudioDefinitionSO audioDefinition)
        {
            if (clip == null || oneShotSource == null || audioDefinition == null)
            {
                return;
            }

            oneShotSource.pitch = audioDefinition.GetRandomPitch();
            oneShotSource.volume = audioDefinition.Volume;
            oneShotSource.spatialBlend = audioDefinition.SpatialBlend;
            oneShotSource.PlayOneShot(clip, audioDefinition.Volume);
        }

        private WeaponAudioDefinitionSO ResolveCurrentAudioDefinition()
        {
            WeaponRuntimeState weaponState = weaponInventory != null
                ? weaponInventory.CurrentWeaponState
                : weaponRuntime != null
                    ? weaponRuntime.CurrentWeaponState
                    : null;

            return ResolveAudioDefinition(weaponState);
        }

        private static WeaponAudioDefinitionSO ResolveAudioDefinition(WeaponRuntimeState weaponState)
        {
            return weaponState != null && weaponState.WeaponDefinition != null
                ? weaponState.WeaponDefinition.audioDefinition
                : null;
        }

        private void ConfigureSourcesFromCurrentWeapon()
        {
            WeaponAudioDefinitionSO audioDefinition = ResolveCurrentAudioDefinition();
            if (audioDefinition != null)
            {
                ConfigureSources(audioDefinition);
            }
        }

        private void ConfigureSources(WeaponAudioDefinitionSO audioDefinition)
        {
            if (audioDefinition == null)
            {
                return;
            }

            if (oneShotSource != null)
            {
                ConfigureSource(oneShotSource, audioDefinition, false);
            }

            if (beamChargeSource != null)
            {
                ConfigureSource(beamChargeSource, audioDefinition, false);
            }

            if (beamLoopSource != null)
            {
                ConfigureSource(beamLoopSource, audioDefinition, true);
            }

            if (beamStopSource != null)
            {
                ConfigureSource(beamStopSource, audioDefinition, false);
            }
        }

        private static void ConfigureSource(
            AudioSource source,
            WeaponAudioDefinitionSO audioDefinition,
            bool loop)
        {
            if (source == null || audioDefinition == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.volume = audioDefinition.Volume;
            source.pitch = audioDefinition.GetRandomPitch();
            source.spatialBlend = audioDefinition.SpatialBlend;
        }

        private void ResolveReferences()
        {
            if (weaponInventory == null)
            {
                weaponInventory = GetComponentInParent<PlayerWeaponInventory>();
            }

            if (weaponInventory == null)
            {
                weaponInventory = GetComponentInChildren<PlayerWeaponInventory>(true);
            }

            if (weaponRuntime == null)
            {
                weaponRuntime = GetComponentInParent<PlayerWeaponRuntime>();
            }

            if (weaponRuntime == null)
            {
                weaponRuntime = GetComponentInChildren<PlayerWeaponRuntime>(true);
            }

            if (projectileLauncher == null)
            {
                projectileLauncher = GetComponentInParent<PlayerProjectileLauncher>();
            }

            if (projectileLauncher == null)
            {
                projectileLauncher = GetComponentInChildren<PlayerProjectileLauncher>(true);
            }

            ValidateAudioSources();
        }

        private void ValidateAudioSources()
        {
            if (oneShotSource == null)
            {
                oneShotSource = GetComponent<AudioSource>();
            }

            if (oneShotSource == null)
            {
                Debug.LogError("[PlayerWeaponAudioController] One Shot AudioSource is not connected.", this);
            }

            if (beamChargeSource == null)
            {
                Debug.LogError("[PlayerWeaponAudioController] Beam Charge AudioSource is not connected.", this);
            }

            if (beamLoopSource == null)
            {
                Debug.LogError("[PlayerWeaponAudioController] Beam Loop AudioSource is not connected.", this);
            }

            if (beamStopSource == null)
            {
                Debug.LogError("[PlayerWeaponAudioController] Beam Stop AudioSource is not connected.", this);
            }
        }

        private void LogAudioEvent(string eventName, AudioClip clip)
        {
            if (!logAudioEvents)
            {
                return;
            }

            Debug.Log(
                $"[PlayerWeaponAudioController] {eventName}: {(clip != null ? clip.name : "None")}",
                this);
        }
    }
}

/*
Unity setup outline:
1. Add PlayerWeaponAudioController to PlayerSystems/CombatRuntime or a child audio object.
2. Assign PlayerWeaponInventory, PlayerWeaponRuntime, and PlayerProjectileLauncher, or leave them empty for auto-binding.
3. Add one AudioSource for normal one-shot sounds and optional separate AudioSources for beam charge, beam loop, and beam stop.
4. Assign WeaponAudioDefinitionSO assets to each WeaponItemDefinitionSO.audioDefinition.
5. Use Playback Mode ContinuousBeam for laser cannon style weapons.
*/
