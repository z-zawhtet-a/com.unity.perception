using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using ZipUtility;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class RunInUSimWindow : EditorWindow
    {
        SceneAsset m_MainScene;

        [MenuItem("Window/Run in USim")]
        public static void ShowWindow()
        {
            var window = GetWindow<RunInUSimWindow>();
            window.titleContent = new GUIContent("Run In Unity Simulation");
            window.minSize = new Vector2(250, 50);
            window.Show();
        }

        void OnEnable()
        {
            var root = rootVisualElement;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/RunInUSimWindow.uxml").CloneTree(root);
            // root.Bind(this);
        }

        bool CreateLinuxBuildAndZip(string buildName)
        {
            var pathToZip = string.Empty;
            var pathToProjectBuild = Application.dataPath + "/../" + "Build/";
            if (!Directory.Exists(pathToProjectBuild + buildName))
                Directory.CreateDirectory(pathToProjectBuild + buildName);

            pathToProjectBuild = pathToProjectBuild + buildName + "/";

            // Create Linux build
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainScene.unity" },
                locationPathName = Path.Combine(pathToProjectBuild,buildName + ".x86_64"),
                target = BuildTarget.StandaloneLinux64
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded.");
                EditorUtility.DisplayProgressBar("Compress Project", "Compressing Project Build...", 0);
                ulong totalSize = summary.totalSize;

                // Zip the build
                pathToZip = Application.dataPath + "/../" + "Build/" + buildName;

                Zip.DirectoryContents(pathToZip, buildName);

                EditorUtility.ClearProgressBar();

                // Return path to .zip file
                string[] st = Directory.GetFiles(pathToZip + "/../", buildName + ".zip");
                if (st.Length != 0)
                    pathToZip = Path.GetFullPath(st[0]);
                else
                    return false;
            }
            else
            {
                // m_BuildZipPathField.value = null;
                return false;
            }
            // m_BuildZipPathField.value = pathToZip;

            return true;
        }
    }
}
