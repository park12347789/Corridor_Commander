using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class PlayerAimSkillTargetProvider : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private float maxDistance = 60f;
        [SerializeField] private LayerMask aimLayers = ~0;

        public void Configure(Camera configuredCamera, float configuredMaxDistance, LayerMask configuredAimLayers)
        {
            aimCamera = configuredCamera;
            maxDistance = Mathf.Max(1f, configuredMaxDistance);
            aimLayers = configuredAimLayers;
        }

        public bool TryCreateContext(GameObject user, out SkillUseContext context)
        {
            Camera resolvedCamera = ResolveCamera();
            if (resolvedCamera == null)
            {
                context = default;
                return false;
            }

            Ray ray = resolvedCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPoint = ray.origin + ray.direction * maxDistance;
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, aimLayers, QueryTriggerInteraction.Ignore))
            {
                targetPoint = hit.point;
            }

            context = new SkillUseContext(user != null ? user : gameObject, targetPoint, resolvedCamera);
            return true;
        }

        private Camera ResolveCamera()
        {
            if (aimCamera != null)
            {
                return aimCamera;
            }

            aimCamera = Camera.main;
            return aimCamera;
        }
    }
}
