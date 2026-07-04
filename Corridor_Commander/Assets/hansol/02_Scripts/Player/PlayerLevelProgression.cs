using System;
using UnityEngine;
using UnityEngine.Events;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class PlayerLevelProgression : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] private int startingLevel = 1;
        [SerializeField] private int startingStatPoints = 0;

        [Header("Kill Progress")]
        [SerializeField] private int baseKillsForLevelUp = 10;
        [SerializeField] private int killsRequiredIncreasePerLevel = 1;

        [Header("Events")]
        [SerializeField] private UnityEvent<int> levelChanged;
        [SerializeField] private UnityEvent<int> statPointsChanged;
        [SerializeField] private UnityEvent<int> killProgressChanged;
        [SerializeField] private UnityEvent<int> leveledUp;

        private int currentLevel;
        private int currentStatPoints;
        private int currentKillProgress;

        public int CurrentLevel => currentLevel;
        public int CurrentStatPoints => currentStatPoints;
        public int CurrentKillProgress => currentKillProgress;
        public int RequiredKillsForNextLevel => CalculateRequiredKills(currentLevel);

        public event Action<int> LevelChanged;
        public event Action<int> StatPointsChanged;
        public event Action<int> KillProgressChanged;
        public event Action<int> LeveledUp;

        private void Awake()
        {
            currentLevel = Mathf.Max(1, startingLevel);
            currentStatPoints = Mathf.Max(0, startingStatPoints);
            currentKillProgress = 0;

            NotifyLevelChanged();
            NotifyStatPointsChanged();
            NotifyKillProgressChanged();
        }

        public void AddKillProgress(int killCount)
        {
            if (killCount <= 0)
            {
                return;
            }

            currentKillProgress += killCount;
            Debug.Log($"[PlayerLevelProgression] Kill Progress Added: +{killCount}, Current: {currentKillProgress}/{RequiredKillsForNextLevel}");

            ProcessLevelUps();
            NotifyKillProgressChanged();
        }

        public bool TrySpendStatPoint(int amount)
        {
            if (amount <= 0 || currentStatPoints < amount)
            {
                return false;
            }

            currentStatPoints -= amount;
            Debug.Log($"[PlayerLevelProgression] Stat Point Spent: -{amount}, Current: {currentStatPoints}");
            NotifyStatPointsChanged();

            return true;
        }

        public void AddStatPoints(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentStatPoints += amount;
            Debug.Log($"[PlayerLevelProgression] Stat Point Added: +{amount}, Current: {currentStatPoints}");
            NotifyStatPointsChanged();
        }

        private void ProcessLevelUps()
        {
            while (currentKillProgress >= RequiredKillsForNextLevel)
            {
                int requiredKills = RequiredKillsForNextLevel;
                currentKillProgress -= requiredKills;
                currentLevel++;
                currentStatPoints++;

                Debug.Log($"[PlayerLevelProgression] Level Up: Level {currentLevel}, Stat Points: {currentStatPoints}");

                LeveledUp?.Invoke(currentLevel);
                leveledUp?.Invoke(currentLevel);
                NotifyLevelChanged();
                NotifyStatPointsChanged();
            }
        }

        private int CalculateRequiredKills(int level)
        {
            int safeBaseKills = Mathf.Max(1, baseKillsForLevelUp);
            int safeIncrease = Mathf.Max(0, killsRequiredIncreasePerLevel);
            int safeLevel = Mathf.Max(1, level);

            return safeBaseKills + ((safeLevel - 1) * safeIncrease);
        }

        private void NotifyLevelChanged()
        {
            LevelChanged?.Invoke(currentLevel);
            levelChanged?.Invoke(currentLevel);
        }

        private void NotifyStatPointsChanged()
        {
            StatPointsChanged?.Invoke(currentStatPoints);
            statPointsChanged?.Invoke(currentStatPoints);
        }

        private void NotifyKillProgressChanged()
        {
            KillProgressChanged?.Invoke(currentKillProgress);
            killProgressChanged?.Invoke(currentKillProgress);
        }
    }
}

/*
Unity setup:
1. Add PlayerLevelProgression to the player root or PlayerSystems object.
2. Set Base Kills For Level Up. Example: 10 starts at 10 kills.
3. Set Kills Required Increase Per Level. Example: 1 makes 10, 11, 12. Example: 2 makes 10, 12, 14.
4. Stat shop systems should call TrySpendStatPoint() before applying stat upgrades.
*/
