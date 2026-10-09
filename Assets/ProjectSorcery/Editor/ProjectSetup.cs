using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectSorcery.EditorTools
{
    /// <summary>
    /// First-open setup: creates the boot scene, registers it in Build Settings, fills in player settings
    /// and makes sure both input backends are enabled (gamepads need the Input System).
    /// Re-run any time from the menu: Project Sorcery > Setup Project.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/ProjectSorcery/Scenes/Main.unity";
        const string DoneKey = "ProjectSorcery.SetupDone.v1";

        static ProjectSetup()
        {
            if (Application.isBatchMode) return; // command-line builds use BuildScript instead
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(DoneKey, false))
                {
                    SessionState.SetBool(DoneKey, true);
                    Setup(false);
                }
            };
        }

        [MenuItem("Project Sorcery/Setup Project")]
        public static void SetupMenu() => Setup(true);

        static void Setup(bool verbose)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // 1. boot scene (empty: the game builds itself at runtime)
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
                Debug.Log("[Project Sorcery] Created boot scene at " + ScenePath);
            }

            // 2. build settings
            var scenes = EditorBuildSettings.scenes;
            bool present = false;
            foreach (var s in scenes) if (s.path == ScenePath) present = true;
            if (!present)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
                list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = list.ToArray();
            }

            // 3. player settings
            PlayerSettings.productName = "Project Sorcery";
            if (PlayerSettings.companyName == "DefaultCompany" || string.IsNullOrEmpty(PlayerSettings.companyName)) PlayerSettings.companyName = "jajaa17";
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1" || PlayerSettings.bundleVersion == "1.0") PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;

            // 4. input backends: enable "Both" so keyboards work either way and gamepads work through the Input System
            bool needsRestart = false;
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets != null && assets.Length > 0)
            {
                var so = new SerializedObject(assets[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null && prop.intValue == 0)
                {
                    prop.intValue = 2;
                    so.ApplyModifiedProperties();
                    needsRestart = true;
                }
            }

            // 5. open the boot scene if nothing meaningful is open
            var active = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path) || active.path != ScenePath)
            {
                if (string.IsNullOrEmpty(active.path) || active.rootCount <= 2) EditorSceneManager.OpenScene(ScenePath);
            }

            AssetDatabase.SaveAssets();
            if (needsRestart)
                EditorUtility.DisplayDialog("Project Sorcery", "Input handling was set to 'Both' so gamepads work.\nPlease restart the Unity editor once for it to take effect.", "OK");
            else if (verbose)
                EditorUtility.DisplayDialog("Project Sorcery", "Setup complete. Press Play in the Main scene.", "OK");
        }
    }
}
