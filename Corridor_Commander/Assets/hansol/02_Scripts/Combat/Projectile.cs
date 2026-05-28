using UnityEngine;

namespace CorridorCommander
{
    [RequireComponent(typeof(Collider))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float lifetime = 3f;

        private Vector3 direction = Vector3.forward;
        private GameObject source;
        private float spawnTime;

        private void Awake()
        {
            Collider projectileCollider = GetComponent<Collider>();
            projectileCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            spawnTime = Time.time;
        }

        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;

            if (Time.time - spawnTime >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        public void Launch(Vector3 launchDirection, float launchDamage, GameObject launchSource)
        {
            if (launchDirection.sqrMagnitude > 0.0001f)
            {
                direction = launchDirection.normalized;
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            damage = launchDamage;
            source = launchSource;
            spawnTime = Time.time;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (source != null && other.transform.root == source.transform.root)
            {
                return;
            }

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                return;
            }

            damageable.TakeDamage(new DamageInfo(damage, source, transform.position));
            Destroy(gameObject);
        }
    }
}
