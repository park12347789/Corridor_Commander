using System;
using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerItems;
using UnityEngine;

namespace CorridorCommander
{
    [Serializable]
    public sealed class TutorialChapterDefinition
    {
        [SerializeField] private string chapterId = "chapter";
        [SerializeField] private int number = 1;
        [SerializeField] private string displayName = "Chapter";
        [SerializeField, TextArea(2, 4)] private string description = "";
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform guideTarget;
        [SerializeField] private int minimumMoney;
        [SerializeField] private bool clearPlacementPoints;
        [SerializeField] private bool restorePlayerHealth;
        [SerializeField] private TutorialWeaponGrant[] weaponGrants;
        [SerializeField] private TutorialAmmoGrant[] ammoGrants;
        [SerializeField] private TutorialItemGrant[] itemGrants;
        [SerializeField] private GameObject[] activateObjects;
        [SerializeField] private GameObject[] deactivateObjects;

        public string ChapterId => chapterId;
        public int Number => number;
        public string DisplayName => displayName;
        public string Description => description;
        public Transform StartPoint => startPoint;
        public Transform GuideTarget => guideTarget;
        public int MinimumMoney => minimumMoney;
        public bool ClearPlacementPoints => clearPlacementPoints;
        public bool RestorePlayerHealth => restorePlayerHealth;
        public TutorialWeaponGrant[] WeaponGrants => weaponGrants;
        public TutorialAmmoGrant[] AmmoGrants => ammoGrants;
        public TutorialItemGrant[] ItemGrants => itemGrants;
        public GameObject[] ActivateObjects => activateObjects;
        public GameObject[] DeactivateObjects => deactivateObjects;
    }

    [Serializable]
    public sealed class TutorialWeaponGrant
    {
        [SerializeField] private WeaponItemDefinitionSO weaponDefinition;
        [SerializeField] private bool fillMagazine = true;
        [SerializeField] private bool equipAfterGrant;

        public WeaponItemDefinitionSO WeaponDefinition => weaponDefinition;
        public bool FillMagazine => fillMagazine;
        public bool EquipAfterGrant => equipAfterGrant;
    }

    [Serializable]
    public sealed class TutorialAmmoGrant
    {
        [SerializeField] private AmmoDefinitionSO ammoDefinition;
        [SerializeField] private int amount = 30;

        public AmmoDefinitionSO AmmoDefinition => ammoDefinition;
        public int Amount => amount;
    }

    [Serializable]
    public sealed class TutorialItemGrant
    {
        [SerializeField] private ItemDefinitionSO itemDefinition;
        [SerializeField] private int amount = 1;

        public ItemDefinitionSO ItemDefinition => itemDefinition;
        public int Amount => amount;
    }
}
