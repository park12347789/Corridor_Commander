#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CorridorCommander.Tests
{
    public sealed class MapExpansionGateRuntimeSmokeDriver : MonoBehaviour
    {
        [SerializeField] private MapExpansionDoorOpener[] doorOpeners;
        [SerializeField] private string resultPath;
        [SerializeField] private float completionWaitSeconds = 1.25f;

        private readonly List<string> failures = new List<string>();

        public void Configure(MapExpansionDoorOpener[] openers, string outputPath)
        {
            doorOpeners = openers;
            resultPath = outputPath;
        }

        private IEnumerator Start()
        {
            yield return null;

            ValidateInitialReferences();
            if (failures.Count == 0)
            {
                yield return RunDoorChecks();
            }

            WriteResultAndExit();
        }

        private IEnumerator RunDoorChecks()
        {
            for (int i = 0; i < doorOpeners.Length; i++)
            {
                MapExpansionDoorOpener opener = doorOpeners[i];
                if (opener == null)
                {
                    failures.Add("Door opener " + i + " is null.");
                    continue;
                }

                DoorRuntimeReferences refs = DoorRuntimeReferences.Resolve(opener);
                refs.Validate(opener.name, failures);
                if (!refs.IsValid)
                {
                    continue;
                }

                int openedEventCount = 0;
                opener.Opened += _ => openedEventCount++;
                opener.Open();

                if (!opener.IsOpen)
                {
                    failures.Add(opener.name + " did not set IsOpen immediately.");
                }

                if (openedEventCount != 1)
                {
                    failures.Add(opener.name + " did not invoke Opened exactly once immediately.");
                }

                if (!refs.ActivationGroup.IsActive)
                {
                    failures.Add(opener.name + " did not activate target group immediately.");
                }

                if (refs.ActivationTarget != null && !refs.ActivationTarget.activeSelf)
                {
                    failures.Add(opener.name + " activation target did not become active immediately.");
                }

                if (!refs.PassageBlocker.activeSelf)
                {
                    failures.Add(opener.name + " blocker was disabled before animation completed.");
                }

                if (refs.OpenedRoot.activeSelf)
                {
                    failures.Add(opener.name + " opened visual was visible before animation completed.");
                }

                if (!refs.ClosedRoot.activeSelf)
                {
                    failures.Add(opener.name + " closed visual was hidden before animation completed.");
                }

                yield return new WaitForSecondsRealtime(completionWaitSeconds);

                if (opener.IsOpening)
                {
                    failures.Add(opener.name + " stayed in opening state after completion wait.");
                }

                if (refs.PassageBlocker.activeSelf)
                {
                    failures.Add(opener.name + " blocker stayed active after animation completed.");
                }

                if (refs.ClosedRoot.activeSelf)
                {
                    failures.Add(opener.name + " closed visual stayed active after animation completed.");
                }

                if (!refs.OpenedRoot.activeSelf)
                {
                    failures.Add(opener.name + " opened visual did not become active after animation completed.");
                }
            }
        }

        private void ValidateInitialReferences()
        {
            if (doorOpeners == null || doorOpeners.Length == 0)
            {
                failures.Add("No door openers configured.");
            }

            if (string.IsNullOrWhiteSpace(resultPath))
            {
                failures.Add("Result path is empty.");
            }
        }

        private void WriteResultAndExit()
        {
            string output = failures.Count == 0
                ? "PASS"
                : "FAIL\n" + string.Join("\n", failures);

            if (!string.IsNullOrWhiteSpace(resultPath))
            {
                File.WriteAllText(resultPath, output);
            }

            if (failures.Count == 0)
            {
                Debug.Log("[MapExpansionGateRuntimeSmokeDriver] PASS");
                FinishEditorRun(0);
            }
            else
            {
                Debug.LogError("[MapExpansionGateRuntimeSmokeDriver] " + output);
                FinishEditorRun(1);
            }
        }

        private static void FinishEditorRun(int exitCode)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
                return;
            }

            EditorApplication.isPlaying = false;
        }

        private sealed class DoorRuntimeReferences
        {
            public GameObject ClosedRoot { get; private set; }
            public GameObject OpenedRoot { get; private set; }
            public GameObject PassageBlocker { get; private set; }
            public MapExpansionActivationTargetGroup ActivationGroup { get; private set; }
            public GameObject ActivationTarget { get; private set; }
            public bool IsValid => ClosedRoot != null
                && OpenedRoot != null
                && PassageBlocker != null
                && ActivationGroup != null;

            public static DoorRuntimeReferences Resolve(MapExpansionDoorOpener opener)
            {
                DoorRuntimeReferences refs = new DoorRuntimeReferences
                {
                    ClosedRoot = FindChild(opener.transform, "DoorClosedRoot"),
                    OpenedRoot = FindChild(opener.transform, "DoorOpenedVisual"),
                    PassageBlocker = FindChild(opener.transform, "DoorClosedBlocker"),
                    ActivationGroup = opener.GetComponentInParent<MapExpansionActivationTargetGroup>()
                };

                refs.ActivationTarget = refs.ReadFirstActivationTarget();
                return refs;
            }

            public void Validate(string label, List<string> failures)
            {
                if (ClosedRoot == null)
                {
                    failures.Add(label + " missing DoorClosedRoot.");
                }

                if (OpenedRoot == null)
                {
                    failures.Add(label + " missing DoorOpenedVisual.");
                }

                if (PassageBlocker == null)
                {
                    failures.Add(label + " missing DoorClosedBlocker.");
                }

                if (ActivationGroup == null)
                {
                    failures.Add(label + " missing activation group.");
                }
            }

            private GameObject ReadFirstActivationTarget()
            {
                if (ActivationGroup == null)
                {
                    return null;
                }

                SerializedObject so = new SerializedObject(ActivationGroup);
                SerializedProperty targets = so.FindProperty("activationTargets");
                if (targets == null || !targets.isArray || targets.arraySize == 0)
                {
                    return null;
                }

                return targets.GetArrayElementAtIndex(0).objectReferenceValue as GameObject;
            }

            private static GameObject FindChild(Transform root, string childName)
            {
                Transform[] children = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i].name == childName)
                    {
                        return children[i].gameObject;
                    }
                }

                return null;
            }
        }
    }
}
#endif
