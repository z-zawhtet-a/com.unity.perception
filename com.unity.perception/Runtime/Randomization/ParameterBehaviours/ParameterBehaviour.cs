using System;
using System.Collections.Generic;
using System.Linq;
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
        static HashSet<ParameterBehaviour> s_ActiveBehaviours = new HashSet<ParameterBehaviour>();
        static Queue<ParameterBehaviour> s_PendingBehaviours = new Queue<ParameterBehaviour>();

        internal static IEnumerable<ParameterBehaviour> behaviours
        {
            get
            {
                var currentBehaviours = s_ActiveBehaviours.ToArray();
                foreach (var behaviour in currentBehaviours)
                    if (s_ActiveBehaviours.Contains(behaviour))
                        yield return behaviour;
                while (s_PendingBehaviours.Count > 0)
                {
                    var behaviour = s_PendingBehaviours.Dequeue();
                    if (!s_ActiveBehaviours.Contains(behaviour))
                    {
                        s_ActiveBehaviours.Add(behaviour);
                        yield return behaviour;
                    }
                }
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
            s_PendingBehaviours.Enqueue(this);
            ResetState();
        }

        /// <summary>
        /// This method is called when the ParameterBehaviour is disabled
        /// </summary>
        protected void OnDisable()
        {
            s_PendingBehaviours.Enqueue(this);
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
        internal void ResetState()
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
