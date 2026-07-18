using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class DotweenUiValueChangeFeedback : MonoBehaviour
    {
        [SerializeField] private TMP_Text observedText;
        [SerializeField] private RectTransform motionRoot;
        [SerializeField, Range(1f, 1.25f)] private float pulseScaleMultiplier = 1.08f;
        [SerializeField, Min(0.02f)] private float pulseDuration = 0.16f;

        private Sequence activeSequence;
        private Vector3 authoredScale;
        private string lastText;
        private bool hasCapturedState;

        private void Awake()
        {
            ResolveReferences();
            CaptureState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CaptureState();
            lastText = observedText != null ? observedText.text : string.Empty;
        }

        private void LateUpdate()
        {
            if (observedText == null || motionRoot == null)
            {
                return;
            }

            string currentText = observedText.text;
            if (currentText == lastText)
            {
                return;
            }

            lastText = currentText;
            PlayPulse();
        }

        private void OnDisable()
        {
            KillSequence();
            if (motionRoot != null && hasCapturedState)
            {
                motionRoot.localScale = authoredScale;
            }
        }

        private void ResolveReferences()
        {
            observedText = observedText != null ? observedText : GetComponent<TMP_Text>();
            motionRoot = motionRoot != null ? motionRoot : transform as RectTransform;
        }

        private void CaptureState()
        {
            if (motionRoot == null || hasCapturedState)
            {
                return;
            }

            authoredScale = motionRoot.localScale;
            hasCapturedState = true;
            lastText = observedText != null ? observedText.text : string.Empty;
        }

        private void PlayPulse()
        {
            KillSequence();
            float halfDuration = Mathf.Max(0.01f, pulseDuration * 0.5f);
            activeSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            activeSequence.Append(motionRoot.DOScale(authoredScale * pulseScaleMultiplier, halfDuration).SetEase(Ease.OutQuad));
            activeSequence.Append(motionRoot.DOScale(authoredScale, halfDuration).SetEase(Ease.OutCubic));
            activeSequence.OnKill(() => activeSequence = null);
            activeSequence.OnComplete(() => activeSequence = null);
        }

        private void KillSequence()
        {
            if (activeSequence != null && activeSequence.IsActive())
            {
                activeSequence.Kill(false);
            }

            activeSequence = null;
        }
    }
}
