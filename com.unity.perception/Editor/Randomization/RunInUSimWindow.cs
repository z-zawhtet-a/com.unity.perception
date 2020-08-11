using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Boo.Lang.Runtime;
using Unity.Simulation.Client;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.UIElements;
using ZipUtility;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class RunInUSimWindow : EditorWindow
    {
        SceneAsset m_MainScene;
        USimScenario m_Scenario;
        SysParamDefinition m_SysParam;
        string m_RunName;
        string m_BuildZipPath;
        int m_TotalIterations;
        int m_InstanceCount;

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
            Project.Activate();
            Project.clientReadyStateChanged += CreateEstablishingConnectionUI;
            CreateEstablishingConnectionUI(Project.projectIdState);
        }

        void CreateEstablishingConnectionUI(Project.State state)
        {
            rootVisualElement.Clear();
            if (Project.projectIdState == Project.State.Pending)
            {
                var waitingText = new TextElement();
                waitingText.text = "Waiting for connection to Unity Cloud...";
                rootVisualElement.Add(waitingText);
            }
            else if (Project.projectIdState == Project.State.Invalid)
            {
                var waitingText = new TextElement();
                waitingText.text = "The current project must be associated with a valid Unity Cloud project " +
                    "to run in Unity Simulation";
                rootVisualElement.Add(waitingText);
            }
            else
            {
                CreateRunInUSimUI();
            }
        }

        /// <summary>
        /// Enables a visual element to remember values between editor sessions
        /// </summary>
        static void SetViewDataKey(VisualElement element)
        {
            element.viewDataKey = $"RunInUSim_{element.name}";
        }

        void CreateRunInUSimUI()
        {
            var root = rootVisualElement;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/RunInUSimWindow.uxml").CloneTree(root);

            var runNameField = root.Q<TextField>("run-name");
            runNameField.RegisterCallback<ChangeEvent<string>>(evt => { m_RunName = evt.newValue; });
            SetViewDataKey(runNameField);

            var totalIterationsField = root.Q<IntegerField>("total-iterations");
            totalIterationsField.RegisterCallback<ChangeEvent<int>>(evt => { m_TotalIterations = evt.newValue; });
            SetViewDataKey(totalIterationsField);

            var instanceCountField = root.Q<IntegerField>("instance-count");
            instanceCountField.RegisterCallback<ChangeEvent<int>>(evt => { m_InstanceCount = evt.newValue; });
            instanceCountField.viewDataKey = "RunInUSim_instanceCount";
            SetViewDataKey(instanceCountField);

            var mainSceneField = root.Q<ObjectField>("main-scene");
            mainSceneField.objectType = typeof(SceneAsset);
            mainSceneField.RegisterCallback<ChangeEvent<Object>>(evt => { m_MainScene = (SceneAsset)evt.newValue; });

            var scenarioField = root.Q<ObjectField>("scenario");
            scenarioField.objectType = typeof(ScenarioBase);
            scenarioField.RegisterCallback<ChangeEvent<Object>>(evt => { m_Scenario = (USimScenario)evt.newValue; });

            var sysParamDefinitions = API.GetSysParams();
            var sysParamMenu = root.Q<ToolbarMenu>("sys-param");
            foreach (var definition in sysParamDefinitions)
                sysParamMenu.menu.AppendAction(definition.description, action => m_SysParam = definition);
            sysParamMenu.text = sysParamDefinitions[0].description;
            m_SysParam = sysParamDefinitions[0];

            var runButton = root.Q<Button>("run-button");
            runButton.clicked += RunInUSim;
        }

        void RunInUSim()
        {
            ValidateSettings();
            CreateLinuxBuildAndZip();
            StartUSimRun();
        }

        void ValidateSettings()
        {
            if (string.IsNullOrEmpty(m_RunName))
                throw new RuntimeException("Invalid run name");
            if (m_Scenario == null)
                throw new RuntimeException("Null scenario");
            if (m_MainScene == null)
                throw new RankException("Null main scene");
        }

        void CreateLinuxBuildAndZip()
        {
            // Create build directory
            var pathToProjectBuild = Application.dataPath + "/../" + "Build/";
            if (!Directory.Exists(pathToProjectBuild + m_RunName))
                Directory.CreateDirectory(pathToProjectBuild + m_RunName);

            pathToProjectBuild = pathToProjectBuild + m_RunName + "/";

            // Create Linux build
            Debug.Log("Creating Linux build...");
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { AssetDatabase.GetAssetPath(m_MainScene) },
                locationPathName = Path.Combine(pathToProjectBuild, m_RunName + ".x86_64"),
                target = BuildTarget.StandaloneLinux64
            };
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new RuntimeException($"Build did not succeed: status = {summary.result}");
            Debug.Log("Created Linux build");

            // Zip the build
            Debug.Log("Starting to zip...");
            var buildFolder = Application.dataPath + "/../" + "Build/" + m_RunName;
            Zip.DirectoryContents(buildFolder, m_RunName);
            m_BuildZipPath = buildFolder + ".zip";
            Debug.Log("Created build zip");
        }

        List<AppParam> GenerateAppParamIds(CancellationToken token)
        {
            var appParamIds = new List<AppParam>();
            for (var i = 0; i < m_InstanceCount; i++)
            {
                if (token.IsCancellationRequested)
                    return null;
                var appParamName = $"{m_RunName}_{i}";
                var appParamId = API.UploadAppParam(appParamName, new USimConstants
                {
                    totalIterations = m_TotalIterations,
                    instanceCount = m_InstanceCount,
                    instanceIndex = i
                });
                appParamIds.Add(new AppParam()
                {
                    id = appParamId,
                    name = appParamName,
                    num_instances = 1
                });
            }
            return appParamIds;
        }

        async void StartUSimRun()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            Debug.Log("Uploading build...");
            var buildId = await API.UploadBuildAsync(
                m_RunName,
                m_BuildZipPath,
                cancellationTokenSource: cancellationTokenSource);

            Debug.Log($"Build upload complete: build id {buildId}");

            var appParams = GenerateAppParamIds(token);
            if (token.IsCancellationRequested)
                return;
            Debug.Log($"Generated app-param ids: {appParams.Count}");

            var runDefinitionId = API.UploadRunDefinition(new RunDefinition
            {
                app_params = appParams.ToArray(),
                name = m_RunName,
                sys_param_id = m_SysParam.id,
                build_id = buildId
            });
            Debug.Log($"Run definition upload complete: run definition id {runDefinitionId}");

            var run = Run.CreateFromDefinitionId(runDefinitionId);
            run.Execute();
            cancellationTokenSource.Dispose();
            Debug.Log("Executed run");
        }
    }
}
