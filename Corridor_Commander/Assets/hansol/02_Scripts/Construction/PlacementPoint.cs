using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class PlacementPoint : MonoBehaviour
    {
        [SerializeField] private Transform buildAnchor;
        [SerializeField] private GameObject turretPrefab;
        [SerializeField] private GameObject barricadePrefab;
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color availableColor = new Color(0.05f, 1f, 0.2f);
        [SerializeField] private Color occupiedColor = new Color(0.2f, 0.35f, 0.2f);

        private GameObject placedObject;

        public bool IsOccupied => placedObject != null;

        private void Awake()
        {
            if (buildAnchor == null)
            {
                buildAnchor = transform;
            }

            RefreshColor();
        }

        public bool CanBuild(BuildableKind kind)
        {
            return !IsOccupied && GetPrefab(kind) != null;
        }

        public GameObject Build(BuildableKind kind, GameObject builder)
        {
            if (!CanBuild(kind))
            {
                return null;
            }

            GameObject prefab = GetPrefab(kind);
            placedObject = Instantiate(prefab, buildAnchor.position, buildAnchor.rotation);
            placedObject.name = $"{kind}_Built_From_{name}";
            RefreshColor();
            return placedObject;
        }

        private GameObject GetPrefab(BuildableKind kind)
        {
            return kind == BuildableKind.Turret ? turretPrefab : barricadePrefab;
        }

        private void RefreshColor()
        {
            if (indicatorRenderer == null)
            {
                indicatorRenderer = GetComponentInChildren<Renderer>();
            }

            if (indicatorRenderer != null)
            {
                indicatorRenderer.material.color = IsOccupied ? occupiedColor : availableColor;
            }
        }
    }
}
