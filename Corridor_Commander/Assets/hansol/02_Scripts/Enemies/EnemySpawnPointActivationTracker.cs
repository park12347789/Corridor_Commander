using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemySpawner))]
    public sealed class EnemySpawnPointActivationTracker : MonoBehaviour
    {
        private EnemySpawner spawner;

        public static EnemySpawner LastActivatedSpawner { get; private set; }

        private void Awake()
        {
            spawner = GetComponent<EnemySpawner>();
        }

        private void OnEnable()
        {
            LastActivatedSpawner = spawner != null ? spawner : GetComponent<EnemySpawner>();
        }
    }
}
