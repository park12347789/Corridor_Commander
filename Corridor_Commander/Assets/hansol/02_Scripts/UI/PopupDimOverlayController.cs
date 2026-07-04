using System.Collections.Generic;
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

        private readonly Dictionary<Object, Transform> activeOwners = new Dictionary<Object, Transform>();

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
            if (instance == this)
            {
                instance = null;
            }
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
            if (visible && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (overlayImage != null)
            {
                Color color = overlayImage.color;
                color.a = visible ? dimAlpha : 0f;
                overlayImage.color = color;
                overlayImage.raycastTarget = visible;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
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

            if (!visible)
            {
                gameObject.SetActive(false);
                RestoreCursorPolicyAfterOverlayHidden();
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
