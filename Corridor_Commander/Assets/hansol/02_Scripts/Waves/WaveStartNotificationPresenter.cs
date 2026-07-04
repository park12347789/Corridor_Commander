using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class WaveStartNotificationPresenter : MonoBehaviour
    {
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private GameObject root;
        [SerializeField] private Text messageText;
        [SerializeField] private TMP_Text messageTmpText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0.1f)] private float visibleDuration = 1.6f;

        private Coroutine hideRoutine;

        private void OnEnable()
        {
            if (waveDirector == null)
            {
                Debug.LogError("[WaveStartNotificationPresenter] WaveDirector is not assigned.", this);
                return;
            }

            waveDirector.WaveStarted -= HandleWaveStarted;
            waveDirector.WaveStarted += HandleWaveStarted;
            HideImmediate();
        }

        private void OnDisable()
        {
            if (waveDirector != null)
            {
                waveDirector.WaveStarted -= HandleWaveStarted;
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }
        }

        private void HandleWaveStarted(WaveStartedInfo info)
        {
            string message = info.HasBoss
                ? $"대형 좀비 등장\nWave {info.WaveNumber}  x{Mathf.Max(1, info.BossCount)}"
                : $"적습 시작\nWave {info.WaveNumber}";
            Show(message);
        }

        private void Show(string message)
        {
            if (root == null)
            {
                Debug.LogError("[WaveStartNotificationPresenter] Root is not assigned.", this);
                return;
            }

            if (messageTmpText == null && messageText == null)
            {
                Debug.LogError("[WaveStartNotificationPresenter] MessageText is not assigned.", this);
                return;
            }

            SetText(message);
            SetVisible(true);

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, visibleDuration));
            HideImmediate();
            hideRoutine = null;
        }

        private void HideImmediate()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (root == null)
            {
                return;
            }

            if (!root.activeSelf)
            {
                root.SetActive(true);
            }

            if (canvasGroup == null)
            {
                canvasGroup = root.GetComponent<CanvasGroup>();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void SetText(string message)
        {
            if (messageTmpText != null)
            {
                messageTmpText.text = message;
                return;
            }

            if (messageText != null)
            {
                messageText.text = message;
            }
        }
    }
}
