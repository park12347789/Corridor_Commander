using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class TutorialFloatingGuideArrow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.6f, 0f);
        [SerializeField] private float bobHeight = 0.35f;
        [SerializeField] private float bobSpeed = 2.8f;
        [SerializeField] private float rotateSpeed = 65f;

        public Transform Target => target;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 position = target.position + worldOffset;
            position.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = position;
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        public void SetTarget(Transform nextTarget)
        {
            target = nextTarget;
            gameObject.SetActive(target != null);
        }
    }
}
