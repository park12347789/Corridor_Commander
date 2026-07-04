using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class MousePositionIconPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform iconRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Vector2 pointerOffset = new Vector2(18f, -18f);
        [SerializeField] private bool hideSystemCursorWhileVisible = true;
        [SerializeField] private bool renderAbovePopupWhileVisible = true;
        [SerializeField] private int renderOrderOffset = 1000;

        private RectTransform canvasRect;
        private Canvas iconCanvas;
        private bool isVisible;
        private bool isHidingSystemCursor;
        private bool isRenderLayerOverridden;
        private bool addedIconCanvas;
        private CursorLockMode cursorLockBeforeIcon;
        private bool cursorVisibleBeforeIcon;
        private bool iconCanvasEnabledBefore;
        private bool iconCanvasOverrideBefore;
        private int iconCanvasSortingOrderBefore;

        private void Awake()
        {
            ResolveReferences();
            if (!isVisible)
            {
                SetVisible(false);
            }
        }

        private void Update()
        {
            if (!isVisible)
            {
                return;
            }

            UpdatePosition();
        }

        private void OnDisable()
        {
            RestoreRenderLayer();

            if (isHidingSystemCursor)
            {
                RestoreSystemCursor();
            }

            isVisible = false;
        }

        private void UpdatePosition()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                SetVisible(false);
                return;
            }

            if (iconRoot == null || canvasRect == null)
            {
                Debug.LogError("[MousePositionIconPresenter] Required RectTransform references are missing.", this);
                SetVisible(false);
                return;
            }

            Camera eventCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    mouse.position.ReadValue(),
                    eventCamera,
                    out Vector2 localPoint))
            {
                iconRoot.anchoredPosition = localPoint + pointerOffset;
            }
        }

        public void SetVisible(bool visible)
        {
            ResolveReferences();
            isVisible = visible;

            if (iconRoot != null)
            {
                iconRoot.gameObject.SetActive(visible);
            }

            if (visible)
            {
                HideSystemCursor();
                SetRenderOnTop(true);
                UpdatePosition();
            }
            else
            {
                SetRenderOnTop(false);
                RestoreSystemCursor();
            }
        }

        public void SetRenderOnTop(bool renderOnTop)
        {
            if (!renderAbovePopupWhileVisible)
            {
                return;
            }

            if (renderOnTop)
            {
                EnableRenderLayerOverride();
            }
            else
            {
                RestoreRenderLayer();
            }
        }

        private void HideSystemCursor()
        {
            if (!hideSystemCursorWhileVisible || isHidingSystemCursor)
            {
                return;
            }

            cursorLockBeforeIcon = Cursor.lockState;
            cursorVisibleBeforeIcon = Cursor.visible;
            isHidingSystemCursor = true;
            Cursor.visible = false;
        }

        private void RestoreSystemCursor()
        {
            if (!isHidingSystemCursor)
            {
                return;
            }

            Cursor.lockState = cursorLockBeforeIcon;
            Cursor.visible = cursorVisibleBeforeIcon;
            isHidingSystemCursor = false;
        }

        private void EnableRenderLayerOverride()
        {
            if (isRenderLayerOverridden)
            {
                ApplyRenderLayerOverride();
                return;
            }

            iconCanvas = GetComponent<Canvas>();
            if (iconCanvas == null)
            {
                iconCanvas = gameObject.AddComponent<Canvas>();
                addedIconCanvas = true;
            }

            iconCanvasEnabledBefore = iconCanvas.enabled;
            iconCanvasOverrideBefore = iconCanvas.overrideSorting;
            iconCanvasSortingOrderBefore = iconCanvas.sortingOrder;
            isRenderLayerOverridden = true;
            ApplyRenderLayerOverride();
        }

        private void ApplyRenderLayerOverride()
        {
            if (iconCanvas == null)
            {
                return;
            }

            int baseSortingOrder = targetCanvas != null ? targetCanvas.sortingOrder : 0;
            iconCanvas.enabled = true;
            iconCanvas.overrideSorting = true;
            iconCanvas.sortingOrder = baseSortingOrder + renderOrderOffset;
            transform.SetAsLastSibling();
        }

        private void RestoreRenderLayer()
        {
            if (!isRenderLayerOverridden)
            {
                return;
            }

            if (addedIconCanvas && iconCanvas != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(iconCanvas);
                }
                else
                {
                    DestroyImmediate(iconCanvas);
                }
            }
            else if (iconCanvas != null)
            {
                iconCanvas.enabled = iconCanvasEnabledBefore;
                iconCanvas.overrideSorting = iconCanvasOverrideBefore;
                iconCanvas.sortingOrder = iconCanvasSortingOrderBefore;
            }

            iconCanvas = null;
            addedIconCanvas = false;
            isRenderLayerOverridden = false;
        }

        private void ResolveReferences()
        {
            iconRoot ??= GetComponent<RectTransform>();
            iconImage ??= GetComponent<Image>();
            targetCanvas ??= GetComponentInParent<Canvas>();
            iconCanvas ??= GetComponent<Canvas>();
            canvasRect = targetCanvas != null ? targetCanvas.GetComponent<RectTransform>() : null;

            if (iconRoot == null)
            {
                Debug.LogError("[MousePositionIconPresenter] Icon root is not assigned.", this);
            }

            if (iconImage == null)
            {
                Debug.LogError("[MousePositionIconPresenter] Icon image is not assigned.", this);
            }
            else
            {
                iconImage.raycastTarget = false;
                if (iconImage.sprite == null)
                {
                    Debug.LogError("[MousePositionIconPresenter] Icon image sprite is not assigned.", this);
                }
            }

            if (targetCanvas == null || canvasRect == null)
            {
                Debug.LogError("[MousePositionIconPresenter] Target canvas is not assigned.", this);
            }
        }
    }
}
