using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander.EditorTools
{
    internal enum InGameUiChromeFunction
    {
        HudBackground,
        HudHeader,
        HudContent,
        StatusPanel,
        PrimaryButton,
        SecondaryButton,
        Divider,
        Glow
    }

    internal sealed class InGameUiChromeAssets
    {
        private const string FontPath = "Assets/hansol/09_Settings/Font/BMJUA/BMJUA_ttf.ttf";
        private const string TmpFontPath = "Assets/hansol/09_Settings/Font/BMJUA/BMJUA Korean Dynamic SDF.asset";
        private const string LayerLabSpriteRoot = "Assets/90_ThirdParty/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites";
        private const string MarkerSpritePath = LayerLabSpriteRoot + "/Components/Icon_PictoIcons/512/PictoIcon_Map_Pin.Png";
        private const string MapIconPath = "Assets/hansol/04_Art/UI/Generated/map_icon_imagegen.png";
        private const string RangeRingPath = LayerLabSpriteRoot + "/Components/UI_Etc/Alert_Circle_l_Bg.png";
        private const string PopupBackgroundPath = LayerLabSpriteRoot + "/Components/Popup/Popup02~09_Topber_White_Bg.png";
        private const string PopupHeaderPath = LayerLabSpriteRoot + "/Components/Popup/Popup02~09_Topber_White_BgTop.png";
        private const string PopupPatternPath = LayerLabSpriteRoot + "/Components/Popup/Popup06_Pattern.png";
        private const string PrimaryButtonPath = LayerLabSpriteRoot + "/Components/Button/Button01_l_Blue.png";
        private const string SecondaryButtonPath = LayerLabSpriteRoot + "/Components/Button/Button01_l_DarkGray.png";
        private const string SkillButtonDarkPath = LayerLabSpriteRoot + "/Components/Button/Button_SkillBtn_Dark.png";
        private const string SkillButtonBluePath = LayerLabSpriteRoot + "/Components/Button/Button_SkillBtn_Blue.png";
        private const string DividerPath = LayerLabSpriteRoot + "/Components/Label/Title_Line03_Divider.png";
        private const string GlowCirclePath = LayerLabSpriteRoot + "/Demo/Demo_Image/Glow_Circle02.png";

        private InGameUiChromeAssets(
            Font font,
            TMP_FontAsset tmpFont,
            Sprite marker,
            Sprite mapIcon,
            Sprite rangeRing,
            Sprite popupBackground,
            Sprite popupHeader,
            Sprite popupPattern,
            Sprite primaryButton,
            Sprite secondaryButton,
            Sprite skillButtonDark,
            Sprite skillButtonBlue,
            Sprite divider,
            Sprite glowCircle)
        {
            Font = font;
            TmpFont = tmpFont;
            Marker = marker;
            MapIcon = mapIcon;
            RangeRing = rangeRing;
            PopupBackground = popupBackground;
            PopupHeader = popupHeader;
            PopupPattern = popupPattern;
            PrimaryButton = primaryButton;
            SecondaryButton = secondaryButton;
            SkillButtonDark = skillButtonDark;
            SkillButtonBlue = skillButtonBlue;
            Divider = divider;
            GlowCircle = glowCircle;
        }

        public Font Font { get; }
        public TMP_FontAsset TmpFont { get; }
        public Sprite Marker { get; }
        public Sprite MapIcon { get; }
        public Sprite RangeRing { get; }
        public Sprite PopupBackground { get; }
        public Sprite PopupHeader { get; }
        public Sprite PopupPattern { get; }
        public Sprite PrimaryButton { get; }
        public Sprite SecondaryButton { get; }
        public Sprite SkillButtonDark { get; }
        public Sprite SkillButtonBlue { get; }
        public Sprite Divider { get; }
        public Sprite GlowCircle { get; }

        public static InGameUiChromeAssets Load()
        {
            return new InGameUiChromeAssets(
                InGameUiChromeFactory.LoadRequiredAsset<Font>(FontPath),
                InGameUiChromeFactory.LoadRequiredAsset<TMP_FontAsset>(TmpFontPath),
                InGameUiChromeFactory.EnsureSpriteAsset(MarkerSpritePath),
                InGameUiChromeFactory.EnsureSpriteAsset(MapIconPath),
                InGameUiChromeFactory.EnsureSpriteAsset(RangeRingPath),
                InGameUiChromeFactory.LoadRequiredAsset<Sprite>(PopupBackgroundPath),
                InGameUiChromeFactory.LoadRequiredAsset<Sprite>(PopupHeaderPath),
                InGameUiChromeFactory.LoadRequiredAsset<Sprite>(PopupPatternPath),
                InGameUiChromeFactory.LoadRequiredAsset<Sprite>(PrimaryButtonPath),
                InGameUiChromeFactory.LoadRequiredAsset<Sprite>(SecondaryButtonPath),
                InGameUiChromeFactory.LoadRequiredAsset<Sprite>(SkillButtonDarkPath),
                InGameUiChromeFactory.LoadRequiredAsset<Sprite>(SkillButtonBluePath),
                InGameUiChromeFactory.LoadRequiredAsset<Sprite>(DividerPath),
                InGameUiChromeFactory.LoadRequiredAsset<Sprite>(GlowCirclePath));
        }
    }

    internal static class InGameUiChromeFactory
    {
        public static Image CreatePanel(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 anchor,
            InGameUiChromeFunction function,
            InGameUiChromeAssets assets)
        {
            Image image = CreateImage(name, parent, size, anchor);
            ApplyChrome(image, function, assets);
            return image;
        }

        public static Image CreatePanel(
            string name,
            Transform parent,
            RectTransformBounds bounds,
            InGameUiChromeFunction function,
            InGameUiChromeAssets assets)
        {
            Image image = CreateImage(name, parent, bounds);
            ApplyChrome(image, function, assets);
            return image;
        }

        public static Image CreatePanel(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 anchor,
            Color color,
            Sprite sprite,
            Image.Type type)
        {
            Image image = CreateImage(name, parent, size, anchor);
            image.color = color;
            image.sprite = sprite;
            image.type = sprite == null ? Image.Type.Simple : type;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static Image CreatePanel(
            string name,
            Transform parent,
            RectTransformBounds bounds,
            Color color,
            Sprite sprite,
            Image.Type type)
        {
            Image image = CreateImage(name, parent, bounds);
            image.color = color;
            image.sprite = sprite;
            image.type = sprite == null ? Image.Type.Simple : type;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 anchor,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color,
            InGameUiChromeAssets assets)
        {
            GameObject textObject = CreateChild(name, parent);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            SetRect(rect, size, anchor);
            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = assets.Font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static TMP_Text CreateTmpText(
            string name,
            Transform parent,
            RectTransformBounds bounds,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            InGameUiChromeAssets assets)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            ApplyBounds(rect, bounds);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.text = value;
            text.font = assets.TmpFont;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 anchor,
            string label,
            InGameUiChromeFunction function,
            InGameUiChromeAssets assets)
        {
            Image image = CreatePanel(name, parent, size, anchor, function, assets);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText("Label", image.transform, size - new Vector2(18f, 10f), new Vector2(0.5f, 0.5f), label, 18, TextAnchor.MiddleCenter, Color.white, assets);
            text.raycastTarget = false;
            return button;
        }

        public static RectTransformBounds Stretch()
        {
            return new RectTransformBounds(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        public static RectTransformBounds Anchored(Vector2 min, Vector2 max)
        {
            return new RectTransformBounds(min, max, Vector2.zero, Vector2.zero);
        }

        public static RectTransformBounds Anchored(Vector2 min, Vector2 max, Vector2 size)
        {
            return new RectTransformBounds(min, max, size, Vector2.zero);
        }

        public static void ApplyBounds(RectTransform rect, RectTransformBounds bounds)
        {
            rect.anchorMin = bounds.AnchorMin;
            rect.anchorMax = bounds.AnchorMax;
            rect.sizeDelta = bounds.SizeDelta;
            rect.anchoredPosition = bounds.AnchoredPosition;
            rect.localScale = Vector3.one;
        }

        public static void SetRect(RectTransform rect, Vector2 size, Vector2 anchor)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        public static void ClearChildren(Transform target)
        {
            for (int i = target.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(target.GetChild(i).gameObject);
            }
        }

        public static T LoadRequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new System.InvalidOperationException("Required UI chrome asset missing: " + path);
            }

            return asset;
        }

        public static Sprite EnsureSpriteAsset(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException("UI chrome sprite texture missing: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new System.InvalidOperationException("UI chrome sprite import failed: " + path);
            }

            return sprite;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 size, Vector2 anchor)
        {
            GameObject imageObject = CreateChild(name, parent);
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            SetRect(rect, size, anchor);
            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateImage(string name, Transform parent, RectTransformBounds bounds)
        {
            GameObject imageObject = CreateChild(name, parent);
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            ApplyBounds(rect, bounds);
            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void ApplyChrome(Image image, InGameUiChromeFunction function, InGameUiChromeAssets assets)
        {
            image.color = ResolveColor(function);
            image.sprite = ResolveSprite(function, assets);
            image.type = image.sprite == null ? Image.Type.Simple : ResolveImageType(function);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static Image.Type ResolveImageType(InGameUiChromeFunction function)
        {
            switch (function)
            {
                case InGameUiChromeFunction.Glow:
                    return Image.Type.Simple;
                default:
                    return Image.Type.Sliced;
            }
        }

        private static Sprite ResolveSprite(InGameUiChromeFunction function, InGameUiChromeAssets assets)
        {
            switch (function)
            {
                case InGameUiChromeFunction.HudBackground:
                    return assets.PopupBackground;
                case InGameUiChromeFunction.HudHeader:
                    return assets.PopupHeader;
                case InGameUiChromeFunction.HudContent:
                case InGameUiChromeFunction.SecondaryButton:
                    return assets.SkillButtonDark;
                case InGameUiChromeFunction.StatusPanel:
                case InGameUiChromeFunction.PrimaryButton:
                    return assets.SkillButtonBlue;
                case InGameUiChromeFunction.Divider:
                    return assets.Divider;
                case InGameUiChromeFunction.Glow:
                    return assets.GlowCircle;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(function), function, null);
            }
        }

        private static Color ResolveColor(InGameUiChromeFunction function)
        {
            switch (function)
            {
                case InGameUiChromeFunction.HudBackground:
                    return new Color(0.025f, 0.1f, 0.22f, 0.94f);
                case InGameUiChromeFunction.HudHeader:
                    return new Color(0.1f, 0.57f, 1f, 0.96f);
                case InGameUiChromeFunction.HudContent:
                    return new Color(0.015f, 0.055f, 0.13f, 0.88f);
                case InGameUiChromeFunction.StatusPanel:
                    return new Color(0.04f, 0.2f, 0.42f, 0.92f);
                case InGameUiChromeFunction.PrimaryButton:
                    return new Color(0.1f, 0.57f, 1f, 1f);
                case InGameUiChromeFunction.SecondaryButton:
                    return new Color(0.08f, 0.18f, 0.3f, 0.92f);
                case InGameUiChromeFunction.Divider:
                    return new Color(0.42f, 0.9f, 1f, 0.38f);
                case InGameUiChromeFunction.Glow:
                    return new Color(0.16f, 0.72f, 1f, 0.1f);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(function), function, null);
            }
        }
    }

    internal readonly struct RectTransformBounds
    {
        public RectTransformBounds(Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            AnchorMin = anchorMin;
            AnchorMax = anchorMax;
            SizeDelta = sizeDelta;
            AnchoredPosition = anchoredPosition;
        }

        public Vector2 AnchorMin { get; }
        public Vector2 AnchorMax { get; }
        public Vector2 SizeDelta { get; }
        public Vector2 AnchoredPosition { get; }
    }
}
