using System;
using UnityEngine;
using UnityEngine.Events;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerStaminaController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStatModifier statModifier;

        [Header("Stamina")]
        [SerializeField] private float baseMaxStamina = 100f;
        [SerializeField] private float runDrainPerSecond = 9f;
        [SerializeField] private float jumpStaminaCost = 10f;
        [SerializeField] private float regenPerSecond = 25f;
        [SerializeField] private float regenDelayAfterUse = 0.6f;
        [SerializeField] private float minimumStaminaToStartRun = 1f;

        [Header("Events")]
        [SerializeField] private UnityEvent<float> staminaChanged;
        [SerializeField] private UnityEvent<float> maxStaminaChanged;

        private float currentStamina;
        private float lastUseTime;

        public float CurrentStamina => currentStamina;
        public float MaxStamina => Mathf.Max(1f, baseMaxStamina + GetStaminaBonus());
        public bool CanRun => currentStamina >= Mathf.Max(0f, minimumStaminaToStartRun);
        public bool CanJump => currentStamina >= Mathf.Max(0f, jumpStaminaCost);

        public event Action<float> StaminaChanged;
        public event Action<float> MaxStaminaChanged;

        private void Awake()
        {
            ResolveReferences();
            currentStamina = MaxStamina;
            NotifyMaxStaminaChanged();
            NotifyStaminaChanged();
        }

        private void OnEnable()
        {
            if (statModifier != null)
            {
                statModifier.StatsChanged += HandleStatsChanged;
            }
        }

        private void OnDisable()
        {
            if (statModifier != null)
            {
                statModifier.StatsChanged -= HandleStatsChanged;
            }
        }

        public void TickStamina(bool isRunning)
        {
            if (isRunning)
            {
                Drain(runDrainPerSecond * Time.deltaTime);
                return;
            }

            if (Time.time < lastUseTime + regenDelayAfterUse)
            {
                return;
            }

            Restore(regenPerSecond * Time.deltaTime);
        }

        public bool TryConsumeJumpStamina()
        {
            float cost = Mathf.Max(0f, jumpStaminaCost);

            if (currentStamina < cost)
            {
                Debug.Log("[PlayerStaminaController] Not Enough Stamina For Jump.");
                return false;
            }

            Drain(cost);
            return true;
        }

        public void Restore(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            SetCurrentStamina(Mathf.Min(MaxStamina, currentStamina + amount));
        }

        private void Drain(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            lastUseTime = Time.time;
            SetCurrentStamina(Mathf.Max(0f, currentStamina - amount));
        }

        private void HandleStatsChanged()
        {
            currentStamina = Mathf.Min(currentStamina, MaxStamina);
            NotifyMaxStaminaChanged();
            NotifyStaminaChanged();
        }

        private float GetStaminaBonus()
        {
            return statModifier != null ? statModifier.MaxStaminaBonus : 0f;
        }

        private void SetCurrentStamina(float value)
        {
            float nextValue = Mathf.Clamp(value, 0f, MaxStamina);

            if (Mathf.Approximately(currentStamina, nextValue))
            {
                return;
            }

            currentStamina = nextValue;
            NotifyStaminaChanged();
        }

        private void NotifyStaminaChanged()
        {
            StaminaChanged?.Invoke(currentStamina);
            staminaChanged?.Invoke(currentStamina);
        }

        private void NotifyMaxStaminaChanged()
        {
            MaxStaminaChanged?.Invoke(MaxStamina);
            maxStaminaChanged?.Invoke(MaxStamina);
        }

        private void ResolveReferences()
        {
            if (statModifier == null)
            {
                statModifier = GetComponent<PlayerStatModifier>();
            }

            if (statModifier == null)
            {
                statModifier = GetComponentInParent<PlayerStatModifier>();
            }

            if (statModifier == null)
            {
                statModifier = GetComponentInChildren<PlayerStatModifier>(true);
            }
        }
    }
}

/*
Unity setup:
1. Add PlayerStaminaController to the player root or PlayerSystems object.
2. Connect PlayerStatModifier, or leave it empty for auto-binding.
3. PlayerLocomotionController should reference this component for run and jump costs.
4. UI can subscribe to StaminaChanged and MaxStaminaChanged to draw a stamina bar.
*/
