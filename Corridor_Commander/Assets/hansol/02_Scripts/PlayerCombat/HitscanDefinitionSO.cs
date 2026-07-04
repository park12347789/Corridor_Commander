using System.Collections;
using UnityEngine;

namespace CorridorCommander.PlayerCombat
{
    public enum BeamVfxStretchAxis
    {
        X,
        Y,
        Z
    }

    [CreateAssetMenu(
        menuName = "Corridor Commander/Combat/Hitscan Definition",
        fileName = "HitscanDefinition"
    )]
    public sealed class HitscanDefinitionSO : ScriptableObject
    {
        [Header("Basic")]
        public float damage = 10f;
        public float range = 50f;

        [Header("Collision")]
        public LayerMask hitLayers = ~0;

        [Header("Splash Damage")]
        public bool useSplashDamage = false;
        public float splashRadius = 2f;
        public float splashDamage = 10f;

        [Header("VFX")]
        public GameObject beamVfxPrefab;
        public GameObject hitVfxPrefab;
        public float beamVisibleTime = 0.05f;
        [Tooltip("Euler offset applied after aligning the beam prefab +Z axis to the actual hitscan ray.")]
        public Vector3 beamVfxRotationOffset;

        [Header("Continuous Beam VFX")]
        [SerializeField] private bool stretchBeamVfxToHitPoint;
        [SerializeField] [Min(0.01f)] private float beamVfxReferenceLength = 1f;
        [SerializeField] private BeamVfxStretchAxis beamVfxStretchAxis = BeamVfxStretchAxis.Z;
        [SerializeField] private string beamVfxStretchTransformName = "position";
        [SerializeField] private string beamVfxStretchChildNameContains = "line";

        [Header("Tracer Flight VFX")]
        [SerializeField] private bool moveBeamVfxToHitPoint;
        [SerializeField] [Min(0.1f)] private float beamVfxTravelSpeed = 90f;
        [SerializeField] [Min(0.05f)] private float beamVfxMovingSegmentLength = 1.4f;
        [SerializeField] [Min(0.01f)] private float beamVfxMovingLineWidth = 0.055f;

        public bool StretchBeamVfxToHitPoint => stretchBeamVfxToHitPoint;
        public float BeamVfxReferenceLength => Mathf.Max(0.01f, beamVfxReferenceLength);
        public BeamVfxStretchAxis BeamVfxStretchAxis => beamVfxStretchAxis;
        public string BeamVfxStretchTransformName => beamVfxStretchTransformName;
        public string BeamVfxStretchChildNameContains => beamVfxStretchChildNameContains;
        public bool MoveBeamVfxToHitPoint => moveBeamVfxToHitPoint;
        public float BeamVfxTravelSpeed => Mathf.Max(0.1f, beamVfxTravelSpeed);
        public float BeamVfxMovingSegmentLength => Mathf.Max(0.05f, beamVfxMovingSegmentLength);
        public float BeamVfxMovingLineWidth => Mathf.Max(0.01f, beamVfxMovingLineWidth);

        [Header("Debug")]
        public bool drawDebugRay = true;
    }

    [DisallowMultipleComponent]
    public sealed class ContinuousBeamVfxRuntime : MonoBehaviour
    {
        private HitscanDefinitionSO definition;
        private Transform stretchTarget;
        private Vector3 initialStretchScale;
        private LineRenderer[] lineRenderers;
        private LineRenderer runtimeCoreLineRenderer;
        private LineRenderer runtimeGlowLineRenderer;
        private Material runtimeCoreMaterial;
        private Material runtimeGlowMaterial;
        private float beamLengthMultiplier = 1f;
        private float beamEndPadding;

        public void Initialize(
            HitscanDefinitionSO nextDefinition,
            bool createRuntimeLineRenderer = false,
            float runtimeLineWidth = 0.2f,
            float visualLengthMultiplier = 1f,
            float visualEndPadding = 0f)
        {
            definition = nextDefinition;
            beamLengthMultiplier = Mathf.Max(0.01f, visualLengthMultiplier);
            beamEndPadding = Mathf.Max(0f, visualEndPadding);
            lineRenderers = GetComponentsInChildren<LineRenderer>(true);
            stretchTarget = ResolveStretchTarget();
            initialStretchScale = stretchTarget != null ? stretchTarget.localScale : Vector3.one;

            if (createRuntimeLineRenderer && lineRenderers.Length == 0)
            {
                CreateRuntimeBeamLines(runtimeLineWidth);
            }

            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem.MainModule main = particleSystems[i].main;
                main.loop = true;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                particleSystems[i].Play(true);
            }
        }

