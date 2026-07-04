using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class MapExpansionActivationTargetGroup : MonoBehaviour
    {
        [SerializeField] private GameObject[] activationTargets;
        [SerializeField] private GameObject[] deactivationTargets;
        [SerializeField] private bool deactivateTargetsOnAwake = true;

        public bool IsActive { get; private set; }

        private void Awake()
        {
            if (deactivateTargetsOnAwake)
            {
                Deactivate();
            }
        }

        public void Activate()
        {
            IsActive = true;
            SetTargetsActive(deactivationTargets, false);
            SetTargetsActive(activationTargets, true);
        }

        public void Deactivate()
        {
            IsActive = false;
            SetTargetsActive(activationTargets, false);
        }

        private void SetTargetsActive(GameObject[] targets, bool active)
        {
            if (targets == null)
            {
                return;
            }

            foreach (GameObject target in targets)
            {
                if (target != null)
                {
                    target.SetActive(active);
                }
            }
        }
    }
}
