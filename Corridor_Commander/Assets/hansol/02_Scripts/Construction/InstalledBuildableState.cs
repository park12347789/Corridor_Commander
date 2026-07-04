using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class InstalledBuildableState : MonoBehaviour
    {
        [SerializeField] private Health health;

        private BuildContext installContext;
        private bool hasInstallContext;
        private bool placementReleased;

        public bool HasInstallContext => hasInstallContext;
        public BuildContext InstallContext => installContext;

        private void Awake()
        {
            ResolveReferences();
            SubscribeToHealth();
        }

        private void OnDestroy()
        {
            ReleasePlacement();

            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        public void Initialize(BuildContext context)
        {
            installContext = context;
            hasInstallContext = context.PlacementPoint != null;
            placementReleased = false;

            ResolveReferences();
            SubscribeToHealth();
        }

        public bool ReleasePlacement()
        {
            if (placementReleased || !hasInstallContext || installContext.PlacementPoint == null)
            {
                return false;
            }

            placementReleased = true;
            return installContext.PlacementPoint.ReleasePlacedObject(gameObject);
        }

        public bool TransferPlacement(GameObject replacement)
        {
            if (placementReleased
                || replacement == null
                || !hasInstallContext
                || installContext.PlacementPoint == null)
            {
                return false;
            }

            if (!installContext.PlacementPoint.ReplacePlacedObject(gameObject, replacement))
            {
                return false;
            }

            placementReleased = true;
            return true;
        }

        private void ResolveReferences()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        private void SubscribeToHealth()
        {
            if (health == null)
            {
                return;
            }

            health.Died -= HandleDied;
            health.Died += HandleDied;
        }

        private void HandleDied(Health deadHealth)
        {
            ReleasePlacement();
        }
    }
}
