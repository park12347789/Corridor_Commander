using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        private static readonly List<IInteractionPromptSource> Sources = new List<IInteractionPromptSource>(32);
        private static InteractionPromptPresenter instance;
        private static bool missingPresenterWarned;

        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, 34f);
        [SerializeField] private Vector2 minimumPromptSize = new Vector2(360f, 72f);
        [SerializeField] private int promptFontSize = 26;
        [SerializeField] private bool useDefaultFixedScreenPromptPosition = true;
        [SerializeField] private Vector2 defaultFixedPromptViewportAnchor = new Vector2(0.5f, 0.22f);
        [SerializeField] private Vector2 defaultFixedPromptScreenOffset = Vector2.zero;

        private Canvas parentCanvas;
        private RectTransform promptRect;
        private Camera cachedCamera;
        private readonly List<GameObject> promptVisualRoots = new List<GameObject>(4);
        private readonly List<RectTransform> promptVisualRects = new List<RectTransform>(4);

        public static InteractionPromptPresenter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<InteractionPromptPresenter>(FindObjectsInactive.Include);
                }

                WarnIfMissingPresenter();

                return instance;
            }
        }

        public static void Register(IInteractionPromptSource source)
        {
            if (source == null || Sources.Contains(source))
            {
                return;
            }

            Sources.Add(source);
            _ = Instance;
        }

        public static void Unregister(IInteractionPromptSource source)
        {
            if (source == null)
            {
                return;
            }

            Sources.Remove(source);
            if (instance != null)
            {
                instance.RefreshPrompt();
            }
        }

        public static bool HasVisiblePrompt()
        {
            RebuildSourcesIfEmpty();
            return ResolveBestSourceFromRegistered() != null;
        }

        public static bool IsBestVisibleSource(IInteractionPromptSource source)
        {
            if (source == null)
            {
                return false;
            }

            RebuildSourcesIfEmpty();
            return ReferenceEquals(ResolveBestSourceFromRegistered(), source);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            missingPresenterWarned = false;
            ResolveReferences();
            WarnIfMissingReferences();
            SetPromptActive(false);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void LateUpdate()
        {
            RefreshPrompt();
        }

        private void RefreshPrompt()
        {
            ResolveReferences();
            if (FieldInteractionAimVisibilityController.IsSuppressed || TutorialDialoguePresenter.HasVisibleDialogue)
            {
                SetPromptActive(false);
                return;
            }

            RebuildSourcesIfEmpty();

            IInteractionPromptSource source = ResolveBestSourceFromRegistered();
            if (source == null)
            {
                SetPromptActive(false);
                return;
            }

            if (promptText != null)
            {
                promptText.text = source.PromptText;
                ApplyPromptStyle(promptText, ResolveSize(promptFontSize, 42));
            }

            ApplyMinimumSize(promptRoot, ResolveSize(minimumPromptSize, new Vector2(440f, 82f)));
            if (!TrySetPromptPosition(source))
            {
                SetPromptActive(false);
                return;
            }

            SetPromptActive(true);
        }

        private static IInteractionPromptSource ResolveBestSourceFromRegistered()
        {
            IInteractionPromptSource bestSource = null;
            int bestPriority = int.MinValue;
            float bestDistance = float.MaxValue;

            for (int i = Sources.Count - 1; i >= 0; i--)
            {
                IInteractionPromptSource source = Sources[i];
                if (source == null || source is Object unityObject && unityObject == null)
                {
                    Sources.RemoveAt(i);
                    continue;
                }

                if (!source.IsPromptVisible || string.IsNullOrWhiteSpace(source.PromptText))
                {
                    continue;
                }

                int priority = source.PromptPriority;
                float distance = Mathf.Max(0f, source.PromptDistanceSqr);
                if (bestSource == null
                    || priority > bestPriority
                    || priority == bestPriority && distance < bestDistance)
                {
                    bestSource = source;
                    bestPriority = priority;
                    bestDistance = distance;
                }
            }

            return bestSource;
        }

        private static void RebuildSourcesIfEmpty()
        {
            if (Sources.Count > 0)
            {
                return;
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInteractionPromptSource source && !Sources.Contains(source))
                {
                    Sources.Add(source);
                }
            }
        }

        private bool TrySetPromptPosition(IInteractionPromptSource source)
        {
            if (promptVisualRects.Count == 0)
            {
                return false;
            }

            Vector2 promptSize = ResolveSize(minimumPromptSize, new Vector2(520f, 116f));
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            if (source is IInteractionPromptScreenPosition screenPositionSource
                && screenPositionSource.TryGetPromptScreenPosition(screenSize, promptSize, out Vector2 fixedScreenPoint))
            {
                fixedScreenPoint = ClampToScreen(fixedScreenPoint, promptSize);
                return TryApplyScreenPosition(fixedScreenPoint, null);
            }

            if (useDefaultFixedScreenPromptPosition)
            {
                Vector2 defaultScreenPoint = new Vector2(
                    screenSize.x * defaultFixedPromptViewportAnchor.x,
                    screenSize.y * defaultFixedPromptViewportAnchor.y);
                defaultScreenPoint += defaultFixedPromptScreenOffset;
                defaultScreenPoint = ClampToScreen(defaultScreenPoint, promptSize);
                return TryApplyScreenPosition(defaultScreenPoint, null);
            }

            Camera camera = ResolveCamera();
            if (camera == null)
            {
                return false;
            }

            Vector3 worldPosition = source.PromptWorldPosition;
            Vector3 viewportPoint = camera.WorldToViewportPoint(worldPosition);
            if (viewportPoint.z <= 0f)
            {
                return false;
            }

            Vector2 screenPoint = camera.WorldToScreenPoint(worldPosition);
            screenPoint += screenOffset;
            screenPoint = ClampToScreen(screenPoint, promptSize);

            return TryApplyScreenPosition(screenPoint, camera);
        }

        private bool TryApplyScreenPosition(Vector2 screenPoint, Camera fallbackCamera)
        {
            if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                SetVisualScreenPosition(screenPoint);
                return true;
            }

            RectTransform canvasRect = parentCanvas.transform as RectTransform;
            if (canvasRect == null)
            {
                SetVisualScreenPosition(screenPoint);
                return true;
            }

            Camera canvasCamera = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : fallbackCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    canvasCamera,
                out Vector2 localPoint))
            {
                SetVisualLocalPosition(localPoint);
                return true;
            }

            return false;
        }

        private Vector2 ClampToScreen(Vector2 screenPoint, Vector2 promptSize)
        {
            Vector2 halfSize = promptSize * 0.5f;
            float padding = 12f;
            screenPoint.x = Mathf.Clamp(screenPoint.x, halfSize.x + padding, Screen.width - halfSize.x - padding);
            screenPoint.y = Mathf.Clamp(screenPoint.y, halfSize.y + padding, Screen.height - halfSize.y - padding);
            return screenPoint;
        }

        private void SetVisualScreenPosition(Vector2 screenPoint)
        {
            for (int i = 0; i < promptVisualRects.Count; i++)
            {
                RectTransform visualRect = promptVisualRects[i];
                if (visualRect != null)
                {
                    visualRect.position = screenPoint;
                }
            }
        }

        private void SetVisualLocalPosition(Vector2 localPoint)
        {
            for (int i = 0; i < promptVisualRects.Count; i++)
            {
                RectTransform visualRect = promptVisualRects[i];
                if (visualRect != null)
                {
                    visualRect.localPosition = localPoint;
                }
            }
        }

        private Camera ResolveCamera()
        {
            if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
            {
                return cachedCamera;
            }

            cachedCamera = Camera.main;
            return cachedCamera;
        }

        private void ResolveReferences()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (promptRoot == null)
            {
                Transform foundRoot = transform.Find("InteractionPromptRoot");
                if (foundRoot != null)
                {
                    promptRoot = foundRoot.gameObject;
                }
            }

            if (promptRoot != null)
            {
                promptRect = promptRoot.GetComponent<RectTransform>();
                if (promptText == null)
                {
                    promptText = promptRoot.GetComponentInChildren<Text>(true);
                }
            }

            RebuildPromptVisualRoots();
        }

        private void SetPromptActive(bool active)
        {
            if (promptVisualRoots.Count == 0)
            {
                RebuildPromptVisualRoots();
            }

            for (int i = 0; i < promptVisualRoots.Count; i++)
            {
                GameObject visualRoot = promptVisualRoots[i];
                if (visualRoot != null && visualRoot.activeSelf != active)
                {
                    visualRoot.SetActive(active);
                }
            }
        }

        private void RebuildPromptVisualRoots()
        {
            promptVisualRoots.Clear();
            promptVisualRects.Clear();
            AddPromptVisualRoot(promptRoot);

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                AddPromptVisualRoot(child != null ? child.gameObject : null);
            }
        }

        private void AddPromptVisualRoot(GameObject visualRoot)
        {
            if (visualRoot == null || promptVisualRoots.Contains(visualRoot))
            {
                return;
            }

            promptVisualRoots.Add(visualRoot);
            RectTransform visualRect = visualRoot.GetComponent<RectTransform>();
            if (visualRect != null && !promptVisualRects.Contains(visualRect))
            {
                promptVisualRects.Add(visualRect);
            }
        }

        private void WarnIfMissingReferences()
        {
            if (promptRoot == null)
            {
                Debug.LogWarning("[InteractionPromptPresenter] Prompt Root is not assigned.", this);
            }

            if (promptText == null)
            {
                Debug.LogWarning("[InteractionPromptPresenter] Prompt Text is not assigned.", this);
            }
        }

        private static void WarnIfMissingPresenter()
        {
            if (instance == null && !missingPresenterWarned)
            {
                Debug.LogWarning("[InteractionPromptPresenter] No presenter exists in the active scene.");
                missingPresenterWarned = true;
            }
        }

        private static void ApplyPromptStyle(Text text, int fontSize)
        {
            text.fontStyle = FontStyle.Bold;
            text.fontSize = Mathf.Max(text.fontSize, fontSize);
            text.resizeTextForBestFit = false;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static int ResolveSize(int configuredValue, int fallback)
        {
            return configuredValue > 0 ? configuredValue : fallback;
        }

        private static Vector2 ResolveSize(Vector2 configuredValue, Vector2 fallback)
        {
            return configuredValue.x > 0f && configuredValue.y > 0f ? configuredValue : fallback;
        }

        private static void ApplyMinimumSize(GameObject target, Vector2 minimumSize)
        {
            if (target == null)
            {
                return;
            }

            RectTransform rectTransform = target.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                Mathf.Max(rectTransform.rect.width, minimumSize.x));
            rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(rectTransform.rect.height, minimumSize.y));
        }
    }
}
