using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CorridorCommander.PlayerControl;

namespace CorridorCommander.PlayerUI
{
    public sealed class PlayerHUDPresenter : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject statInfoPanel;

        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private PlayerStaminaController staminaController;
        [SerializeField] private PlayerLevelProgression levelProgression;
        [SerializeField] private PlayerCurrencyWallet currencyWallet;
        [SerializeField] private PlayerStatModifier statModifier;

        [Header("Texts")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text staminaText;
        [SerializeField] private TMP_Text levelProgressText;
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text statPointText;
        [SerializeField] private TMP_Text healthUpgradeLevelText;
        [SerializeField] private TMP_Text damageUpgradeLevelText;
        [SerializeField] private TMP_Text moveSpeedUpgradeLevelText;
        [SerializeField] private TMP_Text staminaUpgradeLevelText;

        [Header("Fill Images")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image staminaFillImage;
        [SerializeField] private Image levelProgressFillImage;

        [Header("Input")]
        [SerializeField] private Key toggleStatPanelKey = Key.U;

        [Header("Auto Binding")]
        [SerializeField] private bool autoBindNamedHudElements = true;

        private float lastHealthCurrent = -1f;
        private float lastHealthMax = -1f;
        private float lastStaminaCurrent = -1f;
        private float lastStaminaMax = -1f;
        private int lastLevel = -1;
        private int lastKillProgress = -1;
        private int lastRequiredKills = -1;

        private void Awake()
        {
            ResolveReferences();
            AutoBindHudElements();

            if (statInfoPanel != null)
            {
                statInfoPanel.SetActive(false);
            }

            RefreshAll();
        }

        private void OnEnable()
        {
            if (levelProgression != null)
            {
                levelProgression.StatPointsChanged += HandleStatPointsChanged;
                levelProgression.LevelChanged += HandleLevelChanged;
                levelProgression.KillProgressChanged += HandleKillProgressChanged;
            }

            if (currencyWallet != null)
            {
                currencyWallet.MoneyChanged += HandleMoneyChanged;
            }

            if (statModifier != null)
            {
                statModifier.StatsChanged += HandleStatsChanged;
            }

            if (staminaController != null)
            {
                staminaController.StaminaChanged += HandleStaminaChanged;
                staminaController.MaxStaminaChanged += HandleMaxStaminaChanged;
            }
        }

        private void OnDisable()
        {
            if (levelProgression != null)
            {
                levelProgression.StatPointsChanged -= HandleStatPointsChanged;
                levelProgression.LevelChanged -= HandleLevelChanged;
                levelProgression.KillProgressChanged -= HandleKillProgressChanged;
            }

            if (currencyWallet != null)
            {
                currencyWallet.MoneyChanged -= HandleMoneyChanged;
            }

            if (statModifier != null)
            {
                statModifier.StatsChanged -= HandleStatsChanged;
            }

            if (staminaController != null)
            {
                staminaController.StaminaChanged -= HandleStaminaChanged;
                staminaController.MaxStaminaChanged -= HandleMaxStaminaChanged;
            }
        }

        private void Update()
        {
            RefreshRuntimeRowsIfChanged();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard[toggleStatPanelKey].wasPressedThisFrame)
            {
                ToggleStatInfoPanel();
            }
        }

        public void ToggleStatInfoPanel()
        {
            if (statInfoPanel == null)
            {
                return;
            }

            statInfoPanel.SetActive(!statInfoPanel.activeSelf);
            RefreshAll();
        }

        private void HandleMoneyChanged(int money)
        {
            RefreshMoney();
        }

        private void HandleStatPointsChanged(int statPoints)
        {
            RefreshStatPoints();
        }

        private void HandleLevelChanged(int level)
        {
            RefreshLevelProgress();
            RefreshStatPoints();
        }

        private void HandleKillProgressChanged(int killProgress)
        {
            RefreshLevelProgress();
        }

        private void HandleStatsChanged()
        {
            RefreshStatLevels();
            RefreshHealth();
            RefreshStamina();
        }

        private void HandleStaminaChanged(float stamina)
        {
            RefreshStamina();
        }

        private void HandleMaxStaminaChanged(float maxStamina)
        {
            RefreshStamina();
        }

        private void RefreshAll()
        {
            RefreshHealth();
            RefreshStamina();
            RefreshLevelProgress();
            RefreshMoney();
            RefreshStatPoints();
            RefreshStatLevels();
        }

        private void RefreshRuntimeRowsIfChanged()
        {
            if (health != null
                && (!Mathf.Approximately(lastHealthCurrent, health.CurrentHitPoints)
                    || !Mathf.Approximately(lastHealthMax, health.MaxHitPoints)))
            {
                RefreshHealth();
            }

            if (staminaController != null
                && (!Mathf.Approximately(lastStaminaCurrent, staminaController.CurrentStamina)
                    || !Mathf.Approximately(lastStaminaMax, staminaController.MaxStamina)))
            {
                RefreshStamina();
            }

            if (levelProgression != null
                && (lastLevel != levelProgression.CurrentLevel
                    || lastKillProgress != levelProgression.CurrentKillProgress
                    || lastRequiredKills != levelProgression.RequiredKillsForNextLevel))
            {
                RefreshLevelProgress();
            }
        }

        private void RefreshHealth()
        {
            if (health == null)
            {
                return;
            }

            lastHealthCurrent = health.CurrentHitPoints;
            lastHealthMax = health.MaxHitPoints;

            if (healthText != null)
            {
                healthText.text = $"HP: {Mathf.CeilToInt(health.CurrentHitPoints)}/{Mathf.CeilToInt(health.MaxHitPoints)}";
            }

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = health.MaxHitPoints > 0f
                    ? Mathf.Clamp01(health.CurrentHitPoints / health.MaxHitPoints)
                    : 0f;
            }
        }

