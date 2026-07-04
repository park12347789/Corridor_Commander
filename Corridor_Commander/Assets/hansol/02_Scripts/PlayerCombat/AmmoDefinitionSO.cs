using UnityEngine;

namespace CorridorCommander.PlayerCombat
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Combat/Ammo Definition",
        fileName = "AmmoDefinition"
    )]
    public sealed class AmmoDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string ammoId;
        public string displayName;

        [Header("Carry")]
        public int maxCarryAmount = 300;
    }
}