using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class StatusEffectReceiver : MonoBehaviour, IStatusEffectReceiver
    {
        private sealed class ActiveStatusEffect
        {
            public StatusEffectDefinitionSO Definition;
            public GameObject Source;
            public float ExpiresAt;
        }

        private sealed class TintTarget
        {
            public Renderer Renderer;
            public MaterialPropertyBlock OriginalBlock;
            public MaterialPropertyBlock TintBlock;
        }

        private readonly List<ActiveStatusEffect> activeEffects = new List<ActiveStatusEffect>();
        private readonly List<TintTarget> tintTargets = new List<TintTarget>();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color SlowTintColor = new Color(0.35f, 0.65f, 1f, 1f);
        private bool slowTintApplied;

        public float MoveSpeedMultiplier
        {
            get
            {
                PruneExpiredEffects();

                float multiplier = 1f;
                for (int i = 0; i < activeEffects.Count; i++)
                {
                    StatusEffectDefinitionSO definition = activeEffects[i].Definition;
                    if (definition != null && definition.AffectsMoveSpeed)
                    {
                        multiplier = Mathf.Min(multiplier, definition.SpeedMultiplier);
                    }
                }

                return multiplier;
            }
        }

        private void Update()
        {
            PruneExpiredEffects();
            RefreshSlowTint();
        }

        private void OnDisable()
        {
            ClearSlowTint();
        }

        public void ApplyStatusEffect(StatusEffectDefinitionSO definition, GameObject source, Vector3 hitPoint)
        {
            if (definition == null)
            {
                return;
            }

            ActiveStatusEffect effect = FindEffect(definition);
            if (effect == null)
            {
                effect = new ActiveStatusEffect
                {
                    Definition = definition
                };
                activeEffects.Add(effect);
            }

            effect.Source = source;
            effect.ExpiresAt = Time.time + definition.Duration;
            SpawnVfx(definition.ApplyVfxPrefab, hitPoint);
            RefreshSlowTint();
        }

        private ActiveStatusEffect FindEffect(StatusEffectDefinitionSO definition)
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].Definition == definition)
                {
                    return activeEffects[i];
                }
            }

            return null;
        }

        private void PruneExpiredEffects()
        {
            float now = Time.time;
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].Definition == null || activeEffects[i].ExpiresAt <= now)
                {
                    activeEffects.RemoveAt(i);
                }
            }
        }

        private void RefreshSlowTint()
        {
            if (IsMoveSpeedReduced())
            {
                ApplySlowTint();
            }
            else
            {
                ClearSlowTint();
            }
        }

        private bool IsMoveSpeedReduced()
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                StatusEffectDefinitionSO definition = activeEffects[i].Definition;
                if (definition != null && definition.AffectsMoveSpeed)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplySlowTint()
        {
            if (slowTintApplied)
            {
                return;
            }

            tintTargets.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null
                    || targetRenderer is ParticleSystemRenderer
                    || targetRenderer is LineRenderer
                    || targetRenderer is TrailRenderer)
                {
                    continue;
                }

                TintTarget target = new TintTarget
                {
                    Renderer = targetRenderer,
                    OriginalBlock = new MaterialPropertyBlock(),
                    TintBlock = new MaterialPropertyBlock()
                };

                targetRenderer.GetPropertyBlock(target.OriginalBlock);
                targetRenderer.GetPropertyBlock(target.TintBlock);
                target.TintBlock.SetColor(BaseColorId, SlowTintColor);
                target.TintBlock.SetColor(ColorId, SlowTintColor);
                targetRenderer.SetPropertyBlock(target.TintBlock);
                tintTargets.Add(target);
            }

            slowTintApplied = tintTargets.Count > 0;
        }

        private void ClearSlowTint()
        {
            if (!slowTintApplied)
            {
                return;
            }

            for (int i = 0; i < tintTargets.Count; i++)
            {
                TintTarget target = tintTargets[i];
                if (target.Renderer != null)
                {
                    target.Renderer.SetPropertyBlock(target.OriginalBlock);
                }
            }

            tintTargets.Clear();
            slowTintApplied = false;
        }

        private static void SpawnVfx(GameObject prefab, Vector3 position)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            float maxLifetime = 0f;
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle == null)
                {
                    continue;
                }

                particle.Play(true);
                ParticleSystem.MainModule main = particle.main;
                maxLifetime = Mathf.Max(maxLifetime, main.duration + main.startLifetime.constantMax);
            }

            if (particles.Length > 0)
            {
                DestroyRuntimeObject(instance, maxLifetime + 0.25f);
            }
        }

        private static void DestroyRuntimeObject(GameObject target, float delay)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target, delay);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
