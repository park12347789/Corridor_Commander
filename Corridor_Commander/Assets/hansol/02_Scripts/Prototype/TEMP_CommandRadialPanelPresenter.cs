using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TEMP_CommandRadialPanelPresenter : MonoBehaviour
    {
        private const int CategoryCount = 3;
        private const float DeadZoneSqrMagnitude = 64f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text hintText;
        [SerializeField] private Image[] sectorImages = new Image[CategoryCount];
        [SerializeField] private Text[] sectorTexts = new Text[CategoryCount];
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.72f);
        [SerializeField] private Color activeColor = new Color(0.5f, 0.8f, 1f, 0.9f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.2f, 0.16f, 0.88f);

        private TEMP_CommandPanelCategory activeCategory;
        private TEMP_CommandPanelCategory selectedCategory;

        public TEMP_CommandPanelCategory SelectedCategory => selectedCategory;

        private void Awake()
        {
            WarnIfMissingReferences();
            Hide();
        }

        public void Show(TEMP_CommandPanelCategory category)
        {
            if (panelRoot == null)
            {
                Debug.LogWarning("[TEMP_CommandRadialPanelPresenter] Panel Root is not assigned.", this);
                return;
            }

            activeCategory = category;
            selectedCategory = category;
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

        public void SetActiveCategory(TEMP_CommandPanelCategory category)
        {
            activeCategory = category;
            Refresh();
        }

        public void UpdateSelection(Vector2 screenPosition)
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 delta = screenPosition - center;
            if (delta.sqrMagnitude < DeadZoneSqrMagnitude)
            {
                return;
            }

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }

            if (angle >= 330f || angle < 90f)
            {
                selectedCategory = TEMP_CommandPanelCategory.TurretSkills;
            }
            else if (angle < 210f)
            {
                selectedCategory = TEMP_CommandPanelCategory.Weapons;
            }
            else
            {
                selectedCategory = TEMP_CommandPanelCategory.SquadCommands;
            }

            Refresh();
        }

        private void Refresh()
        {
            if (titleText != null)
            {
                titleText.text = "Q 선택";
            }

            for (int i = 0; i < CategoryCount; i++)
            {
                TEMP_CommandPanelCategory category = (TEMP_CommandPanelCategory)i;
                bool selected = category == selectedCategory;
                bool active = category == activeCategory;

                if (sectorImages != null && i < sectorImages.Length && sectorImages[i] != null)
                {
                    sectorImages[i].color = selected ? selectedColor : active ? activeColor : normalColor;
                }

                if (sectorTexts != null && i < sectorTexts.Length && sectorTexts[i] != null)
                {
                    sectorTexts[i].text = ResolveLabel(category, active);
                }
            }

            if (hintText != null)
            {
                hintText.text = "Q 떼기 선택 / 좌클릭 호출 유지";
            }
        }

        private static string ResolveLabel(TEMP_CommandPanelCategory category, bool active)
        {
            string label = category switch
            {
                TEMP_CommandPanelCategory.Weapons => "총",
                TEMP_CommandPanelCategory.TurretSkills => "포탑",
                TEMP_CommandPanelCategory.SquadCommands => "분대명령",
                _ => "명령"
            };

            return active ? label + "\n활성" : label;
        }

        private void WarnIfMissingReferences()
        {
            if (panelRoot == null)
            {
                Debug.LogWarning("[TEMP_CommandRadialPanelPresenter] Panel Root is not assigned.", this);
            }
        }
    }
}
