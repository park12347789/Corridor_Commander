using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class WallPlacementPoint : PlacementPoint
    {
        protected override Quaternion ResolveBuildRotation(Quaternion? rotationOverride)
        {
            if (rotationOverride.HasValue)
            {
                return rotationOverride.Value;
            }

            Transform anchor = BuildAnchor;
            Vector3 wallNormal = ResolveWallNormal(anchor);
            Vector3 wallUp = Vector3.ProjectOnPlane(anchor.up, wallNormal);
            if (wallUp.sqrMagnitude <= 0.0001f)
            {
                wallUp = Vector3.ProjectOnPlane(Vector3.up, wallNormal);
            }

            if (wallUp.sqrMagnitude <= 0.0001f)
            {
                wallUp = Vector3.forward;
            }

            return Quaternion.LookRotation(wallUp.normalized, wallNormal);
        }

        protected override void AlignPlacedObjectToSurface(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (TryGetBuildablePlacementAnchor(target, out Transform placementAnchor))
            {
                target.transform.position += BuildAnchor.position - placementAnchor.position;
                return;
            }

            target.transform.position = BuildAnchor.position;
        }

        private static Vector3 ResolveWallNormal(Transform anchor)
        {
            if (anchor == null || anchor.forward.sqrMagnitude <= 0.0001f)
            {
                return Vector3.forward;
            }

            return anchor.forward.normalized;
        }
    }
}
