using UnityEngine;

namespace CorridorCommander
{
    public interface IMovementMotor
    {
        void Move(Vector3 direction);
        void MoveTo(Vector3 worldPosition);
        void Stop();
    }
}
