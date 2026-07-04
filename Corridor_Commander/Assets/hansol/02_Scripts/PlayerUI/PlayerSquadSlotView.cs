using CorridorCommander.PlayerControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander.PlayerUI
{
    [DisallowMultipleComponent]
    public sealed class PlayerSquadSlotView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image background;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text memberText;
        [SerializeField] private TMP_Text healthText;

        private void Awake()
        {
            ResolveReferences();
        }

        public void Refresh(
            int slotNumber,
            AlliedSquadMemberFollower member,
            Health health,
            bool isSelected,
            Color normalColor,
            Color selectedColor)
        {
            ResolveReferences();

            bool hasMember = member != null;
            gameObject.SetActive(true);

            if (background != null)
            {
                background.enabled = true;
                background.color = isSelected ? selectedColor : normalColor;
                background.raycastTarget = false;
            }

            if (healthFill != null)
            {
                healthFill.enabled = hasMember;
            }

            if (portraitImage != null)
            {
                portraitImage.enabled = hasMember;
            }

            if (memberText != null)
            {
                memberText.enabled = hasMember;
            }

            if (healthText != null)
            {
                healthText.enabled = hasMember;
            }

            if (!hasMember)
            {
                SetHealthFillAmount(0f);
                return;
            }

            if (health == null)
            {
                health = ResolveHealth(member);
            }

            float currentHealth = health != null ? health.CurrentHitPoints : 0f;
            float maxHealth = health != null ? health.MaxHitPoints : 0f;

            if (healthFill != null)
            {
                float fillAmount = maxHealth > 0f
                    ? Mathf.Clamp01(currentHealth / maxHealth)
                    : 0f;

                SetHealthFillAmount(fillAmount);
            }

            if (memberText != null)
            {
                string displayName = ResolveDisplayName(member);
                memberText.text = healthText == null
                    ? $"{displayName}  {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}"
                    : displayName;
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
            }

            if (portraitImage != null)
            {
                Sprite portrait = member.RosterIcon;
                portraitImage.sprite = portrait;
                portraitImage.enabled = portrait != null;
                portraitImage.preserveAspect = true;
            }
        }

        public void Clear()
        {
            ResolveReferences();

            gameObject.SetActive(true);
            SetImageEnabled(background, true);
            SetImageEnabled(healthFill, false);
            SetImageEnabled(portraitImage, false);
            SetHealthFillAmount(0f);

            if (memberText != null)
            {
                memberText.enabled = false;
                memberText.text = string.Empty;
            }

            if (healthText != null)
            {
                healthText.enabled = false;
                healthText.text = string.Empty;
            }
        }

        private static string ResolveDisplayName(AlliedSquadMemberFollower member)
        {
            AlliedSquadMemberCombat combat = member.GetComponent<AlliedSquadMemberCombat>();
            if (combat == null)
            {
                combat = member.GetComponentInChildren<AlliedSquadMemberCombat>(true);
            }

            string weaponName = combat != null && combat.WeaponDefinition != null
                ? combat.WeaponDefinition.name
                : string.Empty;

            switch (weaponName)
            {
                case "Weapon_AK2":
                    return "rifle";
                case "Weapon_Shotgun":
                    return "Shotgun";
                case "Weapon_LaserGun":
                    return "lasergun";
                case "Weapon_BeamCannon":
                    return "Cannon";
            }

            string objectName = member.name;
            if (objectName.Contains("TEMP_AlliedDummy_laser_gun"))
            {
                return "lasergun";
            }

            if (objectName.Contains("TEMP_AlliedDummy_Blue"))
            {
                return "Shotgun";
            }

            if (objectName.Contains("TEMP_AlliedDummy_purple"))
            {
                return "Cannon";
            }

            if (objectName.Contains("TEMP_AlliedDummy"))
            {
                return "rifle";
            }

            return objectName.Replace("(Clone)", string.Empty).Trim();
        }

        private static Health ResolveHealth(AlliedSquadMemberFollower member)
        {
            if (member == null)
            {
                return null;
            }

            Health health = member.GetComponent<Health>();
            if (health != null)
            {
                return health;
            }

            health = member.GetComponentInChildren<Health>(true);
            if (health != null)
            {
                return health;
            }

            health = member.GetComponentInParent<Health>();
            if (health != null)
            {
                return health;
            }

            Transform root = member.transform.root;
            return root != null ? root.GetComponentInChildren<Health>(true) : null;
        }

        private void ResolveReferences()
        {
            if (background == null)
            {
                Transform bgTransform = transform.Find("bg");
                if (bgTransform != null)
                {
                    background = bgTransform.GetComponent<Image>();
                }

                if (background == null)
                {
                    background = GetComponent<Image>();
                }
            }

            Image namedHealthFill = FindImageByNames(transform, "HealthFill", "HealthBarFill", "Fill");
            if (namedHealthFill != null && healthFill != namedHealthFill)
            {
                healthFill = namedHealthFill;
            }

            if (healthFill == null)
            {
                healthFill = FindImageByNames(transform, "HealthFill", "HealthBarFill", "Fill");
            }

            ConfigureHealthFill(healthFill);

            if (portraitImage == null)
            {
                Transform portraitTransform = transform.Find("Portrait");
                if (portraitTransform == null)
                {
                    portraitTransform = transform.Find("BG/Portrait");
                }

                if (portraitTransform == null)
                {
                    portraitTransform = transform.Find("bg/Portrait");
                }

                if (portraitTransform == null)
                {
                    portraitTransform = transform.Find("BG/Image");
                }

                if (portraitTransform == null)
                {
                    portraitTransform = transform.Find("bg/Image");
                }

                if (portraitTransform != null)
                {
                    portraitImage = portraitTransform.GetComponent<Image>();
                }
            }

            TMP_Text namedMemberText = FindTmpTextByNames(transform, "MemberText", "NameText", "Text_Member");
            if (namedMemberText != null && memberText != namedMemberText)
            {
                memberText = namedMemberText;
            }

            if (memberText == null)
            {
                memberText = FindTmpTextByNames(transform, "MemberText", "NameText", "Text_Member");
            }

            TMP_Text namedHealthText = FindTmpTextByNames(transform, "HealthText", "HpText", "HPText", "Text_Health");
            if (namedHealthText != null && healthText != namedHealthText)
            {
                healthText = namedHealthText;
            }

            if (healthText == null)
            {
                healthText = FindTmpTextByNames(transform, "HealthText", "HpText", "HPText", "Text_Health");
            }
        }

        private static void ConfigureHealthFill(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Right;
            image.fillAmount = 0f;
            image.raycastTarget = false;

            RectTransform rectTransform = image.rectTransform;
            rectTransform.offsetMin = new Vector2(0f, rectTransform.offsetMin.y);
            rectTransform.offsetMax = new Vector2(0f, rectTransform.offsetMax.y);
        }

        private static Image FindImageByNames(Transform root, params string[] names)
        {
            Transform child = FindChildRecursive(root, names);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static TMP_Text FindTmpTextByNames(Transform root, params string[] names)
        {
            Transform child = FindChildRecursive(root, names);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static Transform FindChildRecursive(Transform root, params string[] names)
        {
            if (root == null || names == null)
            {
                return null;
            }

            for (int i = 0; i < names.Length; i++)
            {
                if (root.name == names[i])
                {
                    return root;
                }
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), names);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void SetImageEnabled(Image image, bool enabled)
        {
            if (image != null)
            {
                image.enabled = enabled;
            }
        }

        private void SetHealthFillAmount(float fillAmount)
        {
            if (healthFill == null)
            {
                return;
            }

            float clampedFill = Mathf.Clamp01(fillAmount);
            healthFill.fillAmount = clampedFill;

            RectTransform healthRect = healthFill.rectTransform;
            healthRect.pivot = new Vector2(1f, healthRect.pivot.y);
            Vector3 scale = healthRect.localScale;
            scale.x = clampedFill;
            healthRect.localScale = scale;
        }
    }
}
