using UnityEngine;
using UnityEngine.Events;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private UnityEvent gameOver;

        public static GameManager Instance { get; private set; }
        public bool IsGameOver { get; private set; }
        public string GameOverReason { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            IsGameOver = false;
            GameOverReason = string.Empty;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void TriggerGameOver(string reason)
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            GameOverReason = string.IsNullOrWhiteSpace(reason) ? "Game Over" : reason;
            Debug.Log($"Game Over: {GameOverReason}", this);
            gameOver?.Invoke();
        }
    }
}
