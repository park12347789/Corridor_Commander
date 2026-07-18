using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class PlayerStatsArtifactPopupRuntimeSmoke
    {
        private const string MenuPath =
            "Tools/Corridor Commander/UI/Run Player Stats Artifact Popup Runtime Smoke";
        private const string MainScenePath =
            "Assets/hansol/01_Scenes/MainScene.unity";
        private const string RequestPath =
            "Library/PlayerStatsArtifactPopupRuntimeSmoke.request";
        private const string ResultPath =
            "Temp/PlayerStatsArtifactPopupRuntimeSmoke.result";
        private const string OriginalScenePathKey =
            "CorridorCommander.PlayerStatsArtifactPopupRuntimeSmoke.OriginalScenePath";
        private const string PendingKey =
            "CorridorCommander.PlayerStatsArtifactPopupRuntimeSmoke.Pending";
        private const string RuntimeStartedKey =
            "CorridorCommander.PlayerStatsArtifactPopupRuntimeSmoke.RuntimeStarted";
        private const string OriginalBackgroundBehaviorKey =
            "CorridorCommander.PlayerStatsArtifactPopupRuntimeSmoke.OriginalBackgroundBehavior";
        private const double StepTimeoutSeconds = 5d;

        private static SmokeStep step;
        private static int queuedFrame;
        private static double deadline;
        private static bool running;
        private static DotweenUiPanelTransition transition;
        private static Keyboard smokeKeyboard;

        private enum SmokeStep
        {
            None,
            WaitForSceneReady,
            WaitForIPressProcessed,
            WaitForIReleaseFrame,
            WaitForOpen,
            WaitForEscapePressProcessed,
            WaitForEscapeReleaseFrame,
            WaitForClosed,
        }

        [MenuItem(MenuPath)]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Player stats artifact popup runtime smoke must start from Edit Mode.");
            }

            Scene originalScene = SceneManager.GetActiveScene();
            if (!originalScene.IsValid() || string.IsNullOrWhiteSpace(originalScene.path))
            {
                throw new InvalidOperationException(
                    "Save the active scene before running the popup runtime smoke.");
            }

            if (originalScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Save the active scene before running the popup runtime smoke.");
            }

            if (File.Exists(ResultPath))
            {
                File.Delete(ResultPath);
            }

            SessionState.SetString(OriginalScenePathKey, originalScene.path);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(RuntimeStartedKey, false);
            SessionState.SetInt(OriginalBackgroundBehaviorKey, -1);
            ClearConsole();

            if (!string.Equals(originalScene.path, MainScenePath, StringComparison.Ordinal))
            {
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            }

            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update -= Tick;

            if (SessionState.GetBool(PendingKey, false)
                && SessionState.GetBool(RuntimeStartedKey, false)
                && EditorApplication.isPlaying)
            {
                EditorApplication.delayCall += AbortAfterUnexpectedDomainReload;
            }

            if (File.Exists(RequestPath))
            {
                File.Delete(RequestPath);
                EditorApplication.delayCall += Run;
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode
                && SessionState.GetBool(PendingKey, false))
            {
                BeginRuntimeCheck();
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            EditorApplication.update -= Tick;
            running = false;
            step = SmokeStep.None;
            transition = null;
            DisposeSmokeKeyboard();
            RestoreBackgroundBehavior();

            if (SessionState.GetBool(PendingKey, false))
            {
                SessionState.SetBool(PendingKey, false);
                SessionState.SetBool(RuntimeStartedKey, false);
                WriteResult(
                    false,
                    "Play Mode exited before the popup runtime smoke completed.");
            }

            EditorApplication.delayCall += RestoreOriginalScene;
        }

        private static void BeginRuntimeCheck()
        {
            running = true;
            SessionState.SetBool(RuntimeStartedKey, true);
            step = SmokeStep.WaitForSceneReady;
            transition = null;
            SetDeadline();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!running || !EditorApplication.isPlaying)
            {
                return;
            }

            try
            {
                switch (step)
                {
                    case SmokeStep.WaitForSceneReady:
                        WaitForSceneReady();
                        break;
                    case SmokeStep.WaitForIPressProcessed:
                        WaitForIPressProcessed();
                        break;
                    case SmokeStep.WaitForIReleaseFrame:
                        WaitForIReleaseFrame();
                        break;
                    case SmokeStep.WaitForOpen:
                        WaitForOpen();
                        break;
                    case SmokeStep.WaitForEscapePressProcessed:
                        WaitForEscapePressProcessed();
                        break;
                    case SmokeStep.WaitForEscapeReleaseFrame:
                        WaitForEscapeReleaseFrame();
                        break;
                    case SmokeStep.WaitForClosed:
                        WaitForClosed();
                        break;
                }
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static void WaitForSceneReady()
        {
            if (!string.Equals(
                    SceneManager.GetActiveScene().path,
                    MainScenePath,
                    StringComparison.Ordinal))
            {
                FailIfTimedOut("MainScene did not become active.");
                return;
            }

            PlayerStatsArtifactPopupPresenter[] presenters =
                UnityEngine.Object.FindObjectsByType<PlayerStatsArtifactPopupPresenter>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (presenters.Length != 1)
            {
                FailIfTimedOut(
                    "Expected exactly one player stats artifact popup presenter, found "
                    + presenters.Length + ".");
                return;
            }

            PlayerStatsArtifactPopupPresenter presenter = presenters[0];
            if (!presenter.isActiveAndEnabled)
            {
                Complete(
                    false,
                    "Player stats artifact popup presenter is not active and enabled.");
                return;
            }

            Transform popup = presenter.transform.Find("PlayerStatsArtifactPopup");
            DotweenUiPanelTransition[] transitions = popup != null
                ? popup.GetComponentsInChildren<DotweenUiPanelTransition>(true)
                : Array.Empty<DotweenUiPanelTransition>();
            if (popup == null || transitions.Length != 1)
            {
                Complete(
                    false,
                    "Expected one direct authored popup with one transition, found "
                    + transitions.Length + " transitions.");
                return;
            }

            transition = transitions[0];
            if (transition.ActivationRoot == null
                || transition.MotionRoot == null
                || transition.CanvasGroup == null
                || transition.ActivationRoot != transition.gameObject
                || transition.MotionRoot.gameObject != transition.gameObject
                || transition.CanvasGroup.gameObject != transition.gameObject)
            {
                Complete(false, "Popup transition references are not canonical.");
                return;
            }

            if (transition.ActivationRoot.activeSelf
                || transition.IsVisible
                || transition.IsTransitioning
                || !Mathf.Approximately(transition.CanvasGroup.alpha, 0f))
            {
                FailIfTimedOut("Popup did not reach its authored initial hidden state.");
                return;
            }

            DisposeSmokeKeyboard();
            SessionState.SetInt(
                OriginalBackgroundBehaviorKey,
                (int)InputSystem.settings.backgroundBehavior);
            InputSystem.settings.backgroundBehavior =
                InputSettings.BackgroundBehavior.IgnoreFocus;
            smokeKeyboard = InputSystem.AddDevice<Keyboard>();
            if (smokeKeyboard == null)
            {
                Complete(false, "Failed to create the isolated Input System keyboard.");
                return;
            }

            smokeKeyboard.MakeCurrent();
            QueueKeyboardState(Key.I);
            step = SmokeStep.WaitForIPressProcessed;
            SetDeadline();
        }

        private static void WaitForIPressProcessed()
        {
            if (!transition.IsVisible || !transition.ActivationRoot.activeSelf)
            {
                FailIfTimedOut(
                    "The presenter Update path did not process the I key press.");
                return;
            }

            QueueKeyboardState();
            step = SmokeStep.WaitForIReleaseFrame;
            SetDeadline();
        }

        private static void WaitForIReleaseFrame()
        {
            if (!HasAdvancedPastQueuedFrame())
            {
                FailIfTimedOut("The player loop did not process the I key release.");
                return;
            }

            step = SmokeStep.WaitForOpen;
            SetDeadline();
        }

        private static void WaitForOpen()
        {
            if (!transition.IsTransitioning
                && transition.IsVisible
                && transition.ActivationRoot.activeSelf
                && Mathf.Approximately(transition.CanvasGroup.alpha, 1f))
            {
                QueueKeyboardState(Key.Escape);
                step = SmokeStep.WaitForEscapePressProcessed;
                SetDeadline();
                return;
            }

            FailIfTimedOut(
                "I did not open and settle the authored popup transition.");
        }

        private static void WaitForEscapePressProcessed()
        {
            if (transition.IsVisible)
            {
                FailIfTimedOut(
                    "The presenter Update path did not process the Escape key press.");
                return;
            }

            QueueKeyboardState();
            step = SmokeStep.WaitForEscapeReleaseFrame;
            SetDeadline();
        }

        private static void WaitForEscapeReleaseFrame()
        {
            if (!HasAdvancedPastQueuedFrame())
            {
                FailIfTimedOut("The player loop did not process the Escape key release.");
                return;
            }

            step = SmokeStep.WaitForClosed;
            SetDeadline();
        }

        private static void WaitForClosed()
        {
            if (!transition.IsTransitioning
                && !transition.IsVisible
                && !transition.ActivationRoot.activeSelf
                && Mathf.Approximately(transition.CanvasGroup.alpha, 0f))
            {
                Complete(
                    true,
                    "Input=I->Escape, Presenter=1, Transition=1, "
                    + "Open=active/alpha1, Closed=inactive/alpha0.");
                return;
            }

            FailIfTimedOut(
                "Escape did not close and settle the authored popup transition.");
        }

        private static void QueueKeyboardState(Key? pressedKey = null)
        {
            if (smokeKeyboard == null || !smokeKeyboard.added)
            {
                throw new InvalidOperationException(
                    "The isolated Input System keyboard is unavailable.");
            }

            KeyboardState state = pressedKey.HasValue
                ? new KeyboardState(pressedKey.Value)
                : new KeyboardState();
            InputSystem.QueueStateEvent(smokeKeyboard, state);
            queuedFrame = Time.frameCount;
        }

        private static bool HasAdvancedPastQueuedFrame()
        {
            return Time.frameCount > queuedFrame;
        }

        private static void SetDeadline()
        {
            deadline = EditorApplication.timeSinceStartup + StepTimeoutSeconds;
        }

        private static void FailIfTimedOut(string message)
        {
            if (EditorApplication.timeSinceStartup >= deadline)
            {
                Complete(false, message);
            }
        }

        private static void Complete(bool passed, string details)
        {
            if (!running)
            {
                return;
            }

            TryReleaseKeys();
            running = false;
            step = SmokeStep.None;
            EditorApplication.update -= Tick;
            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool(RuntimeStartedKey, false);
            DisposeSmokeKeyboard();
            RestoreBackgroundBehavior();
            WriteResult(passed, details);

            EditorApplication.delayCall += ExitPlayMode;
        }

        private static void TryReleaseKeys()
        {
            if (smokeKeyboard == null || !smokeKeyboard.added)
            {
                return;
            }

            try
            {
                InputSystem.QueueStateEvent(smokeKeyboard, new KeyboardState());
            }
            catch (Exception)
            {
                // Play Mode shutdown will reset the device state.
            }
        }

        private static void DisposeSmokeKeyboard()
        {
            if (smokeKeyboard == null)
            {
                return;
            }

            if (smokeKeyboard.added)
            {
                InputSystem.RemoveDevice(smokeKeyboard);
            }

            smokeKeyboard = null;
        }

        private static void RestoreBackgroundBehavior()
        {
            int savedValue =
                SessionState.GetInt(OriginalBackgroundBehaviorKey, -1);
            if (savedValue < 0)
            {
                return;
            }

            InputSystem.settings.backgroundBehavior =
                (InputSettings.BackgroundBehavior)savedValue;
            SessionState.SetInt(OriginalBackgroundBehaviorKey, -1);
        }

        private static void AbortAfterUnexpectedDomainReload()
        {
            if (!SessionState.GetBool(PendingKey, false)
                || !SessionState.GetBool(RuntimeStartedKey, false))
            {
                return;
            }

            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool(RuntimeStartedKey, false);
            DisposeSmokeKeyboard();
            RestoreBackgroundBehavior();
            WriteResult(
                false,
                "An unexpected domain reload interrupted the popup runtime smoke.");
            ExitPlayMode();
        }

        private static void WriteResult(bool passed, string details)
        {
            string output = (passed ? "PASS" : "FAIL")
                + Environment.NewLine
                + details;
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath) ?? "Temp");
            File.WriteAllText(ResultPath, output);

            if (passed)
            {
                Debug.Log(
                    "[PlayerStatsArtifactPopupRuntimeSmoke] PASS " + details);
            }
            else
            {
                Debug.LogError(
                    "[PlayerStatsArtifactPopupRuntimeSmoke] FAIL " + details);
            }
        }

        private static void ExitPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void RestoreOriginalScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += RestoreOriginalScene;
                return;
            }

            string originalScenePath =
                SessionState.GetString(OriginalScenePathKey, string.Empty);
            SessionState.EraseString(OriginalScenePathKey);
            if (string.IsNullOrWhiteSpace(originalScenePath)
                || string.Equals(
                    SceneManager.GetActiveScene().path,
                    originalScenePath,
                    StringComparison.Ordinal))
            {
                return;
            }

            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        private static void ClearConsole()
        {
            Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
            MethodInfo clearMethod = logEntriesType?.GetMethod(
                "Clear",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            clearMethod?.Invoke(null, null);
        }
    }
}
