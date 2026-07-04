using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TutorialLobbyButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private string sceneName = "StartMenu";

        private void OnEnable()
        {
            if (button == null)
            {
                Debug.LogError("[TutorialLobbyButton] Button is not assigned.", this);
                return;
            }

            button.onClick.RemoveListener(ReturnToLobby);
            button.onClick.AddListener(ReturnToLobby);
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(ReturnToLobby);
            }
        }

        public void ReturnToLobby()
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[TutorialLobbyButton] Lobby scene name is empty.", this);
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
