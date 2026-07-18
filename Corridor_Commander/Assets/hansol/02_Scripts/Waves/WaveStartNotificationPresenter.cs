using DG.Tweening;
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
        [SerializeField] private RectTransform motionRoot;
        [SerializeField, Min(0.1f)] private float visibleDuration = 1.6f;
        [SerializeField, Min(0.01f)] private float showDuration = 0.14f;
        [SerializeField, Min(0.01f)] private float hideDuration = 0.2f;
        [SerializeField] private float hiddenVerticalOffset = 12f;

        private Sequence activeSequence;
        private Vector2 authoredAnchoredPosition;
        private bool hasCapturedPosition;

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

            KillSequence();
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
            ResolveMotionReferences();
            KillSequence();

            if (!root.activeSelf)
            {
                root.SetActive(true);
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (motionRoot != null)
            {
                motionRoot.anchoredPosition = authoredAnchoredPosition + Vector2.up * hiddenVerticalOffset;
            }

            activeSequence = DOTween.Sequence().SetUpdate(true).SetLink(root, LinkBehaviour.KillOnDisable);
            activeSequence.Join(canvasGroup.DOFade(1f, Mathf.Max(0.01f, showDuration)).SetEase(Ease.OutCubic));
            if (motionRoot != null)
            {
                activeSequence.Join(
                    motionRoot.DOAnchorPos(authoredAnchoredPosition, Mathf.Max(0.01f, showDuration))
                        .SetEase(Ease.OutCubic));
            }

            activeSequence.AppendInterval(Mathf.Max(0.1f, visibleDuration));
            activeSequence.Append(canvasGroup.DOFade(0f, Mathf.Max(0.01f, hideDuration)).SetEase(Ease.InCubic));
            activeSequence.OnComplete(() =>
            {
                activeSequence = null;
                HideImmediate();
            });
            activeSequence.OnKill(() => activeSequence = null);
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

        private void ResolveMotionReferences()
        {
            if (canvasGroup == null && root != null)
            {
                canvasGroup = root.GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null && root != null)
            {
                canvasGroup = root.AddComponent<CanvasGroup>();
            }

            motionRoot = motionRoot != null ? motionRoot : root != null ? root.GetComponent<RectTransform>() : null;
            if (!hasCapturedPosition && motionRoot != null)
            {
                authoredAnchoredPosition = motionRoot.anchoredPosition;
                hasCapturedPosition = true;
            }
        }

        private void KillSequence()
        {
            if (activeSequence != null && activeSequence.IsActive())
            {
                activeSequence.Kill(false);
            }

            activeSequence = null;
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
