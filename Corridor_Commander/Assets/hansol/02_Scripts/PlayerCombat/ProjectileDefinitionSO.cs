using UnityEngine;

namespace CorridorCommander.PlayerCombat
{
    public enum ProjectileMoveType
    {
        Straight,
        GravityArc,
        BezierArc,
        BallisticArc
    }

    [CreateAssetMenu(
        menuName = "Corridor Commander/Combat/Projectile Definition",
        fileName = "ProjectileDefinition"
    )]
    public sealed class ProjectileDefinitionSO : ScriptableObject
    {
        [Header("Prefab")]
        public GameObject projectilePrefab;

        [Header("Basic")]
        public float damage = 10f;
        public float speed = 20f;
        public float lifeTime = 3f;

        [Header("Movement")]
        public ProjectileMoveType moveType = ProjectileMoveType.Straight;
        public float gravity = -9.81f;

        [Header("Visual Rotation")]
        [Tooltip("Euler offset applied after aligning the projectile +Z axis to its movement direction.")]
        public Vector3 visualRotationOffset;

        [Header("Bezier Arc")]
        [Tooltip("BezierArc only. Height added to the midpoint between launch and target.")]
        public float bezierArcHeight = 3f;

        [Tooltip("BezierArc only. If 0, travel duration is calculated from distance / speed.")]
        public float bezierTravelDuration = 0f;

        [Tooltip("BezierArc only. Used when no explicit target point is supplied.")]
        public float bezierFallbackDistance = 20f;

        [Header("Ballistic Arc")]
        [Tooltip("BallisticArc only. Height added above the higher point between launch and target.")]
        public float ballisticArcHeight = 4f;

        [Tooltip("BallisticArc only. Used when no explicit target point is supplied.")]
        public float ballisticFallbackDistance = 20f;

        [Header("Splash Damage")]
        public bool useSplashDamage = false;
        public float splashRadius = 2f;
        public float splashDamage = 10f;

        [Header("Collision")]
        public LayerMask hitLayers = ~0;

        [Header("Pooling")]
        public int initialPoolSize = 20;

        [Header("VFX")]
        public GameObject hitVfxPrefab;
        public float hitVfxLifeTime = 1.5f;

        [Header("Hit Audio")]
        public AudioClip[] hitAudioClips;
        [Range(0f, 1f)] public float hitAudioVolume = 1f;
        public Vector2 hitAudioPitchRange = new Vector2(0.96f, 1.04f);
        [Range(0f, 1f)] public float hitAudioSpatialBlend = 1f;
        [Tooltip("If 0, the spawned audio object lives for the selected clip length.")]
        public float hitAudioLifeTime = 0f;
    }

    public static class BallisticTrajectoryUtility
    {
        public static Vector3 CalculateVelocityByArcHeight(
            Vector3 startPoint,
            Vector3 targetPoint,
            float gravity,
            float arcHeight,
            Vector3 fallbackDirection,
            float fallbackSpeed)
        {
            float gravityMagnitude = Mathf.Abs(gravity);

            if (gravityMagnitude <= 0.0001f)
            {
                return ResolveFallbackVelocity(fallbackDirection, fallbackSpeed);
            }

            float apexY = Mathf.Max(startPoint.y, targetPoint.y) + Mathf.Max(0.01f, arcHeight);
            float ascentHeight = Mathf.Max(0.01f, apexY - startPoint.y);
            float descentHeight = Mathf.Max(0.01f, apexY - targetPoint.y);

            Vector3 displacement = targetPoint - startPoint;
            Vector3 horizontalDisplacement = new Vector3(displacement.x, 0f, displacement.z);
            float horizontalDistance = horizontalDisplacement.magnitude;
            float cappedHorizontalSpeed = Mathf.Max(0.01f, fallbackSpeed);

            float minimumVerticalVelocity = Mathf.Sqrt(2f * gravityMagnitude * ascentHeight);
            float timeUp = minimumVerticalVelocity / gravityMagnitude;
            float timeDown = Mathf.Sqrt(2f * descentHeight / gravityMagnitude);
            float minimumArcTime = Mathf.Max(0.01f, timeUp + timeDown);
            float speedBasedTime = horizontalDistance / cappedHorizontalSpeed;
            float totalTime = Mathf.Max(minimumArcTime, speedBasedTime);

            float verticalVelocity = (displacement.y + 0.5f * gravityMagnitude * totalTime * totalTime) / totalTime;
            Vector3 horizontalVelocity = horizontalDisplacement / totalTime;

            return horizontalVelocity + Vector3.up * verticalVelocity;
        }

        private static Vector3 ResolveFallbackVelocity(Vector3 fallbackDirection, float fallbackSpeed)
        {
            Vector3 direction = fallbackDirection.sqrMagnitude > 0.0001f
                ? fallbackDirection.normalized
                : Vector3.forward;

            return direction * Mathf.Max(0.01f, fallbackSpeed);
        }
    }
}
