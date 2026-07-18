using System.Collections.Generic;
using CorridorCommander.PlayerControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CorridorCommander
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PlayerStatsArtifactPopupPresenter : MonoBehaviour
    {
        private const string ContentRootName = "StatsContent_EditMe";
        private const string ContentVersionMarkerName = "StatsContentLayout_V2";
        private const string SummaryTextName = "SummaryText";
        private const string BonusTextName = "BonusText";
        private const string ArtifactListTextName = "ArtifactListText";

        private static readonly ArtifactStat[] OrderedStats =
        {
            ArtifactStat.Damage,
            ArtifactStat.Range,
            ArtifactStat.AttackInterval,
            ArtifactStat.Cooldown,
            ArtifactStat.Health,
            ArtifactStat.MoveSpeed
        };

        [Header("Popup Prefab")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private DotweenUiPanelTransition panelTransition;
        [SerializeField] private Vector2 panelSize = new Vector2(660f, 760f);
        [SerializeField] private Vector2 anchoredPosition = new Vector2(-24f, -86f);

        [Header("Target Icons")]
        [SerializeField] private Sprite playerIcon;
        [SerializeField] private Sprite turretIcon;
        [SerializeField] private Sprite mortarIcon;
        [SerializeField] private Sprite squadIcon;

        [Header("List Icon")]
        [SerializeField] private Sprite artifactIcon;

        private readonly List<TargetBonusView> targetBonusViews = new List<TargetBonusView>(4);
        private TMP_Text summaryText;
        private TMP_Text artifactListText;
        private PlayerCurrencyWallet wallet;
        private PlayerLevelProgression levelProgression;
        private ArtifactInventory artifactInventory;
        private ArtifactStatManager artifactStatManager;
        private bool isOpen;
        private bool authoredPanelFailureLogged;
#if UNITY_EDITOR
        private bool editorEnsureQueued;
#endif

        private void Awake()
        {
            if (Application.isPlaying)
            {
                ResolveRuntimeReferences();
                if (!EnsurePanel())
                {
                    enabled = false;
                    return;
                }

                isOpen = false;
                panelTransition.HideImmediate();
                PopupDimOverlayController.Release(this);
            }
#if UNITY_EDITOR
            else
            {
                QueueEditorEnsurePanel();
            }
#endif
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

#if UNITY_EDITOR
            QueueEditorEnsurePanel();
#endif
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Subscribe();
            }
#if UNITY_EDITOR
            else
            {
                QueueEditorEnsurePanel();
            }
#endif
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                Unsubscribe();
                isOpen = false;
                UiInputCoordinator.EndContextIfActive(this);
                PopupDimOverlayController.Release(this);
            }
        }

#if UNITY_EDITOR
        private void QueueEditorEnsurePanel()
        {
            if (EditorUtility.IsPersistent(this) || editorEnsureQueued)
            {
                return;
            }

            editorEnsureQueued = true;
            EditorApplication.delayCall += EnsurePanelAfterValidation;
        }

        private void EnsurePanelAfterValidation()
        {
            editorEnsureQueued = false;
            if (this == null || Application.isPlaying || EditorUtility.IsPersistent(this))
            {
                return;
            }

            EnsurePanel();
        }
