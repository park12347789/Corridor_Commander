using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CorridorCommander.PlayerControl;
using System.Collections.Generic;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class PlayerRuntimeHudBinding : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private Health health;
        [SerializeField] private PlayerStaminaController staminaController;
        [SerializeField] private PlayerLevelProgression levelProgression;
        [SerializeField] private PlayerCurrencyWallet currencyWallet;

        [Header("Status Bars")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private TMP_Text staminaText;
        [SerializeField] private Slider experienceSlider;
        [SerializeField] private TMP_Text experienceText;

        [Header("Commodity")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text[] levelTexts;
        [SerializeField] private TMP_Text levelPointText;
        [SerializeField] private TMP_Text moneyText;

        private void Awake()
        {
            ResolveLevelTextReferences();
            WarnIfMissingReferences();
            ConfigureSliders();
            RefreshAll();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += HandleHealthChanged;
                health.Died += HandleHealthDied;
            }

            if (staminaController != null)
            {
                staminaController.StaminaChanged += HandleStaminaChanged;
                staminaController.MaxStaminaChanged += HandleMaxStaminaChanged;
            }

            if (levelProgression != null)
            {
                levelProgression.LevelChanged += HandleLevelChanged;
                levelProgression.KillProgressChanged += HandleKillProgressChanged;
                levelProgression.StatPointsChanged += HandleStatPointsChanged;
            }

            if (currencyWallet != null)
            {
                currencyWallet.MoneyChanged += HandleMoneyChanged;
            }

            RefreshAll();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= HandleHealthChanged;
                health.Died -= HandleHealthDied;
            }

            if (staminaController != null)
            {
                staminaController.StaminaChanged -= HandleStaminaChanged;
                staminaController.MaxStaminaChanged -= HandleMaxStaminaChanged;
            }

            if (levelProgression != null)
            {
                levelProgression.LevelChanged -= HandleLevelChanged;
                levelProgression.KillProgressChanged -= HandleKillProgressChanged;
                levelProgression.StatPointsChanged -= HandleStatPointsChanged;
            }

            if (currencyWallet != null)
            {
                currencyWallet.MoneyChanged -= HandleMoneyChanged;
            }
        }

        private void LateUpdate()
        {
            RefreshAll();
        }

        private void ConfigureSliders()
        {
            ConfigureNormalizedSlider(healthSlider);
            ConfigureNormalizedSlider(staminaSlider);
            ConfigureNormalizedSlider(experienceSlider);
        }

        private void ResolveLevelTextReferences()
        {
            if (levelTexts != null && levelTexts.Length > 0)
            {
                return;
            }

            TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
            List<TMP_Text> badgeTexts = new List<TMP_Text>(2);
            for (int i = 0; i < allTexts.Length; i++)
            {
                TMP_Text candidate = allTexts[i];
                if (candidate == null || candidate.transform.parent == null)
                {
                    continue;
                }

                if (candidate.transform.parent.name == "Slider_Level02_Icon_Badge_Blue")
                {
                    badgeTexts.Add(candidate);
                }
            }

            if (badgeTexts.Count > 0)
            {
                levelTexts = badgeTexts.ToArray();
            }
        }

        private void ConfigureNormalizedSlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.interactable = false;
        }

        private void RefreshAll()
        {
            RefreshHealth();
            RefreshStamina();
            RefreshLevel();
            RefreshMoney();
        }

        private void RefreshHealth()
        {
            if (health == null)
            {
                return;
            }

            float current = Mathf.Max(0f, health.CurrentHitPoints);
            float max = Mathf.Max(1f, health.MaxHitPoints);

            if (healthSlider != null)
            {
                healthSlider.value = Mathf.Clamp01(current / max);
            }

            if (healthText != null)
            {
                healthText.SetText("{0}/{1}", Mathf.CeilToInt(current), Mathf.CeilToInt(max));
            }
        }

        private void RefreshStamina()
        {
            if (staminaController == null)
            {
                return;
            }

            float current = Mathf.Max(0f, staminaController.CurrentStamina);
            float max = Mathf.Max(1f, staminaController.MaxStamina);

            if (staminaSlider != null)
            {
                staminaSlider.value = Mathf.Clamp01(current / max);
            }

            if (staminaText != null)
            {
                staminaText.SetText("{0}/{1}", Mathf.CeilToInt(current), Mathf.CeilToInt(max));
            }
        }

        private void RefreshLevel()
        {
            if (levelProgression == null)
            {
                return;
            }

            int current = Mathf.Max(0, levelProgression.CurrentKillProgress);
            int required = Mathf.Max(1, levelProgression.RequiredKillsForNextLevel);

            if (experienceSlider != null)
            {
                experienceSlider.value = Mathf.Clamp01(current / (float)required);
            }

            if (experienceText != null)
            {
                experienceText.SetText("{0}/{1}", current, required);
            }

            if (levelText != null)
            {
                levelText.SetText("{0}", levelProgression.CurrentLevel);
            }

            if (levelTexts != null)
            {
                for (int i = 0; i < levelTexts.Length; i++)
                {
                    if (levelTexts[i] != null)
                    {
                        levelTexts[i].SetText("{0}", levelProgression.CurrentLevel);
                    }
                }
            }

            if (levelPointText != null)
            {
                levelPointText.SetText("{0}", levelProgression.CurrentStatPoints);
            }
        }

        private void RefreshMoney()
        {
            if (currencyWallet == null || moneyText == null)
            {
                return;
            }

            moneyText.SetText("{0}", currencyWallet.CurrentMoney);
        }

        private void HandleHealthChanged(Health changedHealth, float damageAmount)
        {
            RefreshHealth();
        }

        private void HandleHealthDied(Health deadHealth)
        {
            RefreshHealth();
        }

        private void HandleStaminaChanged(float stamina)
        {
            RefreshStamina();
        }

        private void HandleMaxStaminaChanged(float maxStamina)
        {
            RefreshStamina();
        }

        private void HandleLevelChanged(int level)
        {
            RefreshLevel();
        }

        private void HandleKillProgressChanged(int killProgress)
        {
            RefreshLevel();
        }

        private void HandleStatPointsChanged(int statPoints)
        {
            RefreshLevel();
        }

        private void HandleMoneyChanged(int money)
        {
            RefreshMoney();
        }

        private void WarnIfMissingReferences()
        {
            if (health == null)
            {
                Debug.LogWarning("[PlayerRuntimeHudBinding] Health is not assigned.", this);
            }

            if (staminaController == null)
            {
                Debug.LogWarning("[PlayerRuntimeHudBinding] PlayerStaminaController is not assigned.", this);
            }

            if (levelProgression == null)
            {
                Debug.LogWarning("[PlayerRuntimeHudBinding] PlayerLevelProgression is not assigned.", this);
            }

            if (currencyWallet == null)
            {
                Debug.LogWarning("[PlayerRuntimeHudBinding] PlayerCurrencyWallet is not assigned.", this);
            }

            if (healthSlider == null || healthText == null)
            {
                Debug.LogWarning("[PlayerRuntimeHudBinding] Health UI references are not fully assigned.", this);
            }

            if (staminaSlider == null || staminaText == null)
            {
                Debug.LogWarning("[PlayerRuntimeHudBinding] Stamina UI references are not fully assigned.", this);
            }

            if (experienceSlider == null || experienceText == null)
            {
                Debug.LogWarning("[PlayerRuntimeHudBinding] Experience UI references are not fully assigned.", this);
            }
        }
    }
}
