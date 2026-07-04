using TMPro;
using CorridorCommander.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class GameOverScreenPresenter : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string retrySceneName;
        [SerializeField] private string lobbySceneName = "StartMenu";

        [Header("Roots")]
        [SerializeField] private GameObject screenRoot;

        [Header("Text")]
        [SerializeField] private TMP_Text reasonText;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button lobbyButton;

        [Header("Audio")]
        [SerializeField] private BgmPlayer bgmPlayer;
        [SerializeField] private BgmDefinitionSO gameOverBgm;

        private GameManager subscribedManager;

        private void Awake()
        {
            HideImmediate();
        }

        private void OnEnable()
        {
            Subscribe(GameManager.Instance);
            retryButton?.onClick.AddListener(Retry);
            lobbyButton?.onClick.AddListener(ReturnToLobby);
        }

        private void OnDisable()
        {
            Unsubscribe();
            retryButton?.onClick.RemoveListener(Retry);
            lobbyButton?.onClick.RemoveListener(ReturnToLobby);
            UiInputCoordinator.EndContextIfActive(this);
            PopupDimOverlayController.Release(this);
        }

        private void Update()
        {
            if (subscribedManager == null)
            {
                Subscribe(GameManager.Instance);
            }
        }

        public void Show(string reason)
        {
            Time.timeScale = 0f;
            UiInputCoordinator.Instance?.TryBeginContext(this, UiInputContext.GameOverScreen, true);

            if (screenRoot != null)
            {
                screenRoot.SetActive(true);
            }

            PopupDimOverlayController.RequestShow(this, screenRoot != null ? screenRoot.transform : transform);

            if (reasonText != null)
            {
                reasonText.text = string.IsNullOrWhiteSpace(reason) ? "Game Over" : reason;
            }

            ResolveBgmPlayer();
            if (bgmPlayer != null && gameOverBgm != null)
            {
                bgmPlayer.PlayOverride(gameOverBgm);
            }
        }

        public void Retry()
        {
            string sceneName = string.IsNullOrWhiteSpace(retrySceneName)
                ? SceneManager.GetActiveScene().name
                : retrySceneName;
            LoadScene(sceneName);
        }

        public void ReturnToLobby()
        {
            LoadScene(lobbySceneName);
        }

        private void Subscribe(GameManager manager)
        {
            if (manager == null || subscribedManager == manager)
            {
                return;
            }

            Unsubscribe();
            subscribedManager = manager;
            subscribedManager.GameOverTriggered += HandleGameOverTriggered;

            if (subscribedManager.IsGameOver)
            {
                Show(subscribedManager.GameOverReason);
            }
        }

        private void Unsubscribe()
        {
            if (subscribedManager != null)
            {
                subscribedManager.GameOverTriggered -= HandleGameOverTriggered;
                subscribedManager = null;
            }
        }

        private void HandleGameOverTriggered(string reason)
        {
            Show(reason);
        }

        private void HideImmediate()
        {
            if (screenRoot != null)
            {
                screenRoot.SetActive(false);
            }
        }

        private void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[GameOverScreenPresenter] Scene name is empty.", this);
                return;
            }

            Time.timeScale = 1f;
            UiInputCoordinator.EndContextIfActive(this);
            PopupDimOverlayController.Release(this);
            bgmPlayer?.ClearOverrideAndResumeScene();
            SceneManager.LoadScene(sceneName);
        }

        private void ResolveBgmPlayer()
        {
            if (bgmPlayer == null)
            {
                bgmPlayer = BgmPlayer.Instance;
            }

            if (bgmPlayer == null)
            {
                bgmPlayer = FindFirstObjectByType<BgmPlayer>(FindObjectsInactive.Include);
            }
        }
    }
}
