using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TutorialChapterCompletionPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private TMP_Text titleTmpText;
        [SerializeField] private Text detailText;
        [SerializeField] private TMP_Text detailTmpText;
        [SerializeField] private Text hintText;
        [SerializeField] private TMP_Text hintTmpText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button chapterSelectButton;
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Text nextButtonText;
        [SerializeField] private TMP_Text nextButtonTmpText;
        [SerializeField] private Text chapterSelectButtonText;
        [SerializeField] private TMP_Text chapterSelectButtonTmpText;
        [SerializeField] private Text lobbyButtonText;
        [SerializeField] private TMP_Text lobbyButtonTmpText;

        private bool controlsBound;
        private bool hasNextChapter;
        private int shownFrame = -1;

        public event Action NextChapterRequested;
        public event Action ChapterSelectRequested;
        public event Action LobbyRequested;

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

            if (Time.frameCount <= shownFrame)
            {
                return;
            }

            HandleKeyboard();
        }

        public void Show(TutorialChapterDefinition completedChapter, TutorialChapterDefinition nextChapter)
        {
            SetActive(gameObject, true);
            BindControls();

            hasNextChapter = nextChapter != null;

            SetText(titleTmpText, titleText, "튜토리얼 완료");

            if (detailTmpText != null || detailText != null)
            {
                string completedName = completedChapter != null ? completedChapter.DisplayName : "현재 챕터";
                string nextName = nextChapter != null ? nextChapter.DisplayName : "없음";
                SetText(detailTmpText, detailText, hasNextChapter
                    ? $"{completedName} 완료\n다음 훈련: {nextName}"
                    : $"{completedName} 완료\n모든 튜토리얼을 끝냈습니다.");
            }

            SetText(hintTmpText, hintText, hasNextChapter
                    ? "1 다음 / 2 챕터 / 3·ESC 로비"
                    : "1 챕터 / 2·ESC 로비");

            SetActive(nextButton != null ? nextButton.gameObject : null, hasNextChapter);

            SetText(nextButtonTmpText, nextButtonText, "1. 다음 훈련");
            SetText(chapterSelectButtonTmpText, chapterSelectButtonText, hasNextChapter ? "2. 챕터 선택" : "1. 챕터 선택");
            SetText(lobbyButtonTmpText, lobbyButtonText, hasNextChapter ? "3. 메인 로비" : "2. 메인 로비");

            SetActive(panelRoot, true);
            PopupDimOverlayController.RequestShow(this, panelRoot != null ? panelRoot.transform : transform);
            shownFrame = Time.frameCount;
            StartCoroutine(SelectDefaultButtonNextFrame());

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

            if (hasNextChapter)
            {
                if (KeyboardInputMessenger.WasMenuSlotPressed(1)
                    || keyboard.nKey.wasPressedThisFrame
                    || KeyboardInputMessenger.WasContextConfirmPressed())
                {
                    RequestNextChapter();
                    return;
                }

                if (KeyboardInputMessenger.WasMenuSlotPressed(2) || keyboard.cKey.wasPressedThisFrame)
                {
                    RequestChapterSelect();
                    return;
                }

                if (KeyboardInputMessenger.WasMenuSlotPressed(3)
                    || keyboard.lKey.wasPressedThisFrame
                    || KeyboardInputMessenger.WasCancelPressed())
                {
                    RequestLobby();
                }

                return;
            }

            if (KeyboardInputMessenger.WasMenuSlotPressed(1) || keyboard.cKey.wasPressedThisFrame)
            {
                RequestChapterSelect();
                return;
            }

            if (KeyboardInputMessenger.WasMenuSlotPressed(2)
                || keyboard.lKey.wasPressedThisFrame
                || KeyboardInputMessenger.WasContextConfirmPressed()
                || KeyboardInputMessenger.WasCancelPressed())
            {
                RequestLobby();
            }
        }

        private void RequestNextChapter()
        {
            if (!hasNextChapter)
            {
                Debug.LogError("[TutorialChapterCompletionPresenter] Next chapter requested but no next chapter is available.", this);
                return;
            }

            NextChapterRequested?.Invoke();
        }

        private void RequestChapterSelect()
        {
            ChapterSelectRequested?.Invoke();
        }

        private void RequestLobby()
        {
            LobbyRequested?.Invoke();
        }

        private void BindControls()
        {
            if (controlsBound)
            {
                return;
            }

            nextButton?.onClick.AddListener(RequestNextChapter);
            chapterSelectButton?.onClick.AddListener(RequestChapterSelect);
            lobbyButton?.onClick.AddListener(RequestLobby);
            controlsBound = true;
        }

        private void SelectDefaultButton()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            Button defaultButton = hasNextChapter ? nextButton : lobbyButton;
            if (defaultButton != null)
            {
                eventSystem.SetSelectedGameObject(defaultButton.gameObject);
            }
        }

        private IEnumerator SelectDefaultButtonNextFrame()
        {
            yield return null;

            if (panelRoot == null || !panelRoot.activeSelf)
            {
                yield break;
            }

            SelectDefaultButton();
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null)
            {
                target.SetActive(value);
            }
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
    }
}
