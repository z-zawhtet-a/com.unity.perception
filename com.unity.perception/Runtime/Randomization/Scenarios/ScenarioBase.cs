using System;
using System.Linq;
using Randomization.ParameterBehaviours;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// The base class of all scenario classes
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public abstract class ScenarioBase : MonoBehaviour
    {
        static ScenarioBase s_ActiveScenario;
        bool m_SkipFrame = true;
        bool m_FirstScenarioFrame = true;
        bool m_WaitingForFinalUploads;

        [SerializeReference] public RandomParametersAsset[] parameters;

        /// <summary>
        /// If true, this scenario will quit the Unity application when it's finished executing
        /// </summary>
        [HideInInspector] public bool quitOnComplete = true;

        /// <summary>
        /// When true, this scenario will deserializes constants from a Json file before it begins executing
        /// </summary>
        [HideInInspector] public bool deserializeOnStart;

        /// <summary>
        /// The name of the Json file this scenario's constants are serialized to/from.
        /// </summary>
        [HideInInspector] public string serializedConstantsFileName = "constants";

        /// <summary>
        /// Returns the active parameter scenario in the scene
        /// </summary>
        public static ScenarioBase ActiveScenario
        {
            get => s_ActiveScenario;
            private set
            {
                if (value != null && s_ActiveScenario != null && value != s_ActiveScenario)
                    throw new ScenarioException("There cannot be more than one active Scenario");
                s_ActiveScenario = value;
            }
        }

        /// <summary>
        /// Returns the file location of the JSON serialized constants
        /// </summary>
        public string serializedConstantsFilePath =>
            Application.dataPath + "/StreamingAssets/" + serializedConstantsFileName + ".json";

        /// <summary>
        /// The number of frames that have elapsed since the current scenario iteration was Setup
        /// </summary>
        public int currentIterationFrame { get; private set; }

        /// <summary>
        /// The number of frames that have elapsed since the scenario was initialized
        /// </summary>
        public int framesSinceInitialization { get; private set; }

        /// <summary>
        /// The current iteration index of the scenario
        /// </summary>
        public int currentIteration { get; protected set; }

        /// <summary>
        /// Returns whether the current scenario iteration has completed
        /// </summary>
        public abstract bool isIterationComplete { get; }

        /// <summary>
        /// Returns whether the entire scenario has completed
        /// </summary>
        public abstract bool isScenarioComplete { get; }

        /// <summary>
        /// Serializes the scenario's constants to a JSON file located at serializedConstantsFilePath
        /// </summary>
        public abstract void Serialize();

        /// <summary>
        /// Deserializes constants saved in a JSON file located at serializedConstantsFilePath
        /// </summary>
        public abstract void Deserialize();

        void Awake()
        {
            ActiveScenario = this;
        }

        void OnEnable()
        {
            ActiveScenario = this;
        }

        void OnDisable()
        {
            s_ActiveScenario = null;
        }

        void Start()
        {
            if (deserializeOnStart)
                Deserialize();
            foreach (var behaviour in ParameterBehaviour.behaviours)
                behaviour.Validate();
        }

        void Update()
        {
            // TODO: remove this check when the perception camera can capture the first frame of output
            if (m_SkipFrame)
            {
                m_SkipFrame = false;
                return;
            }

            // Wait for any final uploads before exiting quitting
            if (m_WaitingForFinalUploads)
            {
                if (!Manager.FinalUploadsDone)
                    return;

                if (quitOnComplete)
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.ExitPlaymode();
#else
                    Application.Quit();
#endif
                return;
            }

            // Iterate Scenario
            if (m_FirstScenarioFrame)
            {
                m_FirstScenarioFrame = false;
            }
            else
            {
                currentIterationFrame++;
                framesSinceInitialization++;
                if (isIterationComplete)
                {
                    currentIteration++;
                    currentIterationFrame = 0;
                    foreach (var behaviour in ParameterBehaviour.behaviours)
                        behaviour.OnIterationEnd();
                }
            }

            // Quit if scenario is complete
            if (isScenarioComplete)
            {
                foreach (var behaviour in ParameterBehaviour.behaviours)
                    behaviour.OnScenarioComplete();
                Manager.Instance.Shutdown();
                DatasetCapture.ResetSimulation();
                m_WaitingForFinalUploads = true;
                return;
            }

            // Perform new iteration tasks
            if (currentIterationFrame == 0)
            {
                DatasetCapture.StartNewSequence();
                foreach (var behaviour in ParameterBehaviour.behaviours)
                    behaviour.ResetState();
                foreach (var behaviour in ParameterBehaviour.behaviours)
                    behaviour.OnIterationStart();
            }

            // Perform new frame tasks
            foreach (var behaviour in ParameterBehaviour.behaviours)
                behaviour.OnFrameStart();
        }
    }
}
