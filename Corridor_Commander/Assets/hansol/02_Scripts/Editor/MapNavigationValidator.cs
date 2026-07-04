using System.Collections.Generic;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

#pragma warning disable 0618
namespace CorridorCommander.EditorTools
{
    public static class MapNavigationValidator
    {
        private const float MaxOffMeshLinkDistance = 4f;
        private const float MaxOffMeshLinkHeightDelta = 1.5f;
        private const float AgentRadius = 0.45f;
        private const float AgentHeight = 2f;
        private const float NavMeshSampleDistance = 2f;

        [MenuItem("Corridor Commander/Navigation/Validate Map Links")]
        public static void ValidateMapLinks()
        {
            List<string> failures = CollectUnsafeNavigationLinks(disableUnsafeLinks: false);
            failures.AddRange(CollectEnemyRoutePathFailures(includeInactiveSpawners: false));
            if (failures.Count > 0)
            {
                string failureMessage = string.Join(" | ", failures);
                Debug.LogError("Map navigation validation failed: " + failureMessage);
                throw new System.InvalidOperationException("Map navigation validation failed: " + failureMessage);
            }

            Debug.Log("Map navigation validated.");
        }

        [MenuItem("Corridor Commander/Navigation/Disable Unsafe Map Links")]
        public static void DisableUnsafeMapLinks()
        {
            List<string> failures = CollectUnsafeNavigationLinks(disableUnsafeLinks: true);
            if (failures.Count == 0)
            {
                Debug.Log("No unsafe navigation links found.");
                return;
            }

            EditorSceneManagerUtility.SaveActiveSceneIfDirty();
            Debug.LogWarning("Disabled unsafe navigation links:\n" + string.Join("\n", failures));
        }

        public static List<string> CollectUnsafeOffMeshLinks(bool disableUnsafeLinks)
        {
            return CollectUnsafeNavigationLinks(disableUnsafeLinks);
        }

        public static List<string> CollectUnsafeNavigationLinks(bool disableUnsafeLinks)
        {
            Physics.SyncTransforms();
            List<string> failures = new List<string>();
            CollectUnsafeNavMeshLinks(disableUnsafeLinks, failures);
            CollectUnsafeLegacyOffMeshLinks(disableUnsafeLinks, failures);
            return failures;
        }

        public static List<string> CollectEnemyRoutePathFailures(bool includeInactiveSpawners = false)
        {
            Physics.SyncTransforms();
            int walkableAreaMask = NavMesh.AllAreas;
            int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");
            if (notWalkableArea >= 0)
            {
                walkableAreaMask &= ~(1 << notWalkableArea);
            }

            List<string> failures = new List<string>();
            NavMeshPath path = new NavMeshPath();
            EnemySpawner[] spawners = Object.FindObjectsByType<EnemySpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < spawners.Length; i++)
            {
                CollectSpawnerRoutePathFailures(spawners[i], includeInactiveSpawners, walkableAreaMask, path, failures);
            }

            return failures;
        }

