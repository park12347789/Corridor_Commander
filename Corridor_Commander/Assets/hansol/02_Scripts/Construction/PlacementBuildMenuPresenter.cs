using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class PlacementBuildMenuPresenter : MonoBehaviour
    {
        private const int CategorySlotCount = 3;
        private const int MaxKeyboardSlotCount = 5;

        private enum MenuMode
        {
            CategorySelection,
            BuildableSelection
        }

        private readonly struct BuildMenuEntry
        {
            public BuildMenuEntry(BuildableDefinitionSO definition)
            {
                Definition = definition;
                Kind = definition != null ? definition.Kind : BuildableKind.Turret;
                Label = ResolveDefinitionName(definition);
                Price = definition != null ? definition.Price : 0;
                Explanation = ResolveExplanation(definition);
                Icon = definition != null ? definition.Icon : null;
            }

            public BuildMenuEntry(string label, BuildableKind kind)
            {
                Definition = null;
                Kind = kind;
                Label = label;
                Price = 0;
                Explanation = string.Empty;
                Icon = null;
            }

            public BuildableDefinitionSO Definition { get; }
            public BuildableKind Kind { get; }
            public string Label { get; }
            public int Price { get; }
            public string Explanation { get; }
            public Sprite Icon { get; }
            public bool HasDefinition => Definition != null;
        }

        private sealed class CategoryView
        {
            public GameObject Root;
            public Button Button;
            public TMP_Text NumberText;
        }

        private sealed class ListFrameView
        {
            public GameObject Root;
            public Button Button;
            public TMP_Text NameText;
            public TMP_Text NumberText;
            public TMP_Text PriceText;
            public TMP_Text ExplanationText;
            public Image IconImage;
        }

        [Header("Legacy References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Button turretButton;
        [SerializeField] private Text turretButtonText;
        [SerializeField] private Button barricadeButton;
        [SerializeField] private Text barricadeButtonText;
        [SerializeField] private Button mortarButton;
        [SerializeField] private Text mortarButtonText;
        [SerializeField] private Text hintText;

        [Header("New Panel Auto Binding")]
        [SerializeField] private string newPanelName = "new";
        [SerializeField] private string categoryPanelName = "MenuPanel";

        [Header("Style")]
        [SerializeField] private int titleFontSize = 38;
        [SerializeField] private int buttonFontSize = 28;
        [SerializeField] private int hintFontSize = 24;
        [SerializeField] private Vector2 minimumPanelSize = new Vector2(460f, 340f);
        [SerializeField] private Vector2 minimumButtonSize = new Vector2(380f, 72f);

        private readonly List<BuildMenuEntry> visibleEntries = new List<BuildMenuEntry>();
        private readonly List<BuildableDefinitionSO> candidateDefinitions = new List<BuildableDefinitionSO>();
        private readonly CategoryView[] categoryViews = new CategoryView[CategorySlotCount];
        private readonly List<ListFrameView> frameViews = new List<ListFrameView>();
        private readonly List<GameObject>[] listFrameSets =
        {
            new List<GameObject>(),
            new List<GameObject>(),
            new List<GameObject>()
        };

        private PlacementPointInteraction currentInteraction;
        private MenuMode mode;
        private BuildableCategory currentCategory;
        private Transform newRoot;
        private GameObject categoryPanelRoot;
        private Button exitButton;
        private GameObject[] categoryContentRoots;
        private Transform[] listRoots;
        private Transform[] listContentRoots;
        private GameObject[] listTemplates;
        private bool newPanelBound;

        private void Awake()
        {
            BindLegacyButtons();
            BindNewPanelIfNeeded();
            SetPanelActive(false);
            ShowCategorySelection();
        }

        private void Update()
        {
            if (currentInteraction == null || panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }

            int keyboardSlotCount = mode == MenuMode.CategorySelection
                ? CategorySlotCount
                : Mathf.Min(MaxKeyboardSlotCount, visibleEntries.Count);
            UiInputCoordinator inputCoordinator = UiInputCoordinator.Instance;
            if (inputCoordinator == null)
            {
                return;
            }

            for (int i = 0; i < keyboardSlotCount; i++)
            {
                if (KeyboardInputMessenger.WasMenuSlotPressed(i + 1)
                    && inputCoordinator.TryConsumeMenuSlot(currentInteraction, i + 1))
                {
                    SubmitSlot(i);
                }
            }

            if (KeyboardInputMessenger.WasCancelPressed()
                && inputCoordinator.TryConsumeCancel(currentInteraction))
            {
                HandleBackRequested();
            }
        }

        public void Show(PlacementPointInteraction interaction)
        {
            currentInteraction = interaction;
            BindNewPanelIfNeeded();
            ShowCategorySelection();
            SetPanelActive(true);
        }

        public void Hide(PlacementPointInteraction interaction)
        {
            if (currentInteraction != null && currentInteraction != interaction)
            {
                return;
            }

            Hide();
        }

        public void Hide()
        {
            currentInteraction?.NotifyMenuClosed(this);
            currentInteraction = null;
            SetPanelActive(false);
        }

        private void SubmitSlot(int slotIndex)
        {
            if (currentInteraction == null)
            {
                Hide();
                return;
            }

            if (mode == MenuMode.CategorySelection)
            {
                if (slotIndex >= CategorySlotCount)
                {
                    return;
                }

                BuildableCategory selectedCategory = ResolveCategory(slotIndex);
                if (HasEntriesInCategory(selectedCategory))
                {
                    ShowBuildableSelection(selectedCategory);
                }

                return;
            }

            if (slotIndex < 0 || slotIndex >= visibleEntries.Count)
            {
                return;
            }

            BuildMenuEntry entry = visibleEntries[slotIndex];
            if (entry.HasDefinition)
            {
                currentInteraction.TryBuildFromMenu(entry.Definition);
            }
            else
            {
                currentInteraction.TryBuildFromMenu(entry.Kind);
            }
        }

        private void ShowCategorySelection()
        {
            mode = MenuMode.CategorySelection;
            ClearEntries();
            RefreshCategorySelection();
        }

        private void ShowBuildableSelection(BuildableCategory category)
        {
            mode = MenuMode.BuildableSelection;
            currentCategory = category;
            RefreshBuildableSelection();
        }

        private void RefreshCategorySelection()
        {
            if (newPanelBound)
            {
                ClearGeneratedFrames();
                SetActive(categoryPanelRoot, true);
                SetCategoryContentActive(null);

                for (int i = 0; i < CategorySlotCount; i++)
                {
                    BuildableCategory category = ResolveCategory(i);
                    bool hasEntries = HasEntriesInCategory(category);
                    if (categoryViews[i] != null)
                    {
                        SetActive(categoryViews[i].Root, true);
                        if (categoryViews[i].Button != null)
                        {
                            categoryViews[i].Button.interactable = hasEntries;
                        }

                        if (categoryViews[i].NumberText != null)
                        {
                            categoryViews[i].NumberText.SetText("{0}", i + 1);
                        }
                    }
                }

                ApplyLegacyHeaderText("Build Menu", "1-3 \uC120\uD0DD / ESC\u00B7\uB4A4\uB85C \uB2EB\uAE30");
                return;
            }

            RefreshLegacyCategorySelectionText();
        }

        private void RefreshBuildableSelection()
        {
            ClearEntries();
            AddBuildableEntries(currentCategory);

            if (newPanelBound)
            {
                SetActive(categoryPanelRoot, false);
                SetCategoryContentActive(currentCategory);
                RebuildListFrames(currentCategory);
                NormalizeListLayout(currentCategory);
                ApplyLegacyHeaderText(GetCategoryTitle(currentCategory), "1-5 \uC124\uCE58 / ESC\u00B7\uB4A4\uB85C \uBAA9\uB85D");
                return;
            }

            RefreshLegacyBuildableSelectionText();
        }

        private void RebuildListFrames(BuildableCategory category)
        {
            ClearGeneratedFrames();

            int categoryIndex = CategoryToIndex(category);
            if (listRoots == null
                || categoryIndex < 0
                || categoryIndex >= listRoots.Length
                || listRoots[categoryIndex] == null)
            {
                return;
            }

            int visibleCount = Mathf.Min(visibleEntries.Count, MaxKeyboardSlotCount);
            List<GameObject> frames = listFrameSets[categoryIndex];
            EnsureListFrameCapacity(categoryIndex, visibleCount);
            if (visibleCount > frames.Count)
            {
                Debug.LogWarning(
                    $"[PlacementBuildMenuPresenter] Not enough authored ListFrame objects for {category}. Required: {visibleCount}, Available: {frames.Count}.",
                    this);
            }

            int activeCount = Mathf.Min(visibleCount, frames.Count);
            for (int i = 0; i < activeCount; i++)
            {
                GameObject frame = frames[i];
                frame.SetActive(true);
                ListFrameView frameView = BindListFrame(frame, i);
                ApplyFrame(frameView, visibleEntries[i], i);
            }
        }

        private void EnsureListFrameCapacity(int categoryIndex, int requiredCount)
        {
            if (categoryIndex < 0
                || categoryIndex >= listFrameSets.Length
                || requiredCount <= 0)
            {
                return;
            }

            List<GameObject> frames = listFrameSets[categoryIndex];
            if (frames.Count >= requiredCount)
            {
                return;
            }

            GameObject template = listTemplates != null && categoryIndex < listTemplates.Length
                ? listTemplates[categoryIndex]
                : null;
            if (template == null)
            {
                return;
            }

            Transform parent = listContentRoots != null && categoryIndex < listContentRoots.Length && listContentRoots[categoryIndex] != null
                ? listContentRoots[categoryIndex]
                : template.transform.parent;

            while (frames.Count < requiredCount)
            {
                GameObject frame = Instantiate(template, parent);
                frame.name = template.name + "_Runtime_" + (frames.Count + 1);
                frame.SetActive(false);
                frames.Add(frame);
            }
        }

        private void ApplyFrame(ListFrameView frameView, BuildMenuEntry entry, int slotIndex)
        {
            if (frameView == null)
            {
                return;
            }

            bool canBuild = CanBuildEntry(entry);

            if (frameView.NumberText != null)
            {
                frameView.NumberText.SetText("{0}", slotIndex + 1);
            }

            if (frameView.NameText != null)
            {
                frameView.NameText.SetText(entry.Label);
            }

            if (frameView.PriceText != null)
            {
                frameView.PriceText.SetText(canBuild ? entry.Price.ToString() : "LOCK");
            }

            if (frameView.ExplanationText != null)
            {
                frameView.ExplanationText.SetText(ResolveEntryExplanation(entry, canBuild));
            }

            if (frameView.IconImage != null)
            {
                frameView.IconImage.sprite = entry.Icon;
                frameView.IconImage.color = Color.white;
                frameView.IconImage.enabled = entry.Icon != null;
                frameView.IconImage.preserveAspect = true;
                RestoreListIconLayout(frameView.IconImage.rectTransform);
            }

            if (frameView.Button != null)
            {
                int capturedIndex = slotIndex;
                frameView.Button.onClick.RemoveAllListeners();
                frameView.Button.onClick.AddListener(() => SubmitSlot(capturedIndex));
                frameView.Button.interactable = canBuild;
                ApplyReadableDisabledColors(frameView.Button);
            }
        }

        private void AddBuildableEntries(BuildableCategory category)
        {
            candidateDefinitions.Clear();
            currentInteraction?.GetBuildableDefinitionsFromMenu(category, candidateDefinitions);

            for (int i = 0; i < candidateDefinitions.Count; i++)
            {
                BuildableDefinitionSO definition = candidateDefinitions[i];
                if (definition != null)
                {
                    AddBuildableEntry(new BuildMenuEntry(definition));
                }
            }

        }

        private static void RestoreListIconLayout(RectTransform iconRectTransform)
        {
            if (iconRectTransform == null)
            {
                return;
            }

            iconRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconRectTransform.pivot = new Vector2(0.5f, 0.5f);
            iconRectTransform.anchoredPosition = new Vector2(0f, 0.3f);
            iconRectTransform.localScale = Vector3.one;
            iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 160f);
            iconRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 160f);
        }

        private bool CanBuildEntry(BuildMenuEntry entry)
        {
            if (currentInteraction == null)
            {
                return false;
            }

            return entry.HasDefinition
                ? currentInteraction.CanBuildFromMenu(entry.Definition)
                : true;
        }

        private static string ResolveEntryExplanation(BuildMenuEntry entry, bool canBuild)
        {
            if (canBuild)
            {
                return entry.Explanation;
            }

            string baseText = string.IsNullOrWhiteSpace(entry.Explanation)
                ? "Unlock required"
                : entry.Explanation;

            return baseText + "\nUnlock required";
        }

        private static void ApplyReadableDisabledColors(Button button)
        {
            ColorBlock colors = button.colors;
            Color disabled = colors.normalColor;
            disabled.a = Mathf.Max(disabled.a, 0.86f);
            colors.disabledColor = disabled;
            button.colors = colors;
        }

        private bool HasEntriesInCategory(BuildableCategory category)
        {
            if (currentInteraction == null)
            {
                return false;
            }

            candidateDefinitions.Clear();
            currentInteraction.GetBuildableDefinitionsFromMenu(category, candidateDefinitions);
            return candidateDefinitions.Count > 0;
        }

        private bool CanBuildInCategory(BuildableCategory category)
        {
            if (currentInteraction == null)
            {
                return false;
            }

            candidateDefinitions.Clear();
            currentInteraction.GetBuildableDefinitionsFromMenu(category, candidateDefinitions);
            for (int i = 0; i < candidateDefinitions.Count; i++)
            {
                if (currentInteraction.CanBuildFromMenu(candidateDefinitions[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void BindNewPanelIfNeeded()
        {
            if (newPanelBound || panelRoot == null)
            {
                return;
            }

            newRoot = panelRoot.transform.Find(newPanelName);
            if (newRoot == null)
            {
                newRoot = panelRoot.transform;
            }

            categoryPanelRoot = FindDeepChild(newRoot, categoryPanelName)?.gameObject;
            categoryContentRoots = new[]
            {
                FindDeepChild(newRoot, "Set01")?.gameObject ?? FindDeepChild(newRoot, "defense")?.gameObject,
                FindDeepChild(newRoot, "Set02")?.gameObject ?? FindDeepChild(newRoot, "attack")?.gameObject,
                FindDeepChild(newRoot, "Set03")?.gameObject ?? FindDeepChild(newRoot, "skill")?.gameObject
            };
            listRoots = new Transform[CategorySlotCount];
            listContentRoots = new Transform[CategorySlotCount];
            listTemplates = new GameObject[CategorySlotCount];

            BindCategoryViews();
            BindExitButton();
            BindListTemplates();
            HideOldPanelChildren();
            newPanelBound = categoryPanelRoot != null
                && categoryContentRoots[0] != null
                && categoryContentRoots[1] != null
                && categoryContentRoots[2] != null;
        }

        private void BindExitButton()
        {
            if (newRoot == null)
            {
                return;
            }

            Transform exitRoot = FindChildExact(newRoot, "Button01_sm");
            if (exitRoot == null)
            {
                exitRoot = FindChildNameContains(newRoot, "exit");
            }

            if (exitRoot == null)
            {
                exitRoot = FindChildNameContains(newRoot, "close");
            }

            if (exitRoot == null)
            {
                return;
            }

            exitButton = EnsureButton(exitRoot.gameObject);
            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(HandleBackRequested);
            }
        }

        private void HandleBackRequested()
        {
            if (mode == MenuMode.BuildableSelection)
            {
                ShowCategorySelection();
            }
            else
            {
                Hide();
            }
        }

        private void BindCategoryViews()
        {
            if (categoryPanelRoot == null)
            {
                return;
            }

            categoryViews[0] = BindCategoryView(categoryPanelRoot.transform, "defense", 0);
            categoryViews[1] = BindCategoryView(categoryPanelRoot.transform, "attack", 1);
            categoryViews[2] = BindCategoryView(categoryPanelRoot.transform, "skill", 2);
        }

        private CategoryView BindCategoryView(Transform root, string namePart, int slotIndex)
        {
            Transform viewRoot = FindChildNameContains(root, namePart);
            if (viewRoot == null)
            {
                return null;
            }

            Button button = EnsureButton(viewRoot.gameObject);
            int capturedIndex = slotIndex;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SubmitSlot(capturedIndex));
            }

            return new CategoryView
            {
                Root = viewRoot.gameObject,
                Button = button,
                NumberText = FindNamedText(viewRoot, "Text_Num")
            };
        }

        private void BindListTemplates()
        {
            for (int i = 0; i < CategorySlotCount; i++)
            {
                Transform categoryRoot = categoryContentRoots != null && i < categoryContentRoots.Length && categoryContentRoots[i] != null
                    ? categoryContentRoots[i].transform
                    : null;
                Transform listRoot = categoryRoot != null ? FindChildExact(categoryRoot, "list") : null;
                Transform contentRoot = EnsureScrollableList(listRoot);
                listRoots[i] = listRoot;
                listContentRoots[i] = contentRoot;
                CollectListFrames(contentRoot, listFrameSets[i]);
                listTemplates[i] = listFrameSets[i].Count > 0 ? listFrameSets[i][0] : null;
            }
        }

        private ListFrameView BindListFrame(GameObject frame, int slotIndex)
        {
            if (slotIndex < 0 || frame == null)
            {
                return null;
            }

            while (frameViews.Count <= slotIndex)
            {
                frameViews.Add(null);
            }

            Transform root = frame.transform;
            frameViews[slotIndex] = new ListFrameView
            {
                Root = frame,
                Button = EnsureButton(frame),
                NameText = FindNamedText(root, "Text_name"),
                NumberText = FindNamedText(root, "Text_Num"),
                PriceText = FindNamedText(root, "prices_text"),
                ExplanationText = FindNamedText(root, "explanation"),
                IconImage = FindNamedImage(root, "Icon")
            };

            return frameViews[slotIndex];
        }

        private void HideOldPanelChildren()
        {
            if (newRoot == panelRoot.transform)
            {
                return;
            }

            for (int i = 0; i < panelRoot.transform.childCount; i++)
            {
                Transform child = panelRoot.transform.GetChild(i);
                if (child != newRoot)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static Button EnsureButton(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            Button button = target.GetComponent<Button>();
            if (button == null)
            {
                button = target.GetComponentInChildren<Button>(true);
            }

            return button;
        }

        private static TMP_Text FindNamedText(Transform root, string name)
        {
            Transform found = FindChildExact(root, name);
            return found != null ? found.GetComponent<TMP_Text>() : null;
        }

        private static Image FindNamedImage(Transform root, string name)
        {
            Transform found = FindChildExact(root, name);
            return found != null ? found.GetComponent<Image>() : null;
        }

        private static Transform FindChildExact(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildExact(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            return FindChildExact(root, name);
        }

        private static Transform FindChildNameContains(Transform root, string namePart)
        {
            if (root == null)
            {
                return null;
            }

            string lowered = namePart.ToLowerInvariant();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name.ToLowerInvariant().Contains(lowered))
                {
                    return child;
                }

                Transform found = FindChildNameContains(child, namePart);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void CollectListFrames(Transform listRoot, List<GameObject> results)
        {
            results.Clear();
            if (listRoot == null)
            {
                return;
            }

            for (int i = 0; i < listRoot.childCount; i++)
            {
                Transform child = listRoot.GetChild(i);
                if (child.name.Contains("ListFrame"))
                {
                    results.Add(child.gameObject);
                }
            }
        }

        private static Transform EnsureScrollableList(Transform listRoot)
        {
            if (listRoot == null)
            {
                return null;
            }

            ScrollRect scrollRect = listRoot.GetComponent<ScrollRect>();
            if (scrollRect != null && scrollRect.content != null)
            {
                return scrollRect.content;
            }

            Transform content = FindDirectChild(listRoot, "RuntimeListContent");
            if (content == null)
            {
                content = FindDirectChild(listRoot, "Content");
            }

            if (content == null)
            {
                content = listRoot;
            }

            return content;
        }

        private static Transform FindDirectChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private void SetCategoryContentActive(BuildableCategory? activeCategory)
        {
            if (categoryContentRoots == null)
            {
                return;
            }

            for (int i = 0; i < categoryContentRoots.Length; i++)
            {
                bool active = activeCategory.HasValue && i == CategoryToIndex(activeCategory.Value);
                SetActive(categoryContentRoots[i], active);
            }
        }

        private void ClearGeneratedFrames()
        {
            for (int i = 0; i < listFrameSets.Length; i++)
            {
                for (int frameIndex = 0; frameIndex < listFrameSets[i].Count; frameIndex++)
                {
                    SetActive(listFrameSets[i][frameIndex], false);
                }
            }
        }

        private void NormalizeListLayout(BuildableCategory category)
        {
            // Layout is authored in the scene/prefab.
        }

        private void BindLegacyButtons()
        {
            if (turretButton != null)
            {
                turretButton.onClick.AddListener(() => SubmitSlot(0));
            }

            if (barricadeButton != null)
            {
                barricadeButton.onClick.AddListener(() => SubmitSlot(1));
            }

            if (mortarButton != null)
            {
                mortarButton.onClick.AddListener(() => SubmitSlot(2));
            }
        }

        private void RefreshLegacyCategorySelectionText()
        {
            ApplyLegacyHeaderText("Build Menu", "Press 1/2/3 to choose a list / Esc to close");

            bool canBuildDefense = CanBuildInCategory(BuildableCategory.Defense);
            bool canBuildOffense = CanBuildInCategory(BuildableCategory.Offense);
            bool canBuildSkill = CanBuildInCategory(BuildableCategory.Skill);

            SetSlotText(turretButton, turretButtonText, canBuildDefense, CreateCategoryLabel(1, "Defense List", canBuildDefense));
            SetSlotText(barricadeButton, barricadeButtonText, canBuildOffense, CreateCategoryLabel(2, "Attack List", canBuildOffense));
            SetSlotText(mortarButton, mortarButtonText, canBuildSkill, CreateCategoryLabel(3, "Skill List", canBuildSkill));
            ApplyMinimumSizes();
        }

        private void RefreshLegacyBuildableSelectionText()
        {
            ApplyLegacyHeaderText(GetCategoryTitle(currentCategory), "Press 1/2/3 to build / Esc for lists");
            RefreshLegacyBuildableSlot(turretButton, turretButtonText, 0);
            RefreshLegacyBuildableSlot(barricadeButton, barricadeButtonText, 1);
            RefreshLegacyBuildableSlot(mortarButton, mortarButtonText, 2);
            ApplyMinimumSizes();
        }

        private void RefreshLegacyBuildableSlot(Button button, Text text, int slotIndex)
        {
            bool hasEntry = slotIndex >= 0 && slotIndex < visibleEntries.Count;
            string label = hasEntry ? $"[{slotIndex + 1}] {visibleEntries[slotIndex].Label}" : string.Empty;
            SetSlotText(button, text, hasEntry, label, hasEntry);
        }

        private void ApplyLegacyHeaderText(string title, string hint)
        {
            if (titleText != null)
            {
                titleText.text = title;
                ApplyTextStyle(titleText, ResolveSize(titleFontSize, 38));
            }

            if (hintText != null)
            {
                hintText.text = hint;
                ApplyTextStyle(hintText, ResolveSize(hintFontSize, 24));
            }
        }

        private void SetSlotText(Button button, Text text, bool interactable, string label, bool visible = true)
        {
            ConfigureButton(button, interactable, visible);

            if (text == null)
            {
                return;
            }

            text.text = label;
            ApplyTextStyle(text, ResolveSize(buttonFontSize, 28));
        }

        private void ApplyMinimumSizes()
        {
            ApplyMinimumSize(panelRoot, ResolveSize(minimumPanelSize, new Vector2(460f, 340f)));
            ApplyMinimumSize(turretButton != null ? turretButton.gameObject : null, ResolveSize(minimumButtonSize, new Vector2(380f, 72f)));
            ApplyMinimumSize(barricadeButton != null ? barricadeButton.gameObject : null, ResolveSize(minimumButtonSize, new Vector2(380f, 72f)));
            ApplyMinimumSize(mortarButton != null ? mortarButton.gameObject : null, ResolveSize(minimumButtonSize, new Vector2(380f, 72f)));
        }

        private static BuildableCategory ResolveCategory(int slotIndex)
        {
            return slotIndex switch
            {
                0 => BuildableCategory.Defense,
                1 => BuildableCategory.Offense,
                2 => BuildableCategory.Skill,
                _ => BuildableCategory.Defense
            };
        }

        private static int CategoryToIndex(BuildableCategory category)
        {
            return category switch
            {
                BuildableCategory.Defense => 0,
                BuildableCategory.Offense => 1,
                BuildableCategory.Skill => 2,
                _ => -1
            };
        }

        private static string ResolveDefinitionName(BuildableDefinitionSO definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                return definition.DisplayName;
            }

            if (definition.Prefab != null)
            {
                return definition.Prefab.name;
            }

            return !string.IsNullOrWhiteSpace(definition.BuildableId)
                ? definition.BuildableId
                : definition.Kind.ToString();
        }

        private static string ResolveExplanation(BuildableDefinitionSO definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(definition.Explanation))
            {
                return definition.Explanation;
            }

            return definition.Category switch
            {
                BuildableCategory.Defense => "Defense Builds",
                BuildableCategory.Offense => "Attack Builds",
                BuildableCategory.Skill => "Skill Builds",
                _ => string.Empty
            };
        }

        private static string CreateCategoryLabel(int slotNumber, string listName, bool canBuild)
        {
            return canBuild ? $"[{slotNumber}] {listName}" : $"[{slotNumber}] {listName}\nUnavailable";
        }

        private void AddBuildableEntry(BuildMenuEntry entry)
        {
            visibleEntries.Add(entry);
        }

        private void ClearEntries()
        {
            visibleEntries.Clear();
            frameViews.Clear();
        }

        private void SetPanelActive(bool active)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(active);
            }

            if (active)
            {
                PopupDimOverlayController.RequestShow(this, panelRoot != null ? panelRoot.transform : transform);
            }
            else
            {
                PopupDimOverlayController.Release(this);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void ConfigureButton(Button button, bool interactable, bool visible)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(visible);
            button.interactable = interactable;
        }

        private static string GetCategoryTitle(BuildableCategory category)
        {
            return category switch
            {
                BuildableCategory.Defense => "Defense List",
                BuildableCategory.Offense => "Attack List",
                BuildableCategory.Skill => "Skill List",
                _ => "Build Menu"
            };
        }

        private static void ApplyTextStyle(Text text, int fontSize)
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
