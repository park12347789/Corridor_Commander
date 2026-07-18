using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuPresenter : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string lobbySceneName = "StartMenu";

        [Header("Roots")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private DotweenUiPanelTransition menuTransition;
        [SerializeField] private GameObject mainRoot;
        [SerializeField] private GameObject optionsRoot;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button closeOptionsButton;
        [SerializeField] private Button[] additionalCloseOptionsButtons;

        [Header("Options")]
        [SerializeField] private GameplayOptionsController optionsController;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeValueText;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private TMP_Text bgmVolumeValueText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text sfxVolumeValueText;
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private TMP_Text mouseSensitivityValueText;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button fullscreenLockedButton;
        [SerializeField] private Button windowConfinedButton;
        [SerializeField] private Button windowFreeButton;

        private bool isPaused;
        private float timeScaleBeforePause = 1f;
        private int openedFrame = -1;

        public bool IsPaused => isPaused;

        private void Awake()
        {
            if (optionsController == null)
            {
                optionsController = GetComponent<GameplayOptionsController>();
            }

            HideImmediate();
            SyncControlsFromOptions();
        }

        private void OnEnable()
        {
            AddListeners();
        }

        private void OnDisable()
        {
            RemoveListeners();

            if (isPaused)
            {
                ClosePause();
            }
        }

        private void Update()
        {
            if (!KeyboardInputMessenger.WasCancelPressed())
            {
                return;
            }

            if (isPaused)
            {
                if (openedFrame == Time.frameCount)
                {
                    return;
                }

                if (optionsRoot != null && optionsRoot.activeSelf)
                {
                    ShowMain();
                    return;
                }

                ClosePause();
                return;
            }

            if (UiInputCoordinator.BlocksHotkeys)
            {
                return;
            }

            OpenPause();
        }

        public void OpenPause()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (isPaused)
            {
                openedFrame = Time.frameCount;
                ShowPauseMenu();
                return;
            }

            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;

            UiInputCoordinator inputCoordinator = UiInputCoordinator.Instance;
            if (inputCoordinator == null || !inputCoordinator.TryBeginContext(this, UiInputContext.PauseMenu, true))
            {
                return;
            }

            isPaused = true;
            openedFrame = Time.frameCount;
            Time.timeScale = 0f;

            ShowPauseMenu();
        }

        private void ShowPauseMenu()
        {
            if (menuTransition != null)
            {
                menuTransition.Show();
            }
            else if (menuRoot != null)
            {
                menuRoot.SetActive(true);
            }

            PopupDimOverlayController.RequestShow(this, menuRoot != null ? menuRoot.transform : transform);
            ShowMain();
            SyncControlsFromOptions();
        }

        public void ClosePause()
        {
            if (!isPaused)
            {
                return;
            }

            isPaused = false;
            Time.timeScale = timeScaleBeforePause;
            optionsController?.Save();
            UiInputCoordinator.EndContextIfActive(this);
            if (menuTransition != null)
            {
                menuTransition.Hide();
            }
            else
            {
                HideImmediate();
            }
            PopupDimOverlayController.Release(this);
        }

        public void ShowOptions()
        {
            if (mainRoot != null)
            {
                mainRoot.SetActive(false);
            }

            if (optionsRoot != null)
            {
                optionsRoot.SetActive(true);
            }

            SyncControlsFromOptions();
        }

        public void ShowMain()
        {
            if (mainRoot != null)
            {
                mainRoot.SetActive(true);
            }

            if (optionsRoot != null)
            {
                optionsRoot.SetActive(false);
            }
        }

        public void ReturnToLobby()
        {
            LoadScene(lobbySceneName);
        }

        public void QuitGame()
        {
            optionsController?.Save();
            Time.timeScale = 1f;
            UiInputCoordinator.EndContextIfActive(this);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HideImmediate()
        {
            if (menuRoot != null)
            {
                menuRoot.SetActive(false);
            }

            if (mainRoot != null)
            {
                mainRoot.SetActive(true);
            }

            if (optionsRoot != null)
            {
                optionsRoot.SetActive(false);
            }
        }

        private void AddListeners()
        {
            resumeButton?.onClick.AddListener(ClosePause);
            optionsButton?.onClick.AddListener(ShowOptions);
            lobbyButton?.onClick.AddListener(ReturnToLobby);
            quitButton?.onClick.AddListener(QuitGame);
            closeOptionsButton?.onClick.AddListener(ShowMain);
            AddCloseButtonListeners(additionalCloseOptionsButtons);
            masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
            bgmVolumeSlider?.onValueChanged.AddListener(OnBgmVolumeChanged);
            sfxVolumeSlider?.onValueChanged.AddListener(OnSfxVolumeChanged);
            mouseSensitivitySlider?.onValueChanged.AddListener(OnMouseSensitivityChanged);
            fullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);
            fullscreenLockedButton?.onClick.AddListener(OnFullscreenLockedClicked);
            windowConfinedButton?.onClick.AddListener(OnWindowConfinedClicked);
            windowFreeButton?.onClick.AddListener(OnWindowFreeClicked);
        }

        private void RemoveListeners()
        {
            resumeButton?.onClick.RemoveListener(ClosePause);
            optionsButton?.onClick.RemoveListener(ShowOptions);
            lobbyButton?.onClick.RemoveListener(ReturnToLobby);
            quitButton?.onClick.RemoveListener(QuitGame);
            closeOptionsButton?.onClick.RemoveListener(ShowMain);
            RemoveCloseButtonListeners(additionalCloseOptionsButtons);
            masterVolumeSlider?.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            bgmVolumeSlider?.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            sfxVolumeSlider?.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            mouseSensitivitySlider?.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
            fullscreenToggle?.onValueChanged.RemoveListener(OnFullscreenChanged);
            fullscreenLockedButton?.onClick.RemoveListener(OnFullscreenLockedClicked);
            windowConfinedButton?.onClick.RemoveListener(OnWindowConfinedClicked);
            windowFreeButton?.onClick.RemoveListener(OnWindowFreeClicked);
        }

        private void AddCloseButtonListeners(Button[] buttons)
        {
            if (buttons == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i]?.onClick.AddListener(ShowMain);
            }
        }

        private void RemoveCloseButtonListeners(Button[] buttons)
        {
            if (buttons == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i]?.onClick.RemoveListener(ShowMain);
            }
        }

        private void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[PauseMenuPresenter] Lobby scene name is empty.", this);
                return;
            }

            optionsController?.Save();
            Time.timeScale = 1f;
            UiInputCoordinator.EndContextIfActive(this);
            SceneManager.LoadScene(sceneName);
        }

        private void SyncControlsFromOptions()
        {
            if (optionsController == null)
            {
                return;
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(optionsController.MasterVolume);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(optionsController.BgmVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(optionsController.SfxVolume);
            }

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.SetValueWithoutNotify(optionsController.MouseSensitivity);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(optionsController.Fullscreen);
            }

            UpdateDisplayModeButtons();
            UpdateOptionLabels();
        }

        private void OnMasterVolumeChanged(float value)
        {
            optionsController?.SetMasterVolume(value);
            UpdateOptionLabels();
        }

        private void OnBgmVolumeChanged(float value)
        {
            optionsController?.SetBgmVolume(value);
            UpdateOptionLabels();
        }

        private void OnSfxVolumeChanged(float value)
        {
            optionsController?.SetSfxVolume(value);
            UpdateOptionLabels();
        }

        private void OnMouseSensitivityChanged(float value)
        {
            optionsController?.SetMouseSensitivity(value);
            UpdateOptionLabels();
        }

        private void OnFullscreenChanged(bool value)
        {
            optionsController?.SetFullscreen(value);
            UpdateDisplayModeButtons();
        }

        private void OnFullscreenLockedClicked()
        {
            SetDisplayCursorMode(DisplayCursorMode.FullscreenLocked);
        }

        private void OnWindowConfinedClicked()
        {
            SetDisplayCursorMode(DisplayCursorMode.WindowConfined);
        }

        private void OnWindowFreeClicked()
        {
            SetDisplayCursorMode(DisplayCursorMode.WindowFree);
        }

        private void SetDisplayCursorMode(DisplayCursorMode mode)
        {
            optionsController?.SetDisplayCursorMode(mode);
            SyncControlsFromOptions();
        }

        private void UpdateOptionLabels()
        {
            if (optionsController == null)
            {
                return;
            }

            if (masterVolumeValueText != null)
            {
                masterVolumeValueText.text = Mathf.RoundToInt(optionsController.MasterVolume * 100f) + "%";
            }

            if (bgmVolumeValueText != null)
            {
                bgmVolumeValueText.text = Mathf.RoundToInt(optionsController.BgmVolume * 100f) + "%";
            }

            if (sfxVolumeValueText != null)
            {
                sfxVolumeValueText.text = Mathf.RoundToInt(optionsController.SfxVolume * 100f) + "%";
            }

            if (mouseSensitivityValueText != null)
            {
                mouseSensitivityValueText.text = optionsController.MouseSensitivity.ToString("0.00");
            }
        }

        private void UpdateDisplayModeButtons()
        {
            if (optionsController == null)
            {
                return;
            }

            SetModeButtonSelected(fullscreenLockedButton, optionsController.DisplayCursorMode == DisplayCursorMode.FullscreenLocked);
            SetModeButtonSelected(windowConfinedButton, optionsController.DisplayCursorMode == DisplayCursorMode.WindowConfined);
            SetModeButtonSelected(windowFreeButton, optionsController.DisplayCursorMode == DisplayCursorMode.WindowFree);
        }

        private static void SetModeButtonSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = selected ? new Color(0.18f, 0.74f, 1f, 1f) : new Color(0.08f, 0.18f, 0.3f, 0.92f);
            }
        }
    }
}
