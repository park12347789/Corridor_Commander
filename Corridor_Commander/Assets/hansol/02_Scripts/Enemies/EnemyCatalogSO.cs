using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Enemies/Enemy Catalog",
        fileName = "EnemyCatalog")]
    public sealed class EnemyCatalogSO : ScriptableObject
    {
        [SerializeField] private EnemyDefinitionSO[] enemies;

        public IReadOnlyList<EnemyDefinitionSO> Enemies => enemies;

        public void CollectUnlocked(
            int waveIndex,
            EnemyRank maxRank,
            List<EnemySpawnEntry> results)
        {
            if (results == null || enemies == null)
            {
                return;
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyDefinitionSO enemy = enemies[i];
                if (enemy == null || !enemy.IsUnlocked(waveIndex) || enemy.Rank > maxRank)
                {
                    continue;
                }

                results.Add(new EnemySpawnEntry(enemy, enemy.BaseWeight));
            }
        }
    }
}
