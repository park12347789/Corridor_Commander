using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class InstalledUpgradeStarDisplay : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour providerSource;
        [SerializeField] private Canvas starCanvas;
        [SerializeField] private Image[] starImages;
        [SerializeField] private bool faceMainCamera = true;
        [SerializeField] private bool positionAboveInstalledObject = true;
        [SerializeField] private float verticalOffset = 0.55f;

        private IInstalledUpgradeLevelProvider provider;

        private void Awake()
        {
            ResolveReferences();
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
            PositionAboveObject();
            FaceCamera();
        }

        private void OnValidate()
        {
            ResolveReferences();
            Refresh();
        }

        private void ResolveReferences()
        {
            if (providerSource == null)
            {
                MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IInstalledUpgradeLevelProvider)
                    {
                        providerSource = behaviours[i];
                        break;
                    }
                }
            }

            provider = providerSource as IInstalledUpgradeLevelProvider;

            Canvas namedStarCanvas = FindUpgradeStarCanvas();
            if (namedStarCanvas != null)
            {
                starCanvas = namedStarCanvas;
            }
            else if (starCanvas == null)
            {
                starCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (starImages == null || starImages.Length == 0)
            {
                starImages = starCanvas != null
                    ? starCanvas.GetComponentsInChildren<Image>(true)
                    : GetComponentsInChildren<Image>(true);
            }
        }

        private Canvas FindUpgradeStarCanvas()
        {
            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate != null && candidate.name.Contains("UpgradeStar"))
                {
                    return candidate;
                }
            }

            return null;
        }

        private void Refresh()
        {
            if (starImages == null || starImages.Length == 0)
            {
                return;
            }

            int visibleStars = provider != null ? Mathf.Clamp(provider.VisibleUpgradeStars, 0, starImages.Length) : 0;
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].gameObject.SetActive(i < visibleStars);
                }
            }

            if (starCanvas != null)
            {
                starCanvas.enabled = visibleStars > 0;
            }
        }

        private void PositionAboveObject()
        {
            if (!positionAboveInstalledObject || starCanvas == null)
            {
                return;
            }

            if (!TryGetVisibleRendererBounds(out Bounds bounds))
            {
                return;
            }

            Transform canvasTransform = starCanvas.transform;
            Vector3 rootPosition = transform.position;
            canvasTransform.position = new Vector3(
                rootPosition.x,
                bounds.max.y + Mathf.Max(0f, verticalOffset),
                rootPosition.z);
        }

        private bool TryGetVisibleRendererBounds(out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null
                    || !targetRenderer.enabled
                    || !targetRenderer.gameObject.activeInHierarchy
                    || targetRenderer.transform.IsChildOf(starCanvas.transform))
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

        private void FaceCamera()
        {
            if (!faceMainCamera || starCanvas == null || Camera.main == null)
            {
                return;
            }

            Transform canvasTransform = starCanvas.transform;
            Vector3 direction = canvasTransform.position - Camera.main.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                canvasTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }
    }
}
