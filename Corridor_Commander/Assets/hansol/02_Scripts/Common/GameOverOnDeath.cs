using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class GameOverOnDeath : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private string reason = "Goal destroyed";

        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void HandleDied(Health deadHealth)
        {
            GameManager manager = gameManager != null ? gameManager : GameManager.Instance;
            if (manager != null)
            {
                manager.TriggerGameOver(reason);
                return;
            }

            Debug.LogError("[GameOverOnDeath] GameManager was not found.", this);
        }
    }
}