#endif

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (KeyboardInputMessenger.CurrentKeyboard == null)
            {
                return;
            }

            if (KeyboardInputMessenger.CurrentKeyboard.iKey.wasPressedThisFrame)
            {
                if (isOpen)
                {
                    SetOpen(false);
                }
                else if (!UiInputCoordinator.BlocksHotkeys)
                {
                    SetOpen(true);
                }
            }

            if (isOpen
                && KeyboardInputMessenger.WasCancelPressed()
                && UiInputCoordinator.Instance.TryConsumeCancel(this))
            {
                SetOpen(false);
            }
        }

        private void ResolveRuntimeReferences()
        {
            wallet = wallet != null ? wallet : FindFirstObjectByType<PlayerCurrencyWallet>(FindObjectsInactive.Include);
            levelProgression = levelProgression != null ? levelProgression : FindFirstObjectByType<PlayerLevelProgression>(FindObjectsInactive.Include);
            artifactInventory = artifactInventory != null ? artifactInventory : FindFirstObjectByType<ArtifactInventory>(FindObjectsInactive.Include);
            artifactStatManager = artifactStatManager != null ? artifactStatManager : ArtifactStatManager.Current;
            if (artifactStatManager == null)
            {
                artifactStatManager = FindFirstObjectByType<ArtifactStatManager>(FindObjectsInactive.Include);
            }
        }

        private void Subscribe()
        {
            ResolveRuntimeReferences();
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleValueChanged;
                wallet.MoneyChanged += HandleValueChanged;
            }

            if (levelProgression != null)
            {
                levelProgression.LevelChanged -= HandleValueChanged;
                levelProgression.LevelChanged += HandleValueChanged;
                levelProgression.StatPointsChanged -= HandleValueChanged;
                levelProgression.StatPointsChanged += HandleValueChanged;
                levelProgression.KillProgressChanged -= HandleValueChanged;
                levelProgression.KillProgressChanged += HandleValueChanged;
            }

            if (artifactInventory != null)
            {
                artifactInventory.Changed -= Refresh;
                artifactInventory.Changed += Refresh;
            }

            if (artifactStatManager != null)
            {
                artifactStatManager.StatsChanged -= Refresh;
                artifactStatManager.StatsChanged += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleValueChanged;
            }

            if (levelProgression != null)
            {
                levelProgression.LevelChanged -= HandleValueChanged;
                levelProgression.StatPointsChanged -= HandleValueChanged;
                levelProgression.KillProgressChanged -= HandleValueChanged;
            }

            if (artifactInventory != null)
            {
                artifactInventory.Changed -= Refresh;
            }

            if (artifactStatManager != null)
            {
                artifactStatManager.StatsChanged -= Refresh;
            }
        }

        private void HandleValueChanged(int value)
        {
            Refresh();
        }

        private void SetOpen(bool open)
        {
            if (panelTransition == null)
            {
                return;
            }

            if (open && !UiInputCoordinator.Instance.TryBeginContext(this, UiInputContext.PlayerStatsArtifactPopup, true))
            {
                return;
            }

            isOpen = open;
            if (open)
            {
                panelTransition.Show();
                PopupDimOverlayController.RequestShow(this, panelRoot != null ? panelRoot.transform : transform);
            }
            else
            {
                panelTransition.Hide();
                UiInputCoordinator.EndContextIfActive(this);
                PopupDimOverlayController.Release(this);
            }

            if (open)
            {
                ResolveRuntimeReferences();
                Refresh();
            }
        }

        private void Refresh()
        {
            if (!isOpen)
            {
                return;
            }

            if (summaryText != null)
            {
                int artifactCount = artifactInventory != null && artifactInventory.Artifacts != null
                    ? artifactInventory.Artifacts.Count
                    : 0;
                summaryText.text = "\uBCF4\uC720 \uC720\uBB3C " + artifactCount
                    + "\uAC1C   \uC801\uC6A9 \uBCF4\uB108\uC2A4 "
                    + CountActiveBonuses()
                    + "\uAC1C";
            }

            for (int i = 0; i < targetBonusViews.Count; i++)
            {
                targetBonusViews[i].Refresh(this);
            }

            RefreshArtifactList();
        }

        private void RefreshArtifactList()
        {
            if (artifactListText == null)
            {
                return;
            }

            IReadOnlyList<ArtifactDefinitionSO> artifacts = artifactInventory != null ? artifactInventory.Artifacts : null;
            int count = artifacts != null ? artifacts.Count : 0;
            if (count == 0)
            {
                artifactListText.text = "\uD68D\uB4DD \uC720\uBB3C \uC5C6\uC74C";
                return;
            }

            Dictionary<ArtifactDefinitionSO, int> counts = new Dictionary<ArtifactDefinitionSO, int>();
            for (int i = 0; i < count; i++)
            {
                ArtifactDefinitionSO artifact = artifacts[i];
                if (artifact == null)
                {
                    continue;
                }

                counts.TryGetValue(artifact, out int current);
                counts[artifact] = current + 1;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);
            foreach (KeyValuePair<ArtifactDefinitionSO, int> pair in counts)
            {
                builder.Append(pair.Key.DisplayName);
                if (pair.Value > 1)
                {
                    builder.Append(" x");
                    builder.Append(pair.Value);
                }

                string modifierSummary = BuildArtifactModifierSummary(pair.Key, pair.Value);
                if (!string.IsNullOrWhiteSpace(modifierSummary))
                {
                    builder.Append("  ");
                    builder.Append(modifierSummary);
                }

                builder.AppendLine();
            }

            artifactListText.text = builder.ToString();
        }

        private string BuildArtifactModifierSummary(ArtifactDefinitionSO artifact, int count)
        {
            if (artifact == null || artifact.Modifiers == null)
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(96);
            IReadOnlyList<ArtifactStatModifier> modifiers = artifact.Modifiers;
            for (int i = 0; i < modifiers.Count; i++)
            {
                ArtifactStatModifier modifier = modifiers[i];
                if (modifier == null)
                {
                    continue;
                }

                int percent = CalculateDisplayPercent(modifier.Stat, Mathf.Pow(modifier.Multiplier, Mathf.Max(1, count)));
                if (percent == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(GetTargetLabel(modifier.Target));
                builder.Append(' ');
                builder.Append(GetStatLabel(modifier.Stat));
                builder.Append(' ');
                AppendPercent(builder, percent);
            }

            return builder.ToString();
        }

        private bool EnsurePanel()
        {
#if UNITY_EDITOR
            if (EditorUtility.IsPersistent(this))
            {
                return false;
            }
#endif

            if (panelRoot == null
                || !panelRoot.scene.IsValid()
                || panelRoot.scene != gameObject.scene
                || panelRoot.transform.parent != transform)
            {
                return FailAuthoredPanel(
                    "PanelRoot must be an authored child instance in the presenter's scene.");
            }

            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (rect == null
                || canvasGroup == null
                || panelTransition == null
                || panelTransition.gameObject != panelRoot
                || panelTransition.ActivationRoot != panelRoot
                || panelTransition.MotionRoot != rect
                || panelTransition.CanvasGroup != canvasGroup)
            {
                return FailAuthoredPanel(
                    "PanelRoot, RectTransform, CanvasGroup, and DOTween transition must be assigned in the prefab.");
            }

            if ((rect.sizeDelta - panelSize).sqrMagnitude > 0.01f
                || (rect.anchoredPosition - anchoredPosition).sqrMagnitude > 0.01f)
            {
                return FailAuthoredPanel(
                    "PanelRoot layout must match the authored presenter size and position.");
            }

            Transform content = panelRoot.transform.Find(ContentRootName);
            if (content == null || content.Find(ContentVersionMarkerName) == null)
            {
                return FailAuthoredPanel(
                    "Authored stats content and its layout marker are required.");
            }

            BindContent(content);
            if (summaryText == null || artifactListText == null || targetBonusViews.Count != 4)
            {
                return FailAuthoredPanel(
                    "Authored stats content references are incomplete.");
            }

            authoredPanelFailureLogged = false;
            return true;
        }

        private bool FailAuthoredPanel(string message)
        {
            if (!authoredPanelFailureLogged)
            {
                Debug.LogError("[PlayerStatsArtifactPopupPresenter] " + message, this);
                authoredPanelFailureLogged = true;
            }

            return false;
        }

        private void HidePrefabSampleContent(Transform root)
        {
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (IsUnderContentRoot(texts[i].transform))
                {
                    continue;
                }

                if (texts[i].transform.name == "Text_Title")
                {
                    texts[i].text = "\uC2A4\uD0EF / \uC720\uBB3C";
                    ConfigureText(texts[i], 34f, TextAlignmentOptions.Center, Color.white);
                    continue;
                }

                texts[i].text = string.Empty;
                texts[i].gameObject.SetActive(false);
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].gameObject.SetActive(false);
            }
        }

        private static bool IsUnderContentRoot(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == ContentRootName)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private void EnsureContent(Transform root)
        {
            Transform existing = root.Find(ContentRootName);
            if (existing != null)
            {
                if (existing.Find(ContentVersionMarkerName) == null)
                {
                    DestroyEditSafe(existing.gameObject);
                    BuildContent(root);
                    return;
                }

                BindContent(existing);
                return;
            }

            BuildContent(root);
        }

        private static void DestroyEditSafe(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void BindContent(Transform content)
        {
            targetBonusViews.Clear();
            summaryText = FindText(content, SummaryTextName);
            artifactListText = FindText(content, ArtifactListTextName);
            AddTargetView(content, ArtifactTarget.Player, "PlayerStatsRow_EditMe");
            AddTargetView(content, ArtifactTarget.Turret, "TurretStatsRow_EditMe");
            AddTargetView(content, ArtifactTarget.Mortar, "MortarStatsRow_EditMe");
            AddTargetView(content, ArtifactTarget.Squad, "SquadStatsRow_EditMe");
        }

        private void AddTargetView(Transform content, ArtifactTarget target, string rowName)
        {
            Transform row = content.Find(rowName);
            TMP_Text bonusText = row != null ? FindText(row, BonusTextName) : null;
            if (bonusText != null)
            {
                targetBonusViews.Add(new TargetBonusView(target, bonusText));
            }
        }

        private void BuildContent(Transform root)
        {
            targetBonusViews.Clear();

            GameObject content = new GameObject(ContentRootName, typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(root, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(44f, 54f);
            contentRect.offsetMax = new Vector2(-44f, -112f);

            GameObject versionMarker = new GameObject(ContentVersionMarkerName, typeof(RectTransform));
            versionMarker.transform.SetParent(content.transform, false);
            RectTransform markerRect = versionMarker.GetComponent<RectTransform>();
            markerRect.anchorMin = Vector2.zero;
            markerRect.anchorMax = Vector2.zero;
            markerRect.sizeDelta = Vector2.zero;
            LayoutElement markerLayout = versionMarker.AddComponent<LayoutElement>();
            markerLayout.ignoreLayout = true;

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            summaryText = CreateText(content.transform, SummaryTextName, 19f, TextAlignmentOptions.Center);
            AddLayout(summaryText.gameObject, 38f);

            CreateTargetRow(content.transform, ArtifactTarget.Player, "\uD50C\uB808\uC774\uC5B4", "PlayerStatsRow_EditMe", playerIcon);
            CreateTargetRow(content.transform, ArtifactTarget.Turret, "\uD3EC\uD0D1", "TurretStatsRow_EditMe", turretIcon);
            CreateTargetRow(content.transform, ArtifactTarget.Mortar, "\uBC15\uACA9\uD3EC", "MortarStatsRow_EditMe", mortarIcon);
            CreateTargetRow(content.transform, ArtifactTarget.Squad, "\uC2A4\uCFFC\uB4DC", "SquadStatsRow_EditMe", squadIcon);

            CreateArtifactSection(content.transform);
        }

        private void CreateTargetRow(Transform parent, ArtifactTarget target, string label, string rowName, Sprite icon)
        {
            GameObject row = new GameObject(rowName, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            Image background = row.GetComponent<Image>();
            background.color = new Color(0.04f, 0.17f, 0.34f, 0.72f);
            background.raycastTarget = false;
            AddLayout(row, 58f);

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 14, 7, 7);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateIcon(row.transform, label + "Icon", icon, 28f);
            TMP_Text labelText = CreateText(row.transform, label + "Label", 17f, TextAlignmentOptions.MidlineLeft);
            labelText.text = label;
            AddLayout(labelText.gameObject, 74f, 28f);

            TMP_Text bonusText = CreateText(row.transform, BonusTextName, 18f, TextAlignmentOptions.MidlineLeft);
            bonusText.textWrappingMode = TextWrappingModes.Normal;
            AddLayout(bonusText.gameObject, 390f, 34f);
            targetBonusViews.Add(new TargetBonusView(target, bonusText));
        }

        private void CreateArtifactSection(Transform parent)
        {
            GameObject section = new GameObject("ArtifactsSection", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            section.transform.SetParent(parent, false);
            Image background = section.GetComponent<Image>();
            background.color = new Color(0.02f, 0.1f, 0.2f, 0.66f);
            background.raycastTarget = false;
            AddLayout(section, 160f);

            HorizontalLayoutGroup layout = section.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateIcon(section.transform, "ArtifactIcon", artifactIcon, 30f);
            artifactListText = CreateText(section.transform, ArtifactListTextName, 15f, TextAlignmentOptions.TopLeft);
            artifactListText.textWrappingMode = TextWrappingModes.Normal;
            AddLayout(artifactListText.gameObject, 470f, 138f);
        }

        private Image CreateIcon(Transform parent, string name, Sprite sprite, float size)
        {
            GameObject iconObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            Image image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : new Color(0.35f, 0.8f, 1f, 0.75f);
            image.preserveAspect = true;
            image.raycastTarget = false;
            AddLayout(iconObject, size, size);
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            ConfigureText(text, fontSize, alignment, Color.white);
            return text;
        }

        private static void ConfigureText(TMP_Text text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        private static void AddLayout(GameObject target, float preferredHeight)
        {
            AddLayout(target, 0f, preferredHeight);
        }

        private static void AddLayout(GameObject target, float preferredWidth, float preferredHeight)
        {
            LayoutElement layoutElement = target.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = target.AddComponent<LayoutElement>();
            }

            if (preferredWidth > 0f)
            {
                layoutElement.preferredWidth = preferredWidth;
                layoutElement.minWidth = preferredWidth;
                layoutElement.flexibleWidth = 0f;
            }

            if (preferredHeight > 0f)
            {
                layoutElement.preferredHeight = preferredHeight;
                layoutElement.minHeight = preferredHeight;
                layoutElement.flexibleHeight = 0f;
            }
        }

        private static TMP_Text FindText(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == objectName)
                {
                    return texts[i];
                }
            }

            return null;
        }

        private int CountActiveBonuses()
        {
            int count = 0;
            CountActiveBonuses(ArtifactTarget.Player, ref count);
            CountActiveBonuses(ArtifactTarget.Turret, ref count);
            CountActiveBonuses(ArtifactTarget.Mortar, ref count);
            CountActiveBonuses(ArtifactTarget.Squad, ref count);
            return count;
        }

        private void CountActiveBonuses(ArtifactTarget target, ref int count)
        {
            for (int i = 0; i < OrderedStats.Length; i++)
            {
                if (GetDisplayPercent(target, OrderedStats[i]) != 0)
                {
                    count++;
                }
            }
        }

        private string BuildTargetBonusText(ArtifactTarget target)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(96);
            for (int i = 0; i < OrderedStats.Length; i++)
            {
                ArtifactStat stat = OrderedStats[i];
                int percent = GetDisplayPercent(target, stat);
                if (percent == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("   ");
                }

                builder.Append(GetStatLabel(stat));
                builder.Append(' ');
                AppendPercent(builder, percent);
            }

            return builder.Length > 0 ? builder.ToString() : "\uBCF4\uB108\uC2A4 \uC5C6\uC74C";
        }

        private int GetDisplayPercent(ArtifactTarget target, ArtifactStat stat)
        {
            float multiplier = artifactStatManager != null ? artifactStatManager.GetMultiplier(target, stat) : 1f;
            return CalculateDisplayPercent(stat, multiplier);
        }

        private static int CalculateDisplayPercent(ArtifactStat stat, float multiplier)
        {
            bool lowerIsBetter = stat == ArtifactStat.AttackInterval || stat == ArtifactStat.Cooldown;
            float percentValue = lowerIsBetter ? (1f / Mathf.Max(0.01f, multiplier) - 1f) * 100f : (multiplier - 1f) * 100f;
            return Mathf.RoundToInt(percentValue);
        }

        private static void AppendPercent(System.Text.StringBuilder builder, int percent)
        {
            builder.Append(percent >= 0 ? "+" : string.Empty);
            builder.Append(percent);
            builder.Append('%');
        }

        private static string GetTargetLabel(ArtifactTarget target)
        {
            switch (target)
            {
                case ArtifactTarget.Turret:
                    return "\uD3EC\uD0D1";
                case ArtifactTarget.Mortar:
                    return "\uBC15\uACA9\uD3EC";
                case ArtifactTarget.Squad:
                    return "\uC2A4\uCFFC\uB4DC";
                default:
                    return "\uD50C\uB808\uC774\uC5B4";
            }
        }

        private static string GetStatLabel(ArtifactStat stat)
        {
            switch (stat)
            {
                case ArtifactStat.Range:
                    return "\uC0AC\uAC70\uB9AC";
                case ArtifactStat.AttackInterval:
                    return "\uC5F0\uC0AC";
                case ArtifactStat.Cooldown:
                    return "\uCFFC\uAC10";
                case ArtifactStat.Health:
                    return "\uCCB4\uB825";
                case ArtifactStat.MoveSpeed:
                    return "\uC18D\uB3C4";
                default:
                    return "\uACF5\uACA9";
            }
        }

        private readonly struct TargetBonusView
        {
            private readonly ArtifactTarget target;
            private readonly TMP_Text valueText;

            public TargetBonusView(ArtifactTarget target, TMP_Text valueText)
            {
                this.target = target;
                this.valueText = valueText;
            }

            public void Refresh(PlayerStatsArtifactPopupPresenter presenter)
            {
                if (valueText == null)
                {
                    return;
                }

                valueText.text = presenter.BuildTargetBonusText(target);
                valueText.color = valueText.text == "\uBCF4\uB108\uC2A4 \uC5C6\uC74C"
                    ? new Color(0.72f, 0.86f, 1f, 0.82f)
                    : new Color(0.3f, 1f, 0.72f, 1f);
            }
        }
    }
}
