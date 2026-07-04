using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [Serializable]
    public sealed class WaveSpawnPhase
    {
        [SerializeField, Min(0f)] private float delay;
        [SerializeField] private string announcementText;
        [SerializeField] private List<WaveSpawnRule> spawnRules = new List<WaveSpawnRule>();

        public float Delay => delay;
        public string AnnouncementText => announcementText;
        public IReadOnlyList<WaveSpawnRule> SpawnRules => spawnRules;

        public WaveSpawnPhase()
        {
        }

        public WaveSpawnPhase(float delay, string announcementText, List<WaveSpawnRule> spawnRules)
        {
            this.delay = Mathf.Max(0f, delay);
            this.announcementText = announcementText;
            this.spawnRules = spawnRules ?? new List<WaveSpawnRule>();
        }
    }
}
