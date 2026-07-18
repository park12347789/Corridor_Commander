using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class PopupDimOverlayController : MonoBehaviour
    {
        private static PopupDimOverlayController instance;
        private static bool missingInstanceLogged;

        [SerializeField] private Image overlayImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private MousePositionIconPresenter mouseIconPresenter;
        [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.48f;
        [SerializeField, Min(0.01f)] private float fadeInDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.14f;

        private readonly Dictionary<Object, Transform> activeOwners = new Dictionary<Object, Transform>();
        private Tween visibilityTween;
        private bool targetVisible;

        public static bool HasActivePopup
        {
            get
            {
                PopupDimOverlayController controller = instance;
                if (controller == null)
                {
                    controller = FindFirstObjectByType<PopupDimOverlayController>(FindObjectsInactive.Include);
                    if (controller != null)
                    {
                        instance = controller;
                    }
                }

                return controller != null && controller.activeOwners.Count > 0;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError("[PopupDimOverlayController] Multiple dim overlay controllers exist.", this);
                enabled = false;
                return;
            }

            instance = this;
            missingInstanceLogged = false;
            ResolveReferences();
            if (activeOwners.Count == 0)
            {
                SetVisible(false);
            }
        }

        private void OnDestroy()
        {
            KillVisibilityTween();

            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnDisable()
        {
            KillVisibilityTween();
        }

        public static void RequestShow(Object owner, Transform popupRoot)
        {
            if (owner == null)
            {
                return;
            }

            PopupDimOverlayController controller = ResolveInstance();
            if (controller == null)
            {
                if (!missingInstanceLogged)
                {
                    Debug.LogError("[PopupDimOverlayController] MainCanvas popup dim overlay is missing.");
                    missingInstanceLogged = true;
                }

                return;
            }

            controller.Show(owner, popupRoot);
        }

        public static void Release(Object owner)
        {
            if (owner == null)
            {
                return;
            }

            PopupDimOverlayController controller = ResolveInstance();
            if (controller == null)
            {
                return;
            }

            controller.Hide(owner);
        }

        private static PopupDimOverlayController ResolveInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<PopupDimOverlayController>(FindObjectsInactive.Include);
            if (instance != null)
            {
                missingInstanceLogged = false;
                instance.ResolveReferences();
            }

            return instance;
        }

        private void Show(Object owner, Transform popupRoot)
        {
            activeOwners[owner] = popupRoot;
            MoveBehindPopup(popupRoot);
            SetVisible(true);
        }

        private void Hide(Object owner)
        {
            activeOwners.Remove(owner);
            if (activeOwners.Count == 0)
            {
                SetVisible(false);
            }
        }

        private void MoveBehindPopup(Transform popupRoot)
        {
            Transform popupTopRoot = FindCanvasChildRoot(popupRoot);
            if (popupTopRoot == null || popupTopRoot == transform)
            {
                return;
            }

            popupTopRoot.SetAsLastSibling();
            transform.SetSiblingIndex(Mathf.Max(0, popupTopRoot.GetSiblingIndex() - 1));
        }

        private Transform FindCanvasChildRoot(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            Transform canvasTransform = transform.parent;
            if (canvasTransform == null)
            {
                return null;
            }

            Transform current = target;
            while (current.parent != null && current.parent != canvasTransform)
            {
                current = current.parent;
            }

            return current.parent == canvasTransform ? current : null;
        }

        private void ResolveReferences()
        {
            overlayImage ??= GetComponent<Image>();
            canvasGroup ??= GetComponent<CanvasGroup>();
            ResolveMouseIcon();

            if (overlayImage == null)
            {
                Debug.LogError("[PopupDimOverlayController] Overlay Image is not assigned.", this);
            }

            if (canvasGroup == null)
            {
                Debug.LogError("[PopupDimOverlayController] CanvasGroup is not assigned.", this);
            }
        }

        private void SetVisible(bool visible)
        {
            targetVisible = visible;
            KillVisibilityTween();

            bool wasInactive = !gameObject.activeSelf;
            if (visible && wasInactive)
            {
                gameObject.SetActive(true);
            }

            if (overlayImage != null)
            {
                Color color = overlayImage.color;
                color.a = dimAlpha;
                overlayImage.color = color;
                overlayImage.raycastTarget = visible;
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;

                if (visible)
                {
                    if (wasInactive)
                    {
                        canvasGroup.alpha = 0f;
                    }

                    visibilityTween = canvasGroup
                        .DOFade(1f, fadeInDuration)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true)
                        .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                }
                else if (gameObject.activeSelf)
                {
                    visibilityTween = canvasGroup
                        .DOFade(0f, fadeOutDuration)
                        .SetEase(Ease.InCubic)
                        .SetUpdate(true)
                        .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                        .OnComplete(CompleteHide);
                }
            }

            if (mouseIconPresenter != null)
            {
                if (visible)
                {
                    mouseIconPresenter.SetVisible(true);
                }
                else
                {
                    mouseIconPresenter.SetVisible(false);
                }
            }

            if (!visible && canvasGroup == null)
            {
                CompleteHide();
            }
        }

        private void LateUpdate()
        {
            if (!targetVisible
                && gameObject.activeSelf
                && (visibilityTween == null || !visibilityTween.IsActive()))
            {
                CompleteHide();
            }
        }

        private void CompleteHide()
        {
            visibilityTween = null;
            gameObject.SetActive(false);
            RestoreCursorPolicyAfterOverlayHidden();
        }

        private void KillVisibilityTween()
        {
            if (visibilityTween == null)
            {
                return;
            }

            visibilityTween.Kill();
            visibilityTween = null;

            if (!targetVisible && gameObject.activeSelf)
            {
                CompleteHide();
            }
        }

        private void ResolveMouseIcon()
        {
            mouseIconPresenter ??= GetComponentInChildren<MousePositionIconPresenter>(true);
        }

        private static void RestoreCursorPolicyAfterOverlayHidden()
        {
            if (UiInputCoordinator.HasActiveContext || UiInputCoordinator.PointerModeActive)
            {
                GameplayOptionsController.ApplyPointerCursorPolicy();
            }
            else
            {
                GameplayOptionsController.ApplyGameplayCursorPolicy();
            }
        }
    }
}
