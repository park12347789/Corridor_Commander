using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class InstalledSkillRegistry : MonoBehaviour
    {
        private static InstalledSkillRegistry instance;
        private readonly Dictionary<SkillDefinitionSO, List<ISkillProvider>> providersBySkill =
            new Dictionary<SkillDefinitionSO, List<ISkillProvider>>();
        private readonly List<SkillDefinitionSO> skillsInSlotOrder = new List<SkillDefinitionSO>();

        public static InstalledSkillRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<InstalledSkillRegistry>(FindObjectsInactive.Include);
                }

                if (instance == null)
                {
                    GameObject registryObject = new GameObject(nameof(InstalledSkillRegistry));
                    instance = registryObject.AddComponent<InstalledSkillRegistry>();
                }

                return instance;
            }
        }

        public static InstalledSkillRegistry Current => instance;

        public event Action Changed;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void Register(ISkillProvider provider)
        {
            if (provider == null || provider.SkillDefinition == null)
            {
                return;
            }

            SkillDefinitionSO skill = provider.SkillDefinition;
            if (!providersBySkill.TryGetValue(skill, out List<ISkillProvider> providers))
            {
                providers = new List<ISkillProvider>();
                providersBySkill.Add(skill, providers);
            }

            if (!providers.Contains(provider))
            {
                providers.Add(provider);
            }

            if (!skillsInSlotOrder.Contains(skill))
            {
                skillsInSlotOrder.Add(skill);
                Changed?.Invoke();
            }
        }

        public void Unregister(ISkillProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            SkillDefinitionSO skill = provider.SkillDefinition;
            if (skill == null || !providersBySkill.TryGetValue(skill, out List<ISkillProvider> providers))
            {
                return;
            }

            bool changed = providers.Remove(provider);
            if (changed)
            {
                if (providers.Count == 0)
                {
                    providersBySkill.Remove(skill);
                    skillsInSlotOrder.Remove(skill);
                }
            }
            if (changed)
            {
                Changed?.Invoke();
            }
        }

        public ISkillProvider GetSlotProvider(int slotNumber)
        {
            RemoveInvalidProviders();

            SkillDefinitionSO skill = GetSlotSkill(slotNumber);
            if (skill == null || !providersBySkill.TryGetValue(skill, out List<ISkillProvider> providers))
            {
                return null;
            }

            for (int i = 0; i < providers.Count; i++)
            {
                if (IsProviderValid(providers[i]))
                {
                    return providers[i];
                }
            }

            return null;
        }

        public SkillDefinitionSO GetSlotSkill(int slotNumber)
        {
            RemoveInvalidProviders();

            int index = slotNumber - 1;
            return index >= 0 && index < skillsInSlotOrder.Count
                ? skillsInSlotOrder[index]
                : null;
        }

        public void SetSkillOrder(IReadOnlyList<SkillDefinitionSO> orderedSkills)
        {
            RemoveInvalidProviders();

            if (orderedSkills == null || orderedSkills.Count == 0)
            {
                return;
            }

            List<SkillDefinitionSO> nextOrder = new List<SkillDefinitionSO>(skillsInSlotOrder.Count);
            for (int i = 0; i < orderedSkills.Count; i++)
            {
                SkillDefinitionSO skill = orderedSkills[i];
                if (skill != null
                    && providersBySkill.ContainsKey(skill)
                    && !nextOrder.Contains(skill))
                {
                    nextOrder.Add(skill);
                }
            }

            for (int i = 0; i < skillsInSlotOrder.Count; i++)
            {
                SkillDefinitionSO skill = skillsInSlotOrder[i];
                if (skill != null && !nextOrder.Contains(skill))
                {
                    nextOrder.Add(skill);
                }
            }

            skillsInSlotOrder.Clear();
            skillsInSlotOrder.AddRange(nextOrder);
            Changed?.Invoke();
        }

        public bool TryUseSlot(int slotNumber, SkillUseContext context)
        {
            SkillDefinitionSO skill = GetSlotSkill(slotNumber);
            if (skill == null)
            {
                return false;
            }

            return TryUseSkill(skill, context);
        }

        public int GetTotalCount(SkillDefinitionSO skill)
        {
            RemoveInvalidProviders();

            return skill != null && providersBySkill.TryGetValue(skill, out List<ISkillProvider> providers)
                ? providers.Count
                : 0;
        }

        public int GetReadyCount(SkillDefinitionSO skill)
        {
            RemoveInvalidProviders();

            if (skill == null || !providersBySkill.TryGetValue(skill, out List<ISkillProvider> providers))
            {
                return 0;
            }

            int readyCount = 0;
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i] != null && providers[i].IsReady)
                {
                    readyCount++;
                }
            }

            return readyCount;
        }

        public int GetSlotTotalCount(int slotNumber)
        {
            return GetTotalCount(GetSlotSkill(slotNumber));
        }

        public int GetSlotReadyCount(int slotNumber)
        {
            return GetReadyCount(GetSlotSkill(slotNumber));
        }

        public bool TryUseSkill(SkillDefinitionSO skill, SkillUseContext context)
        {
            RemoveInvalidProviders();

            if (skill == null || !providersBySkill.TryGetValue(skill, out List<ISkillProvider> providers))
            {
                return false;
            }

            for (int i = 0; i < providers.Count; i++)
            {
                ISkillProvider provider = providers[i];
                if (provider == null || !provider.IsReady)
                {
                    continue;
                }

                if (provider.TryUseSkill(context))
                {
                    Changed?.Invoke();
                    return true;
                }
            }

            return false;
        }

        private void RemoveInvalidProviders()
        {
            bool changed = false;

            List<SkillDefinitionSO> emptySkills = null;
            foreach (KeyValuePair<SkillDefinitionSO, List<ISkillProvider>> pair in providersBySkill)
            {
                changed |= RemoveInvalidProvidersFromList(pair.Value);
                if (pair.Value.Count == 0)
                {
                    if (emptySkills == null)
                    {
                        emptySkills = new List<SkillDefinitionSO>();
                    }

                    emptySkills.Add(pair.Key);
                }
            }

            if (emptySkills != null)
            {
                for (int i = 0; i < emptySkills.Count; i++)
                {
                    providersBySkill.Remove(emptySkills[i]);
                    changed |= skillsInSlotOrder.Remove(emptySkills[i]);
                }
            }

            if (changed)
            {
                Changed?.Invoke();
            }
        }

        private static bool RemoveInvalidProvidersFromList(List<ISkillProvider> providers)
        {
            bool changed = false;
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                if (IsProviderValid(providers[i]))
                {
                    continue;
                }

                providers.RemoveAt(i);
                changed = true;
            }

            return changed;
        }

        private static bool IsProviderValid(ISkillProvider provider)
        {
            if (provider == null || provider.SkillDefinition == null)
            {
                return false;
            }

            UnityEngine.Object unityObject = provider as UnityEngine.Object;
            return (object)unityObject == null || unityObject != null;
        }
    }
}
