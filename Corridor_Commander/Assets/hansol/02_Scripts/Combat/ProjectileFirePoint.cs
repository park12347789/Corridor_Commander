using System.Collections;
using UnityEngine;

namespace CorridorCommander
{
    public enum TurretBeamVfxStretchAxis
    {
        X,
        Y,
        Z
    }

    [DisallowMultipleComponent]
    public sealed class ProjectileFirePoint : MonoBehaviour
    {
        [SerializeField] private Transform fireOrigin;
        [SerializeField] private ParticleSystem fireEffect;
        [SerializeField] private GameObject fireVfxPrefab;
        [SerializeField] private LineRenderer beamEffectPrefab;
        [SerializeField] private GameObject beamVfxPrefab;
        [SerializeField] private ParticleSystem impactEffectPrefab;
        [SerializeField] private MuzzleRecoilFeedback recoilFeedback;
        [SerializeField, Min(0.01f)] private float beamEffectLifetime = 0.08f;
        [SerializeField, Min(0.01f)] private float beamEffectWidth = 0.08f;
        [SerializeField, Min(0.01f)] private float beamVfxReferenceLength = 10f;
        [SerializeField, Min(0.01f)] private float beamVfxScale = 1f;
        [SerializeField] private bool beamVfxUsesXAxis;
        [SerializeField] private bool beamVfxMovesToHit;
        [SerializeField] private Vector3 beamVfxRotationOffset;
        [SerializeField] private bool useBeamVfxTint;
        [SerializeField] private Color beamVfxTint = Color.white;
        [SerializeField] private bool stretchBeamVfxToHitPoint;
        [SerializeField] private TurretBeamVfxStretchAxis beamVfxStretchAxis = TurretBeamVfxStretchAxis.Z;
        [SerializeField] private string beamVfxStretchTransformName = "position";
        [SerializeField] private string beamVfxStretchChildNameContains = "line";
        [SerializeField, Min(0.01f)] private float beamVfxVisualLengthMultiplier = 1f;
        [SerializeField, Min(0f)] private float beamVfxEndPadding = 0.1f;
        [SerializeField, Min(0.1f)] private float beamVfxTravelSpeed = 40f;
        [SerializeField, Min(0.05f)] private float beamVfxMovingSegmentLength = 1.6f;
        [SerializeField, Min(0.05f)] private float fireVfxScale = 1f;
        [SerializeField, Min(0.05f)] private float fireVfxLifetime = 0.8f;
        [SerializeField, Min(0.05f)] private float impactEffectScale = 1f;
        [SerializeField, Min(0.05f)] private float impactEffectLifetime = 0.3f;
        [SerializeField] private AudioClip[] chargeAudioClips;
        [SerializeField] private AudioClip[] fireAudioClips;
        [SerializeField] private AudioClip[] impactAudioClips;
        [SerializeField, Range(0f, 1f)] private float chargeAudioVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float fireAudioVolume = 0.75f;
        [SerializeField, Range(0f, 1f)] private float impactAudioVolume = 0.65f;
        [SerializeField, Min(0f)] private float fireAudioMinInterval;
        [SerializeField, Min(0f)] private float impactAudioMinInterval;

        private bool missingFireAudioLogged;
        private bool missingImpactAudioLogged;
        private float nextFireAudioTime;
        private float nextImpactAudioTime;

        public Vector3 Position => fireOrigin != null ? fireOrigin.position : transform.position;

        private void Reset()
        {
            ResolveLocalReferences();
        }

        private void Awake()
        {
            ResolveLocalReferences();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveLocalReferences();
        }
#endif

        public void FireHitscan(IDamageable target, Vector3 direction, Vector3 hitPoint, float damage, GameObject source)
        {
            FireHitscan(target, direction, hitPoint, damage, source, null);
        }

