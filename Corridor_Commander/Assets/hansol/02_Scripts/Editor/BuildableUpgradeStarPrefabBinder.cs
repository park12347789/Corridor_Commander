using CorridorCommander;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander.Editor
{
    public static class BuildableUpgradeStarPrefabBinder
    {
        private const string StarSpritePath = "Assets/90_ThirdParty/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites/Components/IconMisc/GradeIcon_Star_s_Yellow.png";
        private static readonly string[] PrefabPaths =
        {
            "Assets/hansol/03_Prefabs/Turret_Basic.prefab",
            "Assets/hansol/03_Prefabs/Turret_Rapid.prefab",
            "Assets/hansol/03_Prefabs/Turret_LongRange.prefab",
            "Assets/hansol/03_Prefabs/SawTrap_Turret_Yellow.prefab",
            "Assets/hansol/03_Prefabs/Barricade_Basic.prefab",
            "Assets/hansol/03_Prefabs/Barricade_Level2.prefab"
        };

        [MenuItem("Corridor Commander/Bind Buildable Upgrade Stars")]
        public static void Bind()
        {
            Sprite starSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StarSpritePath);
            if (starSprite == null)
            {
                Debug.LogError($"[BuildableUpgradeStarPrefabBinder] Missing star sprite: {StarSpritePath}");
                return;
            }

            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                BindPrefab(PrefabPaths[i], starSprite);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuildableUpgradeStarPrefabBinder] Upgrade stars bound.");
        }

        private static void BindPrefab(string prefabPath, Sprite starSprite)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                MonoBehaviour provider = FindProvider(root);
                Transform canvasParent = ResolveCanvasParent(root.transform);
                Transform canvasTransform = FindChildRecursive(root.transform, "UpgradeStarCanvas");
                if (provider == null)
                {
                    CleanupStars(root, canvasTransform);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    return;
                }

                InstalledUpgradeStarDisplay display = root.GetComponent<InstalledUpgradeStarDisplay>();
                if (display == null)
                {
                    display = root.AddComponent<InstalledUpgradeStarDisplay>();
                }

                if (canvasTransform == null)
                {
                    canvasTransform = CreateCanvas(canvasParent).transform;
                }
                else
                {
                    canvasTransform.SetParent(canvasParent, false);
                    ConfigureCanvasTransform(canvasTransform, 0.012f);
                }

                Image[] starImages = EnsureStarImages(canvasTransform, starSprite);
                SerializedObject serializedDisplay = new SerializedObject(display);
                serializedDisplay.FindProperty("providerSource").objectReferenceValue = provider;
                serializedDisplay.FindProperty("starCanvas").objectReferenceValue = canvasTransform.GetComponent<Canvas>();
                serializedDisplay.FindProperty("verticalOffset").floatValue = ResolveVerticalOffset(prefabPath);
                SerializedProperty starImagesProperty = serializedDisplay.FindProperty("starImages");
                starImagesProperty.arraySize = starImages.Length;
                for (int i = 0; i < starImages.Length; i++)
                {
                    starImagesProperty.GetArrayElementAtIndex(i).objectReferenceValue = starImages[i];
                }

                serializedDisplay.ApplyModifiedPropertiesWithoutUndo();

                BarricadeInstalledActionProvider barricadeProvider = root.GetComponent<BarricadeInstalledActionProvider>();
                if (barricadeProvider != null && prefabPath.Contains("Level2"))
                {
                    SerializedObject serializedBarricade = new SerializedObject(barricadeProvider);
                    serializedBarricade.FindProperty("barricadeLevel").intValue = 2;
                    serializedBarricade.FindProperty("maxBarricadeLevel").intValue = 4;
                    serializedBarricade.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static float ResolveVerticalOffset(string prefabPath)
        {
            if (prefabPath.Contains("Barricade_Basic"))
            {
                return 0.2f;
            }

            if (prefabPath.Contains("Barricade_Level2"))
            {
                return 0.38f;
            }

            return 0.55f;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("UpgradeStarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            ConfigureCanvasTransform(canvasObject.transform, 0.012f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 30;

            RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(180f, 48f);
            return canvas;
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

        private static Image[] EnsureStarImages(Transform canvasTransform, Sprite starSprite)
        {
            Image[] starImages = new Image[3];
            for (int i = 0; i < starImages.Length; i++)
            {
                string name = $"UpgradeStar_{i + 1}";
                Transform starTransform = canvasTransform.Find(name);
                if (starTransform == null)
                {
                    GameObject starObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    starTransform = starObject.transform;
                    starTransform.SetParent(canvasTransform, false);
                }

                RectTransform rectTransform = starTransform.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2((i - 1) * 46f, 0f);
                rectTransform.sizeDelta = new Vector2(42f, 42f);

                Image image = starTransform.GetComponent<Image>();
                image.sprite = starSprite;
                image.raycastTarget = false;
                image.preserveAspect = true;
                image.color = Color.white;
                starImages[i] = image;
            }

            return starImages;
        }

        private static MonoBehaviour FindProvider(GameObject root)
        {
            MonoBehaviour[] behaviours = root.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInstalledUpgradeLevelProvider)
                {
                    return behaviours[i];
                }
            }

            return null;
        }

        private static void CleanupStars(GameObject root, Transform canvasTransform)
        {
            InstalledUpgradeStarDisplay display = root.GetComponent<InstalledUpgradeStarDisplay>();
            if (display != null)
            {
                Object.DestroyImmediate(display, true);
            }

            if (canvasTransform != null)
            {
                Object.DestroyImmediate(canvasTransform.gameObject, true);
            }
        }
    }
}
