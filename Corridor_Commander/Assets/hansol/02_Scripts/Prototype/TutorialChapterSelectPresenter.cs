using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TutorialChapterSelectPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private TMP_Text titleTmpText;
        [SerializeField] private Text detailText;
        [SerializeField] private TMP_Text detailTmpText;
        [SerializeField] private Text hintText;
        [SerializeField] private TMP_Text hintTmpText;
        [SerializeField] private Button[] chapterButtons;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text[] chapterButtonTexts;
        [SerializeField] private TMP_Text[] chapterButtonTmpTexts;
        [SerializeField] private Color selectedTextColor = new Color(1f, 0.92f, 0.42f, 1f);
        [SerializeField] private Color normalTextColor = Color.white;

        private TutorialChapterDefinition[] chapters;
        private int selectedIndex;
        private bool controlsBound;

        public event Action<int> ChapterSelected;

        private void Awake()
        {
            BindControls();
            Hide();
        }

        private void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }

            HandleKeyboard();
        }

        public void Show(TutorialChapterDefinition[] chapterDefinitions)
        {
            chapters = chapterDefinitions;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, GetChapterCount() - 1));
            SetActive(gameObject, true);
            BindControls();
            Refresh();
            SetActive(panelRoot, true);
            PopupDimOverlayController.RequestShow(this, panelRoot != null ? panelRoot.transform : transform);

            if (UiInputCoordinator.Instance != null)
            {
                UiInputCoordinator.Instance.TryBeginContext(this, UiInputContext.TutorialDialogue, true);
            }
        }

        public void Hide()
        {
            SetActive(panelRoot, false);
            PopupDimOverlayController.Release(this);
            UiInputCoordinator.EndContextIfActive(this);
        }

        private void HandleKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            for (int i = 0; i < GetChapterCount() && i < 9; i++)
            {
                if (KeyboardInputMessenger.WasMenuSlotPressed(i + 1))
                {
                    selectedIndex = i;
                    ConfirmSelection();
                    return;
                }
            }

            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            {
                MoveSelection(-1);
                return;
            }

            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                MoveSelection(1);
                return;
            }

            if (KeyboardInputMessenger.WasContextConfirmPressed())
            {
                ConfirmSelection();
                return;
            }

            if (KeyboardInputMessenger.WasCancelPressed())
            {
                Hide();
            }
        }

        private void MoveSelection(int delta)
        {
            int count = GetChapterCount();
            if (count <= 0)
            {
                return;
            }

            selectedIndex = (selectedIndex + delta + count) % count;
            Refresh();
        }

        private void ConfirmSelection()
        {
            if (selectedIndex < 0 || selectedIndex >= GetChapterCount())
            {
                Debug.LogError("[TutorialChapterSelectPresenter] Invalid chapter selection.");
                return;
            }

            ChapterSelected?.Invoke(selectedIndex);
        }

        private void Refresh()
        {
            SetText(titleTmpText, titleText, "튜토리얼 챕터 선택");
            SetText(hintTmpText, hintText, "1-7 선택 / W,S 이동 / Enter 확정 / ESC·뒤로 닫기");

            int buttonCount = chapterButtons != null ? chapterButtons.Length : 0;
            for (int i = 0; i < buttonCount; i++)
            {
                bool hasChapter = i < GetChapterCount();
                SetActive(chapterButtons[i] != null ? chapterButtons[i].gameObject : null, hasChapter);
                if (!hasChapter)
                {
                    continue;
                }

                TutorialChapterDefinition chapter = chapters[i];
                string marker = i == selectedIndex ? "> " : "  ";
                Color textColor = i == selectedIndex ? selectedTextColor : normalTextColor;
                SetText(GetTmpChapterText(i), GetLegacyChapterText(i), $"{marker}{chapter.Number}. {chapter.DisplayName}");
                SetColor(GetTmpChapterText(i), GetLegacyChapterText(i), textColor);
            }

            if ((detailTmpText != null || detailText != null) && selectedIndex >= 0 && selectedIndex < GetChapterCount())
            {
                TutorialChapterDefinition chapter = chapters[selectedIndex];
                SetText(detailTmpText, detailText, $"{chapter.DisplayName}\n{chapter.Description}");
            }

            SelectCurrentButton();
        }

        private void SelectCurrentButton()
        {
            if (chapterButtons == null || selectedIndex < 0 || selectedIndex >= chapterButtons.Length)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null && chapterButtons[selectedIndex] != null)
            {
                eventSystem.SetSelectedGameObject(chapterButtons[selectedIndex].gameObject);
            }
        }

        private int GetChapterCount()
        {
            return chapters != null ? chapters.Length : 0;
        }

        private void BindControls()
        {
            if (controlsBound || chapterButtons == null)
            {
                return;
            }

            for (int i = 0; i < chapterButtons.Length; i++)
            {
                int chapterIndex = i;
                if (chapterButtons[i] != null)
                {
                    chapterButtons[i].onClick.AddListener(() =>
                    {
                        selectedIndex = chapterIndex;
                        Refresh();
                        ConfirmSelection();
                    });
                }
            }

            closeButton ??= FindButton("Button_Back") ?? FindButton("BackButton") ?? FindButton("CloseButton");
            closeButton?.onClick.AddListener(Hide);

            controlsBound = true;
        }

        private Button FindButton(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                {
                    return children[i].GetComponent<Button>();
                }
            }

            return null;
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null)
            {
                target.SetActive(value);
            }
        }

        private Text GetLegacyChapterText(int index)
        {
            return chapterButtonTexts != null && index >= 0 && index < chapterButtonTexts.Length
                ? chapterButtonTexts[index]
                : null;
        }

        private TMP_Text GetTmpChapterText(int index)
        {
            return chapterButtonTmpTexts != null && index >= 0 && index < chapterButtonTmpTexts.Length
                ? chapterButtonTmpTexts[index]
                : null;
        }

        private static void SetText(TMP_Text tmpText, Text legacyText, string value)
        {
            if (tmpText != null)
            {
                tmpText.text = value ?? string.Empty;
                return;
            }

            if (legacyText != null)
            {
                legacyText.text = value ?? string.Empty;
            }
        }

        private static void SetColor(TMP_Text tmpText, Text legacyText, Color color)
        {
            if (tmpText != null)
            {
                tmpText.color = color;
                return;
            }

            if (legacyText != null)
            {
                legacyText.color = color;
            }
        }
    }
}
