using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class StartMenuPresenter : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string tutorialSceneName = "TutorialMap";

        [Header("Roots")]
        [SerializeField] private GameObject mainRoot;
        [SerializeField] private GameObject optionsRoot;
        [SerializeField] private StartMenuStageSelectPresenter stageSelectPopup;
        [SerializeField] private DotweenUiPanelTransition mainTransition;
        [SerializeField] private DotweenUiPanelTransition optionsTransition;

        [Header("Buttons")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button optionsButton;
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

        private void Awake()
        {
            Time.timeScale = 1f;
            GameplayOptionsController.ApplyPointerCursorPolicy();

            if (optionsController == null)
            {
                optionsController = GetComponent<GameplayOptionsController>();
            }

            optionsTransition?.HideImmediate();
            ShowMain();
            SyncControlsFromOptions();
        }

        private void OnEnable()
        {
            AddListeners();
        }

        private void OnDisable()
        {
            RemoveListeners();
            optionsController?.Save();
        }

        private void Update()
        {
            if (optionsRoot != null
                && optionsRoot.activeSelf
                && KeyboardInputMessenger.WasCancelPressed())
            {
                ShowMain();
            }
        }

        public void StartGame()
        {
            if (stageSelectPopup == null)
            {
                Debug.LogError("[StartMenuPresenter] Stage select popup is not assigned.", this);
                return;
            }

            stageSelectPopup.Show();
        }

        public void StartTutorial()
        {
            LoadScene(tutorialSceneName);
        }

        public void ShowOptions()
        {
            if (mainTransition != null)
            {
                mainTransition.Hide();
            }
            else if (mainRoot != null)
            {
                mainRoot.SetActive(false);
            }

            if (optionsTransition != null)
            {
                optionsTransition.Show();
            }
            else if (optionsRoot != null)
            {
                optionsRoot.SetActive(true);
            }

            SyncControlsFromOptions();
        }

        public void ShowMain()
        {
            if (mainTransition != null)
            {
                mainTransition.Show();
            }
            else if (mainRoot != null)
            {
                mainRoot.SetActive(true);
            }

            if (optionsTransition != null)
            {
                optionsTransition.Hide();
            }
            else if (optionsRoot != null)
            {
                optionsRoot.SetActive(false);
            }

            stageSelectPopup?.Hide();
        }

        public void QuitGame()
        {
            optionsController?.Save();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[StartMenuPresenter] Target scene name is empty.", this);
                return;
            }

            optionsController?.Save();
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private void AddListeners()
        {
            startGameButton?.onClick.AddListener(StartGame);
            if (stageSelectPopup != null)
            {
                stageSelectPopup.StageSceneSelected += LoadScene;
            }

            tutorialButton?.onClick.AddListener(StartTutorial);
            optionsButton?.onClick.AddListener(ShowOptions);
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
            startGameButton?.onClick.RemoveListener(StartGame);
            if (stageSelectPopup != null)
            {
                stageSelectPopup.StageSceneSelected -= LoadScene;
            }

            tutorialButton?.onClick.RemoveListener(StartTutorial);
            optionsButton?.onClick.RemoveListener(ShowOptions);
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
