using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TEMP_ItemRadialPanelPresenter : MonoBehaviour
    {
        public const int MaxItemCount = 8;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text[] itemTexts = new Text[MaxItemCount];
        [SerializeField] private Image[] itemImages = new Image[MaxItemCount];
        [SerializeField] private Text hintText;
        [SerializeField] private Color normalColor = new Color(0.06f, 0.18f, 0.28f, 0.92f);
        [SerializeField] private Color selectedColor = new Color(0.13f, 0.56f, 0.94f, 1f);

        private readonly List<TEMP_UsableItemEntry> visibleItems = new List<TEMP_UsableItemEntry>();
        private int selectedIndex;

        public int SelectedIndex => selectedIndex;
        public bool HasItems => visibleItems.Count > 0;

        private void Awake()
        {
            WarnIfMissingReferences();
            Hide();
        }

        public void Show(IReadOnlyList<TEMP_UsableItemEntry> items)
        {
            if (panelRoot == null)
            {
                Debug.LogWarning("[TEMP_ItemRadialPanelPresenter] Panel Root is not assigned.", this);
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
            panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public TEMP_UsableItemEntry GetSelectedItem()
        {
            return selectedIndex >= 0 && selectedIndex < visibleItems.Count ? visibleItems[selectedIndex] : null;
        }

        public void UpdateSelection(Vector2 screenPosition)
        {
            if (visibleItems.Count == 0)
            {
                selectedIndex = -1;
                Refresh();
                return;
            }

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 delta = screenPosition - center;
            if (delta.sqrMagnitude < 64f)
            {
                Refresh();
                return;
            }

            float angle = Mathf.Atan2(delta.y, delta.x);
            if (angle < 0f)
            {
                angle += Mathf.PI * 2f;
            }

            int index = Mathf.RoundToInt(angle / (Mathf.PI * 2f) * visibleItems.Count) % visibleItems.Count;
            if (selectedIndex != index)
            {
                selectedIndex = index;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (titleText != null)
            {
                titleText.text = "아이템 선택";
            }

            for (int i = 0; i < MaxItemCount; i++)
            {
                bool visible = i < visibleItems.Count;
                if (itemTexts != null && i < itemTexts.Length && itemTexts[i] != null)
                {
                    itemTexts[i].text = visible
                        ? $"{visibleItems[i].DisplayName}\nx{visibleItems[i].Charges}"
                        : string.Empty;
                }

                if (itemImages != null && i < itemImages.Length && itemImages[i] != null)
                {
                    itemImages[i].gameObject.SetActive(visible);
                    itemImages[i].color = i == selectedIndex ? selectedColor : normalColor;
                }
            }

            if (hintText != null)
            {
                hintText.text = visibleItems.Count > 0
                    ? "마우스를 끌어 선택, 좌클릭 사용"
                    : "사용 가능한 아이템 없음";
            }
        }

        private void WarnIfMissingReferences()
        {
            if (panelRoot == null)
            {
                Debug.LogWarning("[TEMP_ItemRadialPanelPresenter] Panel Root is not assigned.", this);
            }
        }
    }
}
