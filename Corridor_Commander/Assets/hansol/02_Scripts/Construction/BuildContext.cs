using UnityEngine;

namespace CorridorCommander
{
    public readonly struct BuildContext
    {
        public BuildContext(PlacementPoint placementPoint, BuildableKind kind, GameObject builder, Transform buildAnchor)
            : this(placementPoint, kind, null, builder, buildAnchor)
        {
        }

        public BuildContext(
            PlacementPoint placementPoint,
            BuildableKind kind,
            BuildableDefinitionSO definition,
            GameObject builder,
            Transform buildAnchor)
        {
            PlacementPoint = placementPoint;
            Kind = kind;
            Definition = definition;
            Builder = builder;
            BuildAnchor = buildAnchor;
        }

        public PlacementPoint PlacementPoint { get; }
        public BuildableKind Kind { get; }
        public BuildableDefinitionSO Definition { get; }
        public GameObject Builder { get; }
        public Transform BuildAnchor { get; }
    }
}
