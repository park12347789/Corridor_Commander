using UnityEngine;

namespace CorridorCommander.PlayerCombat
{
    public enum WeaponAnimationType
    {
        None = 0,
        Ranged1H = 1,
        Ranged2H = 2
    }

    [CreateAssetMenu(
        menuName = "Corridor Commander/Combat/Weapon Item Definition",
        fileName = "WeaponItemDefinition"
    )]
    public sealed class WeaponItemDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string weaponId;
        public string displayName;

        [Header("Visual")]
        public Sprite icon;
        public GameObject weaponPrefab;

        [Header("Animation")]
        [SerializeField] private WeaponAnimationType animationType = WeaponAnimationType.None;
        [SerializeField, Min(0f)] private float unaimedFirstShotDelay = 0f;

        [Header("Fire")]
        public WeaponFireDefinitionSO fireDefinition;

        [Header("Audio")]
        public WeaponAudioDefinitionSO audioDefinition;

        [Header("Magazine")]
        [Min(1)]
        public int magazineSize = 30;

        public float reloadTime = 1.5f;

        [Header("Ammo")]
        public AmmoDefinitionSO ammoDefinition;

        public WeaponAnimationType AnimationType => animationType;
        public float UnaimedFirstShotDelay => unaimedFirstShotDelay;
    }
}
