using System;
using System.Collections.Generic;
using System.Linq;
using Randomization.ParameterBehaviours;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Parameters;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// The base class of all scenario classes
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public abstract class ScenarioBase : MonoBehaviour
    {
        static ScenarioBase s_ActiveScenario;

        List<ParameterBehaviour> m_Behaviours = new List<ParameterBehaviour>();
        List<Parameter> m_Parameters = new List<Parameter>();
        bool m_SkipFrame = true;
        bool m_FirstScenarioFrame = true;
        bool m_WaitingForFinalUploads;

        IEnumerable<ParameterBehaviour> m_ActiveBehaviours
        {
            get
            {
                foreach (var behaviour in m_Behaviours)
                    if (behaviour.enabled)
                        yield return behaviour;
            }
        }

        [SerializeReference] public ParameterAsset[] parameterAssets;

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
        public static ScenarioBase activeScenario
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

        /// <summary>
        /// This method executed directly after this scenario has been registered and initialized
        /// </summary>
        protected virtual void OnAwake() { }

        void Awake()
        {
            activeScenario = this;
            foreach (var asset in parameterAssets)
            {
                foreach (var parameter in asset.parameters)
                {
                    parameter.Validate();
                    m_Parameters.Add(parameter);
                }
            }
            OnAwake();
        }

        void OnEnable()
        {
            activeScenario = this;
        }

        void OnDisable()
        {
            s_ActiveScenario = null;
        }

        void Start()
        {
            if (deserializeOnStart)
                Deserialize();
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
                    foreach (var behaviour in m_ActiveBehaviours)
                        behaviour.OnIterationEnd();
                }
            }

            // Quit if scenario is complete
            if (isScenarioComplete)
            {
                foreach (var behaviour in m_ActiveBehaviours)
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
                foreach (var parameter in m_Parameters)
                {
                    parameter.ResetState();
                    parameter.IterateState(currentIteration);
                }
                foreach (var behaviour in m_ActiveBehaviours)
                    behaviour.OnIterationStart();
            }

            // Perform new frame tasks
            foreach (var behaviour in m_ActiveBehaviours)
                behaviour.OnFrameStart();
        }

        public T GetParameterAsset<T>() where T : ParameterAsset
        {
            foreach (var asset in parameterAssets)
                if (asset is T typedAsset)
                    return typedAsset;
            throw new ScenarioException($"A ParameterAsset of type {typeof(T).Name} was not added to this scenario");
        }

        public T GetParameterBehaviour<T>() where T : ParameterBehaviour
        {
            foreach (var behaviour in m_Behaviours)
                if (behaviour is T typedBehaviour)
                    return typedBehaviour;
            throw new ScenarioException($"A ParameterBehaviour of type {typeof(T).Name} was not added to this scenario");
        }

        internal void AddBehaviour<T>(T newBehaviour) where T : ParameterBehaviour
        {
            foreach (var behaviour in m_Behaviours)
                if (behaviour.GetType() == newBehaviour.GetType())
                    throw new ScenarioException(
                        $"Two ParameterBehaviours of the same type {typeof(T).Name} cannot both be active simultaneously");
            m_Behaviours.Add(newBehaviour);
            m_Behaviours.Sort((b1, b2) => b1.executionPriority.CompareTo(b2.executionPriority));
        }

        internal void RemoveBehaviour(ParameterBehaviour behaviour)
        {
            var removed = m_Behaviours.Remove(behaviour);
            if (!removed)
                throw new ScenarioException(
                    $"No active ParameterBehaviour of type {behaviour.GetType().Name} could be removed");
        }
    }
}
