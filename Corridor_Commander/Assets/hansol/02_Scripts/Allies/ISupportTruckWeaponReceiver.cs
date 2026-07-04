using CorridorCommander.PlayerCombat;

namespace CorridorCommander
{
    public interface ISupportTruckWeaponReceiver
    {
        bool TryReceiveSupportTruckWeapon(
            WeaponItemDefinitionSO weaponDefinition,
            bool fillMagazine,
            out string statusMessage);
    }
}