        private void RefreshStamina()
        {
            if (staminaController == null)
            {
                return;
            }

            lastStaminaCurrent = staminaController.CurrentStamina;
            lastStaminaMax = staminaController.MaxStamina;

            if (staminaText != null)
            {
                staminaText.text = $"Stamina: {Mathf.CeilToInt(staminaController.CurrentStamina)}/{Mathf.CeilToInt(staminaController.MaxStamina)}";
            }

            if (staminaFillImage != null)
            {
                staminaFillImage.fillAmount = staminaController.MaxStamina > 0f
                    ? Mathf.Clamp01(staminaController.CurrentStamina / staminaController.MaxStamina)
                    : 0f;
            }
        }

        private void RefreshLevelProgress()
        {
            if (levelProgression == null)
            {
                return;
            }

            lastLevel = levelProgression.CurrentLevel;
            lastKillProgress = levelProgression.CurrentKillProgress;
            lastRequiredKills = levelProgression.RequiredKillsForNextLevel;

            if (levelProgressText != null)
            {
                levelProgressText.text =
                    $"Lv {levelProgression.CurrentLevel} EXP: {levelProgression.CurrentKillProgress}/{levelProgression.RequiredKillsForNextLevel}";
            }

            if (levelProgressFillImage != null)
            {
                levelProgressFillImage.fillAmount = levelProgression.RequiredKillsForNextLevel > 0
                    ? Mathf.Clamp01(levelProgression.CurrentKillProgress / (float)levelProgression.RequiredKillsForNextLevel)
                    : 0f;
            }
        }

        private void RefreshMoney()
        {
            if (moneyText != null && currencyWallet != null)
            {
                moneyText.text = "Money: " + currencyWallet.CurrentMoney.ToString();
            }
        }

        private void RefreshStatPoints()
        {
            if (statPointText != null && levelProgression != null)
            {
                int points = levelProgression.CurrentStatPoints;
                statPointText.text = points > 0
                    ? "Stat Points: +" + points.ToString()
                    : "Stat Points: 0";
            }
        }

