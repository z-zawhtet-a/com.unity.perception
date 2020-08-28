using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios;

namespace Randomization.ParameterBehaviours
{
    /// <summary>
    /// The base class for all randomization scripts
    /// </summary>
    public abstract class ParameterBehaviour : MonoBehaviour
    {
        static ParameterBehaviour s_ActiveBehaviour;
        internal static ParameterBehaviour activeBehaviour
        {
            get => s_ActiveBehaviour;
            private set
            {
                if (value != null && s_ActiveBehaviour != null && value != s_ActiveBehaviour)
                    throw new Exception("There cannot be more than one active Scenario");
                s_ActiveBehaviour = value;
            }
        }

        /// <summary>
        /// The parameters employed by this parameter behaviour
        /// </summary>
        public abstract IEnumerable<Parameter> parameters { get; }

        /// <summary>
        /// This method is called when the ParameterBehaviour is enabled
        /// </summary>
        protected void OnEnable()
        {
            activeBehaviour = this;
            ResetParameterRandomStates();
        }

        /// <summary>
        /// This method is called when the ParameterBehaviour is disabled
        /// </summary>
        protected void OnDisable()
        {
            activeBehaviour = null;
        }

        /// <summary>
        /// OnFrameStart is called at the start of every frame if the ParameterBehaviour is enabled
        /// </summary>
        public virtual void OnFrameStart() { }

        /// <summary>
        /// OnIterationStart is called at the start of every iteration if the ParameterBehaviour is enabled
        /// </summary>
        public virtual void OnIterationStart() { }

        /// <summary>
        /// OnIterationEnd is called at the end of every iteration if the ParameterBehaviour is enabled
        /// </summary>
        public virtual void OnIterationEnd() { }

        /// <summary>
        /// Run when the scenario completes
        /// </summary>
        public virtual void OnScenarioComplete() {}

        /// <summary>
        /// Validate all parameters employed by this ParameterBehaviour
        /// </summary>
        public virtual void Validate()
        {
            foreach (var parameter in parameters)
                parameter.Validate();
        }

        /// <summary>
        /// Reset to default values in the Editor
        /// </summary>
        protected virtual void Reset()
        {
            foreach (var parameter in parameters)
            foreach (var sampler in parameter.samplers)
                sampler.baseSeed = SamplerUtility.GenerateRandomSeed();
        }

        /// <summary>
        /// Resets the state of each sampler on every parameter used by this ParameterBehaviour
        /// </summary>
        internal void ResetParameterRandomStates()
        {
            foreach (var parameter in parameters)
            {
                parameter.ResetState();
                parameter.IterateState(ScenarioBase.ActiveScenario.currentIteration);
                parameter.IterateState(GetInstanceID());
            }
        }
    }
}
