using UnityEngine;

namespace CorridorCommander.Enemy
{
    [DisallowMultipleComponent]
    public sealed class ProjectileSpinVisual : MonoBehaviour
    {
        [Header("Spin")]
        [SerializeField, Min(0f)] private float minimumDegreesPerSecond = 540f;
        [SerializeField, Min(0f)] private float maximumDegreesPerSecond = 900f;
        [SerializeField] private bool randomizeAxisOnEnable = true;
        [SerializeField] private Vector3 fixedLocalAxis = Vector3.right;

        private Vector3 activeLocalAxis = Vector3.right;
        private float activeDegreesPerSecond;

        public Vector3 ActiveLocalAxis => activeLocalAxis;
        public float ActiveDegreesPerSecond => activeDegreesPerSecond;

        private void OnEnable()
        {
            activeLocalAxis = randomizeAxisOnEnable
                ? Random.onUnitSphere
                : fixedLocalAxis.normalized;

            if (activeLocalAxis.sqrMagnitude <= 0.0001f)
            {
                activeLocalAxis = Vector3.right;
            }

            float minimum = Mathf.Min(minimumDegreesPerSecond, maximumDegreesPerSecond);
            float maximum = Mathf.Max(minimumDegreesPerSecond, maximumDegreesPerSecond);
            activeDegreesPerSecond = Random.Range(minimum, maximum);

            if (Random.value < 0.5f)
            {
                activeDegreesPerSecond = -activeDegreesPerSecond;
            }
        }

        private void Update()
        {
            transform.Rotate(
                activeLocalAxis,
                activeDegreesPerSecond * Time.deltaTime,
                Space.Self);
        }
    }
}

/*
Unity setup:
1. Add ProjectileSpinVisual to projectile visual prefabs that should tumble in flight.
2. Enable Randomize Axis On Enable for a different tumble direction on every throw.
3. Adjust Minimum and Maximum Degrees Per Second to control spin speed.
*/
