using UnityEngine;
using UnityEngine.AI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class EnemyRouteLineVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform goalPoint;
        [SerializeField] private EnemyRoute route;
        [SerializeField] private EnemySpawner sourceSpawner;
        [SerializeField] private bool autoResolveSpawner = true;
        [SerializeField] private float heightOffset = 0.08f;
        [SerializeField] private bool animateDirection = true;
        [SerializeField] private float flowSpeed = 1.5f;
        [SerializeField] private float arrowsPerMeter = 0f;
        [SerializeField] private Material flowMaterial;
        [SerializeField] private bool autoRefresh = true;
        [SerializeField] private float refreshInterval = 0.25f;
        [SerializeField] private float sampleDistance = 2f;

        private static Texture2D fallbackArrowTexture;
        private LineRenderer lineRenderer;
        private Material runtimeMaterial;
        private NavMeshPath path;
        private float textureOffset;
        private float nextRefreshTime;
        private int walkableAreaMask = NavMesh.AllAreas;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            path = new NavMeshPath();
            ResolveWalkableAreaMask();
            ResolveSpawnerBindings();
            ConfigureLineRenderer();
            Refresh();
        }

        private void OnValidate()
        {
            if (!autoResolveSpawner)
            {
                return;
            }

            ResolveSpawnerBindings();
        }

        private void Update()
        {
            if (autoRefresh && Time.time >= nextRefreshTime)
            {
                nextRefreshTime = Time.time + Mathf.Max(0.05f, refreshInterval);
                Refresh();
            }

            if (!animateDirection || runtimeMaterial == null)
            {
                return;
            }

            textureOffset -= flowSpeed * Time.deltaTime;
            SetTextureOffset(runtimeMaterial, new Vector2(textureOffset, 0f));
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }

        public void Refresh()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                ConfigureLineRenderer();
            }

            ResolveSpawnerBindings();
            if (lineRenderer == null || startPoint == null || goalPoint == null)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.positionCount = 0;
                }

                return;
            }

            Vector3[] corners = BuildCorners();

            lineRenderer.positionCount = corners.Length;
            for (int i = 0; i < corners.Length; i++)
            {
                lineRenderer.SetPosition(i, corners[i] + Vector3.up * heightOffset);
            }

            UpdateTextureScale(corners);
        }

        private void ConfigureLineRenderer()
        {
            if (lineRenderer == null)
            {
                return;
            }

            lineRenderer.textureMode = LineTextureMode.Tile;

            if (!animateDirection)
            {
                return;
            }

            Material sourceMaterial = flowMaterial != null ? flowMaterial : lineRenderer.sharedMaterial;
            runtimeMaterial = sourceMaterial != null
                ? new Material(sourceMaterial)
                : CreateFallbackMaterial();

            if (runtimeMaterial.mainTexture == null)
            {
                runtimeMaterial.mainTexture = GetFallbackArrowTexture();
            }

            lineRenderer.material = runtimeMaterial;
        }

        private void UpdateTextureScale(Vector3[] corners)
        {
            if (runtimeMaterial == null || corners == null || corners.Length < 2)
            {
                return;
            }

            float length = 0f;
            for (int i = 1; i < corners.Length; i++)
            {
                length += Vector3.Distance(corners[i - 1], corners[i]);
            }

            Vector2 scale = new Vector2(Mathf.Max(1f, length * arrowsPerMeter), 1f);
            SetTextureScale(runtimeMaterial, scale);
        }

        private Vector3[] BuildCorners()
        {
            Vector3[] controlPoints = BuildControlPoints();
            if (controlPoints.Length < 2)
            {
                return controlPoints;
            }

            System.Collections.Generic.List<Vector3> corners = new System.Collections.Generic.List<Vector3>();
            for (int i = 1; i < controlPoints.Length; i++)
            {
                if (!TryAppendPathSegment(controlPoints[i - 1], controlPoints[i], corners))
                {
                    return new Vector3[0];
                }
            }

            return corners.Count > 0 ? corners.ToArray() : controlPoints;
        }

        private Vector3[] BuildControlPoints()
        {
            if (route != null && route.Waypoints.Count > 0)
            {
                int extraGoal = route.IncludeFinalTarget && goalPoint != null ? 1 : 0;
                Vector3[] routeCorners = new Vector3[route.Waypoints.Count + 1 + extraGoal];
                routeCorners[0] = startPoint.position;
                for (int i = 0; i < route.Waypoints.Count; i++)
                {
                    routeCorners[i + 1] = route.Waypoints[i].position;
                }

                if (extraGoal > 0)
                {
                    routeCorners[routeCorners.Length - 1] = goalPoint.position;
                }

                return routeCorners;
            }

            return new[] { startPoint.position, goalPoint.position };
        }

        private bool TryAppendPathSegment(Vector3 rawStart, Vector3 rawEnd, System.Collections.Generic.List<Vector3> corners)
        {
            if (!TrySampleNavMesh(rawStart, out Vector3 start) || !TrySampleNavMesh(rawEnd, out Vector3 end))
            {
                return false;
            }

            path ??= new NavMeshPath();
            path.ClearCorners();
            bool hasPath = NavMesh.CalculatePath(start, end, walkableAreaMask, path);
            if (!hasPath || path.status != NavMeshPathStatus.PathComplete || path.corners.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < path.corners.Length; i++)
            {
                AppendCorner(path.corners[i], corners);
            }

            return true;
        }

        private bool TrySampleNavMesh(Vector3 position, out Vector3 sampledPosition)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, sampleDistance, walkableAreaMask))
            {
                sampledPosition = hit.position;
                return true;
            }

            sampledPosition = position;
            return false;
        }

        private static void AppendCorner(Vector3 corner, System.Collections.Generic.List<Vector3> corners)
        {
            if (corners.Count > 0 && Vector3.SqrMagnitude(corners[corners.Count - 1] - corner) <= 0.0001f)
            {
                return;
            }

            corners.Add(corner);
        }

        private void ResolveWalkableAreaMask()
        {
            walkableAreaMask = NavMesh.AllAreas;
            int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");
            if (notWalkableArea >= 0)
            {
                walkableAreaMask &= ~(1 << notWalkableArea);
            }
        }

        private void ResolveSpawnerBindings()
        {
            if (!autoResolveSpawner)
            {
                return;
            }

            if (sourceSpawner == null)
            {
                sourceSpawner = GetComponentInParent<EnemySpawner>();
            }

            if (sourceSpawner == null)
            {
                return;
            }

            if (startPoint == null)
            {
                startPoint = sourceSpawner.SpawnPoint;
            }

            if (goalPoint == null)
            {
                goalPoint = sourceSpawner.Goal;
            }

            if (route == null)
            {
                route = sourceSpawner.Route;
            }
        }

        private static Material CreateFallbackMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            Material material = new Material(shader);
            material.name = "Runtime_EnemyRouteFlow";
            material.mainTexture = GetFallbackArrowTexture();
            material.color = new Color(0f, 0.9f, 1f, 0.85f);
            return material;
        }

        private static Texture2D GetFallbackArrowTexture()
        {
            if (fallbackArrowTexture != null)
            {
                return fallbackArrowTexture;
            }

            fallbackArrowTexture = new Texture2D(64, 16, TextureFormat.RGBA32, false)
            {
                name = "Runtime_EnemyRouteArrowTexture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            Color clear = new Color(1f, 1f, 1f, 0f);
            Color arrow = Color.white;
            Color[] pixels = new Color[64 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            for (int y = 6; y <= 9; y++)
            {
                for (int x = 6; x <= 38; x++)
                {
                    pixels[y * 64 + x] = arrow;
                }
            }

            for (int y = 3; y <= 12; y++)
            {
                int width = 12 - Mathf.Abs(y - 8);
                for (int x = 38; x <= 38 + width; x++)
                {
                    pixels[y * 64 + x] = arrow;
                }
            }

            fallbackArrowTexture.SetPixels(pixels);
            fallbackArrowTexture.Apply();
            return fallbackArrowTexture;
        }

        private static void SetTextureScale(Material material, Vector2 scale)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTextureScale("_BaseMap", scale);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTextureScale("_MainTex", scale);
            }
        }

        private static void SetTextureOffset(Material material, Vector2 offset)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTextureOffset("_BaseMap", offset);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTextureOffset("_MainTex", offset);
            }
        }
    }
}
