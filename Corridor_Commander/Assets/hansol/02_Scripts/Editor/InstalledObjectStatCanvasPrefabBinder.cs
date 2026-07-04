using CorridorCommander;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander.Editor
{
    public static class InstalledObjectStatCanvasPrefabBinder
    {
        private const string StatCanvasPrefabPath = "Assets/hansol/03_Prefabs/UI/InstalledObjectStatCanvas.prefab";
        private const string PreferredKoreanFontPath = "Assets/hansol/09_Settings/Font/BMJUA/BMJUA Korean Dynamic SDF.asset";

        private static readonly string[] PrefabPaths =
        {
            "Assets/hansol/03_Prefabs/Turret_Basic.prefab",
            "Assets/hansol/03_Prefabs/Turret_Rapid.prefab",
            "Assets/hansol/03_Prefabs/Turret_LongRange.prefab",
            "Assets/hansol/03_Prefabs/Turret_Modular_OrangeWhite.prefab",
            "Assets/hansol/03_Prefabs/TEMP_Mortar_Basic.prefab",
            "Assets/hansol/03_Prefabs/TEMP_Mortar_Rapid.prefab",
            "Assets/hansol/03_Prefabs/TEMP_Mortar_Heavy.prefab",
            "Assets/hansol/03_Prefabs/SawTrap_Turret_Yellow.prefab",
            "Assets/hansol/03_Prefabs/Barricade_Basic.prefab",
            "Assets/hansol/03_Prefabs/Barricade_Level2.prefab"
        };

        [MenuItem("Corridor Commander/Bind Installed Object Stat Canvases")]
        public static void Bind()
        {
            EnsureSharedStatCanvasPrefab();
            GameObject statCanvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StatCanvasPrefabPath);
            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                BindPrefab(PrefabPaths[i], statCanvasPrefab);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[InstalledObjectStatCanvasPrefabBinder] Installed object stat canvases bound.");
        }

        private static void BindPrefab(string prefabPath, GameObject statCanvasPrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                MonoBehaviour provider = FindProvider(root);
                Transform canvasParent = ResolveCanvasParent(root.transform);
                Transform canvasTransform = FindChildRecursive(root.transform, "InstalledObjectStatCanvas");
                if (provider == null)
                {
                    if (canvasTransform != null)
                    {
                        Object.DestroyImmediate(canvasTransform.gameObject, true);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    return;
                }

                if (canvasTransform != null && !IsSharedStatCanvasInstance(canvasTransform))
                {
                    Object.DestroyImmediate(canvasTransform.gameObject, true);
                    canvasTransform = null;
                }

                if (canvasTransform == null)
                {
                    GameObject canvasInstance = (GameObject)PrefabUtility.InstantiatePrefab(statCanvasPrefab, canvasParent);
                    canvasInstance.name = "InstalledObjectStatCanvas";
                    canvasTransform = canvasInstance.transform;
                }

                canvasTransform.SetParent(canvasParent, false);
                ConfigureCanvasTransform(canvasTransform, 0.004f);
                ConfigureStatCanvas(canvasTransform, root.transform, ResolveVerticalOffset(prefabPath), false);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureSharedStatCanvasPrefab()
        {
            EnsureFolder("Assets/hansol/03_Prefabs/UI");

            bool existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StatCanvasPrefabPath) != null;
            GameObject root = existingPrefab
                ? PrefabUtility.LoadPrefabContents(StatCanvasPrefabPath)
                : CreateCanvas(null).gameObject;

            try
            {
                root.name = "InstalledObjectStatCanvas";
                ConfigureCanvasTransform(root.transform, 0.004f);
                ConfigureStatCanvas(root.transform, null, 0.9f, true);
                PrefabUtility.SaveAsPrefabAsset(root, StatCanvasPrefabPath);
            }
            finally
            {
                if (existingPrefab)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static void ConfigureStatCanvas(
            Transform canvasTransform,
            Transform installedObjectRoot,
            float verticalOffset,
            bool visibleInPrefab)
        {
            Canvas canvas = canvasTransform.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.enabled = visibleInPrefab;
            canvas.sortingOrder = 62;

            CanvasScaler scaler = canvasTransform.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.dynamicPixelsPerUnit = 10f;
            }

            RectTransform canvasRect = canvasTransform.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(450f, 126f);

            WorldSpaceCameraBillboard billboard = canvasTransform.GetComponent<WorldSpaceCameraBillboard>();
            if (billboard == null)
            {
                billboard = canvasTransform.gameObject.AddComponent<WorldSpaceCameraBillboard>();
            }

            SerializedObject serializedBillboard = new SerializedObject(billboard);
            serializedBillboard.FindProperty("yawOnly").boolValue = true;
            serializedBillboard.FindProperty("faceCameraForward").boolValue = false;
            serializedBillboard.FindProperty("lockWorldY").boolValue = false;
            serializedBillboard.ApplyModifiedPropertiesWithoutUndo();

            InstalledObjectStatCanvasPresenter presenter = canvasTransform.GetComponent<InstalledObjectStatCanvasPresenter>();
            if (presenter == null)
            {
                presenter = canvasTransform.gameObject.AddComponent<InstalledObjectStatCanvasPresenter>();
            }

            ResolveStatLayout(
                canvasTransform,
                out GameObject panelRoot,
                out TMP_Text title,
                out TMP_Text level,
                out TMP_Text stat,
                out TMP_Text health,
                out Image healthFill);

            SerializedObject serializedPresenter = new SerializedObject(presenter);
            serializedPresenter.FindProperty("installedObjectRoot").objectReferenceValue = installedObjectRoot;
            serializedPresenter.FindProperty("targetCanvas").objectReferenceValue = canvas;
            serializedPresenter.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serializedPresenter.FindProperty("titleText").objectReferenceValue = title;
            serializedPresenter.FindProperty("levelText").objectReferenceValue = level;
            serializedPresenter.FindProperty("statText").objectReferenceValue = stat;
            serializedPresenter.FindProperty("healthText").objectReferenceValue = health;
            serializedPresenter.FindProperty("healthFillImage").objectReferenceValue = healthFill;
            serializedPresenter.FindProperty("positionAboveInstalledObject").boolValue = true;
            serializedPresenter.FindProperty("verticalOffset").floatValue = verticalOffset;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ResolveStatLayout(
            Transform canvasTransform,
            out GameObject panelRoot,
            out TMP_Text title,
            out TMP_Text level,
            out TMP_Text stat,
            out TMP_Text health,
            out Image healthFill)
        {
            Transform popup = canvasTransform.Find("Popup_OutTitle");
            Transform slider = canvasTransform.Find("HealthBarTrack/Slider_Basic01_Green");
            if (popup != null && slider != null)
            {
                panelRoot = popup.gameObject;
                title = EnsureExistingText(popup, "Title/Text (TMP)", "Turret");
                level = EnsureExistingText(popup, "Top/Text (TMP) (1)", "LV 1");
                stat = EnsureExistingText(popup, "Top/Text_Info", "Range 10   Damage 6   Cooldown 0.75s");
                health = EnsureExistingText(slider, "Text (TMP)", "HP 30/30");
                healthFill = EnsureExistingImage(slider, "FillMask/FillArea/Fill");
                return;
            }

            Transform panel = EnsurePanel(canvasTransform);
            EnsureAccentBar(panel);
            panelRoot = panel.gameObject;
            title = EnsureText(panel, "TitleText", "Turret", new Vector2(-72f, 35f), new Vector2(280f, 24f), 20f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.92f, 0.96f, 1f, 1f));
            level = EnsureText(panel, "LevelText", "LV 1", new Vector2(160f, 35f), new Vector2(110f, 22f), 15f, FontStyles.Bold, TextAlignmentOptions.Right, new Color(0.98f, 0.88f, 0.34f, 1f));
            stat = EnsureText(panel, "StatText", "Range 10   Damage 6   Cooldown 0.75s", new Vector2(0f, 6f), new Vector2(388f, 22f), 15f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.84f, 0.92f, 1f, 1f));
            health = EnsureText(panel, "HealthText", "HP 30/30", new Vector2(0f, -21f), new Vector2(388f, 18f), 14f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.9f, 1f, 0.92f, 1f));
            healthFill = EnsureHealthFill(panel);
        }

        private static TMP_Text EnsureExistingText(Transform parent, string path, string defaultText)
        {
            Transform textTransform = parent.Find(path);
            TMP_Text text = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
            if (text == null)
            {
                Debug.LogError($"[InstalledObjectStatCanvasPrefabBinder] Missing TMP text at {parent.name}/{path}.", parent);
                return null;
            }

            text.text = defaultText;
            AssignPreferredFont(text);
            text.raycastTarget = false;
            return text;
        }

        private static Image EnsureExistingImage(Transform parent, string path)
        {
            Transform imageTransform = parent.Find(path);
            Image image = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
            if (image == null)
            {
                Debug.LogError($"[InstalledObjectStatCanvasPrefabBinder] Missing Image at {parent.name}/{path}.", parent);
                return null;
            }

            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
            image.raycastTarget = false;
            return image;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject(
                "InstalledObjectStatCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            ConfigureCanvasTransform(canvasObject.transform, 0.0045f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 62;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(450f, 126f);
            return canvas;
        }

        private static bool IsSharedStatCanvasInstance(Transform canvasTransform)
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(canvasTransform.gameObject);
            return source != null && AssetDatabase.GetAssetPath(source) == StatCanvasPrefabPath;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(folderPath);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void ConfigureCanvasTransform(Transform canvasTransform, float localScale)
        {
            canvasTransform.localPosition = new Vector3(0f, 2.2f, 0f);
            canvasTransform.localRotation = Quaternion.identity;
            canvasTransform.localScale = Vector3.one * localScale;
        }

        private static Transform ResolveCanvasParent(Transform root)
        {
            if (root == null || IsUniformScale(root.lossyScale))
            {
                return root;
            }

            Transform canvasRoot = root.Find("InstalledObjectCanvasRoot");
            if (canvasRoot == null)
            {
                GameObject canvasRootObject = new GameObject("InstalledObjectCanvasRoot");
                canvasRoot = canvasRootObject.transform;
                canvasRoot.SetParent(root, false);
            }

            canvasRoot.localPosition = Vector3.zero;
            canvasRoot.localRotation = Quaternion.identity;
            canvasRoot.localScale = ResolveLocalScaleForWorldScale(root, 1f);
            return canvasRoot;
        }

        private static bool IsUniformScale(Vector3 scale)
        {
            return Mathf.Abs(scale.x - scale.y) <= 0.0001f
                && Mathf.Abs(scale.y - scale.z) <= 0.0001f;
        }

        private static Vector3 ResolveLocalScaleForWorldScale(Transform parent, float targetWorldScale)
        {
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            return new Vector3(
                ResolveScaleAxis(parentScale.x, targetWorldScale),
                ResolveScaleAxis(parentScale.y, targetWorldScale),
                ResolveScaleAxis(parentScale.z, targetWorldScale));
        }

        private static float ResolveScaleAxis(float parentScale, float targetWorldScale)
        {
            return Mathf.Approximately(parentScale, 0f)
                ? targetWorldScale
                : targetWorldScale / Mathf.Abs(parentScale);
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform match = FindChildRecursive(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Transform EnsurePanel(Transform canvasTransform)
        {
            Transform panel = canvasTransform.Find("StatPanel");
            if (panel == null)
            {
                GameObject panelObject = new GameObject("StatPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel = panelObject.transform;
                panel.SetParent(canvasTransform, false);
            }

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(430f, 112f);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.13f, 0.92f);
            image.raycastTarget = false;
            return panel;
        }

        private static Image EnsureAccentBar(Transform parent)
        {
            Transform accent = parent.Find("AccentBar");
            if (accent == null)
            {
                GameObject accentObject = new GameObject("AccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                accent = accentObject.transform;
                accent.SetParent(parent, false);
            }

            RectTransform rectTransform = accent.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-206f, 0f);
            rectTransform.sizeDelta = new Vector2(6f, 92f);

            Image image = accent.GetComponent<Image>();
            image.color = new Color(0.13f, 0.44f, 0.95f, 1f);
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text EnsureText(
            Transform parent,
            string name,
            string defaultText,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Color color)
        {
            Transform textTransform = parent.Find(name);
            if (textTransform == null)
            {
                GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textTransform = textObject.transform;
                textTransform.SetParent(parent, false);
            }

            RectTransform rectTransform = textTransform.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            TMP_Text text = textTransform.GetComponent<TMP_Text>();
            text.text = defaultText;
            AssignPreferredFont(text);
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Min(11f, fontSize);
            text.fontSizeMax = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void AssignPreferredFont(TMP_Text text)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredKoreanFontPath);
            if (font == null || font.material == null)
            {
                return;
            }

            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        private static Image EnsureHealthFill(Transform parent)
        {
            Transform track = parent.Find("HealthBarTrack");
            if (track == null)
            {
                GameObject trackObject = new GameObject("HealthBarTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                track = trackObject.transform;
                track.SetParent(parent, false);
            }

            RectTransform trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.5f, 0.5f);
            trackRect.anchorMax = new Vector2(0.5f, 0.5f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.anchoredPosition = new Vector2(0f, -43f);
            trackRect.sizeDelta = new Vector2(382f, 10f);

            Image trackImage = track.GetComponent<Image>();
            trackImage.color = new Color(0.025f, 0.03f, 0.045f, 0.95f);
            trackImage.raycastTarget = false;

            Transform fill = track.Find("HealthBarFill");
            if (fill == null)
            {
                GameObject fillObject = new GameObject("HealthBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fill = fillObject.transform;
                fill.SetParent(track, false);
            }

            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.28f, 0.95f, 0.48f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            fillImage.raycastTarget = false;
            return fillImage;
        }

        private static float ResolveVerticalOffset(string prefabPath)
        {
            if (prefabPath.Contains("Barricade_Basic"))
            {
                return 0.45f;
            }

            if (prefabPath.Contains("Barricade_Level2"))
            {
                return 0.55f;
            }

            if (prefabPath.Contains("SawTrap"))
            {
                return 0.55f;
            }

            if (prefabPath.Contains("Mortar"))
            {
                return 0.75f;
            }

            return 0.9f;
        }

        private static MonoBehaviour FindProvider(GameObject root)
        {
            MonoBehaviour[] behaviours = root.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInstalledAimInfoProvider)
                {
                    return behaviours[i];
                }
            }

            return null;
        }
    }
}
