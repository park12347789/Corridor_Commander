using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class SkillSlotPresenter : MonoBehaviour
    {
        [SerializeField] [Range(1, 9)] private int slotNumber = 1;
        [SerializeField] private PlayerAimSkillTargetProvider targetProvider;
        [SerializeField] private Button useButton;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Text legacyCountText;
        [SerializeField] private Image iconImage;
        [SerializeField] private bool useNumberKey = true;

        private bool useButtonListenerBound;

        public void Configure(
            int configuredSlotNumber,
            PlayerAimSkillTargetProvider configuredTargetProvider,
            Button configuredUseButton,
            TMP_Text configuredCountText,
            Image configuredIconImage,
            bool configuredUseNumberKey)
        {
            UnbindUseButton();

            slotNumber = Mathf.Clamp(configuredSlotNumber, 1, 9);
            targetProvider = configuredTargetProvider;
            useButton = configuredUseButton;
            countText = configuredCountText;
            iconImage = configuredIconImage;
            useNumberKey = configuredUseNumberKey;

            BindUseButton();

            Refresh();
        }

        public void Configure(
            int configuredSlotNumber,
            PlayerAimSkillTargetProvider configuredTargetProvider,
            Button configuredUseButton,
            Text configuredCountText,
            bool configuredUseNumberKey)
        {
            legacyCountText = configuredCountText;
            Configure(
                configuredSlotNumber,
                configuredTargetProvider,
                configuredUseButton,
                null,
                null,
                configuredUseNumberKey);
        }

        private void Awake()
        {
            BindVisualsIfNeeded();
        }

        private void OnEnable()
        {
            BindUseButton();
            InstalledSkillRegistry.Instance.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            InstalledSkillRegistry registry = InstalledSkillRegistry.Current;
            if (registry != null)
            {
                registry.Changed -= Refresh;
            }

            UnbindUseButton();
        }

        private void OnDestroy()
        {
            UnbindUseButton();
        }

        private void Update()
        {
            if (TEMP_CommandInputState.BlocksHotkeys)
            {
                return;
            }

            Refresh();

            if (useNumberKey
                && KeyboardInputMessenger.WasMenuSlotPressed(slotNumber))
            {
                TryUseSkill();
            }
        }

        private void BindUseButton()
        {
            if (useButton == null || useButtonListenerBound)
            {
                return;
            }

            useButton.onClick.AddListener(TryUseSkill);
            useButtonListenerBound = true;
        }

        private void UnbindUseButton()
        {
            if (useButton == null || !useButtonListenerBound)
            {
                useButtonListenerBound = false;
                return;
            }

            useButton.onClick.RemoveListener(TryUseSkill);
            useButtonListenerBound = false;
        }

        public void TryUseSkill()
        {
            if (!TryCreateContext(out SkillUseContext context))
            {
                return;
            }

            InstalledSkillRegistry.Instance.TryUseSlot(slotNumber, context);
            Refresh();
        }

        private bool TryCreateContext(out SkillUseContext context)
        {
            if (targetProvider == null)
            {
                targetProvider = FindFirstObjectByType<PlayerAimSkillTargetProvider>(FindObjectsInactive.Include);
            }

            if (targetProvider == null)
            {
                context = default;
                return false;
            }

            return targetProvider.TryCreateContext(targetProvider.gameObject, out context);
        }

        private void Refresh()
        {
            BindVisualsIfNeeded();

            InstalledSkillRegistry registry = InstalledSkillRegistry.Instance;
            SkillDefinitionSO skill = registry.GetSlotSkill(slotNumber);
            int readyCount = registry.GetSlotReadyCount(slotNumber);
            int totalCount = registry.GetSlotTotalCount(slotNumber);
            string countLabel = CreateCountLabel(skill, readyCount, totalCount);

            if (countText != null)
            {
                countText.text = countLabel;
            }

            if (legacyCountText != null)
            {
                legacyCountText.text = countLabel;
            }

            if (iconImage != null && !IsReservedSlotImage(iconImage))
            {
                Sprite icon = skill != null ? skill.Icon : null;
                iconImage.sprite = icon;
                iconImage.color = Color.white;
                iconImage.enabled = icon != null && totalCount > 0;
            }

            if (useButton != null)
            {
                useButton.interactable = readyCount > 0;
            }
        }

        private string CreateCountLabel(SkillDefinitionSO skill, int readyCount, int totalCount)
        {
            if (skill == null || totalCount <= 0)
            {
                return string.Empty;
            }

            return readyCount.ToString() + "/" + totalCount.ToString();
        }

        private void BindVisualsIfNeeded()
        {
            if (countText == null)
            {
                countText = GetComponentInChildren<TMP_Text>(true);
            }

            if (legacyCountText == null)
            {
                legacyCountText = GetComponentInChildren<Text>(true);
            }

            if (iconImage == null || IsReservedSlotImage(iconImage))
            {
                iconImage = FindNamedIconImage();
            }

        }

        private Image FindNamedIconImage()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image.transform == transform)
                {
                    continue;
                }

                string imageName = image.name;
                if (imageName == "Icon"
                    || imageName == "SkillIcon"
                    || imageName == "IconImage")
                {
                    return image;
                }
            }

            return null;
        }

        private bool IsReservedSlotImage(Image image)
        {
            if (image == null)
            {
                return false;
            }

            if (image.transform == transform)
            {
                return true;
            }

            return useButton != null && useButton.targetGraphic == image;
        }

    }
}
