using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public sealed class DotweenUiButtonFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [SerializeField] private RectTransform motionRoot;
        [SerializeField, Range(1f, 1.15f)] private float hoverScaleMultiplier = 1.035f;
        [SerializeField, Range(0.8f, 1f)] private float pressedScaleMultiplier = 0.96f;
        [SerializeField, Min(0.01f)] private float hoverDuration = 0.1f;
        [SerializeField, Min(0.01f)] private float pressedDuration = 0.06f;
        [SerializeField] private Ease hoverEase = Ease.OutCubic;
        [SerializeField] private Ease pressedEase = Ease.OutQuad;

        private Selectable selectable;
        private Tween activeTween;
        private Vector3 authoredScale;
        private bool hasPointerFocus;
        private bool hasSelectionFocus;
        private bool isPressed;
        private bool hasCapturedScale;

        private void Awake()
        {
            ResolveReferences();
            CaptureScale();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CaptureScale();
            AnimateToCurrentState(0f);
        }

        private void OnDisable()
        {
            KillTween();
            hasPointerFocus = false;
            hasSelectionFocus = false;
            isPressed = false;
            if (motionRoot != null && hasCapturedScale)
            {
                motionRoot.localScale = authoredScale;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hasPointerFocus = true;
            AnimateToCurrentState(hoverDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hasPointerFocus = false;
            isPressed = false;
            AnimateToCurrentState(hoverDuration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            AnimateToCurrentState(pressedDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            AnimateToCurrentState(hoverDuration);
        }

        public void OnSelect(BaseEventData eventData)
        {
            hasSelectionFocus = true;
            AnimateToCurrentState(hoverDuration);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            hasSelectionFocus = false;
            isPressed = false;
            AnimateToCurrentState(hoverDuration);
        }

        private void ResolveReferences()
        {
            selectable = selectable != null ? selectable : GetComponent<Selectable>();
            motionRoot = motionRoot != null ? motionRoot : transform as RectTransform;
        }

        private void CaptureScale()
        {
            if (motionRoot == null || hasCapturedScale)
            {
                return;
            }

            authoredScale = motionRoot.localScale;
            hasCapturedScale = true;
        }

        private void AnimateToCurrentState(float duration)
        {
            if (motionRoot == null || !hasCapturedScale)
            {
                return;
            }

            bool interactable = selectable == null || selectable.IsInteractable();
            float multiplier = 1f;
            Ease ease = hoverEase;
            if (interactable && isPressed)
            {
                multiplier = pressedScaleMultiplier;
                ease = pressedEase;
            }
            else if (interactable && (hasPointerFocus || hasSelectionFocus))
            {
                multiplier = hoverScaleMultiplier;
            }

            KillTween();
            Vector3 targetScale = authoredScale * multiplier;
            if (duration <= 0f)
            {
                motionRoot.localScale = targetScale;
                return;
            }

            activeTween = motionRoot
                .DOScale(targetScale, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnKill(() => activeTween = null);
        }

        private void KillTween()
        {
            if (activeTween != null && activeTween.IsActive())
            {
                activeTween.Kill(false);
            }

            activeTween = null;
        }
    }
}
