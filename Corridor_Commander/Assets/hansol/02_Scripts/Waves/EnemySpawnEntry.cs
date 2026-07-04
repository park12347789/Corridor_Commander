using System;
using UnityEngine;

namespace CorridorCommander
{
    [Serializable]
    public sealed class EnemySpawnEntry
    {
        [SerializeField] private EnemyDefinitionSO enemy;
        [SerializeField, Min(0f)] private float weight = 1f;

        public EnemyDefinitionSO Enemy => enemy;
        public float Weight => weight;

        public EnemySpawnEntry()
        {
        }

        public EnemySpawnEntry(EnemyDefinitionSO enemy, float weight)
        {
            this.enemy = enemy;
            this.weight = Mathf.Max(0f, weight);
        }
    }
}
