using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;

namespace Randomization.ParameterBehaviours
{
    public abstract class ParameterBehaviour : MonoBehaviour
    {
        internal static HashSet<ParameterBehaviour> activeBehaviours = new HashSet<ParameterBehaviour>();

        public abstract IEnumerable<Parameter> parameters { get; }

        void Awake()
        {
            activeBehaviours.Add(this);
            OnInitialize();
        }

        void OnDestroy()
        {
            activeBehaviours.Remove(this);
        }

        protected virtual void OnInitialize() { }

        public virtual void OnFrameStart() { }

        public virtual void OnIterationStart() { }

        public virtual void OnIterationEnd() { }

        public virtual void OnScenarioComplete() {}

        public virtual void Validate() { }

        internal void ResetState(int scenarioIteration)
        {
            foreach (var parameter in parameters)
                parameter.ResetState(scenarioIteration);
        }

        public virtual void Reset()
        {
            foreach (var parameter in parameters)
                foreach (var sampler in parameter.samplers)
                    sampler.baseSeed = SamplerUtility.GenerateRandomSeed();
        }
    }
}
