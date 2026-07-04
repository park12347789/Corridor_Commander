using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class InstalledObjectStatCanvasPresenter : MonoBehaviour, IInstalledObjectStatUi
    {
        [SerializeField] private Transform installedObjectRoot;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text statText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private bool positionAboveInstalledObject = true;
        [SerializeField] private float verticalOffset = 0.9f;

        private bool missingReferenceLogged;

        private void Awake()
        {
            ResolveReferences();
            Hide();
        }

        private void LateUpdate()
        {
            if (panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }

            PositionAboveObject();
        }

        public void Show(InstalledAimInfo info, Camera worldCamera)
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
            if (targetCanvas != null)
            {
                targetCanvas.enabled = true;
            }

            PositionAboveObject();
            panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (targetCanvas != null)
            {
                targetCanvas.enabled = false;
            }
        }

        private bool HasRequiredUi()
        {
            ResolveReferences();
            if (panelRoot != null
                && titleText != null
                && levelText != null
                && statText != null
                && healthText != null
                && healthFillImage != null)
            {
                return true;
            }

            if (!missingReferenceLogged)
            {
                Debug.LogError("[InstalledObjectStatCanvasPresenter] Missing UI reference.", this);
                missingReferenceLogged = true;
            }

            return false;
        }

        private void ResolveReferences()
        {
            if (targetCanvas == null)
            {
                targetCanvas = GetComponent<Canvas>();
            }

            if (installedObjectRoot == null && transform.parent != null)
            {
                installedObjectRoot = transform.parent;
            }
        }

        private void PositionAboveObject()
        {
            if (!positionAboveInstalledObject || targetCanvas == null || installedObjectRoot == null)
            {
                return;
            }

            if (!TryGetVisibleRendererBounds(out Bounds bounds))
            {
                return;
            }

            Transform canvasTransform = targetCanvas.transform;
            Vector3 rootPosition = installedObjectRoot.position;
            canvasTransform.position = new Vector3(
                rootPosition.x,
                bounds.max.y + Mathf.Max(0f, verticalOffset),
                rootPosition.z);
        }

        private bool TryGetVisibleRendererBounds(out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = installedObjectRoot.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null
                    || !targetRenderer.enabled
                    || !targetRenderer.gameObject.activeInHierarchy
                    || targetRenderer.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetRenderer.bounds);
                }
            }

            return hasBounds;
        }
    }
}
