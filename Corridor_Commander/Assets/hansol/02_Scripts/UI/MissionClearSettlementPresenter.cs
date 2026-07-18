using CorridorCommander.PlayerControl;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class MissionClearSettlementPresenter : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string lobbySceneName = "StartMenu";

        [Header("Roots")]
        [SerializeField] private GameObject screenRoot;
        [SerializeField] private DotweenUiPanelTransition screenTransition;

        [Header("Text")]
        [SerializeField] private TMP_Text titleTmpText;
        [SerializeField] private TMP_Text summaryTmpText;
        [SerializeField] private TMP_Text moneyTmpText;
        [SerializeField] private TMP_Text spentTmpText;
        [SerializeField] private TMP_Text levelTmpText;
        [SerializeField] private TMP_Text killProgressTmpText;
        [SerializeField] private TMP_Text statPointTmpText;
        [SerializeField] private TMP_Text timeTmpText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text moneyText;
        [SerializeField] private Text spentText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text killProgressText;
        [SerializeField] private Text statPointText;
        [SerializeField] private Text timeText;

        [Header("Buttons")]
        [SerializeField] private Button lobbyButton;

        [Header("Runtime Sources")]
        [SerializeField] private PlayerCurrencyWallet currencyWallet;
        [SerializeField] private PlayerLevelProgression levelProgression;

        private int startingMoney;
        private int earnedMoney;
        private int spentMoney;
        private float missionStartedAt;
        private bool walletEventsBound;
        private bool startingValuesCaptured;

        private void Awake()
        {
            missionStartedAt = Time.realtimeSinceStartup;
            ResolveRuntimeSources();
            CaptureStartingValuesIfReady();
            HideImmediate();
        }

        private void OnEnable()
        {
            ResolveRuntimeSources();
            CaptureStartingValuesIfReady();
            BindWalletEvents();
            lobbyButton?.onClick.AddListener(ReturnToLobby);
        }

        private void OnDisable()
        {
            UnbindWalletEvents();
            lobbyButton?.onClick.RemoveListener(ReturnToLobby);
            UiInputCoordinator.EndContextIfActive(this);
            PopupDimOverlayController.Release(this);
        }

        public void ShowFinalSettlement(bool pauseGameplay = true)
        {
            ResolveRuntimeSources();
            CaptureStartingValuesIfReady();
            BindWalletEvents();

            UiInputCoordinator coordinator = UiInputCoordinator.Instance;
            bool contextStarted = pauseGameplay
                ? coordinator != null && coordinator.TryBeginPausedContext(this, UiInputContext.MissionClearScreen, true)
                : coordinator != null && coordinator.TryBeginContext(this, UiInputContext.MissionClearScreen, true);
            if (!contextStarted)
            {
                Debug.LogError("[MissionClearSettlementPresenter] Could not enter mission-clear UI context.", this);
                return;
            }

            if (screenTransition != null)
            {
                screenTransition.Show();
            }
            else if (screenRoot != null)
            {
                screenRoot.SetActive(true);
            }

            PopupDimOverlayController.RequestShow(this, screenRoot != null ? screenRoot.transform : transform);
            RefreshText();
            SelectLobbyButton();
        }

        public void ReturnToLobby()
        {
            if (string.IsNullOrWhiteSpace(lobbySceneName))
            {
                Debug.LogError("[MissionClearSettlementPresenter] Lobby scene name is empty.", this);
                return;
            }

            Time.timeScale = 1f;
            UiInputCoordinator.EndContextIfActive(this);
            PopupDimOverlayController.Release(this);
            SceneManager.LoadScene(lobbySceneName);
        }

        private void RefreshText()
        {
            int currentMoney = currencyWallet != null ? currencyWallet.CurrentMoney : 0;
            int level = levelProgression != null ? levelProgression.CurrentLevel : 1;
            int killProgress = levelProgression != null ? levelProgression.CurrentKillProgress : 0;
            int requiredKills = levelProgression != null ? levelProgression.RequiredKillsForNextLevel : 0;
            int statPoints = levelProgression != null ? levelProgression.CurrentStatPoints : 0;

            SetText(titleTmpText, titleText, "MISSION CLEAR");
            SetText(summaryTmpText, summaryText, "탈출 지점 복귀 완료");
            SetText(moneyTmpText, moneyText, $"보유 크레딧  {currentMoney}  (+{earnedMoney} / 시작 {startingMoney})");
            SetText(spentTmpText, spentText, $"사용 크레딧  {spentMoney}");
            SetText(levelTmpText, levelText, $"전투 레벨  {level}");
            SetText(killProgressTmpText, killProgressText, $"전투 데이터  {killProgress}/{requiredKills}");
            SetText(statPointTmpText, statPointText, $"남은 강화 포인트  {statPoints}");
            SetText(timeTmpText, timeText, $"임무 시간  {FormatDuration(Time.realtimeSinceStartup - missionStartedAt)}");
        }

        private void ResolveRuntimeSources()
        {
            if (currencyWallet == null)
            {
                currencyWallet = FindFirstObjectByType<PlayerCurrencyWallet>(FindObjectsInactive.Include);
            }

            if (levelProgression == null)
            {
                levelProgression = FindFirstObjectByType<PlayerLevelProgression>(FindObjectsInactive.Include);
            }
        }

        private void CaptureStartingValuesIfReady()
        {
            if (startingValuesCaptured || currencyWallet == null)
            {
                return;
            }

            startingMoney = currencyWallet.CurrentMoney;
            earnedMoney = 0;
            spentMoney = 0;
            startingValuesCaptured = true;
        }

        private void BindWalletEvents()
        {
            if (walletEventsBound || currencyWallet == null)
            {
                return;
            }

            currencyWallet.MoneyAdded += HandleMoneyAdded;
            currencyWallet.MoneySpent += HandleMoneySpent;
            walletEventsBound = true;
        }

        private void UnbindWalletEvents()
        {
            if (!walletEventsBound || currencyWallet == null)
            {
                return;
            }

            currencyWallet.MoneyAdded -= HandleMoneyAdded;
            currencyWallet.MoneySpent -= HandleMoneySpent;
            walletEventsBound = false;
        }

        private void HandleMoneyAdded(int amount)
        {
            earnedMoney += Mathf.Max(0, amount);
        }

        private void HandleMoneySpent(int amount)
        {
            spentMoney += Mathf.Max(0, amount);
        }

        private void HideImmediate()
        {
            if (screenTransition != null)
            {
                screenTransition.HideImmediate();
            }
            else if (screenRoot != null)
            {
                screenRoot.SetActive(false);
            }
        }

        private void SelectLobbyButton()
        {
            if (EventSystem.current != null && lobbyButton != null && lobbyButton.interactable)
            {
                EventSystem.current.SetSelectedGameObject(lobbyButton.gameObject);
            }
        }

        private static void SetText(TMP_Text tmpText, Text legacyText, string value)
        {
            if (tmpText != null)
            {
                tmpText.text = value;
            }

            if (legacyText != null)
            {
                legacyText.text = value;
            }
        }

        private static string FormatDuration(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }
    }
}
