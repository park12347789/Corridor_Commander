using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class MortarShowcaseAutoFire : MonoBehaviour
    {
        [SerializeField] private MortarSkillRole mortarRole;
        [SerializeField] private Transform targetPoint;
        [SerializeField, Min(0.1f)] private float fireInterval = 2.4f;
        [SerializeField, Min(0f)] private float phaseOffset;

        private float nextFireTime;

        private void Reset()
        {
            mortarRole = GetComponent<MortarSkillRole>();
        }

        private void Awake()
        {
            if (mortarRole == null)
            {
                mortarRole = GetComponent<MortarSkillRole>();
            }
        }

        private void OnEnable()
        {
            nextFireTime = Time.time + phaseOffset;
        }

        private void Update()
        {
            if (mortarRole == null || targetPoint == null || Time.time < nextFireTime)
            {
                return;
            }

            mortarRole.TryUseSkill(new SkillUseContext(gameObject, targetPoint.position, null));
            nextFireTime = Time.time + fireInterval;
        }
    }
}