        public void SetSegment(Vector3 startPoint, Vector3 endPoint)
        {
            if (definition == null)
            {
                return;
            }

            Vector3 segment = endPoint - startPoint;
            float distance = segment.magnitude;
            if (distance <= 0.0001f)
            {
                return;
            }

            Vector3 direction = segment / distance;
            float visualDistance = Mathf.Max(
                0.01f,
                distance * beamLengthMultiplier - beamEndPadding);
            Vector3 visualEndPoint = startPoint + direction * visualDistance;

            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up)
                * Quaternion.Euler(definition.beamVfxRotationOffset);
            transform.SetPositionAndRotation(startPoint, rotation);

            UpdateLineRenderers(startPoint, visualEndPoint);
            UpdateParticleBeamLength(visualDistance);
        }

        public float SetMovingSegment(Vector3 startPoint, Vector3 endPoint)
        {
            if (definition == null)
            {
                return 0f;
            }

            Vector3 segment = endPoint - startPoint;
            float distance = segment.magnitude;
            if (distance <= 0.0001f)
            {
                return 0f;
            }

            Vector3 direction = segment / distance;
            transform.SetPositionAndRotation(
                startPoint,
                Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(definition.beamVfxRotationOffset));

            PrepareMovingLineRenderers(definition.BeamVfxMovingLineWidth);

            float duration = Mathf.Clamp(
                distance / definition.BeamVfxTravelSpeed,
                0.035f,
                Mathf.Max(0.035f, definition.beamVisibleTime));

            StartCoroutine(MoveLineBeamVfx(
                startPoint,
                endPoint,
                duration,
                definition.BeamVfxMovingSegmentLength));
            return duration;
        }

        private void UpdateLineRenderers(Vector3 startPoint, Vector3 endPoint)
        {
            for (int i = 0; i < lineRenderers.Length; i++)
            {
                LineRenderer lineRenderer = lineRenderers[i];
                lineRenderer.positionCount = 2;

                if (lineRenderer.useWorldSpace)
                {
                    lineRenderer.SetPosition(0, startPoint);
                    lineRenderer.SetPosition(1, endPoint);
                }
                else
                {
                    lineRenderer.SetPosition(0, Vector3.zero);
                    lineRenderer.SetPosition(1, transform.InverseTransformPoint(endPoint));
                }
            }

            if (runtimeCoreLineRenderer != null)
            {
                runtimeCoreLineRenderer.SetPosition(0, startPoint);
                runtimeCoreLineRenderer.SetPosition(1, endPoint);
            }

            if (runtimeGlowLineRenderer != null)
            {
                runtimeGlowLineRenderer.SetPosition(0, startPoint);
                runtimeGlowLineRenderer.SetPosition(1, endPoint);
            }
        }

        private void PrepareMovingLineRenderers(float lineWidth)
        {
            for (int i = 0; i < lineRenderers.Length; i++)
            {
                LineRenderer lineRenderer = lineRenderers[i];
                if (lineRenderer == null)
                {
                    continue;
                }

                lineRenderer.positionCount = 2;
                lineRenderer.useWorldSpace = true;
                lineRenderer.widthMultiplier = lineWidth;
            }

            if (runtimeCoreLineRenderer != null)
            {
                runtimeCoreLineRenderer.widthMultiplier = lineWidth;
            }

            if (runtimeGlowLineRenderer != null)
            {
                runtimeGlowLineRenderer.widthMultiplier = lineWidth * 2.4f;
            }
        }

        private IEnumerator MoveLineBeamVfx(
            Vector3 startPoint,
            Vector3 endPoint,
            float duration,
            float segmentLength)
        {
            Vector3 offset = endPoint - startPoint;
            float distance = offset.magnitude;
            if (distance <= 0.0001f)
            {
                yield break;
            }

            Vector3 direction = offset / distance;
            duration = Mathf.Max(0.01f, duration);
            segmentLength = Mathf.Clamp(segmentLength, 0.05f, distance);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float headDistance = distance * t;
                float tailDistance = Mathf.Max(0f, headDistance - segmentLength);
                SetMovingLineSegment(
                    startPoint + direction * tailDistance,
                    startPoint + direction * headDistance);
                yield return null;
            }
        }

        private void SetMovingLineSegment(Vector3 tailPoint, Vector3 headPoint)
        {
            for (int i = 0; i < lineRenderers.Length; i++)
            {
                LineRenderer lineRenderer = lineRenderers[i];
                if (lineRenderer == null)
                {
                    continue;
                }

                lineRenderer.SetPosition(0, tailPoint);
                lineRenderer.SetPosition(1, headPoint);
            }

            if (runtimeCoreLineRenderer != null)
            {
                runtimeCoreLineRenderer.SetPosition(0, tailPoint);
                runtimeCoreLineRenderer.SetPosition(1, headPoint);
            }

            if (runtimeGlowLineRenderer != null)
            {
                runtimeGlowLineRenderer.SetPosition(0, tailPoint);
                runtimeGlowLineRenderer.SetPosition(1, headPoint);
            }
        }

        private void CreateRuntimeBeamLines(float width)
        {
            Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
            if (lineShader == null)
            {
                Debug.LogWarning("[ContinuousBeamVfxRuntime] A compatible unlit shader was not found.", this);
                return;
            }

            runtimeGlowMaterial = CreateAdditiveMaterial(
                lineShader,
                "RuntimeContinuousBeamGlow",
                new Color(0.05f, 0.8f, 4f, 0.22f));
            runtimeCoreMaterial = CreateAdditiveMaterial(
                lineShader,
                "RuntimeContinuousBeamCore",
                new Color(1.2f, 2.5f, 6f, 0.95f));

            float glowWidth = Mathf.Max(0.01f, width);
            runtimeGlowLineRenderer = CreateLineRenderer(
                "RuntimeContinuousBeamGlow",
                runtimeGlowMaterial,
                glowWidth);
            runtimeCoreLineRenderer = CreateLineRenderer(
                "RuntimeContinuousBeamCore",
                runtimeCoreMaterial,
                glowWidth * 0.3f);

            Debug.Log("[ContinuousBeamVfxRuntime] Layered runtime beam lines created.", this);
        }

        private LineRenderer CreateLineRenderer(string objectName, Material material, float width)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, false);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = material;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.widthMultiplier = width;
            lineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.03f, 1f),
                new Keyframe(0.97f, 1f),
                new Keyframe(1f, 0f));
            lineRenderer.numCapVertices = 4;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            return lineRenderer;
        }

        private static Material CreateAdditiveMaterial(
            Shader shader,
            string materialName,
            Color color)
        {
            Material material = new Material(shader)
            {
                name = materialName,
                renderQueue = 3000
            };

            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }

        private void OnDestroy()
        {
            if (runtimeCoreMaterial != null)
            {
                Destroy(runtimeCoreMaterial);
            }

            if (runtimeGlowMaterial != null)
            {
                Destroy(runtimeGlowMaterial);
            }
        }

        private void UpdateParticleBeamLength(float distance)
        {
            if (!definition.StretchBeamVfxToHitPoint || stretchTarget == null)
            {
                return;
            }

            float lengthScale = distance / definition.BeamVfxReferenceLength;
            Vector3 nextScale = initialStretchScale;

            switch (definition.BeamVfxStretchAxis)
            {
                case BeamVfxStretchAxis.X:
                    nextScale.x = initialStretchScale.x * lengthScale;
                    break;

                case BeamVfxStretchAxis.Y:
                    nextScale.y = initialStretchScale.y * lengthScale;
                    break;

                default:
                    nextScale.z = initialStretchScale.z * lengthScale;
                    break;
            }

            stretchTarget.localScale = nextScale;
        }

        private Transform ResolveStretchTarget()
        {
            if (definition == null)
            {
                return transform;
            }

            if (!definition.StretchBeamVfxToHitPoint)
            {
                return transform;
            }

            Transform sourceRoot = transform;
            Transform[] children = GetComponentsInChildren<Transform>(true);
            if (!string.IsNullOrWhiteSpace(definition.BeamVfxStretchTransformName))
            {
                sourceRoot = null;
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i].name == definition.BeamVfxStretchTransformName)
                    {
                        sourceRoot = children[i];
                        break;
                    }
                }
            }

            if (sourceRoot == null)
            {
                Debug.LogWarning(
                    $"[ContinuousBeamVfxRuntime] Stretch target '{definition.BeamVfxStretchTransformName}' was not found.",
                    this);
                return transform;
            }

            string childNameFilter = definition.BeamVfxStretchChildNameContains;
            if (string.IsNullOrWhiteSpace(childNameFilter))
            {
                return sourceRoot;
            }

            GameObject stretchGroupObject = new GameObject("RuntimeBeamStretchGroup");
            Transform stretchGroup = stretchGroupObject.transform;
            stretchGroup.SetParent(sourceRoot, false);

            Transform[] sourceChildren = sourceRoot.GetComponentsInChildren<Transform>(true);
            int matchedCount = 0;

            for (int i = 0; i < sourceChildren.Length; i++)
            {
                Transform candidate = sourceChildren[i];
                if (candidate == sourceRoot
                    || candidate.name.IndexOf(childNameFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                candidate.SetParent(stretchGroup, true);
                matchedCount++;
            }

            if (matchedCount > 0)
            {
                return stretchGroup;
            }

            Destroy(stretchGroupObject);
            Debug.LogWarning(
                $"[ContinuousBeamVfxRuntime] No beam child contained '{childNameFilter}'.",
                this);
            return sourceRoot;
        }
    }
}
