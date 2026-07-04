using CorridorCommander.PlayerControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander.PlayerUI
{
    [DisallowMultipleComponent]
    public sealed class PlayerSquadListPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSquadRoster roster;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private PlayerSquadSlotView[] slotViews = new PlayerSquadSlotView[PlayerSquadRoster.MaxMemberCount];

        [Header("Text")]
        [SerializeField] private string title = "SQUAD";
        [SerializeField] private string allSelectedSuffix = "  [ALL]";

        [Header("Colors")]
        [SerializeField] private Color slotColor = new Color(0.08f, 0.13f, 0.18f, 0.94f);
        [SerializeField] private Color selectedColor = new Color(0.12f, 0.48f, 0.85f, 1f);

        private PlayerSquadRoster subscribedRoster;
        private Image panelImage;

        private void Awake()
        {
            ResolveUiReferences();
            ResolveRoster();
        }

        private void OnEnable()
        {
            ResolveUiReferences();
            ResolveRoster();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (roster == null)
            {
                ResolveRoster();
                Subscribe();
            }

            Refresh();
        }

        private void Subscribe()
        {
            if (roster == null || subscribedRoster == roster)
            {
                return;
            }

            Unsubscribe();
            subscribedRoster = roster;
            subscribedRoster.RosterChanged += Refresh;
            subscribedRoster.SelectionChanged += Refresh;
        }

        private void Unsubscribe()
        {
            if (subscribedRoster == null)
            {
                return;
            }

            subscribedRoster.RosterChanged -= Refresh;
            subscribedRoster.SelectionChanged -= Refresh;
            subscribedRoster = null;
        }

        private void Refresh()
        {
            ResolveUiReferences();

            if (panelRoot == null)
            {
                return;
            }

            if (roster == null || roster.MemberCount <= 0)
            {
                ClearSlots();
                return;
            }

            SetPanelVisible(true);

            if (titleText != null)
            {
                titleText.text = roster.IsAllSelected ? title + allSelectedSuffix : title;
            }

            int count = Mathf.Min(slotViews.Length, PlayerSquadRoster.MaxMemberCount);
            for (int i = 0; i < count; i++)
            {
                slotViews[i]?.Refresh(
                    i + 1,
                    roster.GetMemberAt(i),
                    roster.GetHealthAt(i),
                    roster.IsSlotSelected(i),
                    slotColor,
                    selectedColor);
            }
        }

        private void ClearSlots()
        {
            SetPanelVisible(true);

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (slotViews == null)
            {
                return;
            }

            for (int i = 0; i < slotViews.Length; i++)
            {
                slotViews[i]?.Clear();
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelImage != null)
            {
                panelImage.enabled = false;
                panelImage.raycastTarget = false;
            }

            if (titleText != null)
            {
                titleText.enabled = visible;
            }
        }

        private void ResolveRoster()
        {
            if (roster == null)
            {
                roster = PlayerSquadRoster.Instance;
            }

            if (roster == null)
            {
                roster = FindFirstObjectByType<PlayerSquadRoster>(FindObjectsInactive.Include);
            }
        }

        private void ResolveUiReferences()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (panelImage == null && panelRoot != null)
            {
                panelImage = panelRoot.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.enabled = false;
                    panelImage.raycastTarget = false;
                }
            }

            if (titleText == null)
            {
                Transform titleTransform = FindChildRecursive(transform, "Title");
                if (titleTransform != null)
                {
                    titleText = titleTransform.GetComponent<TMP_Text>();
                }
            }

            if (slotViews == null || slotViews.Length != PlayerSquadRoster.MaxMemberCount)
            {
                slotViews = new PlayerSquadSlotView[PlayerSquadRoster.MaxMemberCount];
            }

            Transform slotsContainer = FindChildRecursive(transform, "SlotsContainer");
            if (slotsContainer == null)
            {
                return;
            }

            for (int i = 0; i < slotViews.Length; i++)
            {
                string slotName = $"SquadSlot_F{i + 1}";
                Transform slotTransform = FindChildRecursive(slotsContainer, slotName);
                if (slotTransform != null)
                {
                    PlayerSquadSlotView foundView = slotTransform.GetComponent<PlayerSquadSlotView>();
                    if (foundView == null)
                    {
                        foundView = slotTransform.gameObject.AddComponent<PlayerSquadSlotView>();
                    }

                    if (slotViews[i] != foundView)
                    {
                        slotViews[i] = foundView;
                    }
                }
            }
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
