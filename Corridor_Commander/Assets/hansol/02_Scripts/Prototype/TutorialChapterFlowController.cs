using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerControl;
using CorridorCommander.PlayerItems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TutorialChapterFlowController : MonoBehaviour
    {
        [SerializeField] private TutorialChapterSelectPresenter chapterSelectPresenter;
        [SerializeField] private TutorialChapterCompletionPresenter chapterCompletionPresenter;
        [SerializeField] private TutorialDialogueStepController dialogueStepController;
        [SerializeField] private TutorialFloatingGuideArrow guideArrow;
        [SerializeField] private Transform player;
        [SerializeField] private CharacterController playerCharacterController;
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerCurrencyWallet playerWallet;
        [SerializeField] private PlayerWeaponInventory weaponInventory;
        [SerializeField] private PlayerAmmoInventory ammoInventory;
        [SerializeField] private PlayerItemInventory itemInventory;
        [SerializeField] private PlacementPoint[] placementPoints;
        [SerializeField] private TutorialChapterDefinition[] chapters;
        [SerializeField] private GameObject[] startupActiveObjects;
        [SerializeField] private bool showSelectorOnStart = true;
        [SerializeField] private string lobbySceneName = "StartMenu";

        private int activeChapterIndex = -1;

        private void Awake()
        {
            BindPresenter();
        }

        private void Start()
        {
            ApplyObjectState(startupActiveObjects, true);

            if (showSelectorOnStart)
            {
                ShowChapterSelect();
            }
        }

        private void OnEnable()
        {
            BindPresenter();
        }

        private void OnDisable()
        {
            if (chapterSelectPresenter != null)
            {
                chapterSelectPresenter.ChapterSelected -= HandleChapterSelected;
            }

            if (dialogueStepController != null)
            {
                dialogueStepController.ChapterCompleted -= HandleChapterCompleted;
            }

            if (chapterCompletionPresenter != null)
            {
                chapterCompletionPresenter.NextChapterRequested -= HandleNextChapterRequested;
                chapterCompletionPresenter.ChapterSelectRequested -= HandleChapterSelectRequested;
                chapterCompletionPresenter.LobbyRequested -= HandleLobbyRequested;
            }
        }

        public void ShowChapterSelect()
        {
            if (chapterSelectPresenter == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Chapter select presenter is not assigned.", this);
                return;
            }

            if (chapters == null || chapters.Length == 0)
            {
                Debug.LogError("[TutorialChapterFlowController] Chapters are not configured.", this);
                return;
            }

            chapterCompletionPresenter?.Hide();
            dialogueStepController?.HideDialogue();
            chapterSelectPresenter.Show(chapters);
        }

        public void StartChapter(string chapterId)
        {
            int index = FindChapterIndex(chapterId);
            if (index < 0)
            {
                Debug.LogError("[TutorialChapterFlowController] Missing chapter id: " + chapterId, this);
                return;
            }

            StartChapter(index);
        }

        private void HandleChapterSelected(int chapterIndex)
        {
            StartChapter(chapterIndex);
        }

        private void StartChapter(int chapterIndex)
        {
            if (chapters == null || chapterIndex < 0 || chapterIndex >= chapters.Length)
            {
                Debug.LogError("[TutorialChapterFlowController] Invalid chapter index: " + chapterIndex, this);
                return;
            }

            TutorialChapterDefinition chapter = chapters[chapterIndex];
            if (chapter == null || string.IsNullOrWhiteSpace(chapter.ChapterId))
            {
                Debug.LogError("[TutorialChapterFlowController] Chapter definition is invalid.", this);
                return;
            }

            activeChapterIndex = chapterIndex;
            chapterCompletionPresenter?.Hide();
            chapterSelectPresenter?.Hide();
            ApplyChapterState(chapter);
            dialogueStepController?.StartChapter(chapter.ChapterId);
            guideArrow?.SetTarget(chapter.GuideTarget);
        }

        private void HandleChapterCompleted(string chapterId)
        {
            int completedIndex = FindChapterIndex(chapterId);
            if (completedIndex < 0)
            {
                Debug.LogError("[TutorialChapterFlowController] Completed unknown chapter id: " + chapterId, this);
                return;
            }

            activeChapterIndex = completedIndex;
            dialogueStepController?.HideDialogue();

            if (chapterCompletionPresenter == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Chapter completion presenter is not assigned.", this);
                return;
            }

            TutorialChapterDefinition nextChapter = GetChapterAt(completedIndex + 1);
            chapterCompletionPresenter.Show(chapters[completedIndex], nextChapter);
        }

        private void HandleNextChapterRequested()
        {
            int nextIndex = activeChapterIndex + 1;
            if (nextIndex < 0 || nextIndex >= GetChapterCount())
            {
                Debug.LogError("[TutorialChapterFlowController] Next chapter is not available.", this);
                return;
            }

            StartChapter(nextIndex);
        }

        private void HandleChapterSelectRequested()
        {
            ShowChapterSelect();
        }

        private void HandleLobbyRequested()
        {
            if (string.IsNullOrWhiteSpace(lobbySceneName))
            {
                Debug.LogError("[TutorialChapterFlowController] Lobby scene name is empty.", this);
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(lobbySceneName);
        }

        private void ApplyChapterState(TutorialChapterDefinition chapter)
        {
            ApplyObjectState(chapter.DeactivateObjects, false);
            ApplyObjectState(chapter.ActivateObjects, true);
            WarpPlayer(chapter.StartPoint);
            GrantMoney(chapter.MinimumMoney);
            GrantWeapons(chapter.WeaponGrants);
            GrantAmmo(chapter.AmmoGrants);
            GrantItems(chapter.ItemGrants);
            RestorePlayerHealth(chapter.RestorePlayerHealth);

            if (chapter.ClearPlacementPoints)
            {
                ClearPlacementPoints();
            }
        }

        private void WarpPlayer(Transform startPoint)
        {
            if (startPoint == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Chapter start point is not assigned.", this);
                return;
            }

            if (player == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Player transform is not assigned.", this);
                return;
            }

            if (playerCharacterController != null)
            {
                playerCharacterController.enabled = false;
            }

            player.SetPositionAndRotation(startPoint.position, startPoint.rotation);

            if (playerCharacterController != null)
            {
                playerCharacterController.enabled = true;
            }
        }

        private void GrantMoney(int minimumMoney)
        {
            if (minimumMoney <= 0)
            {
                return;
            }

            if (playerWallet == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Player wallet is not assigned.", this);
                return;
            }

            int missingMoney = minimumMoney - playerWallet.CurrentMoney;
            if (missingMoney > 0)
            {
                playerWallet.AddMoney(missingMoney);
            }
        }

        private void GrantWeapons(TutorialWeaponGrant[] grants)
        {
            if (grants == null || grants.Length == 0)
            {
                return;
            }

            if (weaponInventory == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Weapon inventory is not assigned.", this);
                return;
            }

            for (int i = 0; i < grants.Length; i++)
            {
                TutorialWeaponGrant grant = grants[i];
                if (grant == null || grant.WeaponDefinition == null)
                {
                    continue;
                }

                WeaponRuntimeState state = weaponInventory.AddWeapon(grant.WeaponDefinition, grant.FillMagazine);
                if (grant.EquipAfterGrant && state != null)
                {
                    weaponInventory.EquipWeaponAt(Mathf.Max(0, weaponInventory.WeaponCount - 1));
                }
            }
        }

        private void GrantAmmo(TutorialAmmoGrant[] grants)
        {
            if (grants == null || grants.Length == 0)
            {
                return;
            }

            if (ammoInventory == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Ammo inventory is not assigned.", this);
                return;
            }

            for (int i = 0; i < grants.Length; i++)
            {
                TutorialAmmoGrant grant = grants[i];
                if (grant != null && grant.AmmoDefinition != null)
                {
                    ammoInventory.AddAmmo(grant.AmmoDefinition, grant.Amount);
                }
            }
        }

        private void GrantItems(TutorialItemGrant[] grants)
        {
            if (grants == null || grants.Length == 0)
            {
                return;
            }

            if (itemInventory == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Item inventory is not assigned.", this);
                return;
            }

            for (int i = 0; i < grants.Length; i++)
            {
                TutorialItemGrant grant = grants[i];
                if (grant != null && grant.ItemDefinition != null)
                {
                    itemInventory.AddItem(grant.ItemDefinition, grant.Amount);
                }
            }
        }

        private void RestorePlayerHealth(bool shouldRestore)
        {
            if (!shouldRestore)
            {
                return;
            }

            if (playerHealth == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Player health is not assigned.", this);
                return;
            }

            playerHealth.RestoreToFull();
        }

        private void ClearPlacementPoints()
        {
            if (placementPoints == null)
            {
                return;
            }

            for (int i = 0; i < placementPoints.Length; i++)
            {
                PlacementPoint point = placementPoints[i];
                if (point == null || point.PlacedObject == null)
                {
                    continue;
                }

                GameObject placedObject = point.PlacedObject;
                point.ReleasePlacedObject(placedObject);
                Destroy(placedObject);
            }
        }

        private static void ApplyObjectState(GameObject[] targets, bool active)
        {
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].SetActive(active);
                }
            }
        }

        private int FindChapterIndex(string chapterId)
        {
            if (chapters == null)
            {
                return -1;
            }

            for (int i = 0; i < chapters.Length; i++)
            {
                if (chapters[i] != null && chapters[i].ChapterId == chapterId)
                {
                    return i;
                }
            }

            return -1;
        }

        private TutorialChapterDefinition GetChapterAt(int index)
        {
            return chapters != null && index >= 0 && index < chapters.Length ? chapters[index] : null;
        }

        private int GetChapterCount()
        {
            return chapters != null ? chapters.Length : 0;
        }

        private void BindPresenter()
        {
            if (chapterSelectPresenter == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Chapter select presenter is not assigned.", this);
            }
            else
            {
                chapterSelectPresenter.ChapterSelected -= HandleChapterSelected;
                chapterSelectPresenter.ChapterSelected += HandleChapterSelected;
            }

            if (dialogueStepController == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Dialogue step controller is not assigned.", this);
            }
            else
            {
                dialogueStepController.ChapterCompleted -= HandleChapterCompleted;
                dialogueStepController.ChapterCompleted += HandleChapterCompleted;
            }

            if (chapterCompletionPresenter == null)
            {
                Debug.LogError("[TutorialChapterFlowController] Chapter completion presenter is not assigned.", this);
            }
            else
            {
                chapterCompletionPresenter.NextChapterRequested -= HandleNextChapterRequested;
                chapterCompletionPresenter.NextChapterRequested += HandleNextChapterRequested;
                chapterCompletionPresenter.ChapterSelectRequested -= HandleChapterSelectRequested;
                chapterCompletionPresenter.ChapterSelectRequested += HandleChapterSelectRequested;
                chapterCompletionPresenter.LobbyRequested -= HandleLobbyRequested;
                chapterCompletionPresenter.LobbyRequested += HandleLobbyRequested;
            }
        }
    }
}