        public void FireHitscan(
            IDamageable target,
            Vector3 direction,
            Vector3 hitPoint,
            float damage,
            GameObject source,
            StatusEffectDefinitionSO[] hitEffects)
        {
            if (target == null || direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 normalizedDirection = direction.normalized;
            PlayFireEffect();
            PlayFireAudio();
            float impactDelay = SpawnBeamEffect(Position, hitPoint);
            if (impactDelay > 0f)
            {
                StartCoroutine(PlayImpactFeedbackDelayed(hitPoint, normalizedDirection, impactDelay));
            }
            else
            {
                PlayImpactFeedback(hitPoint, normalizedDirection);
            }

            target.TakeDamage(new DamageInfo(damage, source, hitPoint));
            StatusEffectUtility.ApplyToTarget(target, hitEffects, source, hitPoint);
        }

        public void PlayChargeAudio()
        {
            if (TryGetRandomClip(chargeAudioClips, out AudioClip clip))
            {
                PlayClipAt(clip, Position, chargeAudioVolume, "TurretChargeSfx");
            }
        }

        private void ResolveLocalReferences()
        {
            if (fireOrigin == null)
            {
                fireOrigin = transform;
            }

            if (fireEffect != null)
            {
                ResolveRecoilFeedback();
                return;
            }

            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem.gameObject == gameObject)
                {
                    continue;
                }

                fireEffect = particleSystem;
                ResolveRecoilFeedback();
                return;
            }

            ResolveRecoilFeedback();
        }

        private void PlayFireEffect()
        {
            if (recoilFeedback != null)
            {
                recoilFeedback.Play();
            }

            if (fireVfxPrefab != null)
            {
                Quaternion rotation = fireOrigin != null ? fireOrigin.rotation : transform.rotation;
                GameObject fireVfx = Instantiate(fireVfxPrefab, Position, rotation);
                fireVfx.transform.localScale *= fireVfxScale;
                PlayParticleSystems(fireVfx, true);
                Destroy(fireVfx, fireVfxLifetime);
                return;
            }

            if (fireEffect != null)
            {
                fireEffect.Play(true);
                return;
            }

            if (beamVfxPrefab != null)
            {
                return;
            }

            Debug.LogError("[ProjectileFirePoint] Fire effect is not assigned.", this);
        }

