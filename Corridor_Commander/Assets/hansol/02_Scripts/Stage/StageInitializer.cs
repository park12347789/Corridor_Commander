using UnityEngine;
using CorridorCommander.PlayerControl;
using CorridorCommander.PlayerItems;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class StageInitializer : MonoBehaviour
    {
        [SerializeField] private StageDefinitionSO stageDefinition;
        [SerializeField] private StageRuntime runtime;
        [SerializeField] private StageLayoutRoot layoutRoot;
        [SerializeField] private bool applyOnAwake = true;
        [SerializeField] private bool restartWaveDirector = true;

        public StageDefinitionSO StageDefinition => stageDefinition;
        public StageRuntime Runtime => runtime;
        public StageLayoutRoot LayoutRoot => layoutRoot;

        private void Awake()
        {
            ResolveMissingReferences();

            if (applyOnAwake)
            {
                ApplyStage();
            }
        }

        public void ApplyStage()
        {
            ResolveMissingReferences();

            if (stageDefinition == null)
            {
                Debug.LogWarning("StageInitializer has no StageDefinition.", this);
                return;
            }

            if (layoutRoot != null)
            {
                layoutRoot.CollectChildren();
                layoutRoot.ApplyDefinition(stageDefinition);
            }

            if (runtime != null)
            {
                runtime.ResolveMissingReferences();

                if (runtime.GameManager != null && layoutRoot != null && layoutRoot.MainTarget != null)
                {
                    runtime.GameManager.SetMainTarget(layoutRoot.MainTarget);
                }

                if (runtime.WaveDirector != null)
                {
                    runtime.WaveDirector.ConfigureStage(stageDefinition, restartWaveDirector);
                }

                ConfigureRewardServices();
            }
        }

        private void ConfigureRewardServices()
        {
            if (runtime == null || layoutRoot == null)
            {
                return;
            }

            PlayerCurrencyWallet currencyWallet = layoutRoot.GetComponentInChildren<PlayerCurrencyWallet>(true);
            PlayerItemInventory itemInventory = layoutRoot.GetComponentInChildren<PlayerItemInventory>(true);
            PlayerLevelProgression levelProgression = layoutRoot.GetComponentInChildren<PlayerLevelProgression>(true);
            runtime.ConfigureRewardServices(currencyWallet, itemInventory, levelProgression);
        }

        private void ResolveMissingReferences()
        {
            if (runtime == null)
            {
                runtime = FindFirstObjectByType<StageRuntime>(FindObjectsInactive.Include);
            }

            if (layoutRoot == null)
            {
                layoutRoot = FindFirstObjectByType<StageLayoutRoot>(FindObjectsInactive.Include);
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ResolveMissingReferences();
            }
        }
    }
}
