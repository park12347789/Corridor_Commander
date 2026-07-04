using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class MapExpansionDoorOpenActivator : MonoBehaviour
    {
        [SerializeField] private MapExpansionDoorOpener doorOpener;
        [SerializeField] private MapExpansionActivationTargetGroup activationTargetGroup;

        public bool IsActivated { get; private set; }

        private void Awake()
        {
            if (doorOpener == null)
            {
                doorOpener = GetComponentInChildren<MapExpansionDoorOpener>(true);
            }

            if (activationTargetGroup == null)
            {
                activationTargetGroup = GetComponent<MapExpansionActivationTargetGroup>();
            }
        }

        private void OnEnable()
        {
            SubscribeDoorOpened();
            TryActivate();
        }

        private void OnDisable()
        {
            UnsubscribeDoorOpened();
        }

        private void Update()
        {
            TryActivate();
        }

        public void TryActivate()
        {
            if (IsActivated || doorOpener == null || !doorOpener.IsOpen)
            {
                return;
            }

            IsActivated = true;
            activationTargetGroup?.Activate();
            EnemySpawnManager.Instance?.TryApplyDoorRule(doorOpener);
        }

        private void HandleDoorOpened(MapExpansionDoorOpener openedDoor)
        {
            if (openedDoor == doorOpener)
            {
                TryActivate();
            }
        }

        private void SubscribeDoorOpened()
        {
            if (doorOpener == null)
            {
                return;
            }

            doorOpener.Opened -= HandleDoorOpened;
            doorOpener.Opened += HandleDoorOpened;
        }

        private void UnsubscribeDoorOpened()
        {
            if (doorOpener != null)
            {
                doorOpener.Opened -= HandleDoorOpened;
            }
        }
    }
}
