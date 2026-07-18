using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class InstalledObjectAimInfoPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private DotweenUiPanelTransition panelTransition;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text statText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, 28f);

        private bool missingReferenceLogged;

        private void Awake()
        {
            HideImmediate();
        }

        public void Show(InstalledAimInfo info)
        {
            Show(info, null, null);
        }

        public void Show(InstalledAimInfo info, Camera worldCamera, Transform target)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (!HasRequiredUi())
            {
                Hide();
                return;
            }

            titleText.text = info.Title;
            levelText.text = info.LevelText;
            statText.text = info.StatText;
            healthText.text = info.HealthText;
            healthFillImage.fillAmount = Mathf.Clamp01(info.HealthFillAmount);
            UpdatePosition(worldCamera, target);
            if (!panelRoot.activeSelf)
            {
                if (panelTransition != null)
                {
                    panelTransition.Show();
                }
                else
                {
                    panelRoot.SetActive(true);
                }
            }
        }

        public void Hide()
        {
            if (panelTransition != null && panelRoot != null && panelRoot.activeSelf)
            {
                panelTransition.Hide();
            }
            else if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void HideImmediate()
        {
            if (panelTransition != null)
            {
                panelTransition.HideImmediate();
            }
            else if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private bool HasRequiredUi()
        {
            if (panelRoot != null
                && titleText != null
                && levelText != null
                && statText != null
                && healthText != null
                && healthFillImage != null)
            {
                return true;
            }

            LogMissingReference("UI reference");
            return false;
        }

        private void LogMissingReference(string missingName)
        {
            if (missingReferenceLogged)
            {
                return;
            }

            Debug.LogError($"[InstalledObjectAimInfoPresenter] Missing reference: {missingName}.", this);
            missingReferenceLogged = true;
        }

        private void UpdatePosition(Camera worldCamera, Transform target)
        {
            if (worldCamera == null || target == null || panelRoot == null)
            {
                return;
            }

            RectTransform panelTransform = panelRoot.transform as RectTransform;
            RectTransform canvasTransform = GetComponentInParent<Canvas>()?.transform as RectTransform;
            if (panelTransform == null || canvasTransform == null)
            {
                return;
            }

            Vector3 worldPosition = ResolveTopWorldPosition(target);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasTransform,
                    screenPoint,
                    ResolveCanvasCamera(canvasTransform),
                    out Vector2 localPoint))
            {
                return;
            }

            panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panelTransform.pivot = new Vector2(0.5f, 0f);
            panelTransform.anchoredPosition = localPoint + screenOffset;
        }

        private static Vector3 ResolveTopWorldPosition(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return target.position + Vector3.up * 2f;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        private static Camera ResolveCanvasCamera(RectTransform canvasTransform)
        {
            Canvas canvas = canvasTransform.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
    }
}
