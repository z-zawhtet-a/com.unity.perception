using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;

namespace Randomization.ParameterBehaviours
{
    /// <summary>
    /// The base class for all randomization scripts
    /// </summary>
    public abstract class ParameterBehaviour : MonoBehaviour
    {
        public virtual int executionPriority => 1;

        public void Awake()
        {
            ScenarioBase.activeScenario.AddBehaviour(this);
        }

        /// <summary>
        /// Included in the base ParameterBehaviour class to activate the enabled toggle in the inspector UI
        /// </summary>
        protected virtual void OnEnable() {}

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
    }
}
