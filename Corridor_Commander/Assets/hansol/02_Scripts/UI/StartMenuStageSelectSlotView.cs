using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class StartMenuStageSelectSlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image selectionImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Color normalColor = new Color(0.08f, 0.18f, 0.3f, 0.92f);
        [SerializeField] private Color selectedColor = new Color(0.16f, 0.58f, 0.78f, 1f);
        [SerializeField] private Color dummyColor = new Color(0.08f, 0.08f, 0.1f, 0.88f);
        [SerializeField] private Color availableTextColor = Color.white;
        [SerializeField] private Color dummyTextColor = new Color(0.55f, 0.58f, 0.62f, 1f);

        private int index;
        private bool listenerBound;

        public event Action<int> Clicked;
        public event Action<int> DoubleClicked;

        private void Awake()
        {
            ResolveMissingReferences();
            BindButton();
        }

        private void OnEnable()
        {
            BindButton();
        }

        private void OnDisable()
        {
            UnbindButton();
        }

        public void Initialize(int slotIndex)
        {
            index = slotIndex;
            ResolveMissingReferences();
            BindButton();
        }

        public void SetContent(string title, string status, bool available, bool selected)
        {
            ResolveMissingReferences();

            if (titleText != null)
            {
                titleText.text = title;
                titleText.color = available ? availableTextColor : dummyTextColor;
            }

            if (statusText != null)
            {
                statusText.text = status;
                statusText.color = available ? availableTextColor : dummyTextColor;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = available
                    ? selected ? selectedColor : normalColor
                    : dummyColor;
            }

            if (selectionImage != null)
            {
                selectionImage.gameObject.SetActive(selected);
            }

            if (button != null)
            {
                button.interactable = true;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.clickCount >= 2)
            {
                DoubleClicked?.Invoke(index);
            }
        }

        private void HandleClick()
        {
            Clicked?.Invoke(index);
        }

        private void BindButton()
        {
            if (listenerBound || button == null)
            {
                return;
            }

            button.onClick.AddListener(HandleClick);
            listenerBound = true;
        }

        private void UnbindButton()
        {
            if (!listenerBound || button == null)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClick);
            listenerBound = false;
        }

        private void ResolveMissingReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }
        }
    }
}
