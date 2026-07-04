using UnityEngine;

namespace CorridorCommander.PlayerItems
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GrenadeProjectile : MonoBehaviour
    {
        [Header("Explosion")]
        [SerializeField] private GameObject explosionVfxPrefab;
        [SerializeField] private bool explodeOnImpact = false;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        [Header("Physics")]
        [SerializeField] private bool configurePhysicsOnAwake = true;
        [SerializeField] private bool disableNonConvexMeshColliders = true;
        [SerializeField] private bool ignoreOwnerCollisionsOnLaunch = true;
        [SerializeField] private CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        [SerializeField] private RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate;

        [Header("Debug Radius")]
        [SerializeField] private bool showDebugExplosionRadius = true;
        [SerializeField] private bool scaleVfxToExplosionRadius = true;
        [SerializeField] private float debugRadiusDuration = 1.25f;
        [SerializeField] private Color debugRadiusColor = new Color(1f, 0.35f, 0.05f, 0.35f);

        private Rigidbody body;
        private ItemDefinitionSO itemDefinition;
        private GameObject owner;
        private float explodeAt;
        private float customGravity;
        private bool hasExploded;
        private bool useCustomGravity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();

            if (configurePhysicsOnAwake)
            {
                ConfigurePhysics();
            }
        }

        private void Update()
        {
            if (!hasExploded && Time.time >= explodeAt)
            {
                Explode();
            }
        }

        private void FixedUpdate()
        {
            if (!useCustomGravity || body == null || body.isKinematic)
            {
                return;
            }

            body.linearVelocity += Vector3.up * customGravity * Time.fixedDeltaTime;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!explodeOnImpact || hasExploded)
            {
                return;
            }

            if (IsOwnerOrOwnerChild(collision.transform))
            {
                return;
            }

            Explode();
        }

        public void Launch(
            ItemDefinitionSO definition,
            GameObject source,
            Vector3 velocity,
            float? overrideGravity = null)
        {
            itemDefinition = definition;
            owner = source;
            hasExploded = false;
            explodeAt = Time.time + Mathf.Max(0.05f, definition != null ? definition.fuseTime : 1f);
            customGravity = overrideGravity.HasValue ? overrideGravity.Value : Physics.gravity.y;
            useCustomGravity = overrideGravity.HasValue && !Mathf.Approximately(customGravity, Physics.gravity.y);

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            body.collisionDetectionMode = collisionDetectionMode;
            body.interpolation = interpolation;
            body.useGravity = !useCustomGravity;
            body.isKinematic = false;
            body.WakeUp();
            body.linearVelocity = velocity;

            if (ignoreOwnerCollisionsOnLaunch)
            {
                IgnoreOwnerCollisions();
            }
        }

        private void IgnoreOwnerCollisions()
        {
            if (owner == null)
            {
                return;
            }

            Collider[] grenadeColliders = GetComponentsInChildren<Collider>(true);
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < grenadeColliders.Length; i++)
            {
                Collider grenadeCollider = grenadeColliders[i];
                if (grenadeCollider == null)
                {
                    continue;
                }

                for (int j = 0; j < ownerColliders.Length; j++)
                {
                    Collider ownerCollider = ownerColliders[j];
                    if (ownerCollider == null)
                    {
                        continue;
                    }

                    Physics.IgnoreCollision(grenadeCollider, ownerCollider, true);
                }
            }
        }

        private void ConfigurePhysics()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            body.collisionDetectionMode = collisionDetectionMode;
            body.interpolation = interpolation;
            body.useGravity = true;
            body.isKinematic = false;

            if (disableNonConvexMeshColliders)
            {
                DisableInvalidMeshColliders();
            }

            EnsureSolidCollider();
        }

        private void DisableInvalidMeshColliders()
        {
            MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>(true);

            for (int i = 0; i < meshColliders.Length; i++)
            {
                MeshCollider meshCollider = meshColliders[i];

                if (meshCollider == null || meshCollider.convex)
                {
                    continue;
                }

                meshCollider.enabled = false;
            }
        }

        private void EnsureSolidCollider()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];

                if (candidate != null && candidate.enabled && !candidate.isTrigger)
                {
                    return;
                }
            }

            Debug.LogError(
                "[GrenadeProjectile] Grenade prefab has no solid runtime collider. Add a SphereCollider or Convex MeshCollider to the prefab.",
                this);
        }

        private void Explode()
        {
            if (hasExploded)
            {
                return;
            }

            hasExploded = true;

            if (itemDefinition != null)
            {
                ApplyDamage();
            }

            SpawnExplosionVfx();
            ItemAudioUtility.PlayExplosionAudio(itemDefinition, transform.position);
            Debug.Log("[GrenadeProjectile] Grenade exploded.", this);
            Destroy(gameObject);
        }

        private void ApplyDamage()
        {
            float radius = Mathf.Max(0f, itemDefinition.radius);
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                radius,
                itemDefinition.effectLayers,
                triggerInteraction);

            int damagedCount = 0;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];

                if (targetCollider == null)
                {
                    continue;
                }

                if (IsOwnerOrOwnerChild(targetCollider.transform))
                {
                    continue;
                }

                IDamageable damageable = targetCollider.GetComponentInParent<IDamageable>();

                if (damageable == null)
                {
                    continue;
                }

                damageable.TakeDamage(new DamageInfo(
                    itemDefinition.value,
                    owner != null ? owner : gameObject,
                    targetCollider.ClosestPoint(transform.position)));
                damagedCount++;
            }

            Debug.Log(
                $"[GrenadeProjectile] Explosion scan. Radius: {radius}, Colliders: {colliders.Length}, Damaged: {damagedCount}",
                this);
        }

        private void SpawnExplosionVfx()
        {
            float radius = itemDefinition != null ? Mathf.Max(0f, itemDefinition.radius) : 0f;

            if (explosionVfxPrefab != null)
            {
                GameObject vfx = Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);

                if (scaleVfxToExplosionRadius && radius > 0f)
                {
                    vfx.transform.localScale = Vector3.one * radius * 2f;
                }

                Destroy(vfx, Mathf.Max(0.1f, debugRadiusDuration));
                return;
            }

            if (!showDebugExplosionRadius || radius <= 0f)
            {
                return;
            }

            GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugSphere.name = "Grenade_DebugExplosionRadius";
            debugSphere.transform.position = transform.position;
            debugSphere.transform.localScale = Vector3.one * radius * 2f;

            Collider debugCollider = debugSphere.GetComponent<Collider>();
            if (debugCollider != null)
            {
                Destroy(debugCollider);
            }

            Renderer renderer = debugSphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = debugRadiusColor;
                renderer.material = material;
            }

            Destroy(debugSphere, Mathf.Max(0.1f, debugRadiusDuration));
        }

        private bool IsOwnerOrOwnerChild(Transform target)
        {
            if (owner == null || target == null)
            {
                return false;
            }

            return target == owner.transform
                || target.IsChildOf(owner.transform)
                || owner.transform.IsChildOf(target);
        }
    }
}

/*
Unity setup outline:
1. Add GrenadeProjectile to the grenade prefab.
2. Add a Rigidbody and Collider to the same prefab.
3. Assign Explosion Vfx Prefab if visual feedback is needed.
4. Connect this prefab to ItemDefinitionSO.projectilePrefab.
5. Enable Show Debug Explosion Radius to preview the damage radius during play mode.
6. Keep Ignore Owner Collisions On Launch enabled to prevent instant self-collision explosions.
*/
