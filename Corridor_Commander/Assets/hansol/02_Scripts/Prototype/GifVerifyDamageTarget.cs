using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class GifVerifyDamageTarget : MonoBehaviour, IDamageable
    {
        [SerializeField] private float hitPoints = 100000f;

        public void TakeDamage(DamageInfo damageInfo)
        {
            hitPoints = Mathf.Max(0f, hitPoints - Mathf.Max(0f, damageInfo.Amount));
            if (hitPoints <= 0f)
            {
                hitPoints = 100000f;
            }
        }
    }
}
