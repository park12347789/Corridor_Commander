using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander.EditorTools
{
    public static class UiReviewScreenshotCapture
    {
        private const int Width = 1920;
        private const int Height = 1080;
        private const string OutDir = "Assets/Screenshots/UIReview";
        private const string FontPath = "Assets/hansol/09_Settings/Font/BMJUA/BMJUA_ttf.ttf";
        private const string MainCanvas = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvas.prefab";
        private const string StageRuntime = "Assets/hansol/03_Prefabs/Stage/StageRuntime.prefab";
        private const string ItemRadial = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PlayerItemRadialPresenter.prefab";
        private const string CommandRadial = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PlayerCommandRadialPresenter.prefab";
        private const string WaveDirectorCanvas = "Assets/hansol/03_Prefabs/UI/InGame/WaveDirectorCanvas.prefab";
        private const string SupportShop = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/SupportTruckShopPresenter.prefab";
        private const string PlacementBuild = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/PlacementBuildMenuPanel.prefab";
        private const string InstalledAction = "Assets/hansol/03_Prefabs/UI/InGame/MainCanvasParts/InstalledObjectActionPresenter.prefab";

        private static readonly List<string> Written = new List<string>();
        private static readonly List<string> Missing = new List<string>();
        private static Font labelFont;

        [MenuItem("Corridor Commander/UI/Capture UI Review Screenshots")]
        public static void Capture()
        {
            Written.Clear();
            Missing.Clear();
            labelFont = RequireAsset<Font>(FontPath);
            Directory.CreateDirectory(ToAbsolute(OutDir));

            CaptureShot("00_ui_review_overview.png", "UI REVIEW OVERVIEW", PopulateOverview);
            CaptureShot("01_hud_map_weapon.png", "HUD: MAP + WEAPON STATUS", PopulateHudMapWeapon);
            CaptureShot("02_realtime_map_hud.png", "REALTIME MAP HUD", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, StageRuntime, "RealtimeMapHud", Vector2.zero, 2.35f, "RealtimeMapHud");
            });
            CaptureShot("03_weapon_hud.png", "WEAPON STATUS HUD", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, MainCanvas, "WeaponHudPanel", Vector2.zero, 2.9f, "WeaponHudPanel");
            });
            CaptureShot("04_item_radial.png", "ITEM RADIAL", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, ItemRadial, "PlayerItemRadialPresenter", Vector2.zero, 1.35f);
            });
            CaptureShot("05_command_radial.png", "COMMAND RADIAL", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, CommandRadial, "PlayerCommandRadialPresenter", Vector2.zero, 1.35f);
            });
            CaptureShot("06_wave_ready_popup.png", "WAVE READY POPUP", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, WaveDirectorCanvas, "WaveReadyPopup", Vector2.zero, 1.55f, "WaveReadyPopupRoot", "WaveReadyPopup");
            });
            CaptureShot("07_treasure_reward.png", "TREASURE REWARD POPUP", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, MainCanvas, "TreasureRewardMenuPresenter", Vector2.zero, 1.15f, "TreasureRewardMenuPresenter", "TreasureRewardPanel");
            });
            CaptureShot("08_build_shop_action.png", "BUILD / SHOP / ACTION UI", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, PlacementBuild, "PlacementBuildMenuPanel", new Vector2(-520f, -20f), 0.82f);
                AddPrefabTarget(canvas, SupportShop, "SupportTruckShopPresenter", new Vector2(220f, 0f), 0.82f);
                AddPrefabTarget(canvas, InstalledAction, "InstalledObjectActionPresenter", new Vector2(680f, -320f), 1.25f);
            });
            CaptureShot("09_pause_aim_info.png", "PAUSE / AIM INFO UI", delegate(Transform canvas)
            {
                AddPrefabTarget(canvas, MainCanvas, "PauseMenuPresenter", new Vector2(-350f, 0f), 1.15f, "PauseMenuPresenter", "PauseMenuRoot");
                AddPrefabTarget(canvas, MainCanvas, "InstalledObjectAimInfoPresenter", new Vector2(520f, -20f), 1.35f, "InstalledObjectAimInfoPresenter", "InstalledObjectAimInfoRoot");
            });

            AssetDatabase.Refresh();
            Debug.Log("[UiReviewScreenshotCapture] Captured: " + string.Join(", ", Written.ToArray()));
            if (Missing.Count > 0)
            {
                Debug.LogWarning("[UiReviewScreenshotCapture] Missing: " + string.Join(", ", Missing.ToArray()));
            }
        }

        private static void PopulateOverview(Transform canvas)
        {
            MakePanel(canvas, "CellA", new Vector2(-620f, 260f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.18f));
            MakeText(canvas, "LabelA", "Map HUD", 24, new Vector2(-800f, 430f), new Vector2(220f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, StageRuntime, "RealtimeMapHud", new Vector2(-620f, 240f), 0.9f, "RealtimeMapHud");

            MakePanel(canvas, "CellB", new Vector2(0f, 260f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.18f));
            MakeText(canvas, "LabelB", "Weapon HUD", 24, new Vector2(-180f, 430f), new Vector2(240f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, MainCanvas, "WeaponHudPanel", new Vector2(0f, 250f), 1.15f, "WeaponHudPanel");

            MakePanel(canvas, "CellC", new Vector2(620f, 260f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.18f));
            MakeText(canvas, "LabelC", "Item Radial", 24, new Vector2(440f, 430f), new Vector2(240f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, ItemRadial, "PlayerItemRadialPresenter", new Vector2(620f, 240f), 0.58f);

            MakePanel(canvas, "CellD", new Vector2(-620f, -150f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.18f));
            MakeText(canvas, "LabelD", "Wave Popup", 24, new Vector2(-800f, 20f), new Vector2(240f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, WaveDirectorCanvas, "WaveReadyPopup", new Vector2(-620f, -170f), 0.72f, "WaveReadyPopupRoot", "WaveReadyPopup");

            MakePanel(canvas, "CellE", new Vector2(0f, -150f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.18f));
            MakeText(canvas, "LabelE", "Treasure Reward", 24, new Vector2(-180f, 20f), new Vector2(260f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, MainCanvas, "TreasureRewardMenuPresenter", new Vector2(0f, -170f), 0.58f, "TreasureRewardMenuPresenter", "TreasureRewardPanel");

            MakePanel(canvas, "CellF", new Vector2(620f, -150f), new Vector2(520f, 360f), new Color(0f, 0f, 0f, 0.18f));
            MakeText(canvas, "LabelF", "Build / Shop", 24, new Vector2(440f, 20f), new Vector2(240f, 36f), TextAnchor.MiddleLeft);
            AddPrefabTarget(canvas, PlacementBuild, "PlacementBuildMenuPanel", new Vector2(535f, -175f), 0.42f);
            AddPrefabTarget(canvas, SupportShop, "SupportTruckShopPresenter", new Vector2(735f, -175f), 0.38f);
        }

        private static void PopulateHudMapWeapon(Transform canvas)
        {
            AddPrefabTarget(canvas, StageRuntime, "RealtimeMapHud", new Vector2(-620f, 160f), 1.35f, "RealtimeMapHud");
            AddPrefabTarget(canvas, MainCanvas, "WeaponHudPanel", new Vector2(540f, -310f), 1.6f, "WeaponHudPanel");
        }

        private static void CaptureShot(string fileName, string title, Action<Transform> populate)
        {
            GameObject root = new GameObject("Codex_UiScreenshotRig");
            root.hideFlags = HideFlags.DontSave;
            try
            {
                Camera camera = CreateCamera(root.transform);
                Canvas canvas = CreateCanvas(root.transform, camera);
                MakePanel(canvas.transform, "Background", Vector2.zero, new Vector2(Width, Height), new Color(0.035f, 0.045f, 0.06f, 1f));
                MakeText(canvas.transform, "CaptureTitle", title, 32, new Vector2(-650f, 482f), new Vector2(600f, 48f), TextAnchor.MiddleLeft);
                populate(canvas.transform);
                WriteCameraPng(camera, OutDir + "/" + fileName);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Camera CreateCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Codex_UiScreenshotCamera", typeof(Camera));
            cameraObject.hideFlags = HideFlags.DontSave;
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -100f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = Height * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.cullingMask = 1 << 5;
            return camera;
        }

        private static Canvas CreateCanvas(Transform parent, Camera camera)
        {
            GameObject canvasObject = new GameObject("Codex_UiScreenshotCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.hideFlags = HideFlags.DontSave;
            canvasObject.transform.SetParent(parent, false);
            SetLayerRecursive(canvasObject, 5);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void WriteCameraPng(Camera camera, string assetPath)
        {
            Canvas.ForceUpdateCanvases();
            RenderTexture rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(ToAbsolute(assetPath), texture.EncodeToPNG());
                Written.Add(assetPath);
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        private static GameObject AddPrefabTarget(Transform parent, string prefabPath, string label, Vector2 position, float scale, params string[] targetNames)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Missing.Add(label + " prefab missing: " + prefabPath);
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                Missing.Add(label + " instantiate failed: " + prefabPath);
                return null;
            }

            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            SetActiveRecursive(instance, true);
            DisableProjectBehaviours(instance);
            ApplySampleText(instance);

            Transform target = targetNames.Length > 0 ? FindByAnyName(instance.transform, targetNames) : instance.transform;
            if (target == null)
            {
                Missing.Add(label + " target missing: " + string.Join(",", targetNames));
                UnityEngine.Object.DestroyImmediate(instance);
                return null;
            }

            target.SetParent(parent, false);
            if (target.gameObject != instance)
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            RemoveNestedCanvasComponents(target.gameObject);
            SetActiveRecursive(target.gameObject, true);
            SetLayerRecursive(target.gameObject, 5);

            RectTransform rect = target.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = target.gameObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one * scale;
            target.gameObject.name = label;
            return target.gameObject;
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
                else if (n.Contains("Title") && string.IsNullOrWhiteSpace(text.text))
                {
                    text.text = "TACTICAL UI";
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

                text.raycastTarget = false;
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
                else if (n.Contains("Label") && string.IsNullOrWhiteSpace(text.text))
                {
                    text.text = "WEAPON";
                }
                else if (string.IsNullOrWhiteSpace(text.text))
                {
                    text.text = "READY";
                }

                text.raycastTarget = false;
            }
        }

        private static void RemoveNestedCanvasComponents(GameObject root)
        {
            foreach (GraphicRaycaster raycaster in root.GetComponentsInChildren<GraphicRaycaster>(true))
            {
                UnityEngine.Object.DestroyImmediate(raycaster);
            }

            foreach (CanvasScaler scaler in root.GetComponentsInChildren<CanvasScaler>(true))
            {
                UnityEngine.Object.DestroyImmediate(scaler);
            }

            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                UnityEngine.Object.DestroyImmediate(canvas);
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

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
            }
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
    }
}
