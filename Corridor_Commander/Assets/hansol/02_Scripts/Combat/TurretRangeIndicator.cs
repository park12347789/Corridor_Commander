using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TurretRangeIndicator : MonoBehaviour, IInstalledRangeIndicator
    {
        private const string FloorName = "RangeIndicator_CyanFloor";
        private const string RingName = "RangeIndicator_CyanRing";

        [SerializeField] private MeshRenderer floorRenderer;
        [SerializeField] private MeshFilter floorMeshFilter;
        [SerializeField] private LineRenderer ringRenderer;
        [SerializeField] private Color floorColor = new Color(0.05f, 0.85f, 1f, 0.2f);
        [SerializeField] private Color ringColor = new Color(0.28f, 0.95f, 1f, 0.95f);
        [SerializeField] [Min(24)] private int segments = 128;
        [SerializeField] [Min(0.001f)] private float ringWidth = 0.14f;
        [SerializeField] private float yOffset = 0.045f;

        private Material floorMaterial;
        private Material ringMaterial;
        private Mesh floorMesh;
        private float currentRange;
        private bool missingShaderLogged;

        public float CurrentRange => currentRange;

        public void ShowRange(float range)
        {
            float radius = Mathf.Max(0f, range);
            currentRange = radius;
            ResolveVisuals();

            if (radius <= 0f || floorRenderer == null || floorMeshFilter == null || ringRenderer == null)
            {
                SetVisible(false);
                return;
            }

            DrawFloor(radius);
            DrawRing(radius);
            SetVisible(true);
        }

        public void SetRange(float range)
        {
            currentRange = Mathf.Max(0f, range);
            if ((floorRenderer != null && floorRenderer.enabled) || (ringRenderer != null && ringRenderer.enabled))
            {
                ShowRange(currentRange);
            }
        }

        public void ShowCachedRange()
        {
            ShowRange(currentRange);
        }

        public void HideRange()
        {
            SetVisible(false);
        }

        private void OnDestroy()
        {
            DestroyOwned(floorMaterial);
            DestroyOwned(ringMaterial);
            DestroyOwned(floorMesh);
        }

        private void ResolveVisuals()
        {
            ResolveFloor();
            ResolveRing();
        }

        private void ResolveFloor()
        {
            if (floorRenderer == null || floorMeshFilter == null)
            {
                Transform existing = transform.Find(FloorName);
                if (existing != null)
                {
                    floorRenderer = existing.GetComponent<MeshRenderer>();
                    floorMeshFilter = existing.GetComponent<MeshFilter>();
                }
            }

            if (floorRenderer == null || floorMeshFilter == null)
            {
                GameObject floorObject = new GameObject(FloorName);
                floorObject.transform.SetParent(transform, false);
                floorMeshFilter = floorObject.AddComponent<MeshFilter>();
                floorRenderer = floorObject.AddComponent<MeshRenderer>();
            }

            Transform floorTransform = floorRenderer.transform;
            floorTransform.position = ResolveFloorPosition();
            floorTransform.rotation = Quaternion.identity;
            floorTransform.localScale = Vector3.one;

            floorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            floorRenderer.receiveShadows = false;
            floorRenderer.sharedMaterial = GetFloorMaterial();
        }

        private void ResolveRing()
        {
            if (ringRenderer == null)
            {
                Transform existing = transform.Find(RingName);
                if (existing != null)
                {
                    ringRenderer = existing.GetComponent<LineRenderer>();
                }
            }

            if (ringRenderer == null)
            {
                GameObject ringObject = new GameObject(RingName);
                ringObject.transform.SetParent(transform, false);
                ringRenderer = ringObject.AddComponent<LineRenderer>();
            }

            Transform ringTransform = ringRenderer.transform;
            ringTransform.position = Vector3.zero;
            ringTransform.rotation = Quaternion.identity;
            ringTransform.localScale = Vector3.one;

            ringRenderer.useWorldSpace = true;
            ringRenderer.loop = true;
            ringRenderer.positionCount = Mathf.Max(24, segments);
            ringRenderer.startWidth = ringWidth;
            ringRenderer.endWidth = ringWidth;
            ringRenderer.startColor = ringColor;
            ringRenderer.endColor = ringColor;
            ringRenderer.numCornerVertices = 4;
            ringRenderer.numCapVertices = 4;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            ringRenderer.material = GetRingMaterial();
        }

        private Material GetFloorMaterial()
        {
            if (floorMaterial != null)
            {
                floorMaterial.color = floorColor;
                return floorMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                LogMissingShader();
                return null;
            }

            floorMaterial = new Material(shader)
            {
                name = "Runtime_TurretRange_CyanFloor",
                hideFlags = HideFlags.DontSave,
                color = floorColor,
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
            };
            return floorMaterial;
        }

        private Material GetRingMaterial()
        {
            if (ringMaterial != null)
            {
                ringMaterial.color = ringColor;
                return ringMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                LogMissingShader();
                return null;
            }

            ringMaterial = new Material(shader)
            {
                name = "Runtime_TurretRange_CyanRing",
                hideFlags = HideFlags.DontSave,
                color = ringColor,
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
            };
            return ringMaterial;
        }

        private void DrawFloor(float radius)
        {
            int pointCount = Mathf.Max(24, segments);
            int vertexCount = pointCount + 1;
            Vector3[] vertices = new Vector3[vertexCount];
            Color[] colors = new Color[vertexCount];
            int[] triangles = new int[pointCount * 3];

            vertices[0] = Vector3.zero;
            colors[0] = new Color(floorColor.r, floorColor.g, floorColor.b, Mathf.Clamp01(floorColor.a * 1.35f));

            for (int i = 0; i < pointCount; i++)
            {
                float angle = (Mathf.PI * 2f * i) / pointCount;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                colors[i + 1] = floorColor;

                int triangleIndex = i * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = i + 1;
                triangles[triangleIndex + 2] = i == pointCount - 1 ? 1 : i + 2;
            }

            if (floorMesh == null)
            {
                floorMesh = new Mesh
                {
                    name = "Runtime_TurretRange_CyanFloorMesh",
                    hideFlags = HideFlags.DontSave
                };
            }
            else
            {
                floorMesh.Clear();
            }

            floorMesh.vertices = vertices;
            floorMesh.colors = colors;
            floorMesh.triangles = triangles;
            floorMesh.RecalculateBounds();
            floorMeshFilter.sharedMesh = floorMesh;
            floorRenderer.transform.position = ResolveFloorPosition();
        }

        private void DrawRing(float radius)
        {
            int pointCount = Mathf.Max(24, segments);
            ringRenderer.positionCount = pointCount;
            Vector3 center = ResolveFloorPosition();

            for (int i = 0; i < pointCount; i++)
            {
                float angle = (Mathf.PI * 2f * i) / pointCount;
                Vector3 position = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + 0.01f,
                    center.z + Mathf.Sin(angle) * radius);
                ringRenderer.SetPosition(i, position);
            }
        }

        private Vector3 ResolveFloorPosition()
        {
            Vector3 position = transform.position;
            position.y += yOffset;
            return position;
        }

        private void SetVisible(bool visible)
        {
            if (floorRenderer != null)
            {
                floorRenderer.enabled = visible;
            }

            if (ringRenderer != null)
            {
                ringRenderer.enabled = visible;
            }
        }

        private void LogMissingShader()
        {
            if (missingShaderLogged)
            {
                return;
            }

            Debug.LogError("[TurretRangeIndicator] Missing required shader: Sprites/Default.", this);
            missingShaderLogged = true;
        }

        private static void DestroyOwned(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
