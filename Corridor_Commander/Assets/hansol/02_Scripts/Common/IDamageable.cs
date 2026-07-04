namespace CorridorCommander
{
    public interface IDamageable
    {
        void TakeDamage(DamageInfo damageInfo);
    }

    public interface IDamageTarget : IDamageable
    {
        bool IsAlive { get; }
        UnityEngine.Transform Transform { get; }
    }
}
