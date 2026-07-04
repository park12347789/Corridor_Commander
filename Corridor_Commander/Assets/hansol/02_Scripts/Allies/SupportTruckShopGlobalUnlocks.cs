using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    public static class SupportTruckShopGlobalUnlocks
    {
        private static readonly HashSet<SupportTruckShopUnlockKey> UnlockedKeys =
            new HashSet<SupportTruckShopUnlockKey>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            UnlockedKeys.Clear();
        }

        public static bool IsUnlocked(SupportTruckShopUnlockKey key)
        {
            return key == SupportTruckShopUnlockKey.None || UnlockedKeys.Contains(key);
        }

        public static bool TryUnlock(SupportTruckShopUnlockKey key)
        {
            return key != SupportTruckShopUnlockKey.None && UnlockedKeys.Add(key);
        }

        public static bool CanBuild(BuildableKind kind)
        {
            return kind != BuildableKind.Mortar || IsUnlocked(SupportTruckShopUnlockKey.MortarInstallation);
        }

        public static bool CanBuild(BuildableDefinitionSO definition)
        {
            if (definition == null || !CanBuild(definition.Kind))
            {
                return false;
            }

            return definition.BuildableId != "saw_trap_turret"
                || IsUnlocked(SupportTruckShopUnlockKey.SawTrapTurret);
        }
    }
}
