using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class BuildablePlacementAnchor : MonoBehaviour
    {
        [SerializeField] private Transform floorAnchor;

        public Transform FloorAnchor => floorAnchor != null ? floorAnchor : transform;
    }
}
