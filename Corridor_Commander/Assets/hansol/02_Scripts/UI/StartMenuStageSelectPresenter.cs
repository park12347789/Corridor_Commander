using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class StartMenuStageSelectPresenter : MonoBehaviour
    {
        [Serializable]
        private sealed class StageSlot
        {
            [SerializeField] private string displayName = "스테이지01";
            [SerializeField] private string sceneName = "MainScene";
            [SerializeField] private bool available = true;
            [SerializeField] private string statusText = "입장가능";

            public StageSlot()
            {
            }

            public StageSlot(string displayName, string sceneName, bool available, string statusText)
            {
                this.displayName = displayName;
                this.sceneName = sceneName;
                this.available = available;
                this.statusText = statusText;
            }

            public string DisplayName => displayName;
            public string SceneName => sceneName;
            public bool Available => available;
            public string StatusText => statusText;
        }

        [Header("Roots")]
        [SerializeField] private GameObject panelRoot;

        [Header("Text")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text hintText;

        [Header("Controls")]
        [SerializeField] private StartMenuStageSelectSlotView[] slotViews = new StartMenuStageSelectSlotView[3];
        [SerializeField] private Button closeButton;
        [SerializeField] private MousePositionIconPresenter mouseIconPresenter;

        [Header("Stages")]
        [SerializeField]
        private StageSlot[] stages =
        {
            new StageSlot("스테이지01", "MainScene", true, "입장가능"),
            new StageSlot("스테이지02", string.Empty, false, "준비중"),
            new StageSlot("스테이지03", string.Empty, false, "준비중")
        };

        private int selectedIndex;
        private bool controlsBound;

        public event Action<string> StageSceneSelected;

        private void Awake()
        {
            BindControls();
            Hide();
        }

        private void OnEnable()
        {
            BindControls();
        }

        private void OnDisable()
        {
            UnbindControls();
        }

        private void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }

            HandleKeyboard();
        }

        public void Show()
        {
            if (panelRoot == null)
            {
                Debug.LogError("[StartMenuStageSelectPresenter] Panel root is not assigned.", this);
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, GetStageCount() - 1));
            panelRoot.SetActive(true);
            SetMouseIconVisible(true);
            Refresh();
            SelectCurrentButton();
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            SetMouseIconVisible(false);
        }

        private void HandleKeyboard()
        {
            Keyboard keyboard = KeyboardInputMessenger.CurrentKeyboard;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                MoveSelection(-1);
                return;
            }

            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                MoveSelection(1);
                return;
            }

            if (KeyboardInputMessenger.WasInteractPressed() || KeyboardInputMessenger.WasContextConfirmPressed())
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
            int count = GetStageCount();
            if (count <= 0)
            {
                Debug.LogError("[StartMenuStageSelectPresenter] No stage slots configured.", this);
                return;
            }

            selectedIndex = (selectedIndex + delta + count) % count;
            Refresh();
            SelectCurrentButton();
        }

        private void SelectSlot(int index)
        {
            if (index < 0 || index >= GetStageCount())
            {
                Debug.LogError("[StartMenuStageSelectPresenter] Invalid stage slot index: " + index, this);
                return;
            }

            selectedIndex = index;
            Refresh();
            SelectCurrentButton();
        }

        private void ConfirmSlot(int index)
        {
            SelectSlot(index);
            ConfirmSelection();
        }

        private void ConfirmSelection()
        {
            StageSlot stage = GetSelectedStage();
            if (stage == null)
            {
                Debug.LogError("[StartMenuStageSelectPresenter] Selected stage slot is missing.", this);
                return;
            }

            if (!stage.Available)
            {
                Debug.LogError("[StartMenuStageSelectPresenter] Stage slot is dummy: " + stage.DisplayName, this);
                return;
            }

            if (string.IsNullOrWhiteSpace(stage.SceneName))
            {
                Debug.LogError("[StartMenuStageSelectPresenter] Stage scene name is empty: " + stage.DisplayName, this);
                return;
            }

            StageSceneSelected?.Invoke(stage.SceneName);
        }

        private void Refresh()
        {
            if (titleText != null)
            {
                titleText.text = "스테이지선택";
            }

            StageSlot selectedStage = GetSelectedStage();
            if (detailText != null)
            {
                detailText.text = selectedStage != null
                    ? selectedStage.DisplayName + "\n" + selectedStage.StatusText
                    : "스테이지정보없음";
            }

            if (hintText != null)
            {
                hintText.text = "A/D 이동 / E 입장 / ESC·뒤로 닫기";
            }

            for (int i = 0; slotViews != null && i < slotViews.Length; i++)
            {
                StartMenuStageSelectSlotView slotView = slotViews[i];
                if (slotView == null)
                {
                    continue;
                }

                StageSlot stage = i < GetStageCount() ? stages[i] : null;
                slotView.SetContent(
                    stage != null ? stage.DisplayName : "빈슬롯",
                    stage != null ? stage.StatusText : "미설정",
                    stage != null && stage.Available,
                    i == selectedIndex);
            }
        }

        private void SelectCurrentButton()
        {
            if (slotViews == null || selectedIndex < 0 || selectedIndex >= slotViews.Length)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null && slotViews[selectedIndex] != null)
            {
                eventSystem.SetSelectedGameObject(slotViews[selectedIndex].gameObject);
            }
        }

        private StageSlot GetSelectedStage()
        {
            return selectedIndex >= 0 && selectedIndex < GetStageCount() ? stages[selectedIndex] : null;
        }

        private int GetStageCount()
        {
            return stages != null ? stages.Length : 0;
        }

        private void BindControls()
        {
            if (controlsBound)
            {
                return;
            }

            closeButton?.onClick.AddListener(Hide);

            if (slotViews != null)
            {
                for (int i = 0; i < slotViews.Length; i++)
                {
                    if (slotViews[i] == null)
                    {
                        continue;
                    }

                    slotViews[i].Initialize(i);
                    slotViews[i].Clicked += SelectSlot;
                    slotViews[i].DoubleClicked += ConfirmSlot;
                }
            }

            controlsBound = true;
        }

        private void UnbindControls()
        {
            if (!controlsBound)
            {
                return;
            }

            closeButton?.onClick.RemoveListener(Hide);

            if (slotViews != null)
            {
                for (int i = 0; i < slotViews.Length; i++)
                {
                    if (slotViews[i] == null)
                    {
                        continue;
                    }

                    slotViews[i].Clicked -= SelectSlot;
                    slotViews[i].DoubleClicked -= ConfirmSlot;
                }
            }

            controlsBound = false;
        }

        private void SetMouseIconVisible(bool visible)
        {
            if (mouseIconPresenter == null)
            {
                if (visible)
                {
                    Debug.LogError("[StartMenuStageSelectPresenter] Mouse icon presenter is not assigned.", this);
                }

                return;
            }

            mouseIconPresenter.SetVisible(visible);
        }
    }
}
