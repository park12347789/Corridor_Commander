using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using CorridorCommander.PlayerControl;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class WaveDirector : MonoBehaviour
    {
        [SerializeField] private List<EnemyWaveDefinition> waves = new List<EnemyWaveDefinition>();
        [SerializeField] private WaveReadyPopup readyPopup;
        [SerializeField] private Text statusText;
        [SerializeField] private TMP_Text statusTmpText;
        [SerializeField] private EnemySpawnManager spawnManager;
        [SerializeField] private EnemyCatalogSO enemyCatalog;
        [SerializeField] private DifficultyProgressionSO difficultyProgression;
        [SerializeField] private List<RegionWaveModifierSO> regionWaveModifiers = new List<RegionWaveModifierSO>();
        [SerializeField] private List<PeriodicWaveModifierSO> periodicWaveModifiers = new List<PeriodicWaveModifierSO>();
        [SerializeField] private BossScheduleSO bossSchedule;
        [SerializeField] private PlayerCentralInputController inputController;
        [SerializeField] private bool startWaitingOnEnable = true;
        [SerializeField] private bool startWaitingOnFirstRegionOpened = true;
        [SerializeField] private bool disableSpawnerAutomationOnEnable = true;

        private readonly StageProgressState progressState = new StageProgressState();
        private readonly WaveRuleResolver ruleResolver = new WaveRuleResolver();
        private int currentWaveIndex;
        private float waitStartedAt;
        private bool isWaiting;
        private bool isRunning;
        private bool isHoldingNextWave;
        private bool hasStartedFirstWave;
        private int heldNextWaveIndex = -1;
        private Coroutine runningWave;

        public event Action<WaveStartedInfo> WaveStarted;
        public event Action<int, EnemyWaveDefinition> WaveCleared;

        public int CurrentWaveNumber => currentWaveIndex + 1;
        public bool IsWaitingForWave => isWaiting;
        public bool IsRunningWave => isRunning;
        public bool IsHoldingWaveReward => isHoldingNextWave;
        public bool HasBossSchedule => bossSchedule != null;
        public string CurrentWaveId => CurrentWave != null ? CurrentWave.WaveId : string.Empty;
        public float CurrentWaveRemainingSeconds =>
            isWaiting && CurrentWave != null
                ? Mathf.Max(0f, CurrentWave.AutoStartDelay - (Time.time - waitStartedAt))
                : 0f;

        private EnemyWaveDefinition CurrentWave =>
            ResolveWaveTemplate(currentWaveIndex);

        private void Awake()
        {
            ResolveSpawnManager();
            ResolveInputController();

            if (readyPopup != null)
            {
                readyPopup.Bind(this);
            }
        }

        private void OnEnable()
        {
            SubscribeSpawnManager();
            SubscribeInputController();

            if (disableSpawnerAutomationOnEnable)
            {
                DisableSpawnerAutomation();
            }

            if (startWaitingOnEnable)
            {
                BeginWaitingForWave(0);
            }
        }

        private void Start()
        {
            SubscribeSpawnManager();
            SubscribeInputController();
        }

        private void OnDisable()
        {
            UnsubscribeSpawnManager();
            UnsubscribeInputController();
            UiInputCoordinator.EndContextIfActive(this);
        }

        private void Update()
        {
            if (!isWaiting || CurrentWave == null)
            {
                RefreshStatus();
                return;
            }

            if (Time.time - waitStartedAt >= CurrentWave.AutoStartDelay)
            {
                StartCurrentWave();
                RefreshStatus();
                return;
            }

            if (!KeyboardInputMessenger.HasKeyboard)
            {
                RefreshStatus();
                return;
            }

            bool useDirectKeyboardInput = inputController == null;
            if (useDirectKeyboardInput
                && KeyboardInputMessenger.WasWavePopupPressed()
                && !ShouldSuppressWavePopupHotkey())
            {
                ShowReadyPopup();
            }

            if (useDirectKeyboardInput && readyPopup != null && readyPopup.IsOpen)
            {
                if (KeyboardInputMessenger.WasWaveConfirmPressed()
                    && UiInputCoordinator.Instance.TryConsumeContextInput(this))
                {
                    ConfirmReady();
                }

                if (KeyboardInputMessenger.WasWaveCancelPressed()
                    && UiInputCoordinator.Instance.TryConsumeCancel(this))
                {
                    CancelReady();
                }
            }

            RefreshStatus();
        }

        public void BeginWaitingForWave(int waveIndex)
        {
            currentWaveIndex = waveIndex;
            waitStartedAt = Time.time;
            isWaiting = CurrentWave != null;
            isRunning = false;
            isHoldingNextWave = false;
            if (waveIndex == 0 && isWaiting)
            {
                hasStartedFirstWave = true;
            }

            heldNextWaveIndex = -1;
            readyPopup?.Hide();
            UiInputCoordinator.EndContextIfActive(this);
            RefreshStatus();
        }

        public void HoldNextWave()
        {
            if (isRunning)
            {
                Debug.LogWarning("[WaveDirector] Cannot hold next wave while current wave is still running.", this);
                return;
            }

            isHoldingNextWave = true;
        }

        public void ContinueAfterWaveHold()
        {
            if (!isHoldingNextWave || heldNextWaveIndex < 0)
            {
                Debug.LogWarning("[WaveDirector] ContinueAfterWaveHold called without an active hold.", this);
                return;
            }

            BeginWaitingForWave(heldNextWaveIndex);
        }

        public void ConfigureWaves(IReadOnlyList<EnemyWaveDefinition> configuredWaves, bool restart)
        {
            waves.Clear();
            if (configuredWaves != null)
            {
                for (int i = 0; i < configuredWaves.Count; i++)
                {
                    if (configuredWaves[i] != null)
                    {
                        waves.Add(configuredWaves[i]);
                    }
                }
            }

            if (runningWave != null)
            {
                StopCoroutine(runningWave);
                runningWave = null;
            }

            if (restart)
            {
                ResetWaveState(0);
                if (startWaitingOnEnable)
                {
                    BeginWaitingForWave(0);
                }
            }
            else
            {
                RefreshStatus();
            }
        }

        public void ConfigureStage(StageDefinitionSO stageDefinition, bool restart)
        {
            if (stageDefinition == null)
            {
                ConfigureWaves(null, restart);
                return;
            }

            enemyCatalog = stageDefinition.EnemyCatalog;
            difficultyProgression = stageDefinition.DifficultyProgression;
            bossSchedule = stageDefinition.BossSchedule;
            regionWaveModifiers.Clear();
            periodicWaveModifiers.Clear();

            IReadOnlyList<RegionWaveModifierSO> configuredModifiers = stageDefinition.RegionWaveModifiers;
            if (configuredModifiers != null)
            {
                for (int i = 0; i < configuredModifiers.Count; i++)
                {
                    if (configuredModifiers[i] != null)
                    {
                        regionWaveModifiers.Add(configuredModifiers[i]);
                    }
                }
            }

            IReadOnlyList<PeriodicWaveModifierSO> configuredPeriodicModifiers = stageDefinition.PeriodicWaveModifiers;
            if (configuredPeriodicModifiers != null)
            {
                for (int i = 0; i < configuredPeriodicModifiers.Count; i++)
                {
                    if (configuredPeriodicModifiers[i] != null)
                    {
                        periodicWaveModifiers.Add(configuredPeriodicModifiers[i]);
                    }
                }
            }

            ConfigureWaves(stageDefinition.Waves, restart);
        }

        public void ConfirmReady()
        {
            if (!isWaiting || CurrentWave == null)
            {
                return;
            }

            StartCurrentWave();
        }

        public void CancelReady()
        {
            readyPopup?.Hide();
            UiInputCoordinator.EndContextIfActive(this);
            RefreshStatus();
        }

        private void ShowReadyPopup()
        {
            if (readyPopup == null || CurrentWave == null)
            {
                return;
            }

            if (!UiInputCoordinator.Instance.TryBeginContext(this, UiInputContext.WaveReadyPopup, true))
            {
                return;
            }

            readyPopup.Show($"{CurrentWave.WaveId} ready?\n\n1/E \uC2DC\uC791 / 2/ESC \uCDE8\uC18C");
        }

        public static bool ShouldSuppressWavePopupHotkey()
        {
            return UiInputCoordinator.BlocksHotkeys || HasVisibleSupportTruckPrompt();
        }

        private static bool HasVisibleSupportTruckPrompt()
        {
            SupportTruckShopInteraction[] shops = FindObjectsByType<SupportTruckShopInteraction>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < shops.Length; i++)
            {
                if (shops[i] != null && shops[i].IsPromptVisible)
                {
                    return true;
                }
            }

            return false;
        }

        private void StartCurrentWave()
        {
            if (isRunning || CurrentWave == null)
            {
                return;
            }

            progressState.SetCurrentWaveIndex(currentWaveIndex);
            readyPopup?.Hide();
            UiInputCoordinator.EndContextIfActive(this);
            isWaiting = false;
            isRunning = true;

            if (runningWave != null)
            {
                StopCoroutine(runningWave);
            }

            EnemyWaveDefinition wave = CurrentWave;
            WaveStarted?.Invoke(new WaveStartedInfo(
                currentWaveIndex,
                wave,
                IsBossWave(currentWaveIndex),
                GetBossSpawnCount(currentWaveIndex)));
            runningWave = StartCoroutine(RunWave(wave));
            RefreshStatus();
        }

        private IEnumerator RunWave(EnemyWaveDefinition wave)
        {
            if (disableSpawnerAutomationOnEnable)
            {
                DisableSpawnerAutomation();
            }

            WaveSpawnPlan spawnPlan = ruleResolver.BuildPlan(
                wave,
                currentWaveIndex,
                enemyCatalog,
                difficultyProgression,
                regionWaveModifiers,
                periodicWaveModifiers,
                bossSchedule,
                progressState);
            ResetWaveSpawners(spawnPlan);
            HashSet<string> contestedSectors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int aliveSpawnedEnemies = 0;

            void HandleSpawnedEnemyDied(Health health)
            {
                if (health != null)
                {
                    health.Died -= HandleSpawnedEnemyDied;
                }

                aliveSpawnedEnemies = Mathf.Max(0, aliveSpawnedEnemies - 1);
            }

            IReadOnlyList<WaveSpawnPhasePlan> phases = spawnPlan.Phases;
            for (int phaseIndex = 0; phaseIndex < phases.Count; phaseIndex++)
            {
                WaveSpawnPhasePlan phase = phases[phaseIndex];
                if (phase.Delay > 0f)
                {
                    yield return new WaitForSeconds(phase.Delay);
                }

                for (int i = 0; i < phase.Rules.Count; i++)
                {
                    WaveSpawnRulePlan rule = phase.Rules[i];
                    string sectorId = GetRuleSectorId(rule);
                    if (!string.IsNullOrWhiteSpace(sectorId))
                    {
                        progressState.MarkSectorContested(sectorId);
                        contestedSectors.Add(sectorId);
                    }

                    IReadOnlyList<EnemySpawner> spawners = GetSpawners(rule);
                    if (spawners.Count == 0)
                    {
                        Debug.LogWarning($"Wave spawner group not found: {GetSpawnGroupName(rule)}", this);
                        continue;
                    }

                    for (int count = 0; count < rule.SpawnCount; count++)
                    {
                        EnemySpawner spawner = spawners[count % spawners.Count];
                        spawner.SetUpdateLoopEnabled(false);
                        EnemyDefinitionSO enemyDefinition = rule.PickEnemy();
                        if (!spawner.TrySpawnOne(enemyDefinition, rule.HealthMultiplier, out GameObject spawnedEnemy))
                        {
                            Debug.LogWarning($"Wave spawn failed: {wave.WaveId} / {spawner.name}", spawner);
                        }
                        else if (spawnedEnemy != null && spawnedEnemy.TryGetComponent(out Health spawnedHealth) && spawnedHealth.IsAlive)
                        {
                            aliveSpawnedEnemies++;
                            spawnedHealth.Died -= HandleSpawnedEnemyDied;
                            spawnedHealth.Died += HandleSpawnedEnemyDied;
                        }

                        if (rule.SpawnInterval > 0f)
                        {
                            yield return new WaitForSeconds(rule.SpawnInterval);
                        }
                    }
                }
            }

            while (aliveSpawnedEnemies > 0)
            {
                yield return null;
            }

            foreach (string sectorId in contestedSectors)
            {
                progressState.MarkSectorSecured(sectorId);
            }

            int clearedWaveIndex = currentWaveIndex;
            heldNextWaveIndex = currentWaveIndex + 1;
            isHoldingNextWave = false;
            isRunning = false;
            runningWave = null;
            WaveCleared?.Invoke(clearedWaveIndex, wave);
            if (!isHoldingNextWave)
            {
                BeginWaitingForWave(heldNextWaveIndex);
            }
            else
            {
                RefreshStatus();
            }
        }

        private IReadOnlyList<EnemySpawner> GetSpawners(WaveSpawnRulePlan rule)
        {
            ResolveSpawnManager();
            if (spawnManager != null)
            {
                IReadOnlyList<EnemySpawner> namedSpawners =
                    FilterSpawnersBySectorState(spawnManager.GetActiveSpawners(rule.SpawnGroup, rule.SpawnerNameContains));
                if (namedSpawners.Count > 0 || string.IsNullOrWhiteSpace(rule.SpawnerNameContains))
                {
                    return namedSpawners;
                }

                IReadOnlyList<EnemySpawner> activeFallback =
                    FilterSpawnersBySectorState(spawnManager.GetActiveSpawners(rule.SpawnGroup));
                if (activeFallback.Count > 0)
                {
                    Debug.Log(
                        $"Wave spawner name not active: {rule.SpawnerNameContains}. Using active spawners from {GetSpawnGroupName(rule)}.",
                        this);
                    return activeFallback;
                }

                return namedSpawners;
            }

            List<EnemySpawner> spawners = new List<EnemySpawner>();
            EnemySpawner[] sceneSpawners = FindObjectsByType<EnemySpawner>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < sceneSpawners.Length; i++)
            {
                EnemySpawner spawner = sceneSpawners[i];
                if (spawner == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(rule.SpawnerNameContains)
                    && !spawner.name.Contains(rule.SpawnerNameContains, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                spawners.Add(spawner);
            }

            return FilterSpawnersBySectorState(spawners);
        }

        private void ResolveInputController()
        {
            if (inputController == null)
            {
                inputController = FindFirstObjectByType<PlayerCentralInputController>(FindObjectsInactive.Include);
            }
        }

        private void SubscribeInputController()
        {
            ResolveInputController();
            if (inputController == null)
            {
                return;
            }

            inputController.WaveReadyPopupRequested -= ShowReadyPopup;
            inputController.WaveReadyPopupRequested += ShowReadyPopup;
            inputController.WaveStartConfirmed -= ConfirmReady;
            inputController.WaveStartConfirmed += ConfirmReady;
            inputController.WaveStartCanceled -= CancelReady;
            inputController.WaveStartCanceled += CancelReady;
        }

        private void UnsubscribeInputController()
        {
            if (inputController == null)
            {
                return;
            }

            inputController.WaveReadyPopupRequested -= ShowReadyPopup;
            inputController.WaveStartConfirmed -= ConfirmReady;
            inputController.WaveStartCanceled -= CancelReady;
        }

        private void ResetWaveSpawners(WaveSpawnPlan spawnPlan)
        {
            List<EnemySpawner> resetSpawners = new List<EnemySpawner>();
            IReadOnlyList<WaveSpawnPhasePlan> phases = spawnPlan != null
                ? spawnPlan.Phases
                : Array.Empty<WaveSpawnPhasePlan>();

            for (int phaseIndex = 0; phaseIndex < phases.Count; phaseIndex++)
            {
                WaveSpawnPhasePlan phase = phases[phaseIndex];
                for (int i = 0; i < phase.Rules.Count; i++)
                {
                    IReadOnlyList<EnemySpawner> spawners = GetSpawners(phase.Rules[i]);
                    for (int spawnerIndex = 0; spawnerIndex < spawners.Count; spawnerIndex++)
                    {
                        EnemySpawner spawner = spawners[spawnerIndex];
                        if (spawner == null || resetSpawners.Contains(spawner))
                        {
                            continue;
                        }

                        spawner.ResetRuntimeState();
                        resetSpawners.Add(spawner);
                    }
                }
            }
        }

        private void RefreshStatus()
        {
            if (statusTmpText == null && statusText == null)
            {
                return;
            }

            EnemyWaveDefinition wave = CurrentWave;
            if (wave == null)
            {
                SetStatusText("No wave");
                return;
            }

            if (isHoldingNextWave)
            {
                SetStatusText("Reward\nSelect reward");
                return;
            }

            if (isRunning)
            {
                SetStatusText($"Wave {currentWaveIndex + 1}\n{wave.WaveId}\nRunning");
                return;
            }

            float remaining = Mathf.Max(0f, wave.AutoStartDelay - (Time.time - waitStartedAt));
            SetStatusText($"P  Wave {currentWaveIndex + 1} ready\n{wave.WaveId}\nAuto start {remaining:0}s");
        }

        private void SetStatusText(string value)
        {
            if (statusTmpText != null)
            {
                statusTmpText.text = value;
                return;
            }

            if (statusText != null)
            {
                statusText.text = value;
            }
        }

        public bool IsBossWave(int waveIndex)
        {
            return bossSchedule != null && bossSchedule.ShouldAddBoss(waveIndex);
        }

        public int GetBossSpawnCount(int waveIndex)
        {
            return bossSchedule != null ? bossSchedule.GetSpawnCount(waveIndex) : 0;
        }

        private EnemyWaveDefinition ResolveWaveTemplate(int waveIndex)
        {
            if (waves == null || waves.Count == 0 || waveIndex < 0)
            {
                return null;
            }

            int index = waveIndex % waves.Count;
            return waves[index];
        }

        private void DisableSpawnerAutomation()
        {
            ResolveSpawnManager();
            if (spawnManager != null)
            {
                spawnManager.SetManagedSpawnerAutomationEnabled(false);
            }
        }

        private void ResolveSpawnManager()
        {
            if (spawnManager == null)
            {
                spawnManager = EnemySpawnManager.Instance;
            }

            if (spawnManager == null)
            {
                spawnManager = FindFirstObjectByType<EnemySpawnManager>(FindObjectsInactive.Include);
            }
        }

        private void SubscribeSpawnManager()
        {
            ResolveSpawnManager();
            if (spawnManager != null)
            {
                spawnManager.RegionOpened -= HandleRegionOpened;
                spawnManager.RegionOpened += HandleRegionOpened;
            }
        }

        private void UnsubscribeSpawnManager()
        {
            if (spawnManager != null)
            {
                spawnManager.RegionOpened -= HandleRegionOpened;
            }
        }

        private void HandleRegionOpened(string regionId)
        {
            progressState.MarkRegionOpened(regionId);
            if (startWaitingOnFirstRegionOpened && !hasStartedFirstWave && !isWaiting && !isRunning && CurrentWave != null)
            {
                BeginWaitingForWave(0);
            }
        }

        private void ResetWaveState(int waveIndex)
        {
            currentWaveIndex = waveIndex;
            waitStartedAt = Time.time;
            isWaiting = false;
            isRunning = false;
            isHoldingNextWave = false;
            hasStartedFirstWave = false;
            heldNextWaveIndex = -1;
            readyPopup?.Hide();
            UiInputCoordinator.EndContextIfActive(this);
            RefreshStatus();
        }

        private static string GetSpawnGroupName(WaveSpawnRulePlan rule)
        {
            if (rule.SpawnGroup != null)
            {
                return rule.SpawnGroup.DisplayName;
            }

            return string.IsNullOrWhiteSpace(rule.SpawnerNameContains)
                ? "None"
                : rule.SpawnerNameContains;
        }

        private IReadOnlyList<EnemySpawner> FilterSpawnersBySectorState(IReadOnlyList<EnemySpawner> spawners)
        {
            if (spawners == null || spawners.Count == 0)
            {
                return Array.Empty<EnemySpawner>();
            }

            List<EnemySpawner> result = new List<EnemySpawner>();
            for (int i = 0; i < spawners.Count; i++)
            {
                EnemySpawner spawner = spawners[i];
                if (spawner == null)
                {
                    continue;
                }

                string sectorId = GetSpawnerSectorId(spawner.name);
                StageSectorState state = progressState.GetSectorState(sectorId);
                if (state == StageSectorState.Secured)
                {
                    continue;
                }

                result.Add(spawner);
            }

            return result;
        }

        private static string GetRuleSectorId(WaveSpawnRulePlan rule)
        {
            return rule != null ? GetSpawnerSectorId(rule.SpawnerNameContains) : string.Empty;
        }

        private static string GetSpawnerSectorId(string spawnerName)
        {
            if (string.IsNullOrWhiteSpace(spawnerName))
            {
                return string.Empty;
            }

            const string prefix = "Stage1_Spawn_";
            string sectorId = spawnerName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? spawnerName.Substring(prefix.Length)
                : spawnerName;

            int suffixIndex = sectorId.IndexOf("_A_RED", StringComparison.OrdinalIgnoreCase);
            if (suffixIndex >= 0)
            {
                sectorId = sectorId.Substring(0, suffixIndex);
            }

            return sectorId;
        }
    }

}
