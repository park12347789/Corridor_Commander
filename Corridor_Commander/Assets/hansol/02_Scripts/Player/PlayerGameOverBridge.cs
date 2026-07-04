using System.Collections;
using CorridorCommander.PlayerCombat;
using UnityEngine;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerGameOverBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private PlayerCentralInputController inputController;
        [SerializeField] private PlayerLocomotionController locomotionController;
        [SerializeField] private PlayerFacingController facingController;
        [SerializeField] private PlayerProjectileLauncher weaponLauncher;

        [Header("Game Over")]
        [SerializeField, Min(0f)] private float gameOverDelay = 1.25f;
        [SerializeField] private string gameOverReason = "Player Defeated";

        private Coroutine gameOverRoutine;

        public bool HasTriggeredGameOver { get; private set; }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (gameOverRoutine != null)
            {
                StopCoroutine(gameOverRoutine);
                gameOverRoutine = null;
            }
        }

        private void Subscribe()
        {
            if (health == null)
            {
                Debug.LogError("[PlayerGameOverBridge] Health reference is missing.", this);
                return;
            }

            health.Died -= HandlePlayerDied;
            health.Died += HandlePlayerDied;
        }

        private void Unsubscribe()
        {
            if (health != null)
            {
                health.Died -= HandlePlayerDied;
            }
        }

        private void HandlePlayerDied(Health deadHealth)
        {
            if (HasTriggeredGameOver || gameOverRoutine != null)
            {
                return;
            }

            LockPlayerControl();
            gameOverRoutine = StartCoroutine(TriggerGameOverAfterDelay());
        }

        private void LockPlayerControl()
        {
            locomotionController?.ClearMoveInput();
            facingController?.ClearCombatInput();
            weaponLauncher?.ClearFireInput();

            SetEnabled(inputController, false);
            SetEnabled(locomotionController, false);
            SetEnabled(facingController, false);
            SetEnabled(weaponLauncher, false);

            Debug.Log("[PlayerGameOverBridge] Player controls locked.", this);
        }

        private IEnumerator TriggerGameOverAfterDelay()
        {
            if (gameOverDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(gameOverDelay);
            }

            gameOverRoutine = null;

            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            }

            if (gameManager == null)
            {
                Debug.LogError("[PlayerGameOverBridge] GameManager was not found.", this);
                yield break;
            }

            HasTriggeredGameOver = true;
            gameManager.TriggerGameOver(gameOverReason);
        }

        private void ResolveReferences()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health == null)
            {
                health = GetComponentInChildren<Health>(true);
            }

            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (inputController == null)
            {
                inputController = GetComponentInChildren<PlayerCentralInputController>(true);
            }

            if (locomotionController == null)
            {
                locomotionController = GetComponent<PlayerLocomotionController>();
            }

            if (locomotionController == null)
            {
                locomotionController = GetComponentInChildren<PlayerLocomotionController>(true);
            }

            if (facingController == null)
            {
                facingController = GetComponent<PlayerFacingController>();
            }

            if (facingController == null)
            {
                facingController = GetComponentInChildren<PlayerFacingController>(true);
            }

            if (weaponLauncher == null)
            {
                weaponLauncher = GetComponentInChildren<PlayerProjectileLauncher>(true);
            }
        }

        private static void SetEnabled(Behaviour behaviour, bool enabled)
        {
            if (behaviour != null)
            {
                behaviour.enabled = enabled;
            }
        }
    }
}

/*
Unity setup outline:
1. Add PlayerGameOverBridge to the Player root in PlayerSetup.
2. Assign the player's Health and control components, or leave them empty for automatic binding.
3. Keep GameOverRoot connected to the existing GameOverScreenPresenter on MainCanvas.
4. Adjust Game Over Delay to match the visible part of the death animation.
5. The bridge calls GameManager.TriggerGameOver without modifying the existing hansol scripts.
*/
