using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class PlacementPoint : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Transform buildAnchor;
        [SerializeField] private GameObject turretPrefab;
        [SerializeField] private GameObject barricadePrefab;
        [SerializeField] private GameObject mortarPrefab;
        [SerializeField] private BuildableDefinitionSO[] buildableDefinitions;
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private bool applyIndicatorColor = true;
        [SerializeField] private Color availableColor = new Color(0.05f, 1f, 0.2f);
        [SerializeField] private Color occupiedColor = new Color(0.2f, 0.35f, 0.2f);
        [SerializeField] private float placementSurfacePadding = 0.01f;

        private GameObject placedObject;
        private MaterialPropertyBlock indicatorPropertyBlock;

        public bool IsOccupied => placedObject != null;
        public GameObject PlacedObject => placedObject;
        public Transform BuildAnchor => buildAnchor != null ? buildAnchor : transform;
        public IReadOnlyList<BuildableDefinitionSO> BuildableDefinitions => buildableDefinitions;

        protected virtual void Awake()
        {
            if (buildAnchor == null)
            {
                buildAnchor = transform;
            }

            RefreshColor();
        }

        public bool CanBuild(BuildableKind kind)
        {
            return !IsOccupied && TryGetPrefab(kind, out _);
        }

        public bool CanBuild(BuildableDefinitionSO definition)
        {
            return !IsOccupied
                && definition != null
                && definition.Prefab != null
                && IsDefinitionAllowed(definition);
        }

        public void ConfigureBuildableDefinitions(System.Collections.Generic.IReadOnlyList<BuildableDefinitionSO> definitions)
        {
            if (definitions == null)
            {
                buildableDefinitions = null;
                return;
            }

            int validCount = 0;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null)
                {
                    validCount++;
                }
            }

            BuildableDefinitionSO[] configuredDefinitions = new BuildableDefinitionSO[validCount];
            int writeIndex = 0;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null)
                {
                    configuredDefinitions[writeIndex] = definitions[i];
                    writeIndex++;
                }
            }

            buildableDefinitions = configuredDefinitions;
        }

        public GameObject Build(BuildableKind kind, GameObject builder)
        {
            return Build(kind, builder, null);
        }

        public GameObject Build(BuildableKind kind, GameObject builder, Quaternion? rotationOverride)
        {
            if (!CanBuild(kind))
            {
                return null;
            }

            TryGetDefinition(kind, out BuildableDefinitionSO definition);
            TryGetPrefab(kind, out GameObject prefab);
            return BuildInternal(kind, definition, prefab, builder, rotationOverride);
        }

        public GameObject Build(BuildableDefinitionSO definition, GameObject builder, Quaternion? rotationOverride)
        {
            if (!CanBuild(definition))
            {
                return null;
            }

            return BuildInternal(definition.Kind, definition, definition.Prefab, builder, rotationOverride);
        }

        public GameObject Build(BuildableDefinitionSO definition, GameObject builder)
        {
            return Build(definition, builder, null);
        }

        public void GetBuildableDefinitions(BuildableCategory category, List<BuildableDefinitionSO> results)
        {
            if (results == null || buildableDefinitions == null)
            {
                return;
            }

            for (int i = 0; i < buildableDefinitions.Length; i++)
            {
                BuildableDefinitionSO definition = buildableDefinitions[i];
                if (definition != null && definition.Category == category && definition.Prefab != null)
                {
                    results.Add(definition);
                }
            }
        }

        public bool TryGetPrefab(BuildableKind kind, out GameObject prefab)
        {
            if (TryGetDefinition(kind, out BuildableDefinitionSO definition) && definition.Prefab != null)
            {
                prefab = definition.Prefab;
                return true;
            }

            if (HasConfiguredDefinitions())
            {
                prefab = null;
                return false;
            }

            prefab = kind switch
            {
                BuildableKind.Turret => turretPrefab,
                BuildableKind.Barricade => barricadePrefab,
                BuildableKind.Mortar => mortarPrefab,
                _ => null
            };

            return prefab != null;
        }

        public bool TryGetDefinition(BuildableKind kind, out BuildableDefinitionSO definition)
        {
            if (buildableDefinitions != null)
            {
                for (int i = 0; i < buildableDefinitions.Length; i++)
                {
                    if (buildableDefinitions[i] != null && buildableDefinitions[i].Kind == kind)
                    {
                        definition = buildableDefinitions[i];
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        public bool RequiresPreviewRotation(BuildableKind kind)
        {
            if (TryGetDefinition(kind, out BuildableDefinitionSO definition))
            {
                return definition.RotateBeforeInstall;
            }

            return kind == BuildableKind.Barricade;
        }

        public bool RequiresPreviewRotation(BuildableDefinitionSO definition)
        {
            return definition != null
                ? definition.RotateBeforeInstall
                : false;
        }

        public void AlignPreviewObject(GameObject target)
        {
            AlignPlacedObjectToSurface(target);
        }

        public bool ReleasePlacedObject(GameObject expectedPlacedObject)
        {
            if (placedObject == null)
            {
                RefreshColor();
                return false;
            }

            if (expectedPlacedObject != null && placedObject != expectedPlacedObject)
            {
                return false;
            }

            placedObject = null;
            RefreshColor();
            return true;
        }

        public bool ReplacePlacedObject(GameObject expectedPlacedObject, GameObject replacement)
        {
            if (placedObject == null || replacement == null)
            {
                RefreshColor();
                return false;
            }

            if (expectedPlacedObject != null && placedObject != expectedPlacedObject)
            {
                return false;
            }

            placedObject = replacement;
            RefreshColor();
            return true;
        }

        private GameObject BuildInternal(
            BuildableKind kind,
            BuildableDefinitionSO definition,
            GameObject prefab,
            GameObject builder,
            Quaternion? rotationOverride)
        {
            Transform anchor = BuildAnchor;
            Quaternion rotation = ResolveBuildRotation(rotationOverride);

            placedObject = Instantiate(prefab, anchor.position, rotation);
            placedObject.name = definition != null && !string.IsNullOrWhiteSpace(definition.BuildableId)
                ? $"{definition.BuildableId}_Built_From_{name}"
                : $"{kind}_Built_From_{name}";
            AlignPlacedObjectToSurface(placedObject);

            BuildContext context = new BuildContext(this, kind, definition, builder, anchor);
            InitializeInstalledState(placedObject, context);

            InitializeInstallables(placedObject, context);

            RefreshColor();
            return placedObject;
        }

        protected virtual Quaternion ResolveBuildRotation(Quaternion? rotationOverride)
        {
            return rotationOverride ?? BuildAnchor.rotation;
        }

        private static void InitializeInstalledState(GameObject root, BuildContext context)
        {
            InstalledBuildableState state = root.GetComponent<InstalledBuildableState>();
            if (state == null)
            {
                state = root.AddComponent<InstalledBuildableState>();
            }

            state.Initialize(context);
        }

        private static void InitializeInstallables(GameObject root, BuildContext context)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IBuildableInstallable installable && installable.Kind == context.Kind)
                {
                    installable.OnInstalled(context);
                }
            }
        }

        private bool IsDefinitionAllowed(BuildableDefinitionSO definition)
        {
            if (buildableDefinitions == null || buildableDefinitions.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < buildableDefinitions.Length; i++)
            {
                if (buildableDefinitions[i] == definition)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasConfiguredDefinitions()
        {
            if (buildableDefinitions == null)
            {
                return false;
            }

            for (int i = 0; i < buildableDefinitions.Length; i++)
            {
                if (buildableDefinitions[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual void AlignPlacedObjectToSurface(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (TryGetBuildablePlacementAnchor(target, out Transform placementAnchor))
            {
                Vector3 anchorSurfacePoint = GetSurfacePoint();
                Vector3 anchorOffset = anchorSurfacePoint - placementAnchor.position + Vector3.up * placementSurfacePadding;
                target.transform.position += anchorOffset;
                return;
            }

            if (!TryGetPlacementBounds(target, out Bounds targetBounds))
            {
                return;
            }

            Vector3 surfacePoint = GetSurfacePoint();
            Vector3 offset = new Vector3(
                surfacePoint.x - targetBounds.center.x,
                surfacePoint.y - targetBounds.min.y + placementSurfacePadding,
                surfacePoint.z - targetBounds.center.z);

            target.transform.position += offset;
        }

        protected virtual Vector3 GetSurfacePoint()
        {
            Vector3 surfacePoint = buildAnchor != null ? buildAnchor.position : transform.position;
            if (indicatorRenderer != null)
            {
                surfacePoint.y = indicatorRenderer.bounds.max.y;
                return surfacePoint;
            }

            Collider pointCollider = GetComponent<Collider>();
            if (pointCollider != null)
            {
                surfacePoint.y = pointCollider.bounds.max.y;
            }

            return surfacePoint;
        }

        protected static bool TryGetPlacementBounds(GameObject target, out Bounds bounds)
        {
            if (TryGetRendererBounds(target, out bounds))
            {
                return true;
            }

            return TryGetColliderBounds(target, out bounds);
        }

        protected static bool TryGetBuildablePlacementAnchor(GameObject target, out Transform placementAnchor)
        {
            placementAnchor = null;
            if (target == null)
            {
                return false;
            }

            BuildablePlacementAnchor anchor = target.GetComponent<BuildablePlacementAnchor>();
            if (anchor == null)
            {
                anchor = target.GetComponentInChildren<BuildablePlacementAnchor>(true);
            }

            if (anchor == null || anchor.FloorAnchor == null)
            {
                return false;
            }

            placementAnchor = anchor.FloorAnchor;
            return true;
        }

        private static bool TryGetRendererBounds(GameObject target, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || renderer.bounds.size.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static bool TryGetColliderBounds(GameObject target, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                if (collider == null || !collider.enabled || collider.bounds.size.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return hasBounds;
        }

        private void RefreshColor()
        {
            if (indicatorRenderer == null)
            {
                indicatorRenderer = GetComponentInChildren<Renderer>();
            }

            if (indicatorRenderer != null && applyIndicatorColor)
            {
                if (indicatorPropertyBlock == null)
                {
                    indicatorPropertyBlock = new MaterialPropertyBlock();
                }

                Color color = IsOccupied ? occupiedColor : availableColor;
                Material material = indicatorRenderer.sharedMaterial;
                indicatorRenderer.GetPropertyBlock(indicatorPropertyBlock);
                indicatorPropertyBlock.SetColor(material != null && material.HasProperty(BaseColorId)
                    ? BaseColorId
                    : ColorId, color);
                indicatorRenderer.SetPropertyBlock(indicatorPropertyBlock);
            }
        }
    }
}
