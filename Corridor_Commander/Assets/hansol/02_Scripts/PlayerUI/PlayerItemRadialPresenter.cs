using System.Collections.Generic;
using CorridorCommander.PlayerItems;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander.PlayerUI
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemRadialPresenter : MonoBehaviour
    {
        public const int MaxItemCount = 8;
        private const float WheelSize = 500f;
        private const float IconRadiusX = 190f;
        private const float IconRadiusY = 118f;
        private const float IconYOffset = -4f;
        private const float FirstSlotAngleDegrees = 90f;
        private const float DeadZoneSqrMagnitude = 64f;

        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text[] itemTexts = new Text[MaxItemCount];
        [SerializeField] private Image[] itemImages = new Image[MaxItemCount];
        [SerializeField] private Image[] itemIconImages = new Image[MaxItemCount];
        [SerializeField] private Text hintText;

        [Header("Labels")]
        [SerializeField] private string titleLabel = "아이템";
        [SerializeField] private string selectHintLabel = "F 떼기 사용 / 숫자 선택";
        [SerializeField] private string emptyHintLabel = "사용 가능한 아이템 없음";

        [Header("Style")]
        [SerializeField] private Color normalColor = new Color(0.01f, 0.08f, 0.28f, 0.38f);
        [SerializeField] private Color selectedColor = new Color(0.08f, 0.42f, 1f, 0.76f);

        private readonly List<PlayerItemRuntimeEntry> visibleItems = new List<PlayerItemRuntimeEntry>();
        private int selectedIndex;

        public int SelectedIndex => selectedIndex;
        public bool HasItems => visibleItems.Count > 0;

        private void Awake()
        {
            WarnIfMissingReferences();
            Hide();
        }

        public void Show(IReadOnlyList<PlayerItemRuntimeEntry> items)
        {
            if (panelRoot == null)
            {
                Debug.LogWarning("[PlayerItemRadialPresenter] Panel Root is not assigned.", this);
                return;
            }

            visibleItems.Clear();
            if (items != null)
            {
                for (int i = 0; i < items.Count && visibleItems.Count < MaxItemCount; i++)
                {
                    if (items[i] != null && items[i].IsAvailable)
                    {
                        visibleItems.Add(items[i]);
                    }
                }
            }

            selectedIndex = visibleItems.Count > 0 ? 0 : -1;
            Refresh();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public PlayerItemRuntimeEntry GetSelectedItem()
        {
            return selectedIndex >= 0 && selectedIndex < visibleItems.Count
                ? visibleItems[selectedIndex]
                : null;
        }

        public void SelectIndex(int index)
        {
            if (visibleItems.Count == 0)
            {
                selectedIndex = -1;
                Refresh();
                return;
            }

            selectedIndex = Mathf.Clamp(index, 0, visibleItems.Count - 1);
            Refresh();
        }

        public void UpdateSelection(Vector2 screenPosition)
        {
            if (visibleItems.Count == 0)
            {
                selectedIndex = -1;
                Refresh();
                return;
            }

            int index = ResolveSlotIndexFromAngle(screenPosition);
            if (index < 0)
            {
                return;
            }

            if (selectedIndex != index)
            {
                selectedIndex = index;
                Refresh();
            }
        }

        private void Refresh()
        {
            ApplyItemListLayout();

            if (titleText != null)
            {
                titleText.text = titleLabel;
            }

            for (int i = 0; i < MaxItemCount; i++)
            {
                bool visible = i < visibleItems.Count;
                if (itemTexts != null && i < itemTexts.Length && itemTexts[i] != null)
                {
                    itemTexts[i].text = visible ? CreateItemLabel(visibleItems[i]) : string.Empty;
                    ApplyItemTextLayout(i, itemTexts[i]);
                }

                if (itemImages != null && i < itemImages.Length && itemImages[i] != null)
                {
                    itemImages[i].gameObject.SetActive(visible);
                    itemImages[i].color = ResolveSlotColor(i == selectedIndex, itemImages[i]);
                    itemImages[i].transform.localScale = i == selectedIndex ? Vector3.one * 1.1f : Vector3.one;
                    if (i == selectedIndex)
                    {
                        itemImages[i].transform.SetAsLastSibling();
                    }
                }

                Sprite icon = visible && visibleItems[i] != null && visibleItems[i].ItemDefinition != null
                    ? visibleItems[i].ItemDefinition.icon
                    : null;
                SetItemIcon(i, icon, visible && icon != null);
            }

            if (hintText != null)
            {
                hintText.text = visibleItems.Count > 0 ? selectHintLabel : emptyHintLabel;
            }
        }

        private void WarnIfMissingReferences()
        {
            if (panelRoot == null)
            {
                Debug.LogWarning("[PlayerItemRadialPresenter] Panel Root is not assigned.", this);
            }
        }

        private static string CreateItemLabel(PlayerItemRuntimeEntry item)
        {
            if (item == null || item.ItemDefinition == null)
            {
                return string.Empty;
            }

            return "x" + item.Count.ToString();
        }

        private void SetItemIcon(int itemIndex, Sprite icon, bool visible)
        {
            Image iconImage = ResolveItemIconImage(itemIndex);
            if (iconImage == null)
            {
                return;
            }

            iconImage.sprite = icon;
            iconImage.color = Color.white;
            iconImage.enabled = visible && icon != null;
            iconImage.preserveAspect = true;
            ApplyItemIconLayout(itemIndex, iconImage.rectTransform);
        }

        private int ResolveSlotIndexFromAngle(Vector2 screenPosition)
        {
            Camera eventCamera = ResolveEventCamera();
            Vector2 center = ResolvePanelScreenCenter(eventCamera);
            Vector2 delta = screenPosition - center;
            if (delta.sqrMagnitude < DeadZoneSqrMagnitude)
            {
                return selectedIndex;
            }

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }

            float sectorSize = 360f / MaxItemCount;
            float clockwiseOffset = Mathf.Repeat(FirstSlotAngleDegrees - angle + sectorSize * 0.5f, 360f);
            int index = Mathf.FloorToInt(clockwiseOffset / sectorSize);
            return Mathf.Clamp(index, 0, visibleItems.Count - 1);
        }

        private Camera ResolveEventCamera()
        {
            Canvas canvas = panelRoot != null ? panelRoot.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private Vector2 ResolvePanelScreenCenter(Camera eventCamera)
        {
            if (panelRoot != null && panelRoot.transform is RectTransform rectTransform)
            {
                Vector3 worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
                return RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
            }

            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        private RectTransform ResolveItemSlotRect(int itemIndex)
        {
            if (itemImages == null || itemIndex < 0 || itemIndex >= itemImages.Length || itemImages[itemIndex] == null)
            {
                return null;
            }

            return itemImages[itemIndex].rectTransform;
        }

        private void ApplyItemListLayout()
        {
            for (int i = 0; i < MaxItemCount; i++)
            {
                RectTransform slotRect = ResolveItemSlotRect(i);
                if (slotRect == null)
                {
                    continue;
                }

                float sectorAngle = -(360f / MaxItemCount) * i;

                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.anchoredPosition = Vector2.zero;
                slotRect.localRotation = Quaternion.Euler(0f, 0f, sectorAngle);
                slotRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, WheelSize);
                slotRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, WheelSize);
            }
        }

        private Color ResolveSlotColor(bool selected, Image slotImage)
        {
            if (selected)
            {
                return selectedColor;
            }

            return slotImage != null && slotImage.sprite != null ? Color.white : normalColor;
        }

        private static void ApplyItemTextLayout(int itemIndex, Text text)
        {
            float angle = ResolveSlotAngleRadians(itemIndex);
            Vector2 position = new Vector2(
                Mathf.Cos(angle) * IconRadiusX,
                Mathf.Sin(angle) * IconRadiusY + IconYOffset - 48f);

            RectTransform rectTransform = text.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, (360f / MaxItemCount) * itemIndex);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 78f);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30f);

            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 16;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void ApplyItemIconLayout(int itemIndex, RectTransform iconRectTransform)
        {
            float angle = ResolveSlotAngleRadians(itemIndex);
            iconRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconRectTransform.pivot = new Vector2(0.5f, 0.5f);
            iconRectTransform.anchoredPosition = new Vector2(
                Mathf.Cos(angle) * IconRadiusX,
                Mathf.Sin(angle) * IconRadiusY + IconYOffset);
            iconRectTransform.localRotation = Quaternion.Euler(0f, 0f, (360f / MaxItemCount) * itemIndex);
            iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 74f);
            iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 74f);
        }

        private static float ResolveSlotAngleRadians(int itemIndex)
        {
            return (FirstSlotAngleDegrees - (360f / MaxItemCount) * itemIndex) * Mathf.Deg2Rad;
        }

        private Image ResolveItemIconImage(int itemIndex)
        {
            if (itemIconImages == null || itemIconImages.Length < MaxItemCount)
            {
                itemIconImages = new Image[MaxItemCount];
            }

            if (itemIndex < 0 || itemIndex >= itemIconImages.Length)
            {
                return null;
            }

            if (itemIconImages[itemIndex] != null)
            {
                return itemIconImages[itemIndex];
            }

            Transform itemRoot = itemImages != null
                && itemIndex < itemImages.Length
                && itemImages[itemIndex] != null
                    ? itemImages[itemIndex].transform
                    : null;

            if (itemRoot == null)
            {
                return null;
            }

            itemIconImages[itemIndex] = CreateIconImage(itemRoot);
            return itemIconImages[itemIndex];
        }

        private static Image CreateIconImage(Transform parent)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(parent, false);

            RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 74f);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 74f);

            Image image = iconObject.AddComponent<Image>();
            image.enabled = false;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

    }
}
