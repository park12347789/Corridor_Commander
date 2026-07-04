using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using TMPro;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TutorialDialoguePresenter : MonoBehaviour
    {
        private readonly struct DialogueRecord
        {
            public DialogueRecord(string speaker, string body)
            {
                Speaker = speaker;
                Body = body;
            }

            public string Speaker { get; }
            public string Body { get; }
        }

        [Header("Dialogue")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text speakerText;
        [SerializeField] private TMP_Text speakerTmpText;
        [SerializeField] private Text bodyText;
        [SerializeField] private TMP_Text bodyTmpText;
        [SerializeField] private Text hintText;
        [SerializeField] private TMP_Text hintTmpText;
        [SerializeField] private GameObject controlHintRoot;
        [SerializeField] private Text controlHintText;
        [SerializeField] private TMP_Text controlHintTmpText;

        [Header("Controls")]
        [SerializeField] private Button nextButton;
        [FormerlySerializedAs("pauseButton")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button historyButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private GameObject resumeRoot;
        [SerializeField] private GameObject historyRoot;
        [SerializeField] private Text historyText;
        [SerializeField] private TMP_Text historyTmpText;
        [SerializeField] private MousePositionIconPresenter mouseIconPresenter;

        [Header("Portrait")]
        [SerializeField] private RectTransform portraitRoot;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image portraitAvatarImage;
        [SerializeField] private Text portraitNameText;
        [SerializeField] private TMP_Text portraitNameTmpText;
        [SerializeField] private Text portraitInitialText;
        [SerializeField] private TMP_Text portraitInitialTmpText;
        [SerializeField] private Sprite operationPortraitSprite;
        [SerializeField] private Sprite playerPortraitSprite;
        [SerializeField] private Sprite guardPortraitSprite;
        [SerializeField] private Color operationPortraitColor = new Color(0.24f, 0.52f, 0.9f, 0.94f);
        [SerializeField] private Color playerPortraitColor = new Color(0.22f, 0.85f, 0.68f, 0.94f);
        [SerializeField] private Color guardPortraitColor = new Color(0.95f, 0.62f, 0.22f, 0.94f);
        [SerializeField] private float talkBobSpeed = 5.2f;
        [SerializeField] private float talkScale = 0.018f;
        [SerializeField] private int historyLimit = 20;

        private readonly List<DialogueRecord> history = new List<DialogueRecord>();
        private Vector2 portraitBasePosition;
        private Vector3 portraitAvatarBaseScale = Vector3.one;
        private bool hasPortraitBasePosition;
        private bool hasPortraitAvatarBaseScale;
        private bool controlsBound;
        private bool continueRequested;
        private bool dismissRequested;
        private bool waitingForContinue;
        private bool isPaused;
        private bool suppressingInteractionPrompt;
        private static int visibleDialogueCount;

        public event Action PreviousRequested;

        public bool HasContinueRequest => continueRequested;
        public static bool HasVisibleDialogue => visibleDialogueCount > 0;

        private void Awake()
        {
            ResolveReferences();
            BindControls();
            Hide();
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
            AnimatePortrait();
        }

        public void Show(string speaker, string body, string hint)
        {
            Show(speaker, body, hint, true);
        }

        public void Show(string speaker, string body, string hint, bool waitForContinue)
        {
            ResolveReferences();
            BindControls();

            waitingForContinue = waitForContinue;
            continueRequested = false;
            dismissRequested = false;
            isPaused = false;

            SetText(speakerTmpText, speakerText, speaker);
            SetText(bodyTmpText, bodyText, body);
            SetText(hintTmpText, hintText, hint);
            SetText(controlHintTmpText, controlHintText, BuildControlHint(waitForContinue));
            SetText(controlHintTmpText, controlHintText, BuildCompactControlHint(waitForContinue));
            ApplyPortrait(speaker);
            AddHistory(speaker, body);

            SetActive(panelRoot, true);
            SetActive(controlHintRoot, true);
            SetActive(resumeRoot, false);
            SetMouseIconVisible(true);
            SetInteractionPromptSuppressed(true);
            RefreshControls();
        }

        public void Hide()
        {
            waitingForContinue = false;
            continueRequested = false;
            dismissRequested = false;
            isPaused = false;
            SetActive(panelRoot, false);
            SetActive(controlHintRoot, false);
            SetActive(resumeRoot, false);
            SetActive(historyRoot, false);
            SetMouseIconVisible(false);
            SetInteractionPromptSuppressed(false);
            ResetPortraitTransform();
        }

        public bool ConsumeContinueRequest()
        {
            if (!continueRequested)
            {
                return false;
            }

            continueRequested = false;
            return true;
        }

        public bool ConsumeDismissRequest()
        {
            if (!dismissRequested)
            {
                return false;
            }

            dismissRequested = false;
            return true;
        }

        public void HideUntilNextStep()
        {
            SetActive(panelRoot, false);
            SetActive(controlHintRoot, false);
            SetActive(historyRoot, false);
            SetMouseIconVisible(false);
            SetInteractionPromptSuppressed(false);
            ResetPortraitTransform();
            RefreshControls();
        }

        public void ToggleHistory()
        {
            if (historyRoot == null)
            {
                Debug.LogError($"{nameof(TutorialDialoguePresenter)} requires a history root.");
                return;
            }

            SetHistoryVisible(!historyRoot.activeSelf);
        }

        public void PauseDialogue()
        {
            if (panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }

            isPaused = true;
            SetActive(panelRoot, false);
            SetActive(controlHintRoot, false);
            SetActive(resumeRoot, true);
            SetMouseIconVisible(true);
            SetInteractionPromptSuppressed(true);
            RefreshControls();
        }

        public void ResumeDialogue()
        {
            if (!isPaused)
            {
                return;
            }

            isPaused = false;
            SetActive(panelRoot, true);
            SetActive(controlHintRoot, true);
            SetActive(resumeRoot, false);
            SetMouseIconVisible(true);
            SetInteractionPromptSuppressed(true);
            RefreshControls();
        }

        private void OnDisable()
        {
            SetInteractionPromptSuppressed(false);
        }

        private void RequestContinue()
        {
            if (isPaused)
            {
                return;
            }

            if (waitingForContinue)
            {
                continueRequested = true;
                return;
            }

            dismissRequested = true;
        }

        private void HandleKeyboardShortcuts()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.hKey.wasPressedThisFrame)
            {
                ToggleHistory();
            }

            if (isPaused)
            {
                if (keyboard.tKey.wasPressedThisFrame || KeyboardInputMessenger.WasContextConfirmPressed())
                {
                    ResumeDialogue();
                }

                return;
            }

            if (panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }

            if (waitingForContinue && KeyboardInputMessenger.WasCancelPressed())
            {
                RequestPrevious();
                return;
            }

            if (KeyboardInputMessenger.WasContextConfirmPressed())
            {
                RequestContinue();
            }
        }

        private void SetHistoryVisible(bool visible)
        {
            SetActive(historyRoot, visible);
            if (!visible)
            {
                return;
            }

            if (historyTmpText == null && historyText == null)
            {
                Debug.LogError($"{nameof(TutorialDialoguePresenter)} requires a history text.");
                return;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < history.Count; i++)
            {
                DialogueRecord record = history[i];
                builder.Append(record.Speaker);
                builder.Append(": ");
                builder.AppendLine(record.Body);
                if (i < history.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            SetText(historyTmpText, historyText, builder.ToString());
        }

        private void AddHistory(string speaker, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            if (history.Count > 0)
            {
                DialogueRecord previous = history[history.Count - 1];
                if (string.Equals(previous.Speaker, speaker, StringComparison.Ordinal) &&
                    string.Equals(previous.Body, body, StringComparison.Ordinal))
                {
                    return;
                }
            }

            history.Add(new DialogueRecord(speaker, body));
            while (history.Count > Mathf.Max(1, historyLimit))
            {
                history.RemoveAt(0);
            }

            if (historyRoot != null && historyRoot.activeSelf)
            {
                SetHistoryVisible(true);
            }
        }

        private static string BuildControlHint(bool waitForContinue)
        {
            return waitForContinue
                ? "Enter 다음 | 이전 | H 기록"
                : "H 기록";
        }

        private static string BuildCompactControlHint(bool waitForContinue)
        {
            return waitForContinue
                ? "Enter \uB2E4\uC74C / ESC\u00B7\uB4A4\uB85C \uC774\uC804 / H \uAE30\uB85D"
                : "H \uAE30\uB85D";
        }

        private void SetMouseIconVisible(bool visible)
        {
            if (mouseIconPresenter == null)
            {
                if (visible)
                {
                    Debug.LogError("[TutorialDialoguePresenter] Mouse icon presenter is not assigned.", this);
                }

                return;
            }

            mouseIconPresenter.SetVisible(visible);
        }

        private void ApplyPortrait(string speaker)
        {
            if (portraitRoot == null)
            {
                return;
            }

            if (!hasPortraitBasePosition)
            {
                portraitBasePosition = portraitRoot.anchoredPosition;
                hasPortraitBasePosition = true;
            }

            bool isPlayer = IsPlayerSpeaker(speaker);
            bool isGuard = IsGuardSpeaker(speaker);
            Sprite portraitSprite = ResolvePortraitSprite(speaker);
            if (portraitImage != null)
            {
                portraitImage.color = isPlayer ? playerPortraitColor : isGuard ? guardPortraitColor : operationPortraitColor;
            }

            if (portraitAvatarImage != null)
            {
                if (!hasPortraitAvatarBaseScale)
                {
                    portraitAvatarBaseScale = portraitAvatarImage.rectTransform.localScale;
                    hasPortraitAvatarBaseScale = true;
                }

                portraitAvatarImage.sprite = portraitSprite;
                portraitAvatarImage.enabled = portraitSprite != null;
                portraitAvatarImage.preserveAspect = true;
                portraitAvatarImage.color = Color.white;
            }

            SetText(portraitNameTmpText, portraitNameText, string.IsNullOrWhiteSpace(speaker) ? "미확인" : speaker);
            SetText(portraitInitialTmpText, portraitInitialText, isPlayer ? "나" : isGuard ? "가드" : "작전");
            SetEnabled(portraitInitialTmpText, portraitInitialText, portraitSprite == null);
        }

        private Sprite ResolvePortraitSprite(string speaker)
        {
            if (IsPlayerSpeaker(speaker))
            {
                return playerPortraitSprite;
            }

            if (IsGuardSpeaker(speaker))
            {
                return guardPortraitSprite;
            }

            return operationPortraitSprite;
        }

        private void AnimatePortrait()
        {
            if (portraitRoot == null || panelRoot == null || !panelRoot.activeSelf)
            {
                ResetPortraitTransform();
                return;
            }

            if (!hasPortraitBasePosition)
            {
                portraitBasePosition = portraitRoot.anchoredPosition;
                hasPortraitBasePosition = true;
            }

            float wave = Mathf.Sin(Time.unscaledTime * talkBobSpeed);
            portraitRoot.anchoredPosition = portraitBasePosition;
            portraitRoot.localScale = Vector3.one;

            if (portraitAvatarImage == null)
            {
                return;
            }

            if (!hasPortraitAvatarBaseScale)
            {
                portraitAvatarBaseScale = portraitAvatarImage.rectTransform.localScale;
                hasPortraitAvatarBaseScale = true;
            }

            float scale = 1f + wave * talkScale;
            portraitAvatarImage.rectTransform.localScale = new Vector3(
                portraitAvatarBaseScale.x * scale,
                portraitAvatarBaseScale.y * scale,
                portraitAvatarBaseScale.z);
        }

        private void ResetPortraitTransform()
        {
            if (portraitRoot == null || !hasPortraitBasePosition)
            {
                return;
            }

            portraitRoot.anchoredPosition = portraitBasePosition;
            portraitRoot.localScale = Vector3.one;

            if (portraitAvatarImage != null && hasPortraitAvatarBaseScale)
            {
                portraitAvatarImage.rectTransform.localScale = portraitAvatarBaseScale;
            }
        }

        private static bool IsPlayerSpeaker(string speaker)
        {
            if (string.IsNullOrWhiteSpace(speaker))
            {
                return false;
            }

            return speaker.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   speaker.Contains("플레이어", StringComparison.Ordinal) ||
                   speaker.Contains("대원", StringComparison.Ordinal);
        }

        private static bool IsGuardSpeaker(string speaker)
        {
            if (string.IsNullOrWhiteSpace(speaker))
            {
                return false;
            }

            return speaker.IndexOf("Guard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   speaker.Contains("가드", StringComparison.Ordinal) ||
                   speaker.Contains("경비", StringComparison.Ordinal);
        }

        private void RefreshControls()
        {
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(panelRoot != null && panelRoot.activeSelf && !isPaused);
            }

            if (previousButton != null)
            {
                previousButton.gameObject.SetActive(panelRoot != null && panelRoot.activeSelf);
            }

            if (historyButton != null)
            {
                historyButton.gameObject.SetActive(history.Count > 0 && panelRoot != null && panelRoot.activeSelf);
            }

            if (resumeRoot != null)
            {
                resumeRoot.SetActive(isPaused);
            }
        }

        private void BindControls()
        {
            if (controlsBound)
            {
                return;
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(RequestContinue);
            }

            if (previousButton != null)
            {
                previousButton.onClick.AddListener(RequestPrevious);
            }

            if (historyButton != null)
            {
                historyButton.onClick.AddListener(ToggleHistory);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeDialogue);
            }

            controlsBound = true;
        }

        private void ResolveReferences()
        {
            panelRoot ??= FindChildGameObject("TutorialDialoguePanel");
            speakerText ??= FindChildText("SpeakerText");
            speakerTmpText ??= FindChildTmpText("SpeakerText");
            bodyText ??= FindChildText("BodyText");
            bodyTmpText ??= FindChildTmpText("BodyText");
            hintText ??= FindChildText("HintText");
            hintTmpText ??= FindChildTmpText("HintText");
            controlHintRoot ??= FindChildGameObject("TutorialDialogueControlHintRoot");
            controlHintText ??= FindChildText("ControlHintText");
            controlHintTmpText ??= FindChildTmpText("ControlHintText");
            nextButton ??= FindChildButton("NextButton");
            previousButton ??= FindChildButton("PreviousButton") ?? FindChildButton("PauseButton");
            historyButton ??= FindChildButton("HistoryButton");
            resumeButton ??= FindChildButton("TutorialDialogueResumeRoot");
            resumeRoot ??= FindChildGameObject("TutorialDialogueResumeRoot");
            historyRoot ??= FindChildGameObject("TutorialDialogueHistoryPanel");
            historyText ??= FindChildText("HistoryText");
            historyTmpText ??= FindChildTmpText("HistoryText");
            portraitRoot ??= FindChildRect("SpeakerPortraitRoot");
            portraitImage ??= FindChildImage("SpeakerPortraitRoot");
            portraitAvatarImage ??= FindChildImage("PortraitAvatarImage");
            portraitNameText ??= FindChildText("PortraitNameText");
            portraitNameTmpText ??= FindChildTmpText("PortraitNameText");
            portraitInitialText ??= FindChildText("PortraitInitialText");
            portraitInitialTmpText ??= FindChildTmpText("PortraitInitialText");
        }

        private GameObject FindChildGameObject(string childName)
        {
            Transform child = FindChildTransform(childName);
            return child != null ? child.gameObject : null;
        }

        private Text FindChildText(string childName)
        {
            Transform child = FindChildTransform(childName);
            if (child == null)
            {
                return null;
            }

            return child.GetComponent<Text>();
        }

        private TMP_Text FindChildTmpText(string childName)
        {
            Transform child = FindChildTransform(childName);
            if (child == null)
            {
                return null;
            }

            return child.GetComponent<TMP_Text>();
        }

        private Button FindChildButton(string childName)
        {
            Transform child = FindChildTransform(childName);
            if (child == null)
            {
                return null;
            }

            return child.GetComponent<Button>();
        }

        private RectTransform FindChildRect(string childName)
        {
            Transform child = FindChildTransform(childName);
            if (child == null)
            {
                return null;
            }

            return child.GetComponent<RectTransform>();
        }

        private Image FindChildImage(string childName)
        {
            Transform child = FindChildTransform(childName);
            if (child == null)
            {
                return null;
            }

            return child.GetComponent<Image>();
        }

        private Transform FindChildTransform(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                {
                    return children[i];
                }
            }

            return null;
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static void SetText(TMP_Text tmpText, Text legacyText, string value)
        {
            if (tmpText != null)
            {
                tmpText.text = value ?? string.Empty;
                return;
            }

            SetText(legacyText, value);
        }

        private static void SetEnabled(TMP_Text tmpText, Text legacyText, bool value)
        {
            if (tmpText != null)
            {
                tmpText.enabled = value;
            }

            if (legacyText != null)
            {
                legacyText.enabled = value;
            }
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null)
            {
                target.SetActive(value);
            }
        }

        private void SetInteractionPromptSuppressed(bool value)
        {
            if (suppressingInteractionPrompt == value)
            {
                return;
            }

            suppressingInteractionPrompt = value;
            visibleDialogueCount = Mathf.Max(0, visibleDialogueCount + (value ? 1 : -1));
        }

        private void RequestPrevious()
        {
            if (isPaused)
            {
                return;
            }

            PreviousRequested?.Invoke();
        }
    }
}
