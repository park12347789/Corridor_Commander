using System.Reflection;
using UnityEngine;

namespace CorridorCommander
{
    public enum DisplayCursorMode
    {
        FullscreenLocked = 0,
        WindowConfined = 1,
        WindowFree = 2
    }

    [DisallowMultipleComponent]
    public sealed class GameplayOptionsController : MonoBehaviour
    {
        private const string MasterVolumeKey = "CC.Options.MasterVolume";
        private const string BgmVolumeKey = "CC.Options.BgmVolume";
        private const string SfxVolumeKey = "CC.Options.SfxVolume";
        private const string MouseSensitivityKey = "CC.Options.MouseSensitivity";
        private const string FullscreenKey = "CC.Options.Fullscreen";
        private const string DisplayCursorModeKey = "CC.Options.DisplayCursorMode";

        [Header("Defaults")]
        [SerializeField] [Range(0f, 1f)] private float defaultMasterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float defaultBgmVolume = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float defaultSfxVolume = 1f;
        [SerializeField] [Range(0.02f, 0.4f)] private float defaultMouseSensitivity = 0.12f;
        [SerializeField] private DisplayCursorMode defaultDisplayCursorMode = DisplayCursorMode.FullscreenLocked;

        [Header("Mouse Look")]
        [SerializeField] private MonoBehaviour mouseLookCamera;
        [SerializeField] private string mouseSensitivityFieldName = "mouseSensitivity";

        public float MasterVolume { get; private set; }
        public float BgmVolume { get; private set; }
        public float SfxVolume { get; private set; }
        public float MouseSensitivity { get; private set; }
        public DisplayCursorMode DisplayCursorMode { get; private set; }
        public bool Fullscreen => DisplayCursorMode == DisplayCursorMode.FullscreenLocked;
        public static float CurrentSfxVolume { get; private set; } = 1f;

        public static DisplayCursorMode CurrentDisplayCursorMode { get; private set; } = DisplayCursorMode.FullscreenLocked;

        private void Awake()
        {
            Load();
            ApplyAll();
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        }

        public void SetBgmVolume(float value)
        {
            BgmVolume = Mathf.Clamp01(value);
            ApplyBgmVolume();
            PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            CurrentSfxVolume = SfxVolume;
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        }

        public void SetMouseSensitivity(float value)
        {
            MouseSensitivity = Mathf.Clamp(value, 0.02f, 0.4f);
            ApplyMouseSensitivity();
            PlayerPrefs.SetFloat(MouseSensitivityKey, MouseSensitivity);
        }

        public void SetFullscreen(bool value)
        {
            SetDisplayCursorMode(value ? DisplayCursorMode.FullscreenLocked : DisplayCursorMode.WindowFree);
        }

        public void SetDisplayCursorMode(DisplayCursorMode mode)
        {
            DisplayCursorMode = mode;
            CurrentDisplayCursorMode = DisplayCursorMode;
            ApplyDisplayMode();
            PlayerPrefs.SetInt(DisplayCursorModeKey, (int)DisplayCursorMode);
            PlayerPrefs.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }

        private void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
            BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, defaultBgmVolume);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);
            MouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, defaultMouseSensitivity);
            DisplayCursorMode = LoadDisplayCursorMode();
            CurrentDisplayCursorMode = DisplayCursorMode;
        }

        private void ApplyAll()
        {
            AudioListener.volume = MasterVolume;
            CurrentSfxVolume = SfxVolume;
            CurrentDisplayCursorMode = DisplayCursorMode;
            ApplyDisplayMode();
            ApplyBgmVolume();
            ApplyMouseSensitivity();
        }

        private DisplayCursorMode LoadDisplayCursorMode()
        {
            if (PlayerPrefs.HasKey(DisplayCursorModeKey))
            {
                return ClampDisplayCursorMode(PlayerPrefs.GetInt(DisplayCursorModeKey, (int)defaultDisplayCursorMode));
            }

            if (PlayerPrefs.HasKey(FullscreenKey))
            {
                return PlayerPrefs.GetInt(FullscreenKey, defaultDisplayCursorMode == DisplayCursorMode.FullscreenLocked ? 1 : 0) != 0
                    ? DisplayCursorMode.FullscreenLocked
                    : DisplayCursorMode.WindowFree;
            }

            return defaultDisplayCursorMode;
        }

        private static DisplayCursorMode ClampDisplayCursorMode(int mode)
        {
            return System.Enum.IsDefined(typeof(DisplayCursorMode), mode)
                ? (DisplayCursorMode)mode
                : DisplayCursorMode.FullscreenLocked;
        }

        private void ApplyDisplayMode()
        {
            Screen.fullScreen = DisplayCursorMode == DisplayCursorMode.FullscreenLocked;
        }

        public static void ApplyGameplayCursorPolicy()
        {
            switch (CurrentDisplayCursorMode)
            {
                case DisplayCursorMode.WindowFree:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;

                case DisplayCursorMode.WindowConfined:
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = true;
                    break;

                default:
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
            }
        }

        public static void ApplyPointerCursorPolicy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ApplyBgmVolume()
        {
            AudioSource[] audioSources = FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < audioSources.Length; i++)
            {
                AudioSource source = audioSources[i];
                if (source == null || !IsBgmSource(source))
                {
                    continue;
                }

                source.volume = BgmVolume;
            }
        }

        private static bool IsBgmSource(AudioSource source)
        {
            string objectName = source.gameObject.name;
            return source.loop
                && (objectName.Contains("BGM", System.StringComparison.OrdinalIgnoreCase)
                    || objectName.Contains("Music", System.StringComparison.OrdinalIgnoreCase)
                    || objectName.Contains("Background", System.StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyMouseSensitivity()
        {
            MonoBehaviour target = ResolveMouseLookCamera();
            if (target == null || string.IsNullOrWhiteSpace(mouseSensitivityFieldName))
            {
                return;
            }

            FieldInfo field = target.GetType().GetField(
                mouseSensitivityFieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(target, MouseSensitivity);
            }
        }

        private MonoBehaviour ResolveMouseLookCamera()
        {
            if (mouseLookCamera != null)
            {
                return mouseLookCamera;
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour.GetType().Name == "MouseLookThirdPersonCamera")
                {
                    mouseLookCamera = behaviour;
                    return mouseLookCamera;
                }
            }

            return null;
        }
    }
}
