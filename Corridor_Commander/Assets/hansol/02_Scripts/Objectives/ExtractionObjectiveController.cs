using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class ExtractionObjectiveController : MonoBehaviour
    {
        [Header("Mission")]
        [SerializeField] private Transform returnTarget;
        [SerializeField] private float returnRadius = 4f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool pauseOnClear = true;

        [Header("Spawners")]
        [SerializeField] private EnemySpawner[] spawnersToActivate;
        [SerializeField] private string spawnerActivationRootName = "map";

        [Header("UI")]
        [SerializeField] private GameObject escapeInstructionRoot;
        [SerializeField] private Text escapeInstructionText;
        [SerializeField] private string escapeInstructionMessage = "탈출 지점으로 복귀하세요";
        [SerializeField] private GameObject escapeWarningRoot;
        [SerializeField] private Text escapeWarningText;
        [SerializeField] private string escapeWarningMessage = "탈출해야 합니다";
        [SerializeField] private float escapeWarningDuration = 3f;
        [SerializeField] private GameObject clearRoot;
        [SerializeField] private Text clearText;
        [SerializeField] private MissionClearSettlementPresenter missionClearSettlementPresenter;
        [SerializeField] private string clearMessage = "MISSION CLEAR\n스타팅 포인트 복귀 완료";

        private bool escapeStarted;
        private bool cleared;
        private float escapeWarningHideTime;

        public bool EscapeStarted => escapeStarted;
        public bool Cleared => cleared;

        private void Awake()
        {
            SetUiActive(escapeInstructionRoot, false);
            SetUiActive(escapeWarningRoot, false);
            SetUiActive(clearRoot, false);
        }

        private void Update()
        {
            if (!escapeStarted || cleared)
            {
                return;
            }

            if (escapeWarningRoot != null
                && escapeWarningRoot.activeSelf
                && escapeWarningDuration > 0f
                && Time.unscaledTime >= escapeWarningHideTime)
            {
                SetUiActive(escapeWarningRoot, false);
            }

            if (IsAnyPlayerAtReturnTarget())
            {
                CompleteExtraction();
            }
        }

        public void BeginEscapeSequence(Object source)
        {
            if (escapeStarted)
            {
                return;
            }

            if (!HasRequiredReferences())
            {
                Debug.LogError("[ExtractionObjectiveController] Cannot begin escape sequence because required scene references are missing.", this);
                return;
            }

            escapeStarted = true;
            ActivateAllSpawners();
            ShowEscapeWarning();
            ShowEscapeInstruction();
            Debug.Log("[ExtractionObjectiveController] Escape sequence started.", source != null ? source : this);
        }

        private bool HasRequiredReferences()
        {
            bool valid = true;
            if (returnTarget == null)
            {
                Debug.LogError("[ExtractionObjectiveController] Return target is not assigned.", this);
                valid = false;
            }

            if (spawnersToActivate == null || spawnersToActivate.Length == 0)
            {
                Debug.LogError("[ExtractionObjectiveController] Spawners to activate are not assigned.", this);
                valid = false;
            }

            if (escapeInstructionRoot == null || escapeInstructionText == null)
            {
                Debug.LogError("[ExtractionObjectiveController] Escape instruction UI is not assigned.", this);
                valid = false;
            }

            if (escapeWarningRoot == null || escapeWarningText == null)
            {
                Debug.LogError("[ExtractionObjectiveController] Escape warning UI is not assigned.", this);
                valid = false;
            }

            if (missionClearSettlementPresenter == null)
            {
                missionClearSettlementPresenter = FindFirstObjectByType<MissionClearSettlementPresenter>(FindObjectsInactive.Include);
            }

            if (missionClearSettlementPresenter == null && (clearRoot == null || clearText == null))
            {
                Debug.LogError("[ExtractionObjectiveController] Clear UI is not assigned.", this);
                valid = false;
            }

            return valid;
        }

        private void ActivateAllSpawners()
        {
            for (int i = 0; i < spawnersToActivate.Length; i++)
            {
                EnemySpawner spawner = spawnersToActivate[i];
                if (spawner == null)
                {
                    Debug.LogError("[ExtractionObjectiveController] Spawner reference is missing.", this);
                    continue;
                }

                ActivateSpawnerHierarchy(spawner.transform);
                spawner.gameObject.SetActive(true);
                spawner.SetUpdateLoopEnabled(true);
                spawner.ResetRuntimeState();

                Behaviour behaviourAgent = spawner.GetComponent("Unity.Behavior.BehaviorGraphAgent") as Behaviour;
                if (behaviourAgent != null)
                {
                    behaviourAgent.enabled = true;
                }
            }
        }

        private void ActivateSpawnerHierarchy(Transform spawnerTransform)
        {
            Transform current = spawnerTransform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }

                if (current.name == spawnerActivationRootName)
                {
                    return;
                }

                current = current.parent;
            }
        }

        private void ShowEscapeInstruction()
        {
            escapeInstructionText.text = escapeInstructionMessage;
            SetUiActive(escapeInstructionRoot, true);
        }

        private void ShowEscapeWarning()
        {
            escapeWarningText.text = escapeWarningMessage;
            escapeWarningHideTime = Time.unscaledTime + escapeWarningDuration;
            SetUiActive(escapeWarningRoot, true);
        }

        private bool IsAnyPlayerAtReturnTarget()
        {
            if (returnTarget == null)
            {
                return false;
            }

            GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
            float radiusSqr = Mathf.Max(0f, returnRadius) * Mathf.Max(0f, returnRadius);
            for (int i = 0; i < players.Length; i++)
            {
                GameObject player = players[i];
                if (player == null)
                {
                    continue;
                }

                if (Vector3.SqrMagnitude(player.transform.position - returnTarget.position) <= radiusSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private void CompleteExtraction()
        {
            cleared = true;
            SetUiActive(escapeInstructionRoot, false);
            SetUiActive(escapeWarningRoot, false);
            if (missionClearSettlementPresenter == null)
            {
                missionClearSettlementPresenter = FindFirstObjectByType<MissionClearSettlementPresenter>(FindObjectsInactive.Include);
            }

            if (missionClearSettlementPresenter != null)
            {
                missionClearSettlementPresenter.ShowFinalSettlement(pauseOnClear);
            }
            else
            {
                clearText.text = clearMessage;
                SetUiActive(clearRoot, true);
            }

            if (pauseOnClear)
            {
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Debug.Log("[ExtractionObjectiveController] Mission clear.", this);
        }

        private static void SetUiActive(GameObject root, bool active)
        {
            if (root != null)
            {
                root.SetActive(active);
            }
        }
    }
}
