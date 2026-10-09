using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectSorcery.EditorTools
{
    /// <summary>
    /// Command-line / CI build entry point (used by .github/workflows/build.yml).
    /// Unity -batchmode -quit -projectPath . -executeMethod ProjectSorcery.EditorTools.BuildScript.Build
    ///       -buildTarget StandaloneWindows64 -customBuildPath build/Windows/ProjectSorcery.exe
    /// </summary>
    public static class BuildScript
    {
        const string ScenePath = "Assets/ProjectSorcery/Scenes/Main.unity";

        public static void Build()
        {
            string path = Arg("-customBuildPath") ?? "build/ProjectSorcery";
            var target = EditorUserBuildSettings.activeBuildTarget;
            string t = Arg("-buildTarget");
            if (!string.IsNullOrEmpty(t) && Enum.TryParse(t, out BuildTarget parsed)) target = parsed;
            if (target == BuildTarget.StandaloneWindows64 && !path.EndsWith(".exe")) path += ".exe";
            if (target == BuildTarget.StandaloneOSX && !path.EndsWith(".app")) path += ".app";

            EnsureScene();
            PlayerSettings.productName = "Project Sorcery";
            PlayerSettings.companyName = "jajaa17";
            string version = Arg("-buildVersion");
            PlayerSettings.bundleVersion = string.IsNullOrEmpty(version) || version == "none" ? "1.0.0" : version;
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            EnableBothInputBackends();

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            });

            var s = report.summary;
            Debug.Log($"[Project Sorcery] Build {s.result}: {s.outputPath} ({s.totalSize / (1024 * 1024)} MB, {s.totalErrors} errors)");
            if (s.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw new Exception("Build failed: " + s.result);
            }
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void EnableBothInputBackends()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null && prop.intValue != 2) { prop.intValue = 2; so.ApplyModifiedProperties(); }
            AssetDatabase.SaveAssets();
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
