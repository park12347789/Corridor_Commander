using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander.EditorTools
{
    [InitializeOnLoad]
    public static class UiReviewInGameScreenshotCapture
    {
        private const string MenuPath = "Corridor Commander/UI/Capture UI Review Screenshots In Game";
        private const string OutDir = "Assets/Screenshots/UIReviewInGame";
        private const string FontPath = "Assets/hansol/09_Settings/Font/BMJUA/BMJUA_ttf.ttf";
        private const string MainCanvas = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvas.prefab";
        private const string StageRuntime = "Assets/hansol/03_Prefabs/Stage/StageRuntime.prefab";
        private const string ItemRadial = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PlayerItemRadialPresenter.prefab";
        private const string CommandRadial = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PlayerCommandRadialPresenter.prefab";
        private const string WaveDirectorCanvas = "Assets/hansol/03_Prefabs/UI/InGame/WaveDirectorCanvas.prefab";
        private const string SupportShop = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/SupportTruckShopPresenter.prefab";
        private const string PlacementBuild = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PlacementBuildMenuPanel.prefab";
        private const string InstalledAction = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/InstalledObjectActionPresenter.prefab";

        private const string RequestedKey = "CorridorCommander.UiReviewInGameScreenshotCapture.Requested";
        private const string RunningKey = "CorridorCommander.UiReviewInGameScreenshotCapture.Running";
        private const string IndexKey = "CorridorCommander.UiReviewInGameScreenshotCapture.Index";
        private const string FrameKey = "CorridorCommander.UiReviewInGameScreenshotCapture.Frame";
        private const string AwaitingFileKey = "CorridorCommander.UiReviewInGameScreenshotCapture.AwaitingFile";
        private const string PendingPathKey = "CorridorCommander.UiReviewInGameScreenshotCapture.PendingPath";
        private const string CapturedThisRunKey = "CorridorCommander.UiReviewInGameScreenshotCapture.CapturedThisRun";

        private static readonly List<CaptureSpec> Specs = new List<CaptureSpec>
        {
            new CaptureSpec("00_ingame_ui_review_overview.png", "UI REVIEW OVERVIEW", PopulateOverview),
            new CaptureSpec("01_ingame_hud_map_weapon.png", "HUD: MAP + WEAPON STATUS", PopulateHudMapWeapon),
            new CaptureSpec("02_ingame_realtime_map_hud.png", "REALTIME MAP HUD", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, StageRuntime, "RealtimeMapHud", Vector2.zero, 2.35f, "RealtimeMapHud");
            }),
            new CaptureSpec("03_ingame_weapon_hud.png", "WEAPON STATUS HUD", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, MainCanvas, "WeaponHudPanel", Vector2.zero, 2.9f, "WeaponHudPanel");
            }),
            new CaptureSpec("04_ingame_item_radial.png", "ITEM RADIAL", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, ItemRadial, "PlayerItemRadialPresenter", Vector2.zero, 1.35f);
            }),
            new CaptureSpec("05_ingame_command_radial.png", "COMMAND RADIAL", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, CommandRadial, "PlayerCommandRadialPresenter", Vector2.zero, 1.35f);
            }),
            new CaptureSpec("06_ingame_wave_ready_popup.png", "WAVE READY POPUP", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, WaveDirectorCanvas, "WaveReadyPopup", Vector2.zero, 1.55f, "WaveReadyPopupRoot", "WaveReadyPopup");
            }),
            new CaptureSpec("07_ingame_treasure_reward.png", "TREASURE REWARD POPUP", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, MainCanvas, "TreasureRewardMenuPresenter", Vector2.zero, 1.15f, "TreasureRewardMenuPresenter", "TreasureRewardPanel");
            }),
            new CaptureSpec("08_ingame_build_shop_action.png", "BUILD / SHOP / ACTION UI", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, PlacementBuild, "PlacementBuildMenuPanel", new Vector2(-520f, -20f), 0.82f);
                AddPrefabTarget(canvas, SupportShop, "SupportTruckShopPresenter", new Vector2(220f, 0f), 0.82f);
                AddPrefabTarget(canvas, InstalledAction, "InstalledObjectActionPresenter", new Vector2(680f, -320f), 1.25f);
            }),
            new CaptureSpec("09_ingame_pause_aim_info.png", "PAUSE / AIM INFO UI", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, MainCanvas, "PauseMenuPresenter", new Vector2(-350f, 0f), 1.15f, "PauseMenuPresenter", "PauseMenuRoot");
                AddPrefabTarget(canvas, MainCanvas, "InstalledObjectAimInfoPresenter", new Vector2(520f, -20f), 1.35f, "InstalledObjectAimInfoPresenter", "InstalledObjectAimInfoRoot");
            }),
        };

        private static GameObject rigRoot;
        private static RectTransform contentRoot;
        private static Font labelFont;

        static UiReviewInGameScreenshotCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem(MenuPath)]
        public static void Begin()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[UiReviewInGameScreenshotCapture] Cannot start while Unity is compiling or changing play mode.");
                return;
            }

            Directory.CreateDirectory(ToAbsolute(OutDir));
            DeleteExistingCaptures();
            EditorPrefs.SetBool(RequestedKey, true);
            EditorPrefs.SetBool(RunningKey, false);
            EditorPrefs.SetBool(AwaitingFileKey, false);
            EditorPrefs.SetBool(CapturedThisRunKey, false);
            EditorPrefs.SetString(PendingPathKey, string.Empty);
            EditorPrefs.SetInt(IndexKey, 0);
            EditorPrefs.SetInt(FrameKey, 0);
            Debug.Log("[UiReviewInGameScreenshotCapture] Starting one-shot Play Mode screenshot sequence.");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && EditorPrefs.GetBool(RequestedKey, false))
            {
                AssetDatabase.Refresh();
            }
        }

        private static void Tick()
        {
            if (!EditorPrefs.GetBool(RequestedKey, false) || EditorApplication.isCompiling)
            {
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = true;
                }

                return;
            }

            if (!EditorPrefs.GetBool(RunningKey, false))
            {
                SetupRig();
                EditorPrefs.SetBool(RunningKey, true);
                EditorPrefs.SetBool(CapturedThisRunKey, false);
                EditorPrefs.SetInt(FrameKey, 0);
                return;
            }

            int frame = EditorPrefs.GetInt(FrameKey, 0) + 1;
            EditorPrefs.SetInt(FrameKey, frame);

            int index = EditorPrefs.GetInt(IndexKey, 0);
            if (index >= Specs.Count)
            {
                if (frame >= 120)
                {
                    Finish();
                }

                return;
            }

            CaptureSpec spec = Specs[index];

            if (frame == 1)
            {
                ClearContent();
                MakeBacking(contentRoot, spec.Title);
                spec.Populate(contentRoot);
                Canvas.ForceUpdateCanvases();
                return;
            }

            if (frame < 30)
            {
                return;
            }

            string path = OutDir + "/" + spec.FileName;
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[UiReviewInGameScreenshotCapture] Capture requested: " + path);
            EditorPrefs.SetInt(IndexKey, index + 1);
            EditorPrefs.SetInt(FrameKey, 0);
        }

        private static void ContinueFromEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorPrefs.GetBool(RunningKey, false))
            {
                int frame = EditorPrefs.GetInt(FrameKey, 0) + 1;
                EditorPrefs.SetInt(FrameKey, frame);
                string pendingPath = EditorPrefs.GetString(PendingPathKey, string.Empty);
                string absolutePath = ToAbsolute(pendingPath);
                if (!File.Exists(absolutePath) || new FileInfo(absolutePath).Length <= 0)
                {
                    if (frame < 600)
                    {
                        return;
                    }

                    Debug.LogError("[UiReviewInGameScreenshotCapture] Capture file missing after Play Mode: " + pendingPath);
                    Finish();
                    return;
                }

                Debug.Log("[UiReviewInGameScreenshotCapture] Capture completed: " + pendingPath);
                EditorPrefs.SetInt(IndexKey, EditorPrefs.GetInt(IndexKey, 0) + 1);
                EditorPrefs.SetBool(RunningKey, false);
                EditorPrefs.SetBool(CapturedThisRunKey, false);
                EditorPrefs.SetString(PendingPathKey, string.Empty);
                EditorPrefs.SetInt(FrameKey, 0);
            }

            if (EditorPrefs.GetInt(IndexKey, 0) >= Specs.Count)
            {
                Finish();
                return;
            }

            EditorApplication.isPlaying = true;
        }

        private static void Finish()
        {
            if (rigRoot != null)
            {
                UnityEngine.Object.Destroy(rigRoot);
                rigRoot = null;
                contentRoot = null;
            }

            EditorPrefs.SetBool(RequestedKey, false);
            EditorPrefs.SetBool(RunningKey, false);
            EditorApplication.isPlaying = false;
            Debug.Log("[UiReviewInGameScreenshotCapture] Completed GameView screenshot sequence: " + OutDir);
        }

        private static void CleanupState()
        {
            EditorPrefs.DeleteKey(RequestedKey);
            EditorPrefs.DeleteKey(RunningKey);
            EditorPrefs.DeleteKey(IndexKey);
            EditorPrefs.DeleteKey(FrameKey);
            EditorPrefs.DeleteKey(AwaitingFileKey);
            EditorPrefs.DeleteKey(PendingPathKey);
            EditorPrefs.DeleteKey(CapturedThisRunKey);
            rigRoot = null;
            contentRoot = null;
        }

        private static void DeleteExistingCaptures()
        {
            string absoluteDirectory = ToAbsolute(OutDir);
            if (!Directory.Exists(absoluteDirectory))
            {
                return;
            }

            string[] files = Directory.GetFiles(absoluteDirectory, "*_ingame_*.png");
            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
                string metaPath = files[i] + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }
        }

        private static void SetupRig()
        {
            labelFont = RequireAsset<Font>(FontPath);
            rigRoot = new GameObject("Codex_InGameUiScreenshotRig");
            UnityEngine.Object.DontDestroyOnLoad(rigRoot);

            Canvas canvas = rigRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            CanvasScaler scaler = rigRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            rigRoot.AddComponent<GraphicRaycaster>();

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(rigRoot.transform, false);
            contentRoot = content.GetComponent<RectTransform>();
            contentRoot.anchorMin = Vector2.zero;
            contentRoot.anchorMax = Vector2.one;
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;
            Debug.Log("[UiReviewInGameScreenshotCapture] Runtime overlay ready.");
        }

        private static void MakeBacking(Transform canvas, string title)
        {
            MakePanel(canvas, "InGameDim", Vector2.zero, new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.35f));
            MakeText(canvas, "CaptureTitle", title, 32, new Vector2(-650f, 482f), new Vector2(620f, 48f), TextAnchor.MiddleLeft);
        }

        private static void PopulateOverview(Transform canvas)
        {
            MakePanel(canvas, "CellA", new Vector2(-620f, 260f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.35f));
            MakeText(canvas, "LabelA", "Map HUD", 24, new Vector2(-800f, 430f), new Vector2(220f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, StageRuntime, "RealtimeMapHud", new Vector2(-620f, 240f), 0.9f, "RealtimeMapHud");

            MakePanel(canvas, "CellB", new Vector2(0f, 260f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.35f));
            MakeText(canvas, "LabelB", "Weapon HUD", 24, new Vector2(-180f, 430f), new Vector2(240f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, MainCanvas, "WeaponHudPanel", new Vector2(0f, 250f), 1.15f, "WeaponHudPanel");

            MakePanel(canvas, "CellC", new Vector2(620f, 260f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.35f));
            MakeText(canvas, "LabelC", "Item Radial", 24, new Vector2(440f, 430f), new Vector2(240f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, ItemRadial, "PlayerItemRadialPresenter", new Vector2(620f, 240f), 0.58f);

            MakePanel(canvas, "CellD", new Vector2(-620f, -150f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.35f));
            MakeText(canvas, "LabelD", "Wave Popup", 24, new Vector2(-800f, 20f), new Vector2(240f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, WaveDirectorCanvas, "WaveReadyPopup", new Vector2(-620f, -170f), 0.72f, "WaveReadyPopupRoot", "WaveReadyPopup");

            MakePanel(canvas, "CellE", new Vector2(0f, -150f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.35f));
            MakeText(canvas, "LabelE", "Treasure Reward", 24, new Vector2(-180f, 20f), new Vector2(260f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, MainCanvas, "TreasureRewardMenuPresenter", new Vector2(0f, -170f), 0.58f, "TreasureRewardMenuPresenter", "TreasureRewardPanel");

            MakePanel(canvas, "CellF", new Vector2(620f, -150f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.35f));
            MakeText(canvas, "LabelF", "Build / Shop", 24, new Vector2(440f, 20f), new Vector2(240f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, PlacementBuild, "PlacementBuildMenuPanel", new Vector2(535f, -175f), 0.42f);
            AddPrefabTarget(canvas, SupportShop, "SupportTruckShopPresenter", new Vector2(735f, -175f), 0.38f);
        }

        private static void PopulateHudMapWeapon(Transform canvas)
        {
            AddPrefabTarget(canvas, StageRuntime, "RealtimeMapHud", new Vector2(-620f, 160f), 1.35f, "RealtimeMapHud");
            AddPrefabTarget(canvas, MainCanvas, "WeaponHudPanel", new Vector2(540f, -310f), 1.6f, "WeaponHudPanel");
        }

        private static GameObject AddPrefabTarget(Transform parent, string prefabPath, string label, Vector2 position, float scale, params string[] targetNames)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogError("[UiReviewInGameScreenshotCapture] Prefab missing: " + prefabPath);
                return null;
            }

            GameObject instance = null;
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform source = targetNames.Length > 0 ? FindByAnyName(prefabRoot.transform, targetNames) : prefabRoot.transform;
                if (source == null)
                {
                    Debug.LogError("[UiReviewInGameScreenshotCapture] Target missing: " + label);
                    return null;
                }

                SetActiveRecursive(source.gameObject, false);
                instance = UnityEngine.Object.Instantiate(source.gameObject);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            instance.name = label;
            DisableProjectBehaviours(instance);
            ApplySampleText(instance);
            instance.transform.SetParent(parent, false);
            SetActiveRecursive(instance, true);

            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = instance.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one * scale;
            return instance;
        }

        private static void ApplySampleText(GameObject root)
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                string n = text.gameObject.name;
                if (n.Contains("WaveTimer"))
                {
                    text.text = "NEXT WAVE 00:27";
                }
                else if (n.Contains("Amount"))
                {
                    text.text = "+80";
                }
                else if (n.Contains("Name") && string.IsNullOrWhiteSpace(text.text))
                {
                    text.text = "SUPPLY CACHE";
                }
                else if (n.Contains("Description"))
                {
                    text.text = "Select reward";
                }
                else if (string.IsNullOrWhiteSpace(text.text))
                {
                    text.text = "READY";
                }
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                string n = text.gameObject.name;
                if (n.Contains("WeaponName"))
                {
                    text.text = "RIFLE MK-II";
                }
                else if (n.Contains("Ammo"))
                {
                    text.text = "24 / 96";
                }
                else if (string.IsNullOrWhiteSpace(text.text))
                {
                    text.text = "READY";
                }
            }
        }

        private static void DisableProjectBehaviours(GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                {
                    continue;
                }

                string ns = behaviour.GetType().Namespace ?? string.Empty;
                if (ns.StartsWith("CorridorCommander", StringComparison.Ordinal))
                {
                    behaviour.enabled = false;
                }
            }
        }

        private static Transform FindByAnyName(Transform root, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Transform found = FindByName(root, names[i]);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindByName(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void ClearContent()
        {
            if (contentRoot == null)
            {
                return;
            }

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(contentRoot.GetChild(i).gameObject);
            }
        }

        private static Text MakeText(Transform parent, string name, string value, int size, Vector2 position, Vector2 box, TextAnchor anchor)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            SetRect(textObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), box, position);
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = labelFont;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Image MakePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            SetRect(panelObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), size, position);
            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void SetActiveRecursive(GameObject go, bool active)
        {
            go.SetActive(active);
            for (int i = 0; i < go.transform.childCount; i++)
            {
                SetActiveRecursive(go.transform.GetChild(i).gameObject, active);
            }
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Required capture asset missing: " + path);
            }

            return asset;
        }

        private static string ToAbsolute(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, assetPath).Replace('\\', '/');
        }

        private sealed class CaptureSpec
        {
            public CaptureSpec(string fileName, string title, Action<Transform> populate)
            {
                FileName = fileName;
                Title = title;
                Populate = populate;
            }

            public string FileName { get; private set; }
            public string Title { get; private set; }
            public Action<Transform> Populate { get; private set; }
        }
    }
}