        private float SpawnBeamEffect(Vector3 origin, Vector3 hitPoint)
        {
            Vector3 offset = hitPoint - origin;
            if (offset.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            if (beamVfxPrefab != null)
            {
                return SpawnBeamVfx(origin, hitPoint, offset);
            }

            if (beamEffectPrefab == null)
            {
                return 0f;
            }

            LineRenderer beam = Instantiate(beamEffectPrefab, origin, Quaternion.identity);
            beam.positionCount = 2;
            beam.useWorldSpace = true;
            beam.SetPosition(0, origin);
            beam.SetPosition(1, hitPoint);
            beam.startWidth = beamEffectWidth;
            beam.endWidth = beamEffectWidth * 0.35f;
            Destroy(beam.gameObject, beamEffectLifetime);
            return 0f;
        }

        private float SpawnBeamVfx(Vector3 origin, Vector3 hitPoint, Vector3 offset)
        {
            Vector3 direction = offset.normalized;
            Quaternion rotation = stretchBeamVfxToHitPoint
                ? Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(beamVfxRotationOffset)
                : beamVfxUsesXAxis
                    ? Quaternion.FromToRotation(Vector3.right, direction) * Quaternion.Euler(beamVfxRotationOffset)
                    : Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(beamVfxRotationOffset);
            GameObject beam = Instantiate(beamVfxPrefab, origin, rotation);
            ApplyBeamVfxTint(beam);
            if (beamVfxMovesToHit)
            {
                float travelDuration = Mathf.Clamp(
                    offset.magnitude / Mathf.Max(0.1f, beamVfxTravelSpeed),
                    0.035f,
                    beamEffectLifetime);
                LineRenderer[] lineRenderers = beam.GetComponentsInChildren<LineRenderer>(true);
                if (lineRenderers.Length > 0)
                {
                    PrepareMovingLineRenderers(lineRenderers, beamVfxScale);
                    StartCoroutine(MoveLineBeamVfx(beam, lineRenderers, origin, hitPoint, travelDuration, beamVfxMovingSegmentLength));
                }
                else
                {
                    beam.transform.localScale = Vector3.one * beamVfxScale;
                    PlayParticleSystems(beam, false);
                    StartCoroutine(MoveBeamVfx(beam, origin, hitPoint, travelDuration));
                }

                return travelDuration;
            }

            if (stretchBeamVfxToHitPoint)
            {
                ApplySegmentBeamVfx(beam, origin, hitPoint, offset.magnitude);
                PlayParticleSystems(beam, true);
                Destroy(beam, beamEffectLifetime);
                return 0f;
            }

            float lengthScale = Mathf.Max(0.01f, offset.magnitude / beamVfxReferenceLength);
            beam.transform.localScale = beamVfxUsesXAxis
                ? new Vector3(lengthScale, beamVfxScale, beamVfxScale)
                : new Vector3(beamVfxScale, beamVfxScale, lengthScale);

            PlayParticleSystems(beam, true);

            Destroy(beam, beamEffectLifetime);
            return 0f;
        }

        private void ApplyBeamVfxTint(GameObject beam)
        {
            if (!useBeamVfxTint || beam == null)
            {
                return;
            }

            ParticleSystem[] particleSystems = beam.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = beamVfxTint;
            }

            LineRenderer[] lineRenderers = beam.GetComponentsInChildren<LineRenderer>(true);
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                lineRenderer.startColor = beamVfxTint;
                lineRenderer.endColor = beamVfxTint;
            }
        }

        private void ApplySegmentBeamVfx(GameObject beam, Vector3 origin, Vector3 hitPoint, float distance)
        {
            if (beam == null)
            {
                return;
            }

            Vector3 direction = (hitPoint - origin).normalized;
            float visualDistance = Mathf.Max(
                0.01f,
                distance * beamVfxVisualLengthMultiplier - beamVfxEndPadding);
            Vector3 visualEndPoint = origin + direction * visualDistance;
            ClampMovingBeamParticlesToSegment(beam, visualDistance);
            beam.transform.SetPositionAndRotation(
                origin,
                Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(beamVfxRotationOffset));

            LineRenderer[] lineRenderers = beam.GetComponentsInChildren<LineRenderer>(true);
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                lineRenderer.positionCount = 2;
                if (lineRenderer.useWorldSpace)
                {
                    lineRenderer.SetPosition(0, origin);
                    lineRenderer.SetPosition(1, visualEndPoint);
                }
                else
                {
                    lineRenderer.SetPosition(0, Vector3.zero);
                    lineRenderer.SetPosition(1, beam.transform.InverseTransformPoint(visualEndPoint));
                }
            }

            Transform stretchTarget = ResolveBeamStretchTarget(beam.transform);
            if (stretchTarget == null)
            {
                return;
            }

            float lengthScale = visualDistance / Mathf.Max(0.01f, beamVfxReferenceLength);
            Vector3 nextScale = stretchTarget.localScale;
            switch (beamVfxStretchAxis)
            {
                case TurretBeamVfxStretchAxis.X:
                    nextScale.x *= lengthScale;
                    break;

                case TurretBeamVfxStretchAxis.Y:
                    nextScale.y *= lengthScale;
                    break;

                default:
                    nextScale.z *= lengthScale;
                    break;
            }

            stretchTarget.localScale = nextScale * beamVfxScale;
        }

        private void ClampMovingBeamParticlesToSegment(GameObject beam, float visualDistance)
        {
            ParticleSystem[] particleSystems = beam.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                float speed = main.startSpeed.constant;
                if (speed <= 0f)
                {
                    continue;
                }

                main.startLifetime = Mathf.Min(
                    main.startLifetime.constant,
                    visualDistance / speed);
            }
        }

        private Transform ResolveBeamStretchTarget(Transform beamRoot)
        {
            if (beamRoot == null)
            {
                return null;
            }

            Transform sourceRoot = beamRoot;
            Transform[] children = beamRoot.GetComponentsInChildren<Transform>(true);
            if (!string.IsNullOrWhiteSpace(beamVfxStretchTransformName))
            {
                sourceRoot = null;
                foreach (Transform child in children)
                {
                    if (child.name == beamVfxStretchTransformName)
                    {
                        sourceRoot = child;
                        break;
                    }
                }
            }

            if (sourceRoot == null)
            {
                Debug.LogWarning(
                    $"[ProjectileFirePoint] Beam stretch target '{beamVfxStretchTransformName}' was not found.",
                    this);
                return beamRoot;
            }

            string childNameFilter = beamVfxStretchChildNameContains;
            if (string.IsNullOrWhiteSpace(childNameFilter))
            {
                return sourceRoot;
            }

            GameObject stretchGroupObject = new GameObject("RuntimeBeamStretchGroup");
            Transform stretchGroup = stretchGroupObject.transform;
            stretchGroup.SetParent(sourceRoot, false);

            Transform[] sourceChildren = sourceRoot.GetComponentsInChildren<Transform>(true);
            int matchedCount = 0;
            foreach (Transform candidate in sourceChildren)
            {
                if (candidate == sourceRoot
                    || candidate == stretchGroup
                    || candidate.name.IndexOf(childNameFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                candidate.SetParent(stretchGroup, true);
                matchedCount++;
            }

            if (matchedCount > 0)
            {
                return stretchGroup;
            }

            Destroy(stretchGroupObject);
            Debug.LogWarning(
                $"[ProjectileFirePoint] No beam child contained '{childNameFilter}'.",
                this);
            return sourceRoot;
        }

        private void ResolveRecoilFeedback()
        {
            if (recoilFeedback != null)
            {
                return;
            }

            recoilFeedback = GetComponentInParent<MuzzleRecoilFeedback>();
        }

        private void PlayFireAudio()
        {
            if (Time.time < nextFireAudioTime)
            {
                return;
            }

            if (TryGetRandomClip(fireAudioClips, out AudioClip clip))
            {
                PlayClipAt(clip, Position, fireAudioVolume, "TurretFireSfx");
                nextFireAudioTime = Time.time + fireAudioMinInterval;
                return;
            }

            if (!missingFireAudioLogged)
            {
                Debug.LogError("[ProjectileFirePoint] Fire audio clips are not assigned.", this);
                missingFireAudioLogged = true;
            }
        }

        private void SpawnImpactEffect(Vector3 hitPoint, Vector3 direction)
        {
            if (impactEffectPrefab == null)
            {
                Debug.LogError("[ProjectileFirePoint] Impact effect prefab is not assigned.", this);
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            ParticleSystem impactEffect = Instantiate(impactEffectPrefab, hitPoint, rotation);
            impactEffect.transform.localScale *= impactEffectScale;
            impactEffect.Play(true);
            Destroy(impactEffect.gameObject, impactEffectLifetime);
        }

        private void PlayImpactFeedback(Vector3 hitPoint, Vector3 direction)
        {
            SpawnImpactEffect(hitPoint, direction);
            PlayImpactAudio(hitPoint);
        }

        private IEnumerator PlayImpactFeedbackDelayed(Vector3 hitPoint, Vector3 direction, float delay)
        {
            yield return new WaitForSeconds(delay);
            PlayImpactFeedback(hitPoint, direction);
        }

        private void PlayImpactAudio(Vector3 hitPoint)
        {
            if (Time.time < nextImpactAudioTime)
            {
                return;
            }

            if (TryGetRandomClip(impactAudioClips, out AudioClip clip))
            {
                PlayClipAt(clip, hitPoint, impactAudioVolume, "TurretImpactSfx");
                nextImpactAudioTime = Time.time + impactAudioMinInterval;
                return;
            }

            if (!missingImpactAudioLogged)
            {
                Debug.LogError("[ProjectileFirePoint] Impact audio clips are not assigned.", this);
                missingImpactAudioLogged = true;
            }
        }

        private static bool TryGetRandomClip(AudioClip[] clips, out AudioClip clip)
        {
            clip = null;
            if (clips == null || clips.Length == 0)
            {
                return false;
            }

            int startIndex = Random.Range(0, clips.Length);
            for (int offset = 0; offset < clips.Length; offset++)
            {
                AudioClip candidate = clips[(startIndex + offset) % clips.Length];
                if (candidate != null)
                {
                    clip = candidate;
                    return true;
                }
            }

            return false;
        }

        private static void PlayClipAt(AudioClip clip, Vector3 position, float volume, string objectName)
        {
            GameObject audioObject = new GameObject(objectName);
            audioObject.transform.position = position;
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.35f;
            audioSource.volume = Mathf.Clamp01(volume);
            audioSource.clip = clip;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 9f;
            audioSource.maxDistance = 70f;
            audioSource.Play();
            Destroy(audioObject, clip.length);
        }

        private static void PlayParticleSystems(GameObject root, bool simulateWarmup)
        {
            ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                particleSystem.Play(true);
                if (simulateWarmup)
                {
                    particleSystem.Simulate(0.04f, true, false, false);
                }
            }
        }

        private static IEnumerator MoveBeamVfx(GameObject beam, Vector3 origin, Vector3 hitPoint, float duration)
        {
            if (beam == null)
            {
                yield break;
            }

            float elapsed = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (elapsed < duration && beam != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                beam.transform.position = Vector3.Lerp(origin, hitPoint, t);
                yield return null;
            }

            if (beam != null)
            {
                Destroy(beam);
            }
        }

        private static void PrepareMovingLineRenderers(LineRenderer[] lineRenderers, float widthScale)
        {
            float safeWidthScale = Mathf.Max(0.01f, widthScale);
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                if (lineRenderer == null)
                {
                    continue;
                }

                lineRenderer.positionCount = 2;
                lineRenderer.useWorldSpace = true;
                lineRenderer.widthMultiplier *= safeWidthScale;
            }
        }

        private static IEnumerator MoveLineBeamVfx(
            GameObject beam,
            LineRenderer[] lineRenderers,
            Vector3 origin,
            Vector3 hitPoint,
            float duration,
            float segmentLength)
        {
            if (beam == null)
            {
                yield break;
            }

            Vector3 offset = hitPoint - origin;
            float distance = offset.magnitude;
            if (distance <= 0.001f)
            {
                Destroy(beam);
                yield break;
            }

            Vector3 direction = offset / distance;
            duration = Mathf.Max(0.01f, duration);
            segmentLength = Mathf.Clamp(segmentLength, 0.05f, distance);

            float elapsed = 0f;
            while (elapsed < duration && beam != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float headDistance = distance * t;
                float tailDistance = Mathf.Max(0f, headDistance - segmentLength);
                SetLineBeamSegment(lineRenderers, origin + direction * tailDistance, origin + direction * headDistance);
                yield return null;
            }

            if (beam != null)
            {
                Destroy(beam);
            }
        }

        private static void SetLineBeamSegment(LineRenderer[] lineRenderers, Vector3 tail, Vector3 head)
        {
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                if (lineRenderer == null)
                {
                    continue;
                }

                lineRenderer.SetPosition(0, tail);
                lineRenderer.SetPosition(1, head);
            }
        }

        private static float GetParticleLifetime(ParticleSystem root)
        {
            float lifetime = 0.1f;
            ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                float particleLifetime = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
                lifetime = Mathf.Max(lifetime, particleLifetime);
            }

            return lifetime;
        }

    }
}