        private void RefreshStatLevels()
        {
            if (statModifier == null)
            {
                return;
            }

            if (healthUpgradeLevelText != null)
            {
                healthUpgradeLevelText.text = "Lv " + statModifier.HealthUpgradeLevel.ToString();
            }

            if (damageUpgradeLevelText != null)
            {
                damageUpgradeLevelText.text = "Lv " + statModifier.DamageUpgradeLevel.ToString();
            }

            if (moveSpeedUpgradeLevelText != null)
            {
                moveSpeedUpgradeLevelText.text = "Lv " + statModifier.MoveSpeedUpgradeLevel.ToString();
            }

            if (staminaUpgradeLevelText != null)
            {
                staminaUpgradeLevelText.text = "Lv " + statModifier.StaminaUpgradeLevel.ToString();
            }
        }

        private void ResolveReferences()
        {
            if (health == null)
            {
                PlayerHealthController healthController =
                    FindFirstObjectByType<PlayerHealthController>(FindObjectsInactive.Include);

                if (healthController != null)
                {
                    health = healthController.Health;
                }
            }

            if (health == null)
            {
                health = FindFirstObjectByType<Health>(FindObjectsInactive.Include);
            }

            if (staminaController == null)
            {
                staminaController = FindFirstObjectByType<PlayerStaminaController>(FindObjectsInactive.Include);
            }

            if (levelProgression == null)
            {
                levelProgression = FindFirstObjectByType<PlayerLevelProgression>(FindObjectsInactive.Include);
            }

            if (currencyWallet == null)
            {
                currencyWallet = FindFirstObjectByType<PlayerCurrencyWallet>(FindObjectsInactive.Include);
            }

            if (statModifier == null)
            {
                statModifier = FindFirstObjectByType<PlayerStatModifier>(FindObjectsInactive.Include);
            }
        }

        private void AutoBindHudElements()
        {
            if (!autoBindNamedHudElements)
            {
                return;
            }

            if (healthText == null)
            {
                healthText = FindTextInNamedChild("HealthRow");
            }

            if (staminaText == null)
            {
                staminaText = FindTextInNamedChild("StaminaRow");
            }

            if (levelProgressText == null)
            {
                levelProgressText = FindTextInNamedChild("LevelProgressRow");
            }

            if (moneyText == null)
            {
                moneyText = FindTextInNamedChild("MoneyText");
            }

            if (statPointText == null)
            {
                statPointText = FindTextInNamedChild("StatPointText");
            }

            if (healthUpgradeLevelText == null)
            {
                healthUpgradeLevelText = FindTextInNamedChild("HealthUpgradeLevelText");
            }

            if (damageUpgradeLevelText == null)
            {
                damageUpgradeLevelText = FindTextInNamedChild("DamageUpgradeLevelText");
            }

            if (moveSpeedUpgradeLevelText == null)
            {
                moveSpeedUpgradeLevelText = FindTextInNamedChild("MoveSpeedUpgradeLevelText");
            }

            if (staminaUpgradeLevelText == null)
            {
                staminaUpgradeLevelText = FindTextInNamedChild("StaminaUpgradeLevelText");
            }

            if (healthFillImage == null)
            {
                healthFillImage = FindFillImageInNamedChild("HealthRow");
            }

            if (staminaFillImage == null)
            {
                staminaFillImage = FindFillImageInNamedChild("StaminaRow");
            }

            if (levelProgressFillImage == null)
            {
                levelProgressFillImage = FindFillImageInNamedChild("LevelProgressRow");
            }
        }

        private TMP_Text FindTextInNamedChild(string childName)
        {
            Transform child = FindChildByName(transform, childName);

            if (child == null)
            {
                return null;
            }

            TMP_Text directText = child.GetComponent<TMP_Text>();
            return directText != null ? directText : child.GetComponentInChildren<TMP_Text>(true);
        }

        private Image FindFillImageInNamedChild(string childName)
        {
            Transform child = FindChildByName(transform, childName);

            if (child == null)
            {
                return null;
            }

            Image[] images = child.GetComponentsInChildren<Image>(true);

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];

                if (image == null)
                {
                    continue;
                }

                if (image.type == Image.Type.Filled || image.name.Contains("Fill"))
                {
                    return image;
                }
            }

            return null;
        }

        private Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                Transform result = FindChildByName(child, childName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
