using UnityEngine;

namespace CorridorCommander
{
    public readonly struct SkillUseContext
    {
        public SkillUseContext(GameObject user, Vector3 targetPoint, Camera aimCamera)
        {
            User = user;
            TargetPoint = targetPoint;
            AimCamera = aimCamera;
        }

        public GameObject User { get; }
        public Vector3 TargetPoint { get; }
        public Camera AimCamera { get; }
    }
}
