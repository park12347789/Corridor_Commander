using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class EnemyMovementController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Health health;
        [SerializeField] private float waypointReachDistance = 0.65f;
        [SerializeField] private float refreshInterval = 0.25f;
        [SerializeField] private bool runUpdateLoop = true;

        private readonly List<Transform> routeTargets = new List<Transform>();
        private IMovementMotor movementMotor;
        private INavigationPathValidator pathValidator;
        private float nextRefreshTime;
        private bool isPaused;
        private UnityEngine.AI.NavMeshPathStatus lastPathStatus;
        private bool canMoveToTarget = true;
        private int routeIndex;
        private bool isDead;

        public UnityEngine.AI.NavMeshPathStatus LastPathStatus => lastPathStatus;

        private void Awake()
        {
            ResolveMovementComponents();
            ResolveHealth();
            ResolveTargetFromGameManager();
        }

        private void OnEnable()
        {
            ResolveHealth();
            isDead = health != null && !health.IsAlive;
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (runUpdateLoop)
            {
                TickMovement();
            }
        }

        public void TickMovement()
        {
            if (isDead)
            {
                movementMotor?.Stop();
                return;
            }

            if (isPaused)
            {
                movementMotor?.Stop();
                return;
            }

            ResolveTargetFromGameManager();
            ResolveMovementComponents();
            if (target == null || movementMotor == null)
            {
                return;
            }

            AdvanceRouteIfNeeded();

            if (pathValidator != null && Time.time >= nextRefreshTime)
            {
                nextRefreshTime = Time.time + refreshInterval;
                canMoveToTarget = pathValidator.CanReach(target.position, out lastPathStatus);
            }

            if (!canMoveToTarget)
            {
                movementMotor.Stop();
                return;
            }

            movementMotor.MoveTo(target.position);
        }

        public void SetUpdateLoopEnabled(bool enabled)
        {
            runUpdateLoop = enabled;
        }

        public void SetTarget(Transform newTarget)
        {
            routeTargets.Clear();
            routeIndex = 0;
            target = newTarget;
            nextRefreshTime = 0f;
            canMoveToTarget = true;
        }

        public void SetRoute(IReadOnlyList<Transform> waypoints, Transform finalTarget)
        {
            routeTargets.Clear();
            if (waypoints != null)
            {
                for (int i = 0; i < waypoints.Count; i++)
                {
                    if (waypoints[i] != null)
                    {
                        routeTargets.Add(waypoints[i]);
                    }
                }
            }

            if (finalTarget != null)
            {
                routeTargets.Add(finalTarget);
            }

            routeIndex = 0;
            target = routeTargets.Count > 0 ? routeTargets[routeIndex] : finalTarget;
            nextRefreshTime = 0f;
            canMoveToTarget = true;
        }

        public void SetPaused(bool paused)
        {
            if (isPaused == paused)
            {
                return;
            }

            isPaused = paused;
            nextRefreshTime = 0f;
            canMoveToTarget = true;

            if (isPaused)
            {
                movementMotor?.Stop();
            }
        }

        private void ResolveTargetFromGameManager()
        {
            if (target == null)
            {
                target = GameManager.Instance?.MainTarget;
            }
        }

        private void ResolveMovementComponents()
        {
            if (movementMotor == null)
            {
                MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IMovementMotor foundMotor)
                    {
                        movementMotor = foundMotor;
                        break;
                    }
                }
            }

            if (pathValidator == null)
            {
                MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is INavigationPathValidator foundValidator)
                    {
                        pathValidator = foundValidator;
                        break;
                    }
                }
            }
        }

        private void ResolveHealth()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        private void Subscribe()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
                health.Died += HandleDied;
            }
        }

        private void Unsubscribe()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void HandleDied(Health deadHealth)
        {
            isDead = true;
            movementMotor?.Stop();
        }

        private void AdvanceRouteIfNeeded()
        {
            if (routeTargets.Count == 0 || routeIndex >= routeTargets.Count)
            {
                return;
            }

            while (routeIndex < routeTargets.Count - 1 && IsInWaypointRange(routeTargets[routeIndex]))
            {
                routeIndex++;
                target = routeTargets[routeIndex];
                nextRefreshTime = 0f;
                canMoveToTarget = true;
            }
        }

        private bool IsInWaypointRange(Transform waypoint)
        {
            if (waypoint == null)
            {
                return true;
            }

            Vector3 toWaypoint = waypoint.position - transform.position;
            toWaypoint.y = 0f;
            return toWaypoint.sqrMagnitude <= waypointReachDistance * waypointReachDistance;
        }
    }
}
