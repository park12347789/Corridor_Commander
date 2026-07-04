using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CorridorCommander.EditorTools
{
    public static class SubmissionBuildExporter
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/hansol/01_Scenes/StartMenu.unity",
            "Assets/hansol/01_Scenes/MainScene.unity",
            "Assets/hansol/01_Scenes/TutorialMap.unity",
        };

        private const string DefaultOutputRoot = "Builds/Submission";
        private const string ExecutableName = "CorridorCommander.exe";
        private const string ReadmeName = "조작법_README.txt";

        private static readonly string[] HansolFontAssetsToRepair =
        {
            "Assets/hansol/09_Settings/Font/Jua/Jua Dynamic SDF.asset",
            "Assets/hansol/09_Settings/Font/Jua/Jua SDF.asset",
            "Assets/hansol/09_Settings/Font/BMJUA/BMJUA SDF.asset",
        };

        [MenuItem("Corridor Commander/Build/Export Submission Windows Build")]
        public static void ExportWindowsBuild()
        {
            ExportWindowsBuildInternal(GetDefaultOutputDirectory(), quitOnFinish: false);
        }

        public static void ExportWindowsBuildBatch()
        {
            var outputDirectory = GetCommandLineOutputDirectory();
            var exitCode = 0;

            try
            {
                ExportWindowsBuildInternal(outputDirectory, quitOnFinish: true);
            }
            catch (Exception ex)
            {
                Debug.LogError("Submission build failed: " + ex);
                exitCode = 1;
            }

            EditorApplication.Exit(exitCode);
        }

        private static void ExportWindowsBuildInternal(string outputDirectory, bool quitOnFinish)
        {
            RepairBrokenHansolTmpFonts();
            ValidateScenes();

            Directory.CreateDirectory(outputDirectory);
            WriteReadme(outputDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = ScenePaths,
                locationPathName = Path.Combine(outputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            var resultLine = string.Format(
                "Submission build result: {0}, errors={1}, warnings={2}, output={3}",
                summary.result,
                summary.totalErrors,
                summary.totalWarnings,
                summary.outputPath);

            if (summary.result != BuildResult.Succeeded || summary.totalErrors > 0)
            {
                throw new InvalidOperationException(resultLine);
            }

            Debug.Log(resultLine);

            if (quitOnFinish)
            {
                Debug.Log("Submission build exported to: " + outputDirectory);
            }
        }

        private static void ValidateScenes()
        {
            var missing = ScenePaths.Where(scenePath => !File.Exists(scenePath)).ToArray();
            if (missing.Length > 0)
            {
                throw new FileNotFoundException("Submission scenes are missing: " + string.Join(", ", missing));
            }
        }

        private static void RepairBrokenHansolTmpFonts()
        {
            var repaired = 0;

            foreach (var path in HansolFontAssetsToRepair)
            {
                var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
                if (font == null)
                {
                    continue;
                }

                var serializedFont = new SerializedObject(font);
                var atlasTextures = serializedFont.FindProperty("m_AtlasTextures");
                if (atlasTextures == null)
                {
                    continue;
                }

                if (atlasTextures.arraySize == 0)
                {
                    atlasTextures.arraySize = 1;
                }

                var firstAtlas = atlasTextures.GetArrayElementAtIndex(0);
                if (firstAtlas.objectReferenceValue != null)
                {
                    continue;
                }

                var width = Mathf.Max(32, serializedFont.FindProperty("m_AtlasWidth").intValue);
                var height = Mathf.Max(32, serializedFont.FindProperty("m_AtlasHeight").intValue);
                var atlas = new Texture2D(width, height, TextureFormat.Alpha8, mipChain: false, linear: true)
                {
                    name = font.name + " Atlas 0",
                };
                atlas.Apply(updateMipmaps: false, makeNoLongerReadable: true);

                AssetDatabase.AddObjectToAsset(atlas, font);
                firstAtlas.objectReferenceValue = atlas;

                var clearDynamicDataOnBuild = serializedFont.FindProperty("m_ClearDynamicDataOnBuild");
                if (clearDynamicDataOnBuild != null)
                {
                    clearDynamicDataOnBuild.boolValue = false;
                }

                serializedFont.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(font);
                repaired++;
            }

            if (repaired > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Repaired TMP font atlas references for submission build: " + repaired);
            }
        }

        private static string GetDefaultOutputDirectory()
        {
            return Path.Combine(
                Directory.GetCurrentDirectory(),
                DefaultOutputRoot,
                "CorridorCommander_Submission_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        }

        private static string GetCommandLineOutputDirectory()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-submissionOutput")
                {
                    return args[i + 1];
                }
            }

            return GetDefaultOutputDirectory();
        }

        private static void WriteReadme(string outputDirectory)
        {
            var text = string.Join(
                Environment.NewLine,
                "Corridor Commander 조작법",
                "",
                "기본 조작",
                "- 이동: WASD",
                "- 시점/조준: 마우스",
                "- 상호작용/설치 메뉴 열기: E",
                "- 선택/확인: 마우스 왼쪽 버튼",
                "- 취소/뒤로가기: ESC",
                "- 일시정지: ESC",
                "",
                "게임 흐름",
                "- 로비 화면에서 메인 게임 또는 튜토리얼을 선택합니다.",
                "- 설치 포인트에 가까이 가서 E로 설치 메뉴를 열고 방어 시설을 배치합니다.",
                "- 포탑, 박격포, 바리케이드를 이용해 적 웨이브를 막습니다.",
                "- 설치된 방어 시설은 상황에 따라 강화, 수리, 철거할 수 있습니다.",
                "- 문이 열리면 다음 구역으로 이동해 진행합니다.",
                "",
                "제출 빌드 포함 씬",
                "- StartMenu: 로비/시작 화면",
                "- MainScene: 메인 게임",
                "- TutorialMap: 튜토리얼",
                "");

            File.WriteAllText(Path.Combine(outputDirectory, ReadmeName), text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }
    }
}
