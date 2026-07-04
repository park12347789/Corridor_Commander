using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CorridorCommander.PlayerControl;
using CorridorCommander.PlayerItems;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class StageRuntime : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private UiInputCoordinator uiInputCoordinator;
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private WaveReadyPopup waveReadyPopup;
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private Text waveStatusText;
        [SerializeField] private TMP_Text waveStatusTmpText;
        [SerializeField] private PlacementBuildMenuPresenter placementBuildMenuPresenter;
        [SerializeField] private InstalledObjectActionPresenter installedObjectActionPresenter;
        [SerializeField] private TreasureRewardMenuPresenter treasureRewardMenuPresenter;
        [SerializeField] private SupportTruckShopPresenter supportTruckShopPresenter;
        [SerializeField] private WaveStartNotificationPresenter waveStartNotificationPresenter;
        [SerializeField] private WaveRewardController waveRewardController;
        [SerializeField] private RewardGrantService rewardGrantService;
        [SerializeField] private ArtifactInventory artifactInventory;
        [SerializeField] private ArtifactStatManager artifactStatManager;

        public GameManager GameManager => gameManager;
        public UiInputCoordinator UiInputCoordinator => uiInputCoordinator;
        public WaveDirector WaveDirector => waveDirector;
        public WaveReadyPopup WaveReadyPopup => waveReadyPopup;
        public Canvas MainCanvas => mainCanvas;
        public Text WaveStatusText => waveStatusText;
        public TMP_Text WaveStatusTmpText => waveStatusTmpText;
        public PlacementBuildMenuPresenter PlacementBuildMenuPresenter => placementBuildMenuPresenter;
        public InstalledObjectActionPresenter InstalledObjectActionPresenter => installedObjectActionPresenter;
        public TreasureRewardMenuPresenter TreasureRewardMenuPresenter => treasureRewardMenuPresenter;
        public SupportTruckShopPresenter SupportTruckShopPresenter => supportTruckShopPresenter;
        public WaveStartNotificationPresenter WaveStartNotificationPresenter => waveStartNotificationPresenter;
        public WaveRewardController WaveRewardController => waveRewardController;
        public RewardGrantService RewardGrantService => rewardGrantService;
        public ArtifactInventory ArtifactInventory => artifactInventory;
        public ArtifactStatManager ArtifactStatManager => artifactStatManager;

        private void Awake()
        {
            ResetGameplayRuntimeState();
        }

        public void ConfigureRewardServices(
            PlayerCurrencyWallet currencyWallet,
            PlayerItemInventory itemInventory,
            PlayerLevelProgression levelProgression)
        {
            if (rewardGrantService == null)
            {
                Debug.LogError("[StageRuntime] RewardGrantService is not assigned.", this);
                return;
            }

            if (artifactInventory == null)
            {
                Debug.LogError("[StageRuntime] ArtifactInventory is not assigned.", this);
                return;
            }

            if (artifactStatManager == null)
            {
                Debug.LogError("[StageRuntime] ArtifactStatManager is not assigned.", this);
                return;
            }

            if (currencyWallet == null || itemInventory == null || levelProgression == null)
            {
                Debug.LogError("[StageRuntime] Player reward service references are incomplete.", this);
                return;
            }

            artifactStatManager.Configure(artifactInventory);
            rewardGrantService.Configure(currencyWallet, itemInventory, levelProgression, artifactInventory);
        }

        public void ResolveMissingReferences()
        {
            if (gameManager == null)
            {
                Debug.LogWarning("[StageRuntime] GameManager is not assigned.", this);
            }

            if (uiInputCoordinator == null)
            {
                Debug.LogWarning("[StageRuntime] UiInputCoordinator is not assigned.", this);
            }

            if (waveDirector == null)
            {
                Debug.LogWarning("[StageRuntime] WaveDirector is not assigned.", this);
            }

            if (rewardGrantService == null)
            {
                Debug.LogWarning("[StageRuntime] RewardGrantService is not assigned.", this);
            }

            if (artifactInventory == null)
            {
                Debug.LogWarning("[StageRuntime] ArtifactInventory is not assigned.", this);
            }

            if (artifactStatManager == null)
            {
                Debug.LogWarning("[StageRuntime] ArtifactStatManager is not assigned.", this);
            }
        }

        private static void ResetGameplayRuntimeState()
        {
            Time.timeScale = 1f;
        }
    }
}
