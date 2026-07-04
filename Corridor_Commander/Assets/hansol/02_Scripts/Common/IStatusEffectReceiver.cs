using UnityEngine;

namespace CorridorCommander
{
    public interface IStatusEffectReceiver
    {
        float MoveSpeedMultiplier { get; }
        void ApplyStatusEffect(StatusEffectDefinitionSO definition, GameObject source, Vector3 hitPoint);
    }
}
