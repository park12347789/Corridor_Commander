using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TEMP_CommandPanelPresenter : MonoBehaviour
    {
        public const int MaxSlotCount = 5;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text[] slotTexts = new Text[MaxSlotCount];
        [SerializeField] private Image[] slotImages = new Image[MaxSlotCount];
        [SerializeField] private Text hintText;
        [SerializeField] private Text statusText;
        [SerializeField] private Color emptySlotColor = new Color(0f, 0f, 0f, 0.42f);
        [SerializeField] private Color filledSlotColor = new Color(0.04f, 0.16f, 0.25f, 0.62f);

        private void Awake()
        {
            WarnIfMissingReferences();
            SetPanelActive(true);
        }

        public void Show(
            TEMP_CommandPanelCategory category,
            IReadOnlyList<string> slotLabels,
            string status,
            float autoHideDelay)
        {
            if (panelRoot == null)
            {
                Debug.LogWarning("[TEMP_CommandPanelPresenter] Panel Root is not assigned.", this);
                return;
            }

            if (titleText != null)
            {
                titleText.text = ResolveTitle(category);
            }

            for (int i = 0; i < MaxSlotCount; i++)
            {
                string label = slotLabels != null && i < slotLabels.Count ? slotLabels[i] : string.Empty;
                bool hasEntry = !string.IsNullOrWhiteSpace(label);
                if (slotTexts != null && i < slotTexts.Length && slotTexts[i] != null)
                {
                    slotTexts[i].text = hasEntry ? $"[{i + 1}] {label}" : $"[{i + 1}] -";
                }

                if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                {
                    slotImages[i].color = hasEntry ? filledSlotColor : emptySlotColor;
                }
            }

            if (hintText != null)
            {
                hintText.text = "Q 짧게 세트 변경 / Q 길게 세트 선택 / 1-5 실행";
            }

            SetStatus(status);
            SetPanelActive(true);
        }

        public void SetStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = string.IsNullOrEmpty(status) ? string.Empty : status;
            }
        }

        public void Hide()
        {
            SetPanelActive(false);
        }

        private static string ResolveTitle(TEMP_CommandPanelCategory category)
        {
            return category switch
            {
                TEMP_CommandPanelCategory.Weapons => "Q 1/3 - 총",
                TEMP_CommandPanelCategory.TurretSkills => "Q 2/3 - 포탑",
                TEMP_CommandPanelCategory.SquadCommands => "Q 3/3 - 분대 명령",
                _ => "Q 명령 패널"
            };
        }

        private void WarnIfMissingReferences()
        {
            if (panelRoot == null)
            {
                Debug.LogWarning("[TEMP_CommandPanelPresenter] Panel Root is not assigned.", this);
            }
        }

        private void SetPanelActive(bool active)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(active);
            }
        }
    }
}
