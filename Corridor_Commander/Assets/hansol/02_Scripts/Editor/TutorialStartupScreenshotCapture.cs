using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CorridorCommander.EditorTools
{
    [InitializeOnLoad]
    public static class TutorialStartupScreenshotCapture
    {
        private const string TutorialScenePath = "Assets/hansol/01_Scenes/TutorialMap.unity";
        private const string ScreenshotDirectory = "Assets/Temp/CodexScreenshots";
        private const string RequestKey = "CorridorCommander.TutorialStartupScreenshotCapture.Requested";
        private const string CapturedKey = "CorridorCommander.TutorialStartupScreenshotCapture.Captured";
        private const string CapturePathKey = "CorridorCommander.TutorialStartupScreenshotCapture.CapturePath";
        private const string OutputPathKey = "CorridorCommander.TutorialStartupScreenshotCapture.OutputPath";
        private const string PlayFrameKey = "CorridorCommander.TutorialStartupScreenshotCapture.PlayFrame";
        private const string WaitFrameKey = "CorridorCommander.TutorialStartupScreenshotCapture.WaitFrame";

        static TutorialStartupScreenshotCapture()
        {
            EditorApplication.update -= UpdateCapture;
            EditorApplication.update += UpdateCapture;
        }

        [MenuItem("Corridor Commander/Tutorial/Capture Startup Screenshot No Prompt")]
        public static void CaptureNoPrompt()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[TutorialStartupScreenshotCapture] Cannot start while play mode is changing.");
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != TutorialScenePath)
            {
                EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
            }

            string capturePath = CreateCapturePath();
            string outputPath = ResolveAbsolutePath(capturePath);
            EditorPrefs.SetBool(RequestKey, true);
            EditorPrefs.SetBool(CapturedKey, false);
            EditorPrefs.SetString(CapturePathKey, capturePath);
            EditorPrefs.SetString(OutputPathKey, outputPath);
            EditorPrefs.SetInt(PlayFrameKey, 0);
            EditorPrefs.SetInt(WaitFrameKey, 0);
            EditorApplication.isPlaying = true;
            Debug.Log("[TutorialStartupScreenshotCapture] Started: " + outputPath);
        }

        private static void UpdateCapture()
        {
            if (!EditorPrefs.GetBool(RequestKey, false))
            {
                return;
            }

            string capturePath = EditorPrefs.GetString(CapturePathKey, string.Empty);
            string outputPath = EditorPrefs.GetString(OutputPathKey, string.Empty);
            if (string.IsNullOrWhiteSpace(capturePath) || string.IsNullOrWhiteSpace(outputPath))
            {
                FinishWithError("Output path is missing.");
                return;
            }

            if (EditorApplication.isPlaying)
            {
                int frame = EditorPrefs.GetInt(PlayFrameKey, 0) + 1;
                EditorPrefs.SetInt(PlayFrameKey, frame);

                if (!EditorPrefs.GetBool(CapturedKey, false) && frame >= 20)
                {
                    EnsureOutputDirectory();
                    ScreenCapture.CaptureScreenshot(capturePath);
                    EditorPrefs.SetBool(CapturedKey, true);
                    Debug.Log("[TutorialStartupScreenshotCapture] Capture requested: " + outputPath);
                }

                if (EditorPrefs.GetBool(CapturedKey, false) && frame >= 240)
                {
                    EditorApplication.isPlaying = false;
                }

                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!EditorPrefs.GetBool(CapturedKey, false))
            {
                return;
            }

            if (!File.Exists(outputPath))
            {
                int waitFrame = EditorPrefs.GetInt(WaitFrameKey, 0) + 1;
                EditorPrefs.SetInt(WaitFrameKey, waitFrame);
                if (waitFrame < 240)
                {
                    return;
                }

                FinishWithError("Screenshot file was not created: " + outputPath);
                return;
            }

            FileInfo fileInfo = new FileInfo(outputPath);
            if (fileInfo.Length <= 0L)
            {
                FinishWithError("Screenshot file is empty: " + outputPath);
                return;
            }

            ClearState();
            AssetDatabase.Refresh();
            Debug.Log("[TutorialStartupScreenshotCapture] Completed: " + outputPath);
        }

        private static string CreateCapturePath()
        {
            EnsureOutputDirectory();
            string fileName = "tutorial_startup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            return (ScreenshotDirectory + "/" + fileName).Replace('\\', '/');
        }

        private static string ResolveAbsolutePath(string capturePath)
        {
            string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
            return Path.Combine(projectRoot, capturePath).Replace('\\', '/');
        }

        private static void EnsureOutputDirectory()
        {
            string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
            string absoluteDirectory = Path.Combine(projectRoot, ScreenshotDirectory);
            Directory.CreateDirectory(absoluteDirectory);
        }

        private static void FinishWithError(string message)
        {
            ClearState();
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            Debug.LogError("[TutorialStartupScreenshotCapture] " + message);
        }

        private static void ClearState()
        {
            EditorPrefs.DeleteKey(RequestKey);
            EditorPrefs.DeleteKey(CapturedKey);
            EditorPrefs.DeleteKey(CapturePathKey);
            EditorPrefs.DeleteKey(OutputPathKey);
            EditorPrefs.DeleteKey(PlayFrameKey);
            EditorPrefs.DeleteKey(WaitFrameKey);
        }
    }
}
