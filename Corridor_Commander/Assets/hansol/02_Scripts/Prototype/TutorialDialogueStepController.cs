using CorridorCommander.PlayerControl;
using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerItems;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TutorialDialogueStepController : MonoBehaviour
    {
        private enum StepGoal
        {
            Acknowledge = 0,
            MoveFromStart = 1,
            BuildAtPlacementPoint = 2,
            UpgradeInstalledObject = 3,
            DismantleInstalledObject = 4,
            SpendMoney = 5,
            FireWeapon = 6,
            ReloadWeapon = 7,
            UseItem = 8,
            ThrowItem = 9,
            IssueSquadCommand = 10
        }

        [System.Serializable]
        private sealed class TutorialStep
        {
            public string chapterId = "basic";
            public string speaker = "오퍼레이션";
            [TextArea(2, 4)] public string line = "";
            public string hint = "Enter로 계속";
            public StepGoal goal = StepGoal.Acknowledge;
        }

        [SerializeField] private TutorialDialoguePresenter presenter;
        [SerializeField] private PlacementPoint tutorialPlacementPoint;
        [SerializeField] private Transform operationSpeaker;
        [SerializeField] private Transform playerSpeaker;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerCurrencyWallet playerWallet;
        [SerializeField] private bool faceOperationOnStart = true;
        [SerializeField] private bool autoStartOnStart = false;
        [SerializeField] private string defaultChapterId = "basic";
        [SerializeField] private float moveDistance = 4f;
        [SerializeField] private PlayerProjectileLauncher projectileLauncher;
        [SerializeField] private PlayerWeaponRuntime weaponRuntime;
        [SerializeField] private PlayerItemUseController itemUseController;
        [SerializeField] private PlayerThrowableItemController throwableItemController;
        [SerializeField] private PlayerSquadCommandController squadCommandController;
        [SerializeField] private SupportTruckShopPresenter supportTruckShopPresenter;
        [SerializeField] private TutorialStep[] steps;

        private int stepIndex;
        private string activeChapterId;
        private Vector3 startPosition;
        private int startingMoney;
        private bool placementWasOccupied;
        private bool completed;
        private int firedCount;
        private int reloadCompletedCount;
        private int itemUsedCount;
        private int thrownItemCount;
        private int squadCommandCount;
        private int supportTruckPurchaseCount;
        private int stepStartFiredCount;
        private int stepStartReloadCompletedCount;
        private int stepStartItemUsedCount;
        private int stepStartThrownItemCount;
        private int stepStartSquadCommandCount;
        private int stepStartSupportTruckPurchaseCount;
        private bool placementOccupiedAtGoalArm;
        private TutorialDialoguePresenter boundPresenter;
        private bool currentGoalArmed;

        public event System.Action<string> ChapterCompleted;

        private void Awake()
        {
            ResolveReferences();
            startPosition = player != null ? player.position : Vector3.zero;
            startingMoney = playerWallet != null ? playerWallet.CurrentMoney : 0;
            FaceOperationSpeaker();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindRuntimeEvents();
        }

        private void Start()
        {
            if (autoStartOnStart)
            {
                StartChapter(defaultChapterId);
            }
        }

        private void Update()
        {
            if (completed || steps == null || stepIndex < 0 || stepIndex >= steps.Length)
            {
                return;
            }

            ResolveReferences();
            if (tutorialPlacementPoint != null && tutorialPlacementPoint.IsOccupied)
            {
                placementWasOccupied = true;
            }

            EnsureAcknowledgeInputContext();
            HideInstructionDialogueIfRequested();

            if (IsCurrentStepComplete())
            {
                AdvanceStep();
            }
        }

        private bool IsCurrentStepComplete()
        {
            TutorialStep step = steps[stepIndex];
            switch (step.goal)
            {
                case StepGoal.Acknowledge:
                    if (presenter == null || !presenter.HasContinueRequest)
                    {
                        return false;
                    }

                    if (!UiInputCoordinator.Instance.TryConsumeContextInput(this))
                    {
                        return false;
                    }

                    return presenter.ConsumeContinueRequest();

                case StepGoal.MoveFromStart:
                    if (!currentGoalArmed)
                    {
                        return false;
                    }

                    return player != null
                        && Vector3.Distance(startPosition, player.position) >= Mathf.Max(0.1f, moveDistance);

                case StepGoal.BuildAtPlacementPoint:
                    if (!currentGoalArmed)
                    {
                        return false;
                    }

                    return !placementOccupiedAtGoalArm
                        && tutorialPlacementPoint != null
                        && tutorialPlacementPoint.IsOccupied;

                case StepGoal.UpgradeInstalledObject:
                    if (!currentGoalArmed)
                    {
                        return false;
                    }

                    return TryGetUpgradeProvider(out IInstalledUpgradeLevelProvider provider)
                        && provider.CurrentUpgradeLevel > 0;

                case StepGoal.DismantleInstalledObject:
                    if (!currentGoalArmed)
                    {
                        return false;
                    }

                    return placementOccupiedAtGoalArm
                        && tutorialPlacementPoint != null
                        && !tutorialPlacementPoint.IsOccupied;

                case StepGoal.SpendMoney:
                    if (!currentGoalArmed)
                    {
                        return false;
                    }

                    return supportTruckPurchaseCount > stepStartSupportTruckPurchaseCount
                        || playerWallet != null && playerWallet.CurrentMoney < startingMoney;

                case StepGoal.FireWeapon:
                    if (!currentGoalArmed)
                    {
                        return false;
                    }

                    return firedCount > stepStartFiredCount;

                case StepGoal.ReloadWeapon:
                    if (!currentGoalArmed)
                    {
                        return false;
                    }

                    return reloadCompletedCount > stepStartReloadCompletedCount;

                case StepGoal.UseItem:
                    if (!currentGoalArmed)
                    {
                        return false;
                    }

                    return itemUsedCount > stepStartItemUsedCount;

                case StepGoal.ThrowItem:
                    if (!currentGoalArmed)
                    {
                        return false;
                    }

                    return thrownItemCount > stepStartThrownItemCount;

                case StepGoal.IssueSquadCommand:
                    if (!currentGoalArmed)
                    {
                        return false;
                    }

                    return squadCommandCount > stepStartSquadCommandCount;

                default:
                    return false;
            }
        }

        private void AdvanceStep()
        {
            TutorialStep completedStep = steps != null && stepIndex >= 0 && stepIndex < steps.Length
                ? steps[stepIndex]
                : null;
            CloseBlockingUiForCompletedStep(completedStep);
            stepIndex++;
            ShowCurrentStep();
        }

        private void CloseBlockingUiForCompletedStep(TutorialStep completedStep)
        {
            if (completedStep == null)
            {
                return;
            }

            if (completedStep.goal == StepGoal.SpendMoney && supportTruckShopPresenter != null)
            {
                supportTruckShopPresenter.Hide();
            }
        }

        private void ShowCurrentStep()
        {
            ResolveReferences();
            if (steps == null || stepIndex < 0 || stepIndex >= steps.Length || !IsStepInActiveChapter(steps[stepIndex]))
            {
                UiInputCoordinator.EndContextIfActive(this);
                completed = true;
                presenter?.Show("오퍼레이션", "챕터 완료. 다음 훈련을 선택하거나 이어서 이동하세요.", "", false);
                ChapterCompleted?.Invoke(activeChapterId);
                return;
            }

            TutorialStep step = steps[stepIndex];
            bool waitsForContinue = step.goal == StepGoal.Acknowledge;
            currentGoalArmed = waitsForContinue;
            presenter?.Show(step.speaker, step.line, step.hint, waitsForContinue);
            UiInputCoordinator.Instance?.TryBeginContext(this, UiInputContext.TutorialDialogue, true);

            if (step.goal == StepGoal.SpendMoney && playerWallet != null)
            {
                startingMoney = playerWallet.CurrentMoney;
            }

            if (waitsForContinue)
            {
                CaptureStepGoalBaselines();
            }
        }

        private void HideInstructionDialogueIfRequested()
        {
            if (presenter == null || steps == null || stepIndex < 0 || stepIndex >= steps.Length)
            {
                return;
            }

            TutorialStep step = steps[stepIndex];
            if (step == null || step.goal == StepGoal.Acknowledge)
            {
                return;
            }

            if (presenter.ConsumeDismissRequest())
            {
                currentGoalArmed = true;
                CaptureStepGoalBaselines();
                presenter.HideUntilNextStep();
                UiInputCoordinator.EndContextIfActive(this);
            }
        }

        private void EnsureAcknowledgeInputContext()
        {
            if (steps == null || stepIndex < 0 || stepIndex >= steps.Length)
            {
                return;
            }

            TutorialStep step = steps[stepIndex];
            if (step == null || step.goal != StepGoal.Acknowledge)
            {
                return;
            }

            UiInputCoordinator.Instance?.TryBeginContext(this, UiInputContext.TutorialDialogue, true);
        }

        private void OnDisable()
        {
            UiInputCoordinator.EndContextIfActive(this);
            UnbindRuntimeEvents();
            UnbindPresenterEvents();
        }

        public void StartChapter(string chapterId)
        {
            ResolveReferences();
            BindRuntimeEvents();

            if (steps == null || steps.Length == 0)
            {
                Debug.LogError("[TutorialDialogueStepController] Steps are not configured.", this);
                return;
            }

            int firstStepIndex = FindFirstStepIndex(chapterId);
            if (firstStepIndex < 0)
            {
                Debug.LogError("[TutorialDialogueStepController] Missing chapter steps: " + chapterId, this);
                return;
            }

            activeChapterId = chapterId;
            stepIndex = firstStepIndex;
            completed = false;
            startPosition = player != null ? player.position : Vector3.zero;
            startingMoney = playerWallet != null ? playerWallet.CurrentMoney : 0;
            placementWasOccupied = tutorialPlacementPoint != null && tutorialPlacementPoint.IsOccupied;
            ShowCurrentStep();
        }

        public void HideDialogue()
        {
            presenter?.Hide();
            UiInputCoordinator.EndContextIfActive(this);
        }

        private bool TryGetUpgradeProvider(out IInstalledUpgradeLevelProvider provider)
        {
            provider = null;
            if (tutorialPlacementPoint == null || tutorialPlacementPoint.PlacedObject == null)
            {
                return false;
            }

            MonoBehaviour[] behaviours = tutorialPlacementPoint.PlacedObject.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInstalledUpgradeLevelProvider found)
                {
                    provider = found;
                    return true;
                }
            }

            return false;
        }

        private void ResolveReferences()
        {
            if (presenter == null)
            {
                presenter = FindFirstObjectByType<TutorialDialoguePresenter>(FindObjectsInactive.Include);
            }

            if (tutorialPlacementPoint == null)
            {
                tutorialPlacementPoint = FindFirstObjectByType<PlacementPoint>(FindObjectsInactive.Include);
            }

            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    player = playerObject.transform;
                }
            }

            if (playerSpeaker == null)
            {
                playerSpeaker = player;
            }

            if (operationSpeaker == null)
            {
                GameObject operationAnchor = GameObject.Find("OperationDialogueAnchor");
                if (operationAnchor != null)
                {
                    operationSpeaker = operationAnchor.transform;
                }
            }

            if (playerWallet == null && player != null)
            {
                playerWallet = player.GetComponentInParent<PlayerCurrencyWallet>();
                if (playerWallet == null)
                {
                    playerWallet = player.GetComponentInChildren<PlayerCurrencyWallet>(true);
                }
            }

            if (projectileLauncher == null)
            {
                projectileLauncher = FindFirstObjectByType<PlayerProjectileLauncher>(FindObjectsInactive.Include);
            }

            if (weaponRuntime == null)
            {
                weaponRuntime = FindFirstObjectByType<PlayerWeaponRuntime>(FindObjectsInactive.Include);
            }

            if (itemUseController == null)
            {
                itemUseController = FindFirstObjectByType<PlayerItemUseController>(FindObjectsInactive.Include);
            }

            if (throwableItemController == null)
            {
                throwableItemController = FindFirstObjectByType<PlayerThrowableItemController>(FindObjectsInactive.Include);
            }

            if (squadCommandController == null)
            {
                squadCommandController = FindFirstObjectByType<PlayerSquadCommandController>(FindObjectsInactive.Include);
            }

            if (supportTruckShopPresenter == null)
            {
                supportTruckShopPresenter = FindFirstObjectByType<SupportTruckShopPresenter>(FindObjectsInactive.Include);
            }
        }

        private void CaptureStepGoalBaselines()
        {
            stepStartFiredCount = firedCount;
            stepStartReloadCompletedCount = reloadCompletedCount;
            stepStartItemUsedCount = itemUsedCount;
            stepStartThrownItemCount = thrownItemCount;
            stepStartSquadCommandCount = squadCommandCount;
            stepStartSupportTruckPurchaseCount = supportTruckPurchaseCount;
            placementOccupiedAtGoalArm = tutorialPlacementPoint != null && tutorialPlacementPoint.IsOccupied;
        }

        private int FindFirstStepIndex(string chapterId)
        {
            string resolvedChapterId = string.IsNullOrWhiteSpace(chapterId) ? defaultChapterId : chapterId;
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] != null && string.Equals(steps[i].chapterId, resolvedChapterId, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsStepInActiveChapter(TutorialStep step)
        {
            return step != null
                && string.Equals(step.chapterId, activeChapterId, System.StringComparison.Ordinal);
        }

        private void BindRuntimeEvents()
        {
            UnbindRuntimeEvents();
            BindPresenterEvents();

            if (projectileLauncher != null)
            {
                projectileLauncher.Fired += HandleWeaponFired;
            }

            if (weaponRuntime != null)
            {
                weaponRuntime.ReloadCompleted += HandleReloadCompleted;
            }

            if (itemUseController != null)
            {
                itemUseController.ItemUsed += HandleItemUsed;
            }

            if (throwableItemController != null)
            {
                throwableItemController.ThrowCommitted += HandleThrowableCommitted;
            }

            if (squadCommandController != null)
            {
                squadCommandController.CommandIssued += HandleSquadCommandIssued;
            }

            if (supportTruckShopPresenter != null)
            {
                supportTruckShopPresenter.OfferPurchased += HandleSupportTruckOfferPurchased;
            }
        }

        private void UnbindRuntimeEvents()
        {
            if (projectileLauncher != null)
            {
                projectileLauncher.Fired -= HandleWeaponFired;
            }

            if (weaponRuntime != null)
            {
                weaponRuntime.ReloadCompleted -= HandleReloadCompleted;
            }

            if (itemUseController != null)
            {
                itemUseController.ItemUsed -= HandleItemUsed;
            }

            if (throwableItemController != null)
            {
                throwableItemController.ThrowCommitted -= HandleThrowableCommitted;
            }

            if (squadCommandController != null)
            {
                squadCommandController.CommandIssued -= HandleSquadCommandIssued;
            }

            if (supportTruckShopPresenter != null)
            {
                supportTruckShopPresenter.OfferPurchased -= HandleSupportTruckOfferPurchased;
            }
        }

        private void BindPresenterEvents()
        {
            if (boundPresenter == presenter)
            {
                return;
            }

            UnbindPresenterEvents();
            boundPresenter = presenter;
            if (boundPresenter != null)
            {
                boundPresenter.PreviousRequested += HandlePreviousRequested;
            }
        }

        private void UnbindPresenterEvents()
        {
            if (boundPresenter != null)
            {
                boundPresenter.PreviousRequested -= HandlePreviousRequested;
                boundPresenter = null;
            }
        }

        private void HandlePreviousRequested()
        {
            if (completed || steps == null || stepIndex <= 0)
            {
                return;
            }

            int previousIndex = stepIndex - 1;
            while (previousIndex >= 0 && !IsStepInActiveChapter(steps[previousIndex]))
            {
                previousIndex--;
            }

            if (previousIndex < 0)
            {
                return;
            }

            stepIndex = previousIndex;
            ShowCurrentStep();
        }

        private void HandleWeaponFired()
        {
            firedCount++;
        }

        private void HandleReloadCompleted(WeaponRuntimeState weaponState)
        {
            reloadCompletedCount++;
        }

        private void HandleItemUsed(ItemDefinitionSO itemDefinition)
        {
            itemUsedCount++;
        }

        private void HandleThrowableCommitted(ItemDefinitionSO itemDefinition)
        {
            thrownItemCount++;
        }

        private void HandleSquadCommandIssued(PlayerSquadCommandType commandType)
        {
            squadCommandCount++;
        }

        private void HandleSupportTruckOfferPurchased(SupportTruckShopOfferEntry offer)
        {
            supportTruckPurchaseCount++;
        }

        private void FaceOperationSpeaker()
        {
            if (!faceOperationOnStart || playerSpeaker == null || operationSpeaker == null)
            {
                return;
            }

            Vector3 direction = operationSpeaker.position - playerSpeaker.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            playerSpeaker.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
