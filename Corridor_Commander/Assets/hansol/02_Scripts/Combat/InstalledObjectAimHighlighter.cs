using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class InstalledObjectAimHighlighter : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] [Min(1f)] private float maxDistance = 60f;
        [SerializeField] private LayerMask aimLayers = ~0;

        private IInstalledAimInfoProvider currentProvider;
        private IInstalledRangeIndicator currentRangeIndicator;
        private IInstalledObjectStatUi currentStatUi;
        private readonly HashSet<int> missingStatUiLoggedTargets = new HashSet<int>();
        private bool missingCameraLogged;

        private void Update()
        {
            if (FieldInteractionAimVisibilityController.IsSuppressed)
            {
                ClearCurrentTarget();
                return;
            }

            if (!UiInputCoordinator.CanLook)
            {
                ClearCurrentTarget();
                return;
            }

            Camera resolvedCamera = ResolveCamera();
            if (resolvedCamera == null)
            {
                ClearCurrentTarget();
                return;
            }

            IInstalledAimInfoProvider nextProvider = ResolveLookedAtProvider(resolvedCamera);
            SetCurrentProvider(nextProvider);
        }

        private void OnDisable()
        {
            ClearCurrentTarget();
        }

        private IInstalledAimInfoProvider ResolveLookedAtProvider(Camera resolvedCamera)
        {
            Ray ray = resolvedCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, aimLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length <= 0)
            {
                return null;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                IInstalledAimInfoProvider provider = ResolveProvider(hitCollider.transform);
                if (provider != null)
                {
                    return provider;
                }
            }

            return null;
        }

        private void SetCurrentProvider(IInstalledAimInfoProvider nextProvider)
        {
            if (ReferenceEquals(currentProvider, nextProvider))
            {
                RefreshCurrentInfo();
                return;
            }

            ClearCurrentTarget();
            currentProvider = nextProvider;
            RefreshCurrentInfo();
        }

        private void RefreshCurrentInfo()
        {
            if (currentProvider == null)
            {
                ClearCurrentTarget();
                return;
            }

            Camera resolvedCamera = ResolveCamera();
            if (resolvedCamera == null)
            {
                return;
            }

            if (!currentProvider.TryGetAimInfo(out InstalledAimInfo info))
            {
                ClearCurrentTarget();
                return;
            }

            IInstalledObjectStatUi statUi = ResolveStatUi(currentProvider);
            if (statUi == null)
            {
                return;
            }

            currentStatUi = statUi;
            statUi.Show(info, resolvedCamera);
            UpdateRangeIndicator(info);
        }

        private void UpdateRangeIndicator(InstalledAimInfo info)
        {
            currentRangeIndicator ??= ResolveRangeIndicator(currentProvider);
            if (currentRangeIndicator == null)
            {
                return;
            }

            if (!info.HasRange)
            {
                currentRangeIndicator.HideRange();
                return;
            }

            currentRangeIndicator.SetRange(info.Range);
            currentRangeIndicator.ShowCachedRange();
        }

        private void ClearCurrentTarget()
        {
            currentStatUi?.Hide();
            currentRangeIndicator?.HideRange();
            currentProvider = null;
            currentRangeIndicator = null;
            currentStatUi = null;
        }

        private Camera ResolveCamera()
        {
            if (aimCamera != null)
            {
                return aimCamera;
            }

            aimCamera = Camera.main;
            if (aimCamera == null && !missingCameraLogged)
            {
                Debug.LogError("[InstalledObjectAimHighlighter] Aim camera is not assigned and no MainCamera exists.", this);
                missingCameraLogged = true;
            }

            return aimCamera;
        }

        private IInstalledObjectStatUi ResolveStatUi(IInstalledAimInfoProvider provider)
        {
            if (provider is not MonoBehaviour behaviour)
            {
                return null;
            }

            MonoBehaviour[] behaviours = behaviour.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInstalledObjectStatUi statUi)
                {
                    return statUi;
                }
            }

            int targetId = behaviour.GetInstanceID();
            if (missingStatUiLoggedTargets.Add(targetId))
            {
                Debug.LogError($"[InstalledObjectAimHighlighter] InstalledObjectStatCanvasPresenter is missing on {behaviour.name}.", behaviour);
            }

            return null;
        }

        private static IInstalledAimInfoProvider ResolveProvider(Transform hitTransform)
        {
            MonoBehaviour[] behaviours = hitTransform.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInstalledAimInfoProvider provider)
                {
                    return provider;
                }
            }

            return null;
        }

        private static IInstalledRangeIndicator ResolveRangeIndicator(IInstalledAimInfoProvider provider)
        {
            if (provider is not MonoBehaviour behaviour)
            {
                return null;
            }

            MonoBehaviour[] behaviours = behaviour.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInstalledRangeIndicator indicator)
                {
                    return indicator;
                }
            }

            return null;
        }

    }
}
