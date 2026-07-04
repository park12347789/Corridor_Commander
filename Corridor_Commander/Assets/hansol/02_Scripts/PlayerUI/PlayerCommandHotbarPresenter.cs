using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CorridorCommander.PlayerUI
{
    [DisallowMultipleComponent]
    public sealed class PlayerCommandHotbarPresenter : MonoBehaviour
    {
        public const int MaxSlotCount = 5;

        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private TMP_Text titleTmpText;
        [SerializeField] private Text[] slotTexts = new Text[MaxSlotCount];
        [SerializeField] private TMP_Text[] slotTmpTexts = new TMP_Text[MaxSlotCount];
        [SerializeField] private Image[] slotImages = new Image[MaxSlotCount];
        [SerializeField] private Image[] slotIconImages = new Image[MaxSlotCount];
        [SerializeField] private Text hintText;
        [SerializeField] private TMP_Text hintTmpText;
        [SerializeField] private Text statusText;
        [SerializeField] private TMP_Text statusTmpText;

        [Header("Style")]
        [SerializeField] private Color emptySlotColor = new Color(0.08f, 0.22f, 0.36f, 0.78f);
        [SerializeField] private Color filledSlotColor = Color.white;

        [Header("Auto Binding")]
        [SerializeField] private bool allowAutoBindNamedSlots = false;
        [SerializeField] private bool createDefaultUiIfMissing = true;

        [Header("Legacy Slot Bar")]
        [SerializeField] private bool disableLegacySkillSlotPresenters = false;
        [SerializeField] private bool reapplyLabelsInLateUpdate = false;

        private readonly List<string> lastSlotLabels = new List<string>(MaxSlotCount);
        private readonly List<Sprite> lastSlotIcons = new List<Sprite>(MaxSlotCount);
        private string lastTitle;
        private string lastStatus;
        private string lastHint;
        private bool hasPresentation;
        private bool suppressedByModalUi;

        public void UseDedicatedGeneratedUiMode()
        {
            allowAutoBindNamedSlots = false;
            createDefaultUiIfMissing = true;
            disableLegacySkillSlotPresenters = false;
            reapplyLabelsInLateUpdate = false;
        }

        private void Awake()
        {
            BindUiIfNeeded();
            DisableConflictingPresentersIfNeeded();
            WarnIfBoundUnderForbiddenUi();
            SetPanelActive(true);
        }

        private void LateUpdate()
        {
            bool shouldSuppress = ShouldSuppressForModalUi();
            if (suppressedByModalUi != shouldSuppress)
            {
                suppressedByModalUi = shouldSuppress;
                if (hasPresentation)
                {
                    SetPanelActive(!suppressedByModalUi);
                }
            }

            if (suppressedByModalUi)
            {
                return;
            }

            if (!reapplyLabelsInLateUpdate || !hasPresentation)
            {
                return;
            }

            ApplyPresentation(lastTitle, lastSlotLabels, lastStatus, lastHint, lastSlotIcons);
        }

        public void Show(
            string title,
            IReadOnlyList<string> slotLabels,
            string status,
            string hint = "Q short: cycle / Q hold: select / 1-5: use",
            IReadOnlyList<Sprite> slotIcons = null)
        {
            BindUiIfNeeded();
            DisableConflictingPresentersIfNeeded();
            WarnIfBoundUnderForbiddenUi();

            if (!HasAnyBoundSlot())
            {
                Debug.LogWarning(
                    "[PlayerCommandHotbarPresenter] No hotbar slots are bound. " +
                    "Assign Slot Texts in the Inspector or name existing slots CommandSlot_01, HotbarSlot_01, " +
                    "PlayerCommandSlot_01, SkillSlot_1, or Slot_01 under a Hotbar/SkillSlotBar/Bottom/CommandPanel parent.");
                return;
            }

            CachePresentation(title, slotLabels, status, hint, slotIcons);
            ApplyPresentation(lastTitle, lastSlotLabels, lastStatus, lastHint, lastSlotIcons);
            suppressedByModalUi = ShouldSuppressForModalUi();
            SetPanelActive(!suppressedByModalUi);
        }

        private void CachePresentation(
            string title,
            IReadOnlyList<string> slotLabels,
            string status,
            string hint,
            IReadOnlyList<Sprite> slotIcons)
        {
            lastTitle = title;
            lastStatus = status;
            lastHint = hint;
            lastSlotLabels.Clear();
            lastSlotIcons.Clear();

            for (int i = 0; i < MaxSlotCount; i++)
            {
                lastSlotLabels.Add(slotLabels != null && i < slotLabels.Count
                    ? slotLabels[i]
                    : string.Empty);

                lastSlotIcons.Add(slotIcons != null && i < slotIcons.Count
                    ? slotIcons[i]
                    : null);
            }

            hasPresentation = true;
        }

        private void ApplyPresentation(
            string title,
            IReadOnlyList<string> slotLabels,
            string status,
            string hint,
            IReadOnlyList<Sprite> slotIcons)
        {
            SetText(titleText, titleTmpText, string.IsNullOrEmpty(title) ? "Command Panel" : title);

            for (int i = 0; i < MaxSlotCount; i++)
            {
                string label = slotLabels != null && i < slotLabels.Count
                    ? slotLabels[i]
                    : string.Empty;

                bool hasEntry = !string.IsNullOrWhiteSpace(label);
                Sprite icon = slotIcons != null && i < slotIcons.Count ? slotIcons[i] : null;
                bool usesIconMode = icon != null;

                if (slotTexts != null && i < slotTexts.Length && slotTexts[i] != null)
                {
                    slotTexts[i].text = usesIconMode
                        ? CreateSlotQuantityLabel(label, hasEntry)
                        : (hasEntry ? $"[{i + 1}] {label}" : string.Empty);

                    if (usesIconMode)
                    {
                        ApplySlotQuantityLayout(slotTexts[i]);
                    }
                }

                if (slotTmpTexts != null && i < slotTmpTexts.Length && slotTmpTexts[i] != null)
                {
                    slotTmpTexts[i].SetText(usesIconMode
                        ? CreateSlotQuantityLabel(label, hasEntry)
                        : (hasEntry ? $"[{i + 1}] {label}" : string.Empty));
                    slotTmpTexts[i].enabled = hasEntry;
                }

                if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                {
                    slotImages[i].color = hasEntry ? filledSlotColor : emptySlotColor;
                }

                SetSlotIcon(i, icon, hasEntry);
            }

            SetText(hintText, hintTmpText, hint);

            SetStatus(status);
        }

        public void SetStatus(string status)
        {
            SetText(statusText, statusTmpText, string.IsNullOrEmpty(status) ? string.Empty : status);
        }

        private static void SetText(Text text, TMP_Text tmpText, string value)
        {
            if (tmpText != null)
            {
                tmpText.SetText(value);
            }

            if (text != null)
            {
                text.text = value;
            }
        }

        public void Hide()
        {
            SetPanelActive(false);
        }

        private static bool ShouldSuppressForModalUi()
        {
            return UiInputCoordinator.CurrentContext switch
            {
                UiInputContext.SupportTruckShop => true,
                UiInputContext.PlacementBuildMenu => true,
                UiInputContext.InstalledObjectMenu => true,
                UiInputContext.TreasureRewardMenu => true,
                UiInputContext.LegacyBuildMenu => true,
                UiInputContext.PauseMenu => true,
                UiInputContext.WaveReadyPopup => true,
                UiInputContext.TutorialDialogue => true,
                UiInputContext.GameOverScreen => true,
                _ => false
            };
        }

        private void SetSlotIcon(int slotIndex, Sprite icon, bool hasEntry)
        {
            Image iconImage = ResolveSlotIconImage(slotIndex);
            if (iconImage == null)
            {
                return;
            }

            iconImage.sprite = icon;
            iconImage.color = Color.white;
            iconImage.enabled = hasEntry && icon != null;
            iconImage.preserveAspect = true;
            ApplySlotIconLayout(slotIndex, iconImage.rectTransform);
            HideUnboundSlotIcons(slotIndex, iconImage);
            SetSlotPlaceholderVisible(slotIndex, !hasEntry);
        }

        private Image ResolveSlotIconImage(int slotIndex)
        {
            if (slotIconImages == null || slotIconImages.Length < MaxSlotCount)
            {
                slotIconImages = new Image[MaxSlotCount];
            }

            if (slotIndex < 0 || slotIndex >= slotIconImages.Length)
            {
                return null;
            }

            if (slotIconImages[slotIndex] != null)
            {
                return slotIconImages[slotIndex];
            }

            Transform slotRoot = ResolveSlotRoot(slotIndex);
            if (slotRoot == null)
            {
                return null;
            }

            Image existingIconImage = FindExistingSlotIconImage(slotIndex, slotRoot);
            if (existingIconImage != null)
            {
                slotIconImages[slotIndex] = existingIconImage;
                existingIconImage.raycastTarget = false;
                existingIconImage.preserveAspect = true;
                ApplySlotIconLayout(slotIndex, existingIconImage.rectTransform);
                return existingIconImage;
            }

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(slotRoot, false);

            RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;

            Image image = iconObject.AddComponent<Image>();
            image.enabled = false;
            image.raycastTarget = false;
            image.preserveAspect = true;
            slotIconImages[slotIndex] = image;
            ApplySlotIconLayout(slotIndex, rectTransform);
            return image;
        }

        private Image FindExistingSlotIconImage(int slotIndex, Transform slotRoot)
        {
            if (slotRoot == null)
            {
                return null;
            }

            Image backgroundImage = GetSlotBackgroundImage(slotIndex);
            Image[] images = slotRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image == backgroundImage || IsSlotPlaceholderImage(slotRoot, image))
                {
                    continue;
                }

                if (IsSlotIconCandidate(slotRoot, image))
                {
                    return image;
                }
            }

            return null;
        }

        private void HideUnboundSlotIcons(int slotIndex, Image boundIconImage)
        {
            Transform slotRoot = ResolveSlotRoot(slotIndex);
            if (slotRoot == null)
            {
                return;
            }

            Image backgroundImage = GetSlotBackgroundImage(slotIndex);
            Image[] images = slotRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null
                    || image == boundIconImage
                    || image == backgroundImage
                    || IsSlotPlaceholderImage(slotRoot, image)
                    || !IsSlotIconCandidate(slotRoot, image))
                {
                    continue;
                }

                image.enabled = false;
                image.sprite = null;
            }
        }

        private static bool IsSlotIconCandidate(Transform slotRoot, Image image)
        {
            if (slotRoot == null || image == null)
            {
                return false;
            }

            if (IsIconName(image.name))
            {
                return true;
            }

            Transform current = image.transform;
            while (current != null && current != slotRoot)
            {
                if (IsIconName(current.name))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsIconName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            return string.Equals(objectName, "Icon", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(objectName, "ItemIcon", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(objectName, "SkillIcon", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(objectName, "IconImage", System.StringComparison.OrdinalIgnoreCase);
        }

        private void ApplySlotIconLayout(int slotIndex, RectTransform iconRectTransform)
        {
            if (iconRectTransform == null)
            {
                return;
            }

            Transform slotRoot = ResolveSlotRoot(slotIndex);
            RectTransform slotRectTransform = slotRoot as RectTransform;
            float iconSize = 72f;
            float iconYOffset = 12f;

            if (slotRectTransform != null)
            {
                Rect rect = slotRectTransform.rect;
                float slotMinSize = Mathf.Min(rect.width, rect.height);
                if (slotMinSize > 0f)
                {
                    iconSize = Mathf.Clamp(slotMinSize - 34f, 58f, 82f);
                }
            }

            iconRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconRectTransform.pivot = new Vector2(0.5f, 0.5f);
            iconRectTransform.anchoredPosition = new Vector2(0f, iconYOffset);
            iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, iconSize);
            iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, iconSize);
        }

        private static string CreateSlotQuantityLabel(string label, bool hasEntry)
        {
            if (!hasEntry)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                return "x1";
            }

            string[] lines = label.Split('\n', '\r');
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i].Trim();
                if (!string.IsNullOrWhiteSpace(line) && !string.Equals(line, label.Trim(), System.StringComparison.Ordinal))
                {
                    return line;
                }
            }

            return "x1";
        }

        private static void ApplySlotQuantityLayout(Text text)
        {
            RectTransform rectTransform = text.rectTransform;
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(-8f, 6f);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 56f);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 24f);

            text.alignment = TextAnchor.LowerRight;
            text.fontSize = 15;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void SetSlotPlaceholderVisible(int slotIndex, bool visible)
        {
            Transform slotRoot = ResolveSlotRoot(slotIndex);
            if (slotRoot == null)
            {
                return;
            }

            Image[] images = slotRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image == GetSlotBackgroundImage(slotIndex) || image == GetSlotIconImage(slotIndex))
                {
                    continue;
                }

                if (IsSlotPlaceholderImage(slotRoot, image))
                {
                    image.enabled = visible;
                }
            }
        }

        private static bool IsSlotPlaceholderImage(Transform slotRoot, Image image)
        {
            if (image == null || slotRoot == null)
            {
                return false;
            }

            if (IsPlaceholderName(image.name)
                || image.sprite != null && IsPlaceholderName(image.sprite.name))
            {
                return true;
            }

            Transform current = image.transform;
            while (current != null && current != slotRoot)
            {
                if (IsPlaceholderName(current.name))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsPlaceholderName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            return objectName.IndexOf("Empty", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || objectName.IndexOf("Placeholder", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || objectName.IndexOf("Cross", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || objectName.IndexOf("Close", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || string.Equals(objectName, "X", System.StringComparison.OrdinalIgnoreCase);
        }

        private Image GetSlotBackgroundImage(int slotIndex)
        {
            return slotImages != null && slotIndex >= 0 && slotIndex < slotImages.Length
                ? slotImages[slotIndex]
                : null;
        }

        private Image GetSlotIconImage(int slotIndex)
        {
            return slotIconImages != null && slotIndex >= 0 && slotIndex < slotIconImages.Length
                ? slotIconImages[slotIndex]
                : null;
        }

        private Transform ResolveSlotRoot(int slotIndex)
        {
            if (slotTmpTexts != null
                && slotIndex >= 0
                && slotIndex < slotTmpTexts.Length
                && slotTmpTexts[slotIndex] != null
                && slotTmpTexts[slotIndex].transform.parent != null)
            {
                return slotTmpTexts[slotIndex].transform.parent;
            }

            if (slotTexts != null
                && slotIndex >= 0
                && slotIndex < slotTexts.Length
                && slotTexts[slotIndex] != null
                && slotTexts[slotIndex].transform.parent != null)
            {
                return slotTexts[slotIndex].transform.parent;
            }

            if (slotImages != null
                && slotIndex >= 0
                && slotIndex < slotImages.Length
                && slotImages[slotIndex] != null)
            {
                return slotImages[slotIndex].transform;
            }

            return null;
        }

        private void BindUiIfNeeded()
        {
            if (HasConfiguredSlots())
            {
                return;
            }

            if ((allowAutoBindNamedSlots || panelRoot != null) && TryBindNamedHotbarSlots())
            {
                return;
            }

            if (createDefaultUiIfMissing)
            {
                CreateDefaultUi();
            }
        }

        private bool HasConfiguredSlots()
        {
            if ((slotTexts == null || slotTexts.Length < MaxSlotCount)
                && (slotTmpTexts == null || slotTmpTexts.Length < MaxSlotCount))
            {
                return false;
            }

            for (int i = 0; i < MaxSlotCount; i++)
            {
                Transform textTransform = GetSlotTextTransform(i);
                if (textTransform == null)
                {
                    return false;
                }

                if (HasForbiddenUiAncestor(textTransform))
                {
                    return false;
                }
            }

            ResolvePanelRootFromSlotsIfNeeded();
            return true;
        }

        private bool HasAnyBoundSlot()
        {
            if (slotTexts != null)
            {
                for (int i = 0; i < slotTexts.Length; i++)
                {
                    if (slotTexts[i] != null)
                    {
                        return true;
                    }
                }
            }

            if (slotTmpTexts != null)
            {
                for (int i = 0; i < slotTmpTexts.Length; i++)
                {
                    if (slotTmpTexts[i] != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryBindNamedHotbarSlots()
        {
            RectTransform[] rectTransforms = FindObjectsByType<RectTransform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (rectTransforms == null || rectTransforms.Length == 0)
            {
                return false;
            }

            Text[] matchedTexts = new Text[MaxSlotCount];
            TMP_Text[] matchedTmpTexts = new TMP_Text[MaxSlotCount];
            Image[] matchedImages = new Image[MaxSlotCount];
            int matchedCount = 0;

            for (int slotIndex = 0; slotIndex < MaxSlotCount; slotIndex++)
            {
                Transform slot = FindStrictHotbarSlotTransform(rectTransforms, slotIndex + 1);

                if (slot == null)
                {
                    continue;
                }

                Text text = FindSlotText(slot);
                TMP_Text tmpText = FindSlotTmpText(slot);

                if (text == null && tmpText == null)
                {
                    continue;
                }

                matchedTexts[slotIndex] = text;
                matchedTmpTexts[slotIndex] = tmpText;

                Image image = slot.GetComponent<Image>();
                if (image == null && text != null)
                {
                    image = text.GetComponentInParent<Image>();
                }

                if (image == null && tmpText != null)
                {
                    image = tmpText.GetComponentInParent<Image>();
                }

                matchedImages[slotIndex] = image;
                matchedCount++;
            }

            if (matchedCount <= 0)
            {
                return false;
            }

            slotTexts = matchedTexts;
            slotTmpTexts = matchedTmpTexts;
            slotImages = matchedImages;
            ResolvePanelRootFromSlotsIfNeeded();
            return true;
        }

        private void DisableConflictingPresentersIfNeeded()
        {
            if (!disableLegacySkillSlotPresenters)
            {
                return;
            }

            for (int i = 0; i < MaxSlotCount; i++)
            {
                Transform textTransform = GetSlotTextTransform(i);
                if (textTransform == null)
                {
                    continue;
                }

                DisableSkillSlotPresenterInParents(textTransform);
            }

            if (panelRoot != null)
            {
                global::CorridorCommander.SkillSlotPresenter[] presenters =
                    panelRoot.GetComponentsInChildren<global::CorridorCommander.SkillSlotPresenter>(true);
                for (int i = 0; i < presenters.Length; i++)
                {
                    if (presenters[i] != null)
                    {
                        presenters[i].enabled = false;
                    }
                }
            }
        }

        private static void DisableSkillSlotPresenterInParents(Transform transform)
        {
            while (transform != null)
            {
                global::CorridorCommander.SkillSlotPresenter presenter =
                    transform.GetComponent<global::CorridorCommander.SkillSlotPresenter>();
                if (presenter != null)
                {
                    presenter.enabled = false;
                }

                transform = transform.parent;
            }
        }

        private static Transform FindStrictHotbarSlotTransform(
            RectTransform[] rectTransforms,
            int slotNumber)
        {
            string plainName = "CommandSlot_" + slotNumber;
            string paddedName = "CommandSlot_" + slotNumber.ToString("00");

            string hotbarPlainName = "HotbarSlot_" + slotNumber;
            string hotbarPaddedName = "HotbarSlot_" + slotNumber.ToString("00");

            string playerPlainName = "PlayerCommandSlot_" + slotNumber;
            string playerPaddedName = "PlayerCommandSlot_" + slotNumber.ToString("00");

            string skillPlainName = "SkillSlot_" + slotNumber;
            string skillPaddedName = "SkillSlot_" + slotNumber.ToString("00");

            string fallbackPaddedName = "Slot_" + slotNumber.ToString("00");

            for (int i = 0; i < rectTransforms.Length; i++)
            {
                RectTransform rectTransform = rectTransforms[i];

                if (rectTransform == null)
                {
                    continue;
                }

                string objectName = rectTransform.name;

                bool nameMatched =
                    objectName == plainName ||
                    objectName == paddedName ||
                    objectName == hotbarPlainName ||
                    objectName == hotbarPaddedName ||
                    objectName == playerPlainName ||
                    objectName == playerPaddedName ||
                    objectName == skillPlainName ||
                    objectName == skillPaddedName ||
                    objectName == fallbackPaddedName;

                if (!nameMatched)
                {
                    continue;
                }

                if (!HasValidHotbarAncestor(rectTransform))
                {
                    continue;
                }

                if (HasForbiddenUiAncestor(rectTransform))
                {
                    continue;
                }

                return rectTransform;
            }

            return null;
        }

        private static bool HasValidHotbarAncestor(Transform transform)
        {
            return HasAncestorNamed(transform, "CommandHotbar")
                   || HasAncestorNamed(transform, "PlayerHotbar")
                   || HasAncestorNamed(transform, "BottomHotbar")
                   || HasAncestorNamed(transform, "InstalledSkillSlotBar")
                   || HasAncestorNamed(transform, "SkillSlotBar")
                   || HasAncestorNamed(transform, "SlotBar")
                   || HasAncestorNamed(transform, "Hotbar")
                   || HasAncestorNamed(transform, "Bottom")
                   || HasAncestorNamed(transform, "PlayerCommandPanel")
                   || HasAncestorNamed(transform, "CommandPanelRoot");
        }

        private static bool HasForbiddenUiAncestor(Transform transform)
        {
            return HasAncestorNamed(transform, "Shop")
                   || HasAncestorNamed(transform, "SupportTruck")
                   || HasAncestorNamed(transform, "Reward")
                   || HasAncestorNamed(transform, "Treasure")
                   || HasAncestorNamed(transform, "Build")
                   || HasAncestorNamed(transform, "Placement")
                   || HasAncestorNamed(transform, "InstalledObject")
                   || HasAncestorNamed(transform, "ActionMenu");
        }

        private static bool HasAncestorNamed(Transform transform, string namePart)
        {
            while (transform != null)
            {
                if (transform.name.IndexOf(
                        namePart,
                        System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }

        private static Text FindSlotText(Transform slot)
        {
            Text[] texts = slot.GetComponentsInChildren<Text>(true);

            if (texts == null || texts.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "LabelText")
                {
                    return texts[i];
                }
            }

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "CountText")
                {
                    return texts[i];
                }
            }

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "Text")
                {
                    return texts[i];
                }
            }

            return texts[0];
        }

        private static TMP_Text FindSlotTmpText(Transform slot)
        {
            TMP_Text[] texts = slot.GetComponentsInChildren<TMP_Text>(true);

            if (texts == null || texts.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "LabelText")
                {
                    return texts[i];
                }
            }

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "CountText")
                {
                    return texts[i];
                }
            }

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "Text")
                {
                    return texts[i];
                }
            }

            return texts[0];
        }

        private Transform GetSlotTextTransform(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlotCount)
            {
                return null;
            }

            if (slotTexts != null
                && slotIndex < slotTexts.Length
                && slotTexts[slotIndex] != null)
            {
                return slotTexts[slotIndex].transform;
            }

            if (slotTmpTexts != null
                && slotIndex < slotTmpTexts.Length
                && slotTmpTexts[slotIndex] != null)
            {
                return slotTmpTexts[slotIndex].transform;
            }

            return null;
        }

        private void ResolvePanelRootFromSlotsIfNeeded()
        {
            if ((slotTexts == null || slotTexts.Length == 0)
                && (slotTmpTexts == null || slotTmpTexts.Length == 0))
            {
                return;
            }

            if (panelRoot != null && !HasForbiddenUiAncestor(panelRoot.transform))
            {
                return;
            }

            panelRoot = null;

            for (int i = 0; i < MaxSlotCount; i++)
            {
                Transform textTransform = GetSlotTextTransform(i);
                if (textTransform == null)
                {
                    continue;
                }

                Transform hotbarRoot = FindPreferredPanelRoot(textTransform);
                if (hotbarRoot != null)
                {
                    panelRoot = hotbarRoot.gameObject;
                }

                return;
            }
        }

        private void WarnIfBoundUnderForbiddenUi()
        {
            if (panelRoot != null && HasForbiddenUiAncestor(panelRoot.transform))
            {
                Debug.LogWarning(
                    "[PlayerCommandHotbarPresenter] Panel Root is under a shop/reward/build UI. " +
                    "Move the hotbar outside temporary menus or assign InstalledSkillSlotBar as Panel Root.",
                    this);
            }

            for (int i = 0; i < MaxSlotCount; i++)
            {
                Transform textTransform = GetSlotTextTransform(i);
                if (textTransform == null || !HasForbiddenUiAncestor(textTransform))
                {
                    continue;
                }

                Debug.LogWarning(
                    "[PlayerCommandHotbarPresenter] Slot Text " + (i + 1).ToString() +
                    " is under a shop/reward/build UI and will disappear when that menu closes.",
                    textTransform);
            }
        }

        private static Transform FindPreferredPanelRoot(Transform transform)
        {
            Transform fallback = null;

            while (transform != null)
            {
                if (HasForbiddenUiAncestor(transform))
                {
                    return null;
                }

                if (transform.name.IndexOf("CommandHotbar", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || transform.name.IndexOf("PlayerHotbar", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || transform.name.IndexOf("BottomHotbar", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || transform.name.IndexOf("InstalledSkillSlotBar", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || transform.name.IndexOf("SkillSlotBar", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || transform.name.IndexOf("SlotBar", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || transform.name.IndexOf("Hotbar", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || transform.name.IndexOf("PlayerCommandPanel", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || transform.name.IndexOf("CommandPanelRoot", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return transform;
                }

                if (fallback == null && transform is RectTransform)
                {
                    fallback = transform;
                }

                transform = transform.parent;
            }

            return fallback;
        }

        private void CreateDefaultUi()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
                panelRoot = null;
            }

            slotTexts = new Text[MaxSlotCount];
            slotTmpTexts = new TMP_Text[MaxSlotCount];
            slotImages = new Image[MaxSlotCount];
            slotIconImages = new Image[MaxSlotCount];

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform parent = transform;

            panelRoot = CreatePanel(
                "PlayerCommandHotbar",
                parent,
                new Vector2(1020f, 112f),
                new Vector2(0.5f, 0f));

            titleText = CreateText(
                "TitleText",
                panelRoot.transform,
                font,
                22,
                TextAnchor.MiddleCenter);

            SetRect(
                titleText.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(260f, 28f),
                new Vector2(0f, -8f));

            float startX = -224f;

            for (int i = 0; i < MaxSlotCount; i++)
            {
                GameObject slot = CreatePanel(
                    "CommandSlot_" + (i + 1).ToString("00"),
                    panelRoot.transform,
                    new Vector2(94f, 64f),
                    new Vector2(0.5f, 0f));

                SetRect(
                    slot.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0f),
                    new Vector2(94f, 64f),
                    new Vector2(startX + i * 112f, 10f));

                Image image = slot.GetComponent<Image>();
                image.color = emptySlotColor;

                slotImages[i] = image;

                slotTexts[i] = CreateText(
                    "LabelText",
                    slot.transform,
                    font,
                    15,
                    TextAnchor.MiddleCenter);
            }

            hintText = CreateText(
                "HintText",
                panelRoot.transform,
                font,
                18,
                TextAnchor.MiddleCenter);

            SetRect(
                hintText.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(360f, 24f),
                new Vector2(12f, -8f));

            statusText = CreateText(
                "StatusText",
                panelRoot.transform,
                font,
                18,
                TextAnchor.MiddleCenter);

            SetRect(
                statusText.rectTransform,
                new Vector2(1f, 1f),
                new Vector2(360f, 24f),
                new Vector2(-12f, -8f));

            EnsureEventSystem();
        }

        private void SetPanelActive(bool active)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(active);
            }
        }

        private static GameObject CreatePanel(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 anchor)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);

            SetRect(
                panel.GetComponent<RectTransform>(),
                anchor,
                size,
                Vector2.zero);

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.76f);

            return panel;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int size,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        private static void SetRect(
            RectTransform rectTransform,
            Vector2 anchor,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}
