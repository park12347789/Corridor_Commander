using UnityEngine;

namespace CorridorCommander.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class EnemyRangedProjectile : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField] private LayerMask collisionLayers = ~(1 << 7);
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.12f;

        [Header("Fallback Visual")]
        [SerializeField] private bool createFallbackVisual = true;
        [SerializeField] private Color fallbackColor = new Color(0.65f, 0.95f, 0.25f, 1f);
        [SerializeField, Min(0.01f)] private float fallbackVisualScale = 0.22f;
        [SerializeField] private bool createFallbackTrail = true;
        [SerializeField, Min(0.01f)] private float fallbackTrailWidth = 0.12f;
        [SerializeField, Min(0.02f)] private float fallbackTrailLifetime = 0.22f;
        [SerializeField] private bool createImpactFlash = true;
        [SerializeField, Min(0.02f)] private float impactFlashScale = 0.42f;
        [SerializeField, Min(0.02f)] private float impactFlashLifetime = 0.18f;

        private GameObject activeVisual;
        private TrailRenderer fallbackTrail;
        private Vector3 velocity;
        private GameObject source;
        private float damage;
        private float gravity;
        private float expirationTime;
        private bool launched;

        public void Launch(
            Vector3 launchVelocity,
            float launchDamage,
            float gravityAcceleration,
            float lifetime,
            GameObject launchSource)
        {
            velocity = launchVelocity;
            damage = Mathf.Max(0f, launchDamage);
            gravity = gravityAcceleration;
            expirationTime = Time.time + Mathf.Max(0.1f, lifetime);
            source = launchSource;
            launched = true;

            if (velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            }
        }

        public void ConfigureVisual(GameObject visualPrefab, Vector3 visualScale)
        {
            if (visualPrefab == null)
            {
                return;
            }

            if (activeVisual != null)
            {
                Destroy(activeVisual);
            }

            activeVisual = Instantiate(visualPrefab, transform);
            activeVisual.name = "ProjectileVisual";
            activeVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            activeVisual.transform.localScale = new Vector3(
                Mathf.Max(0.01f, Mathf.Abs(visualScale.x)),
                Mathf.Max(0.01f, Mathf.Abs(visualScale.y)),
                Mathf.Max(0.01f, Mathf.Abs(visualScale.z)));

            DisableVisualColliders(activeVisual);
        }

        private void Awake()
        {
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = collisionRadius;

            if (createFallbackVisual && GetComponentInChildren<Renderer>(true) == null)
            {
                CreateFallbackVisual();
            }

            if (createFallbackTrail && fallbackTrail == null)
            {
                CreateFallbackTrail();
            }
        }

        private void Update()
        {
            if (!launched)
            {
                return;
            }

            if (Time.time >= expirationTime)
            {
                Destroy(gameObject);
                return;
            }

            float deltaTime = Time.deltaTime;
            velocity += Vector3.up * gravity * deltaTime;
            Vector3 displacement = velocity * deltaTime;
            float distance = displacement.magnitude;

            if (distance > 0f && Physics.SphereCast(
                    transform.position,
                    collisionRadius,
                    displacement.normalized,
                    out RaycastHit hit,
                    distance,
                    collisionLayers,
                    QueryTriggerInteraction.Collide))
            {
                HandleHit(hit);
                return;
            }

            transform.position += displacement;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            }
        }

        private void HandleHit(RaycastHit hit)
        {
            if (source != null && hit.transform.root == source.transform.root)
            {
                transform.position = hit.point + velocity.normalized * collisionRadius;
                return;
            }

            Health targetHealth = ResolveHealth(hit.collider);
            bool isEnemy = HasComponentInHitRoot<EnemyMovementController>(hit.collider)
                || HasComponentInHitRoot<EnemyRangedAttackController>(hit.collider);
            bool isTurret = HasComponentInHitRoot<TurretTargetingController>(hit.collider);

            if (targetHealth != null && targetHealth.IsAlive && !isEnemy && !isTurret)
            {
                targetHealth.TakeDamage(new DamageInfo(damage, source, hit.point));
            }

            if (createImpactFlash)
            {
                CreateImpactFlash(hit.point);
            }

            Destroy(gameObject);
        }

        private static Health ResolveHealth(Collider targetCollider)
        {
            if (targetCollider == null)
            {
                return null;
            }

            Health resolvedHealth = targetCollider.GetComponentInParent<Health>();
            if (resolvedHealth != null)
            {
                return resolvedHealth;
            }

            resolvedHealth = targetCollider.GetComponentInChildren<Health>(true);
            if (resolvedHealth != null)
            {
                return resolvedHealth;
            }

            Transform root = targetCollider.transform.root;
            return root != null ? root.GetComponentInChildren<Health>(true) : null;
        }

        private static bool HasComponentInHitRoot<TComponent>(Collider targetCollider)
            where TComponent : Component
        {
            if (targetCollider == null)
            {
                return false;
            }

            if (targetCollider.GetComponentInParent<TComponent>() != null)
            {
                return true;
            }

            Transform root = targetCollider.transform.root;
            return root != null && root.GetComponentInChildren<TComponent>(true) != null;
        }

        private void CreateFallbackVisual()
        {
            activeVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            activeVisual.name = "ProjectileVisual";
            activeVisual.transform.SetParent(transform, false);
            activeVisual.transform.localScale = Vector3.one * fallbackVisualScale;

            Collider visualCollider = activeVisual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            Renderer renderer = activeVisual.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (renderer != null && shader != null)
            {
                Material material = new Material(shader);
                material.color = fallbackColor;
                renderer.material = material;
            }
        }

        private void CreateFallbackTrail()
        {
            fallbackTrail = gameObject.AddComponent<TrailRenderer>();
            fallbackTrail.time = fallbackTrailLifetime;
            fallbackTrail.startWidth = fallbackTrailWidth;
            fallbackTrail.endWidth = 0f;
            fallbackTrail.minVertexDistance = 0.02f;
            fallbackTrail.alignment = LineAlignment.View;
            fallbackTrail.numCornerVertices = 2;
            fallbackTrail.numCapVertices = 2;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = fallbackColor;
                fallbackTrail.material = material;
            }
        }

        private void CreateImpactFlash(Vector3 position)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "EnemyRangedProjectile_ImpactFlash";
            flash.transform.position = position;
            flash.transform.localScale = Vector3.one * impactFlashScale;

            Collider flashCollider = flash.GetComponent<Collider>();
            if (flashCollider != null)
            {
                Destroy(flashCollider);
            }

            Renderer renderer = flash.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (renderer != null && shader != null)
            {
                Material material = new Material(shader);
                material.color = new Color(fallbackColor.r, fallbackColor.g, fallbackColor.b, 0.78f);
                renderer.material = material;
            }

            Destroy(flash, impactFlashLifetime);
        }

        private static void DisableVisualColliders(GameObject visualRoot)
        {
            Collider[] colliders = visualRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            Rigidbody[] rigidbodies = visualRoot.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].isKinematic = true;
                rigidbodies[i].detectCollisions = false;
            }
        }
    }
}

/*
Unity setup:
1. Add this component and a trigger SphereCollider to an enemy projectile prefab.
2. Keep the projectile on a layer included by Collision Layers.
3. EnemyRangedAttackController launches and configures it at runtime.
*/
