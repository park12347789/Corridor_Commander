#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CorridorCommander.Tests
{
    public sealed class UiMotionRuntimeSmokeDriver : MonoBehaviour
    {
        private const int ExpectedMinimumPanelTransitions = 15;

        [SerializeField] private DotweenUiPanelTransition[] panelTransitions;
        [SerializeField] private DotweenUiButtonFeedback[] buttonFeedbacks;
        [SerializeField] private DotweenUiValueChangeFeedback[] valueFeedbacks;
        [SerializeField] private string resultPath;

        private readonly List<string> failures = new List<string>();

        public void Configure(
            DotweenUiPanelTransition[] transitions,
            DotweenUiButtonFeedback[] buttons,
            DotweenUiValueChangeFeedback[] values,
            string outputPath)
        {
            panelTransitions = transitions;
            buttonFeedbacks = buttons;
            valueFeedbacks = values;
            resultPath = outputPath;
        }

        private IEnumerator Start()
        {
            yield return null;
            NormalizeRuntimeReferences();
            ValidateCoverage();

            if (failures.Count == 0)
            {
                yield return RunPanelTransitionChecks();
                yield return RunMicroFeedbackChecks();
            }

            WriteResultAndExit();
        }

        private void NormalizeRuntimeReferences()
        {
            panelTransitions = FilterAlive(panelTransitions);
            buttonFeedbacks = FilterAlive(buttonFeedbacks);
            valueFeedbacks = FilterAlive(valueFeedbacks);
        }

        private static T[] FilterAlive<T>(T[] source) where T : Component
        {
            if (source == null || source.Length == 0)
            {
                return System.Array.Empty<T>();
            }

            List<T> alive = new List<T>(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                {
                    alive.Add(source[i]);
                }
            }

            return alive.ToArray();
        }

        private void ValidateCoverage()
        {
            if (panelTransitions == null || panelTransitions.Length < ExpectedMinimumPanelTransitions)
            {
                failures.Add(
                    "Expected at least " + ExpectedMinimumPanelTransitions + " prefab panel transitions, found "
                    + (panelTransitions == null ? 0 : panelTransitions.Length)
                    + ".");
            }

            if (buttonFeedbacks == null || buttonFeedbacks.Length == 0)
            {
                failures.Add("No button feedback components were instantiated.");
            }

            if (valueFeedbacks == null || valueFeedbacks.Length == 0)
            {
                failures.Add("No value feedback components were instantiated.");
            }

            if (string.IsNullOrWhiteSpace(resultPath))
            {
                failures.Add("Result path is empty.");
            }
        }

        private IEnumerator RunPanelTransitionChecks()
        {
            for (int i = 0; i < panelTransitions.Length; i++)
            {
                DotweenUiPanelTransition transition = panelTransitions[i];
                if (!HasValidReferences(transition, i))
                {
                    continue;
                }

                transition.Show();
                yield return new WaitForSecondsRealtime(0.35f);

                if (!transition.IsVisible || transition.IsTransitioning)
                {
                    failures.Add(Label(transition, i) + " did not complete Show().");
                }

                if (!transition.ActivationRoot.activeSelf)
                {
                    failures.Add(Label(transition, i) + " activation root stayed inactive after Show().");
                }

                if (!Mathf.Approximately(transition.CanvasGroup.alpha, 1f))
                {
                    failures.Add(Label(transition, i) + " CanvasGroup alpha did not reach 1 after Show().");
                }

                transition.Hide();
                yield return new WaitForSecondsRealtime(0.25f);

                if (transition.IsVisible || transition.IsTransitioning)
                {
                    failures.Add(Label(transition, i) + " did not complete Hide().");
                }

                if (transition.ActivationRoot.activeSelf)
                {
                    failures.Add(Label(transition, i) + " activation root stayed active after Hide().");
                }
            }
        }

        private IEnumerator RunMicroFeedbackChecks()
        {
            PointerEventData pointerData = new PointerEventData(null);
            for (int i = 0; i < buttonFeedbacks.Length; i++)
            {
                DotweenUiButtonFeedback feedback = buttonFeedbacks[i];
                if (feedback == null)
                {
                    failures.Add("Button feedback " + i + " is null.");
                    continue;
                }

                feedback.OnPointerEnter(pointerData);
                feedback.OnPointerDown(pointerData);
                feedback.OnPointerUp(pointerData);
                feedback.OnPointerExit(pointerData);
            }

            for (int i = 0; i < valueFeedbacks.Length; i++)
            {
                DotweenUiValueChangeFeedback feedback = valueFeedbacks[i];
                if (feedback == null)
                {
                    failures.Add("Value feedback " + i + " is null.");
                    continue;
                }

                TMP_Text text = feedback.GetComponent<TMP_Text>();
                if (text == null)
                {
                    failures.Add(feedback.name + " value feedback has no TMP_Text.");
                    continue;
                }

                text.text += " ";
            }

            yield return null;
            yield return new WaitForSecondsRealtime(0.30f);
        }

        private bool HasValidReferences(DotweenUiPanelTransition transition, int index)
        {
            if (transition == null)
            {
                failures.Add("Panel transition " + index + " is null.");
                return false;
            }

            if (transition.ActivationRoot == null
                || transition.MotionRoot == null
                || transition.CanvasGroup == null)
            {
                failures.Add(Label(transition, index) + " has a missing serialized reference.");
                return false;
            }

            return true;
        }

        private static string Label(DotweenUiPanelTransition transition, int index)
        {
            return "Transition[" + index + "] " + (transition != null ? transition.name : "null");
        }

        private void WriteResultAndExit()
        {
            string counts = "Transitions=" + (panelTransitions == null ? 0 : panelTransitions.Length)
                + ", Buttons=" + (buttonFeedbacks == null ? 0 : buttonFeedbacks.Length)
                + ", Values=" + (valueFeedbacks == null ? 0 : valueFeedbacks.Length);
            string output = failures.Count == 0
                ? "PASS\n" + counts
                : "FAIL\n" + counts + "\n" + string.Join("\n", failures);

            if (!string.IsNullOrWhiteSpace(resultPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(resultPath) ?? "Temp");
                File.WriteAllText(resultPath, output);
            }

            if (failures.Count == 0)
            {
                Debug.Log("[UiMotionRuntimeSmokeDriver] PASS " + counts);
            }
            else
            {
                Debug.LogError("[UiMotionRuntimeSmokeDriver] " + output);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
                return;
            }

            EditorApplication.isPlaying = false;
        }
    }
}
#endif
