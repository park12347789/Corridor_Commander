using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    public sealed class WaveRuleResolver
    {
        private readonly List<EnemySpawnEntry> reusableEnemyEntries = new List<EnemySpawnEntry>();

        public WaveSpawnPlan BuildPlan(
            EnemyWaveDefinition wave,
            int waveIndex,
            EnemyCatalogSO enemyCatalog,
            DifficultyProgressionSO difficultyProgression,
            IReadOnlyList<RegionWaveModifierSO> regionModifiers,
            IReadOnlyList<PeriodicWaveModifierSO> periodicModifiers,
            BossScheduleSO bossSchedule,
            StageProgressState progressState)
        {
            WaveSpawnPlan plan = new WaveSpawnPlan();
            if (wave == null)
            {
                return plan;
            }

            float healthMultiplier = difficultyProgression != null
                ? difficultyProgression.GetHealthMultiplier(waveIndex)
                : 1f;
            float countMultiplier = difficultyProgression != null
                ? difficultyProgression.GetSpawnCountMultiplier(waveIndex)
                : 1f;
            float intervalMultiplier = difficultyProgression != null
                ? difficultyProgression.GetSpawnIntervalMultiplier(waveIndex)
                : 1f;

            AddWavePhases(plan, wave.GetResolvedPhases(), waveIndex, enemyCatalog, healthMultiplier, countMultiplier, intervalMultiplier);
            AddRegionModifiers(plan, regionModifiers, progressState, waveIndex, enemyCatalog, healthMultiplier, countMultiplier, intervalMultiplier);
            AddPeriodicModifiers(plan, periodicModifiers, waveIndex, enemyCatalog, healthMultiplier, countMultiplier, intervalMultiplier);
            AddBossPhase(plan, bossSchedule, waveIndex, healthMultiplier);

            return plan;
        }

        private void AddPeriodicModifiers(
            WaveSpawnPlan plan,
            IReadOnlyList<PeriodicWaveModifierSO> periodicModifiers,
            int waveIndex,
            EnemyCatalogSO enemyCatalog,
            float healthMultiplier,
            float countMultiplier,
            float intervalMultiplier)
        {
            if (periodicModifiers == null)
            {
                return;
            }

            for (int i = 0; i < periodicModifiers.Count; i++)
            {
                PeriodicWaveModifierSO modifier = periodicModifiers[i];
                if (modifier != null && modifier.AppliesTo(waveIndex))
                {
                    AddWavePhases(
                        plan,
                        modifier.ExtraPhases,
                        waveIndex,
                        enemyCatalog,
                        healthMultiplier,
                        countMultiplier,
                        intervalMultiplier,
                        modifier.GetPeriodCountBonus(waveIndex));
                }
            }
        }

        private void AddRegionModifiers(
            WaveSpawnPlan plan,
            IReadOnlyList<RegionWaveModifierSO> regionModifiers,
            StageProgressState progressState,
            int waveIndex,
            EnemyCatalogSO enemyCatalog,
            float healthMultiplier,
            float countMultiplier,
            float intervalMultiplier)
        {
            if (regionModifiers == null)
            {
                return;
            }

            for (int i = 0; i < regionModifiers.Count; i++)
            {
                RegionWaveModifierSO modifier = regionModifiers[i];
                if (modifier != null && modifier.AppliesTo(progressState, waveIndex))
                {
                    AddWavePhases(plan, modifier.ExtraPhases, waveIndex, enemyCatalog, healthMultiplier, countMultiplier, intervalMultiplier);
                }
            }
        }

        private void AddBossPhase(WaveSpawnPlan plan, BossScheduleSO bossSchedule, int waveIndex, float healthMultiplier)
        {
            if (bossSchedule == null || !bossSchedule.ShouldAddBoss(waveIndex))
            {
                return;
            }

            int spawnCount = bossSchedule.GetSpawnCount(waveIndex);
            if (spawnCount <= 0)
            {
                return;
            }

            WaveSpawnPhasePlan phase = new WaveSpawnPhasePlan(bossSchedule.PhaseDelay, "Named boss incoming");
            phase.Rules.Add(new WaveSpawnRulePlan(
                bossSchedule.SpawnGroup,
                null,
                spawnCount,
                bossSchedule.SpawnInterval,
                healthMultiplier,
                bossSchedule.BossEnemies));
            plan.AddPhase(phase);
        }

        private void AddWavePhases(
            WaveSpawnPlan plan,
            IReadOnlyList<WaveSpawnPhase> phases,
            int waveIndex,
            EnemyCatalogSO enemyCatalog,
            float healthMultiplier,
            float countMultiplier,
            float intervalMultiplier,
            int spawnCountBonus = 0)
        {
            if (phases == null)
            {
                return;
            }

            for (int i = 0; i < phases.Count; i++)
            {
                WaveSpawnPhase phase = phases[i];
                if (phase == null)
                {
                    continue;
                }

                WaveSpawnPhasePlan phasePlan = new WaveSpawnPhasePlan(phase.Delay, phase.AnnouncementText);
                IReadOnlyList<WaveSpawnRule> rules = phase.SpawnRules;
                for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
                {
                    WaveSpawnRule rule = rules[ruleIndex];
                    if (rule == null)
                    {
                        continue;
                    }

                    IReadOnlyList<EnemySpawnEntry> entries = ResolveEnemyEntries(rule, waveIndex, enemyCatalog);
                    phasePlan.Rules.Add(new WaveSpawnRulePlan(
                        rule.SpawnGroup,
                        rule.LegacySpawnerNameContains,
                        Mathf.CeilToInt(rule.SpawnCount * countMultiplier) + spawnCountBonus,
                        rule.SpawnInterval * intervalMultiplier,
                        healthMultiplier,
                        entries));
                }

                plan.AddPhase(phasePlan);
            }
        }

        private IReadOnlyList<EnemySpawnEntry> ResolveEnemyEntries(
            WaveSpawnRule rule,
            int waveIndex,
            EnemyCatalogSO enemyCatalog)
        {
            if (rule.EnemyEntries != null && rule.EnemyEntries.Count > 0)
            {
                return rule.EnemyEntries;
            }

            reusableEnemyEntries.Clear();
            enemyCatalog?.CollectUnlocked(waveIndex, EnemyRank.Grunt, reusableEnemyEntries);
            return reusableEnemyEntries;
        }
    }
}
