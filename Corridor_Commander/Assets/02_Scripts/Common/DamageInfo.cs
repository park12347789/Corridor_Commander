using UnityEngine;

namespace CorridorCommander
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, GameObject source, Vector3 hitPoint)
        {
            Amount = amount;
            Source = source;
            HitPoint = hitPoint;
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public Vector3 HitPoint { get; }
    }
}
