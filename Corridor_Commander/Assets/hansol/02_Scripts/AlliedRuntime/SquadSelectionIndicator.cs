using UnityEngine;
using UnityEngine.Rendering;

namespace CorridorCommander.PlayerUI
{
    [DisallowMultipleComponent]
    public sealed class SquadSelectionIndicator : MonoBehaviour
    {
        [Header("Ring")]
        [SerializeField, Min(0.1f)] private float radius = 0.72f;
        [SerializeField, Min(0f)] private float heightOffset = 0.08f;
        [SerializeField, Min(0.01f)] private float width = 0.07f;
        [SerializeField, Range(12, 96)] private int segmentCount = 48;

        [Header("Colors")]
        [SerializeField] private Color selectedColor = new Color(1f, 0.78f, 0.08f, 1f);
        [SerializeField] private Color allSelectedColor = new Color(0.1f, 0.58f, 1f, 1f);

        [Header("Pulse")]
        [SerializeField, Min(0f)] private float pulseAmount = 0.07f;
        [SerializeField, Min(0f)] private float pulseSpeed = 3f;

        private LineRenderer ringRenderer;
        private Material runtimeMaterial;
        private Color activeColor;
        private bool isVisible;

        public bool IsVisible => isVisible;

        private void Awake()
        {
            EnsureRenderer();
            SetSelected(false, false);
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }

        private void Update()
        {
            if (!isVisible || ringRenderer == null)
            {
                return;
            }

            float pulse = pulseAmount > 0f
                ? Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount
                : 0f;
            DrawRing(Mathf.Max(0.1f, radius + pulse));
        }

        public void SetSelected(bool selected, bool selectedAsGroup)
        {
            EnsureRenderer();
            isVisible = selected;
            activeColor = selectedAsGroup ? allSelectedColor : selectedColor;

            if (ringRenderer == null)
            {
                return;
            }

            ringRenderer.enabled = selected;
            ringRenderer.startColor = activeColor;
            ringRenderer.endColor = activeColor;

            if (selected)
            {
                DrawRing(radius);
            }
        }

        private void EnsureRenderer()
        {
            if (ringRenderer == null)
            {
                ringRenderer = GetComponent<LineRenderer>();
            }

            if (ringRenderer == null)
            {
                ringRenderer = gameObject.AddComponent<LineRenderer>();
            }

            if (runtimeMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                if (shader != null)
                {
                    runtimeMaterial = new Material(shader)
                    {
                        name = "SquadSelectionRing_Runtime"
                    };
                    ringRenderer.material = runtimeMaterial;
                }
            }

            ringRenderer.useWorldSpace = true;
            ringRenderer.loop = false;
            ringRenderer.widthMultiplier = width;
            ringRenderer.positionCount = Mathf.Max(12, segmentCount) + 1;
            ringRenderer.numCapVertices = 4;
            ringRenderer.numCornerVertices = 2;
            ringRenderer.alignment = LineAlignment.View;
            ringRenderer.textureMode = LineTextureMode.Stretch;
            ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            ringRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        private void DrawRing(float resolvedRadius)
        {
            int segments = Mathf.Max(12, segmentCount);
            if (ringRenderer.positionCount != segments + 1)
            {
                ringRenderer.positionCount = segments + 1;
            }

            Vector3 center = transform.position + Vector3.up * heightOffset;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * resolvedRadius;
                ringRenderer.SetPosition(i, center + offset);
            }
        }
    }
}
