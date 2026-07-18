using System;
using DG.Tweening;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class DotweenUiPanelTransition : MonoBehaviour, IUiPanelTransition
    {
        [Header("References")]
        [SerializeField] private GameObject activationRoot;
        [SerializeField] private RectTransform motionRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Effects")]
        [SerializeField] private bool useFade = true;
        [SerializeField] private bool useScale = true;
        [SerializeField] private bool useHorizontalOffset;
        [SerializeField] private bool useVerticalOffset;
        [SerializeField] private bool playShowOnEnable;
        [SerializeField] private bool manageCanvasInteraction = true;
        [SerializeField, Range(0.01f, 1f)] private float hiddenScaleMultiplier = 0.96f;
        [SerializeField] private float hiddenHorizontalOffset = -18f;
        [SerializeField] private float hiddenVerticalOffset = -18f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float showDuration = 0.16f;
        [SerializeField, Min(0f)] private float hideDuration = 0.12f;
        [SerializeField] private Ease showEase = Ease.OutCubic;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private Sequence activeSequence;
        private Vector3 authoredScale;
        private Vector2 authoredAnchoredPosition;
        private bool hasCapturedAuthoredState;
        private bool missingReferenceLogged;
        private bool isVisible;

        public bool IsVisible => isVisible;
        public bool IsTransitioning => activeSequence != null && activeSequence.IsActive();
        public GameObject ActivationRoot => activationRoot;
        public RectTransform MotionRoot => motionRoot;
        public CanvasGroup CanvasGroup => canvasGroup;

        private void Awake()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            isVisible = activationRoot.activeSelf;
            SetCanvasInteraction(isVisible);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || !playShowOnEnable)
            {
                return;
            }

            ShowInternal(true, null);
        }

        private void LateUpdate()
        {
            if (!isVisible && activationRoot != null && activationRoot.activeSelf && !IsTransitioning)
            {
                ApplyHiddenVisualState();
                SetCanvasInteraction(false);
                activationRoot.SetActive(false);
            }
        }

        private void OnDisable()
        {
            KillActiveSequence();
            if (activationRoot != null && !activationRoot.activeSelf)
            {
                isVisible = false;
            }
        }

        private void OnDestroy()
        {
            KillActiveSequence();
        }

        public void Show(Action onComplete = null)
        {
            ShowInternal(false, onComplete);
        }

        public void Configure(
            GameObject configuredActivationRoot,
            RectTransform configuredMotionRoot,
            CanvasGroup configuredCanvasGroup,
            bool autoShowOnEnable = false,
            bool controlCanvasInteraction = true)
        {
            KillActiveSequence();
            activationRoot = configuredActivationRoot;
            motionRoot = configuredMotionRoot;
            canvasGroup = configuredCanvasGroup;
            playShowOnEnable = autoShowOnEnable;
            manageCanvasInteraction = controlCanvasInteraction;
            hasCapturedAuthoredState = false;
            missingReferenceLogged = false;
            EnsureInitialized();
        }

        public void ConfigureMotion(
            bool fade,
            bool scale,
            bool horizontalOffset,
            bool verticalOffset,
            float hiddenScale,
            float horizontalPixels,
            float verticalPixels,
            float configuredShowDuration,
            float configuredHideDuration)
        {
            useFade = fade;
            useScale = scale;
            useHorizontalOffset = horizontalOffset;
            useVerticalOffset = verticalOffset;
            hiddenScaleMultiplier = Mathf.Clamp(hiddenScale, 0.01f, 1f);
            hiddenHorizontalOffset = horizontalPixels;
            hiddenVerticalOffset = verticalPixels;
            showDuration = Mathf.Max(0f, configuredShowDuration);
            hideDuration = Mathf.Max(0f, configuredHideDuration);
        }

        private void ShowInternal(bool forceHiddenStart, Action onComplete)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            bool wasActive = activationRoot.activeSelf;
            KillActiveSequence();
            activationRoot.SetActive(true);
            isVisible = true;
            if (forceHiddenStart || !wasActive)
            {
                ApplyHiddenVisualState();
            }

            SetCanvasInteraction(false);

            float duration = Mathf.Max(0f, showDuration);
            if (duration <= 0f || !HasAnimatedEffect())
            {
                ApplyShownVisualState();
                onComplete?.Invoke();
                return;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            if (useFade)
            {
                sequence.Join(canvasGroup.DOFade(1f, duration).SetEase(showEase));
            }

            if (useScale)
            {
                sequence.Join(motionRoot.DOScale(authoredScale, duration).SetEase(showEase));
            }

            if (useHorizontalOffset || useVerticalOffset)
            {
                sequence.Join(motionRoot.DOAnchorPos(authoredAnchoredPosition, duration).SetEase(showEase));
            }

            activeSequence = sequence;
            sequence.OnComplete(() =>
            {
                activeSequence = null;
                ApplyShownVisualState();
                onComplete?.Invoke();
            });
        }

        public void Hide(Action onComplete = null)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            KillActiveSequence();
            isVisible = false;
            SetCanvasInteraction(false);

            if (!activationRoot.activeSelf)
            {
                ApplyHiddenVisualState();
                onComplete?.Invoke();
                return;
            }

            float duration = Mathf.Max(0f, hideDuration);
            if (duration <= 0f || !HasAnimatedEffect())
            {
                CompleteHide(onComplete);
                return;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            if (useFade)
            {
                sequence.Join(canvasGroup.DOFade(0f, duration).SetEase(hideEase));
            }

            if (useScale)
            {
                sequence.Join(motionRoot.DOScale(GetHiddenScale(), duration).SetEase(hideEase));
            }

            if (useHorizontalOffset || useVerticalOffset)
            {
                sequence.Join(motionRoot.DOAnchorPos(GetHiddenAnchoredPosition(), duration).SetEase(hideEase));
            }

            activeSequence = sequence;
            sequence.OnComplete(() =>
            {
                activeSequence = null;
                CompleteHide(onComplete);
            });
        }

        public void HideImmediate()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            KillActiveSequence();
            isVisible = false;
            ApplyHiddenVisualState();
            SetCanvasInteraction(false);
            activationRoot.SetActive(false);
        }

        private bool EnsureInitialized()
        {
            if (activationRoot == null || motionRoot == null || canvasGroup == null)
            {
                if (!missingReferenceLogged)
                {
                    Debug.LogError(
                        "[DotweenUiPanelTransition] ActivationRoot, MotionRoot, and CanvasGroup must be assigned in the Inspector.",
                        this);
                    missingReferenceLogged = true;
                }

                return false;
            }

            missingReferenceLogged = false;
            if (!hasCapturedAuthoredState)
            {
                authoredScale = motionRoot.localScale;
                authoredAnchoredPosition = motionRoot.anchoredPosition;
                hasCapturedAuthoredState = true;
            }

            return true;
        }

        private void CompleteHide(Action onComplete)
        {
            ApplyHiddenVisualState();
            SetCanvasInteraction(false);
            activationRoot.SetActive(false);
            onComplete?.Invoke();
        }

        private void ApplyShownVisualState()
        {
            canvasGroup.alpha = 1f;
            motionRoot.localScale = authoredScale;
            motionRoot.anchoredPosition = authoredAnchoredPosition;
            SetCanvasInteraction(true);
            isVisible = true;
        }

        private void ApplyHiddenVisualState()
        {
            canvasGroup.alpha = useFade ? 0f : 1f;
            motionRoot.localScale = useScale ? GetHiddenScale() : authoredScale;
            motionRoot.anchoredPosition = useHorizontalOffset || useVerticalOffset
                ? GetHiddenAnchoredPosition()
                : authoredAnchoredPosition;
        }

        private Vector3 GetHiddenScale()
        {
            float multiplier = Mathf.Clamp(hiddenScaleMultiplier, 0.01f, 1f);
            return new Vector3(
                authoredScale.x * multiplier,
                authoredScale.y * multiplier,
                authoredScale.z * multiplier);
        }

        private Vector2 GetHiddenAnchoredPosition()
        {
            return authoredAnchoredPosition
                + Vector2.right * (useHorizontalOffset ? hiddenHorizontalOffset : 0f)
                + Vector2.up * (useVerticalOffset ? hiddenVerticalOffset : 0f);
        }

        private bool HasAnimatedEffect()
        {
            return useFade || useScale || useHorizontalOffset || useVerticalOffset;
        }

        private void SetCanvasInteraction(bool enabled)
        {
            if (!manageCanvasInteraction)
            {
                return;
            }

            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }

        private void KillActiveSequence()
        {
            if (activeSequence == null)
            {
                return;
            }

            if (activeSequence.IsActive())
            {
                activeSequence.Kill(false);
            }

            activeSequence = null;

            if (!isVisible && activationRoot != null && activationRoot.activeSelf)
            {
                ApplyHiddenVisualState();
                SetCanvasInteraction(false);
                activationRoot.SetActive(false);
            }
        }
    }
}
