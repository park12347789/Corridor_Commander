using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander.PlayerUI
{
    [DisallowMultipleComponent]
    public sealed class PlayerCommandRadialPresenter : MonoBehaviour
    {
        private const int CategoryCount = 3;
        private const float DeadZoneSqrMagnitude = 64f;

        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private DotweenUiPanelTransition panelTransition;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text hintText;

        [Header("Sectors")]
        [SerializeField] private Image[] sectorImages = new Image[CategoryCount];
        [SerializeField] private TMP_Text[] sectorTexts = new TMP_Text[CategoryCount];

        [Header("Labels")]
        [SerializeField] private string titleLabel = "Q 선택";
        [SerializeField] private string hintLabel = "Q 떼기 선택 / 좌클릭 호출 유지";
        [SerializeField] private string weaponsLabel = "총";
        [SerializeField] private string turretSkillsLabel = "포탑";
        [SerializeField] private string squadCommandsLabel = "분대명령";
        [SerializeField] private string activeSuffix = "\n활성";

        [Header("Style")]
        [SerializeField] private Color normalColor = new Color(0.01f, 0.08f, 0.28f, 0.38f);
        [SerializeField] private Color activeColor = new Color(0.02f, 0.18f, 0.7f, 0.56f);
        [SerializeField] private Color selectedColor = new Color(0.08f, 0.42f, 1f, 0.76f);

        [Header("Options")]
        [SerializeField] private bool hideOnAwake = true;

        private PlayerCommandCategory activeCategory;
        private PlayerCommandCategory selectedCategory;
        private Canvas parentCanvas;

        public PlayerCommandCategory SelectedCategory => selectedCategory;

        private void Awake()
        {
            ResolveReferencesIfNeeded();
            Refresh();

            if (hideOnAwake)
            {
                HideImmediate();
            }
        }

        public void Show(PlayerCommandCategory category)
        {
            ResolveReferencesIfNeeded();

            activeCategory = category;
            selectedCategory = category;

            Refresh();

            if (panelTransition != null)
            {
                panelTransition.Show();
            }
            else if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[PlayerCommandRadialPresenter] Panel Root가 연결되지 않았습니다.", this);
            }
        }

        public void Hide()
        {
            if (panelTransition != null)
            {
                panelTransition.Hide();
            }
            else if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void HideImmediate()
        {
            if (panelTransition != null)
            {
                panelTransition.HideImmediate();
            }
            else if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void SetActiveCategory(PlayerCommandCategory category)
        {
            ResolveReferencesIfNeeded();
            activeCategory = category;
            Refresh();
        }

        public void UpdateSelection(Vector2 screenPosition)
        {
            ResolveReferencesIfNeeded();

            Vector2 center = ResolvePanelScreenCenter();
            Vector2 delta = screenPosition - center;

            if (delta.sqrMagnitude < DeadZoneSqrMagnitude)
            {
                return;
            }

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }

            PlayerCommandCategory nextCategory = ResolveCategoryFromAngle(angle);
            if (selectedCategory != nextCategory)
            {
                selectedCategory = nextCategory;
                Refresh();
            }
        }

        private static PlayerCommandCategory ResolveCategoryFromAngle(float angle)
        {
            if (angle >= 330f || angle < 90f)
            {
                return PlayerCommandCategory.TurretSkills;
            }

            if (angle < 210f)
            {
                return PlayerCommandCategory.Weapons;
            }

            return PlayerCommandCategory.SquadCommands;
        }

        private void Refresh()
        {
            ResolveReferencesIfNeeded();

            if (titleText != null)
            {
                titleText.text = titleLabel;
            }

            if (hintText != null)
            {
                hintText.text = hintLabel;
            }

            RefreshCategory(PlayerCommandCategory.Weapons);
            RefreshCategory(PlayerCommandCategory.TurretSkills);
            RefreshCategory(PlayerCommandCategory.SquadCommands);
        }

        private void RefreshCategory(PlayerCommandCategory category)
        {
            int index = (int)category;
            bool isSelected = category == selectedCategory;
            bool isActive = category == activeCategory;

            if (sectorImages != null && index >= 0 && index < sectorImages.Length && sectorImages[index] != null)
            {
                sectorImages[index].gameObject.SetActive(true);
                sectorImages[index].color = isSelected
                    ? selectedColor
                    : isActive
                        ? activeColor
                        : normalColor;
                sectorImages[index].transform.localScale = isSelected
                    ? Vector3.one * 1.1f
                    : isActive
                        ? Vector3.one * 1.04f
                        : Vector3.one;

                if (isSelected)
                {
                    sectorImages[index].transform.SetAsLastSibling();
                }
            }

            if (sectorTexts != null && index >= 0 && index < sectorTexts.Length && sectorTexts[index] != null)
            {
                sectorTexts[index].text = ResolveLabel(category, isActive);
            }
        }

        private string ResolveLabel(PlayerCommandCategory category, bool isActive)
        {
            string label = category switch
            {
                PlayerCommandCategory.Weapons => weaponsLabel,
                PlayerCommandCategory.TurretSkills => turretSkillsLabel,
                PlayerCommandCategory.SquadCommands => squadCommandsLabel,
                _ => "명령"
            };

            return isActive ? label + activeSuffix : label;
        }

        private void ResolveReferencesIfNeeded()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
            }

            EnsureSectorArraySizes();
            BindSectorIfNeeded(PlayerCommandCategory.Weapons, "Weapons");
            BindSectorIfNeeded(PlayerCommandCategory.TurretSkills, "Turret");
            BindSectorIfNeeded(PlayerCommandCategory.SquadCommands, "Squad");
            BindUnassignedSectorsByChildOrder();
        }

        private void EnsureSectorArraySizes()
        {
            if (sectorImages == null || sectorImages.Length != CategoryCount)
            {
                Image[] resizedImages = new Image[CategoryCount];
                if (sectorImages != null)
                {
                    for (int i = 0; i < sectorImages.Length && i < resizedImages.Length; i++)
                    {
                        resizedImages[i] = sectorImages[i];
                    }
                }

                sectorImages = resizedImages;
            }

            if (sectorTexts == null || sectorTexts.Length != CategoryCount)
            {
                TMP_Text[] resizedTexts = new TMP_Text[CategoryCount];
                if (sectorTexts != null)
                {
                    for (int i = 0; i < sectorTexts.Length && i < resizedTexts.Length; i++)
                    {
                        resizedTexts[i] = sectorTexts[i];
                    }
                }

                sectorTexts = resizedTexts;
            }
        }

        private void BindSectorIfNeeded(PlayerCommandCategory category, string namePart)
        {
            int index = (int)category;
            if (index < 0 || index >= CategoryCount || panelRoot == null)
            {
                return;
            }

            Transform sector = null;
            if (sectorImages[index] != null)
            {
                sector = sectorImages[index].transform;
            }
            else
            {
                sector = FindChildByNamePart(panelRoot.transform, namePart);
                if (sector != null)
                {
                    sectorImages[index] = sector.GetComponent<Image>();
                }
            }

            if (sectorTexts[index] == null && sector != null)
            {
                sectorTexts[index] = sector.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void BindUnassignedSectorsByChildOrder()
        {
            if (panelRoot == null || AllSectorImagesAssigned())
            {
                return;
            }

            Image[] images = panelRoot.GetComponentsInChildren<Image>(true);
            if (images == null || images.Length == 0)
            {
                return;
            }

            int imageIndex = 0;
            for (int i = 0; i < CategoryCount && imageIndex < images.Length; i++)
            {
                if (sectorImages[i] != null)
                {
                    continue;
                }

                while (imageIndex < images.Length && images[imageIndex].gameObject == panelRoot)
                {
                    imageIndex++;
                }

                if (imageIndex >= images.Length)
                {
                    break;
                }

                sectorImages[i] = images[imageIndex];
                if (sectorTexts[i] == null)
                {
                    sectorTexts[i] = images[imageIndex].GetComponentInChildren<TMP_Text>(true);
                }

                imageIndex++;
            }
        }

        private bool AllSectorImagesAssigned()
        {
            if (sectorImages == null || sectorImages.Length < CategoryCount)
            {
                return false;
            }

            for (int i = 0; i < CategoryCount; i++)
            {
                if (sectorImages[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private Vector2 ResolvePanelScreenCenter()
        {
            if (panelRoot != null && panelRoot.transform is RectTransform rectTransform)
            {
                Camera canvasCamera = ResolveCanvasCamera();
                Vector3 worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
                return RectTransformUtility.WorldToScreenPoint(canvasCamera, worldCenter);
            }

            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        private Camera ResolveCanvasCamera()
        {
            if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
        }

        private static Transform FindChildByNamePart(Transform root, string namePart)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return child;
                }

                Transform nested = FindChildByNamePart(child, namePart);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}

