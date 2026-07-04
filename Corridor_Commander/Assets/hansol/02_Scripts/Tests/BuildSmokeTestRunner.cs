using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace CorridorCommander.Tests
{
    public sealed class BuildSmokeTestRunner : MonoBehaviour
    {
        private const string SmokeArg = "-cc-build-smoke";
        private const string ResultArg = "-cc-smoke-result";
        private const string ScenesArg = "-cc-smoke-scenes";
        private const string RepeatArg = "-cc-smoke-repeat";

        private readonly List<string> failures = new List<string>();
        private string resultPath;
        private string[] sceneNames = { "MainScene", "TutorialMap" };
        private int repeatCount = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!HasArg(args, SmokeArg))
            {
                return;
            }

            GameObject runnerObject = new GameObject("BuildSmokeTestRunner");
            DontDestroyOnLoad(runnerObject);
            runnerObject.AddComponent<BuildSmokeTestRunner>().Configure(args);
        }

        private void Configure(string[] args)
        {
            resultPath = ReadArgValue(args, ResultArg);
            string sceneList = ReadArgValue(args, ScenesArg);
            if (!string.IsNullOrWhiteSpace(sceneList))
            {
                sceneNames = sceneList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }

            string repeatValue = ReadArgValue(args, RepeatArg);
            if (!string.IsNullOrWhiteSpace(repeatValue) && int.TryParse(repeatValue, out int parsedRepeat))
            {
                repeatCount = Mathf.Clamp(parsedRepeat, 1, 20);
            }
        }

        private IEnumerator Start()
        {
            yield return null;

            for (int repeat = 0; repeat < repeatCount; repeat++)
            {
                for (int i = 0; i < sceneNames.Length; i++)
                {
                    string sceneName = sceneNames[i].Trim();
                    if (string.IsNullOrWhiteSpace(sceneName))
                    {
                        continue;
                    }

                    yield return LoadAndValidateScene(sceneName, repeat);
                }
            }

            WriteResultAndQuit();
        }

        private IEnumerator LoadAndValidateScene(string sceneName, int repeat)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (load == null)
            {
                failures.Add(Label(sceneName, repeat) + " failed to start scene load.");
                yield break;
            }

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return new WaitForSecondsRealtime(1f);

            ValidateCommonScene(sceneName, repeat);

            if (sceneName == "MainScene")
            {
                ValidateMainScene(sceneName, repeat);
            }
            else if (sceneName == "TutorialMap")
            {
                ValidateTutorialMap(sceneName, repeat);
            }
        }

        private void ValidateCommonScene(string sceneName, int repeat)
        {
            string label = Label(sceneName, repeat);
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded || activeScene.name != sceneName)
            {
                failures.Add(label + " active scene mismatch: " + activeScene.name);
            }

            if (FindFirstObjectByType<Camera>() == null)
            {
                failures.Add(label + " missing Camera.");
            }

            if (GameObject.FindGameObjectWithTag("Player") == null)
            {
                failures.Add(label + " missing Player tag object.");
            }
        }

        private void ValidateMainScene(string sceneName, int repeat)
        {
            string label = Label(sceneName, repeat);

            if (FindFirstObjectByType<NavMeshSurface>() == null)
            {
                failures.Add(label + " missing NavMeshSurface.");
            }

            EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            if (spawners.Length == 0)
            {
                failures.Add(label + " missing active EnemySpawner.");
            }

            MapExpansionDoorOpener[] doors = FindObjectsByType<MapExpansionDoorOpener>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            if (doors.Length == 0)
            {
                failures.Add(label + " missing active MapExpansionDoorOpener.");
            }

            GameObject goal = GameObject.Find("Enemy_Goal_AlwaysActive") ?? GameObject.Find("Final_Goal_YELLOW");
            if (goal == null)
            {
                failures.Add(label + " missing enemy goal.");
                return;
            }

            ValidateSpawnerPaths(label, spawners, goal.transform.position);
            ValidateDoorCrossings(label, doors);
        }

        private void ValidateTutorialMap(string sceneName, int repeat)
        {
            string label = Label(sceneName, repeat);

            if (FindFirstObjectByType<TutorialDialoguePresenter>() == null)
            {
                failures.Add(label + " missing TutorialDialoguePresenter.");
            }

            NavMeshSurface surface = FindFirstObjectByType<NavMeshSurface>();
            if (surface == null || surface.navMeshData == null)
            {
                failures.Add(label + " missing baked tutorial NavMeshSurface.");
            }
        }

        private void ValidateSpawnerPaths(string label, EnemySpawner[] spawners, Vector3 goalPosition)
        {
            if (!TrySample(goalPosition, 5f, out Vector3 goal))
            {
                failures.Add(label + " enemy goal is not on NavMesh.");
                return;
            }

            int missingPathCount = 0;
            for (int i = 0; i < spawners.Length; i++)
            {
                EnemySpawner spawner = spawners[i];
                if (spawner == null)
                {
                    continue;
                }

                if (!TrySample(spawner.transform.position, 5f, out Vector3 start)
                    || !HasCompletePath(start, goal))
                {
                    missingPathCount++;
                }
            }

            if (missingPathCount > 0)
            {
                failures.Add(label + " spawner path failures: " + missingPathCount + "/" + spawners.Length);
            }
        }

        private void ValidateDoorCrossings(string label, MapExpansionDoorOpener[] doors)
        {
            int crossingFailures = 0;
            for (int i = 0; i < doors.Length; i++)
            {
                MapExpansionDoorOpener door = doors[i];
                if (door == null)
                {
                    continue;
                }

                Vector3 forward = door.transform.forward;
                Vector3 rawA = door.transform.position - forward * 2.2f;
                Vector3 rawB = door.transform.position + forward * 2.2f;
                if (!TrySample(rawA, 2f, out Vector3 a)
                    || !TrySample(rawB, 2f, out Vector3 b)
                    || !HasCompletePath(a, b))
                {
                    crossingFailures++;
                }
            }

            if (crossingFailures > 0)
            {
                failures.Add(label + " door crossing path failures: " + crossingFailures + "/" + doors.Length);
            }
        }

        private static bool TrySample(Vector3 rawPosition, float distance, out Vector3 sampledPosition)
        {
            int areaMask = GetWalkableAreaMask();
            if (NavMesh.SamplePosition(rawPosition, out NavMeshHit hit, distance, areaMask))
            {
                sampledPosition = hit.position;
                return true;
            }

            sampledPosition = rawPosition;
            return false;
        }

        private static bool HasCompletePath(Vector3 start, Vector3 end)
        {
            NavMeshPath path = new NavMeshPath();
            return NavMesh.CalculatePath(start, end, GetWalkableAreaMask(), path)
                && path.status == NavMeshPathStatus.PathComplete;
        }

        private static int GetWalkableAreaMask()
        {
            int areaMask = NavMesh.AllAreas;
            int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");
            if (notWalkableArea >= 0)
            {
                areaMask &= ~(1 << notWalkableArea);
            }

            return areaMask;
        }

        private void WriteResultAndQuit()
        {
            string output = failures.Count == 0
                ? "PASS"
                : "FAIL\n" + string.Join("\n", failures);

            if (!string.IsNullOrWhiteSpace(resultPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
                File.WriteAllText(resultPath, output);
            }

            if (failures.Count == 0)
            {
                Debug.Log("[BuildSmokeTestRunner] PASS");
                Application.Quit(0);
            }
            else
            {
                Debug.LogError("[BuildSmokeTestRunner] " + output);
                Application.Quit(1);
            }
        }

        private static string Label(string sceneName, int repeat)
        {
            return sceneName + "[run " + (repeat + 1) + "]";
        }

        private static bool HasArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ReadArgValue(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return string.Empty;
        }
    }
}