        private static void CollectUnsafeNavMeshLinks(bool disableUnsafeLinks, List<string> failures)
        {
            NavMeshLink[] links = Object.FindObjectsByType<NavMeshLink>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < links.Length; i++)
            {
                NavMeshLink link = links[i];
                if (link == null || !link.gameObject.activeInHierarchy || !link.activated)
                {
                    continue;
                }

                Vector3 start = GetNavMeshLinkStart(link);
                Vector3 end = GetNavMeshLinkEnd(link);
                if (IsSafeLink(link.name, start, end, out string reason))
                {
                    continue;
                }

                failures.Add($"{link.name}: {reason}");
                if (disableUnsafeLinks)
                {
                    Undo.RecordObject(link, "Disable unsafe NavMeshLink");
                    link.activated = false;
                    EditorUtility.SetDirty(link);
                }
            }
        }

        private static void CollectUnsafeLegacyOffMeshLinks(bool disableUnsafeLinks, List<string> failures)
        {
            OffMeshLink[] links = Object.FindObjectsByType<OffMeshLink>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < links.Length; i++)
            {
                OffMeshLink link = links[i];
                if (link == null || !link.gameObject.activeInHierarchy || !link.activated)
                {
                    continue;
                }

                Vector3 start = link.startTransform != null ? link.startTransform.position : link.transform.position;
                Vector3 end = link.endTransform != null ? link.endTransform.position : link.transform.position;
                if (IsSafeLink(link.name, start, end, out string reason))
                {
                    continue;
                }

                failures.Add($"{link.name}: {reason}");
                if (disableUnsafeLinks)
                {
                    Undo.RecordObject(link, "Disable unsafe OffMeshLink");
                    link.activated = false;
                    EditorUtility.SetDirty(link);
                }
            }
        }

        private static void CollectSpawnerRoutePathFailures(
            EnemySpawner spawner,
            bool includeInactiveSpawners,
            int walkableAreaMask,
            NavMeshPath path,
            List<string> failures)
        {
            if (spawner == null || (!includeInactiveSpawners && !spawner.gameObject.activeInHierarchy))
            {
                return;
            }

            SerializedObject spawnerSo = new SerializedObject(spawner);
            Transform spawnPoint = GetObjectReference(spawnerSo, "spawnPoint") as Transform;
            Transform goal = GetObjectReference(spawnerSo, "goal") as Transform;
            EnemyRoute route = GetObjectReference(spawnerSo, "route") as EnemyRoute;
            if (spawnPoint == null)
            {
                failures.Add($"{spawner.name}: spawnPoint is missing.");
                return;
            }

            if (goal == null)
            {
                failures.Add($"{spawner.name}: goal is missing.");
                return;
            }

            if (route == null)
            {
                failures.Add($"{spawner.name}: route is missing.");
                return;
            }

            Vector3 previous = spawnPoint.position;
            if (route.Waypoints != null)
            {
                for (int waypointIndex = 0; waypointIndex < route.Waypoints.Count; waypointIndex++)
                {
                    Transform waypoint = route.Waypoints[waypointIndex];
                    if (waypoint == null)
                    {
                        continue;
                    }

                    CollectRouteSegmentFailure(spawner.name, previous, waypoint.position, walkableAreaMask, path, failures);
                    previous = waypoint.position;
                }
            }

            if (route.IncludeFinalTarget)
            {
                CollectRouteSegmentFailure(spawner.name, previous, goal.position, walkableAreaMask, path, failures);
            }
        }

        private static void CollectRouteSegmentFailure(
            string spawnerName,
            Vector3 rawStart,
            Vector3 rawEnd,
            int walkableAreaMask,
            NavMeshPath path,
            List<string> failures)
        {
            if (!NavMesh.SamplePosition(rawStart, out NavMeshHit startHit, NavMeshSampleDistance, walkableAreaMask))
            {
                failures.Add($"{spawnerName} route start is not on NavMesh: {rawStart}.");
                return;
            }

            if (!NavMesh.SamplePosition(rawEnd, out NavMeshHit endHit, NavMeshSampleDistance, walkableAreaMask))
            {
                failures.Add($"{spawnerName} route target is not on NavMesh: {rawEnd}.");
                return;
            }

            path.ClearCorners();
            bool hasPath = NavMesh.CalculatePath(startHit.position, endHit.position, walkableAreaMask, path);
            if (!hasPath || path.status != NavMeshPathStatus.PathComplete)
            {
                failures.Add($"{spawnerName} route segment has no complete NavMesh path. status={path.status}, start={startHit.position}, end={endHit.position}.");
            }
        }

        private static Object GetObjectReference(SerializedObject so, string propertyName)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue : null;
        }

        private static Vector3 GetNavMeshLinkStart(NavMeshLink link)
        {
            if (link.startTransform != null)
            {
                return link.startTransform.position;
            }

            return GetUnscaledLocalToWorld(link.transform).MultiplyPoint3x4(link.startPoint);
        }

        private static Vector3 GetNavMeshLinkEnd(NavMeshLink link)
        {
            if (link.endTransform != null)
            {
                return link.endTransform.position;
            }

            return GetUnscaledLocalToWorld(link.transform).MultiplyPoint3x4(link.endPoint);
        }

        private static Matrix4x4 GetUnscaledLocalToWorld(Transform transform)
        {
            return Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        }

        private static bool IsSafeLink(string linkName, Vector3 start, Vector3 end, out string reason)
        {
            Vector3 horizontalDelta = end - start;
            horizontalDelta.y = 0f;

            if (horizontalDelta.magnitude > MaxOffMeshLinkDistance)
            {
                reason = $"distance {horizontalDelta.magnitude:0.00} exceeds {MaxOffMeshLinkDistance:0.00}";
                return false;
            }

            float heightDelta = Mathf.Abs(end.y - start.y);
            if (heightDelta > MaxOffMeshLinkHeightDelta)
            {
                reason = $"height delta {heightDelta:0.00} exceeds {MaxOffMeshLinkHeightDelta:0.00}";
                return false;
            }

            Vector3 direction = end - start;
            float distance = direction.magnitude;
            if (distance > 0.001f)
            {
                float capsuleBottomOffset = AgentRadius + 0.05f;
                float capsuleTopOffset = Mathf.Max(capsuleBottomOffset + 0.01f, AgentHeight - AgentRadius - 0.05f);
                Vector3 capsuleBottom = start + Vector3.up * capsuleBottomOffset;
                Vector3 capsuleTop = start + Vector3.up * capsuleTopOffset;
                RaycastHit[] hits = Physics.CapsuleCastAll(
                        capsuleBottom,
                        capsuleTop,
                        AgentRadius,
                        direction.normalized,
                        distance,
                        ~0,
                        QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider hitCollider = hits[i].collider;
                    if (hitCollider == null || IsControlledDoorBlocker(hitCollider))
                    {
                        continue;
                    }

                    reason = $"{linkName} blocked by {hitCollider.name}";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private static bool IsControlledDoorBlocker(Collider collider)
        {
            return collider.GetComponentInParent<MapExpansionDoorOpener>() != null
                && collider.name.Contains("DoorClosedBlocker");
        }

        private static class EditorSceneManagerUtility
        {
            public static void SaveActiveSceneIfDirty()
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                }
            }
        }
    }
}
#pragma warning restore 0618
