using System;

namespace CorridorCommander
{
    [Serializable]
    public sealed class MovementStats
    {
        public float moveSpeed = 2.6f;
        public float rotationSpeed = 540f;
        public float acceleration = 8f;
        public float stoppingDistance = 0.35f;
    }
}
