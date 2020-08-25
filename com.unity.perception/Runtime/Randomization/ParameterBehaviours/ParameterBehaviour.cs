using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios;

namespace Randomization.ParameterBehaviours
{
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

        public abstract IEnumerable<Parameter> parameters { get; }

        protected virtual void OnEnable()
        {
            s_PendingBehaviours.Enqueue(this);
            ResetState();
            OnInitialize();
        }

        protected virtual void OnDisable()
        {
            s_PendingBehaviours.Enqueue(this);
        }

        protected virtual void OnInitialize() { }

        public virtual void OnFrameStart() { }

        public virtual void OnIterationStart() { }

        public virtual void OnIterationEnd() { }

        public virtual void OnScenarioComplete() {}

        public virtual void Validate() { }

        internal void ResetState()
        {
            foreach (var parameter in parameters)
            {
                parameter.ResetState();
                parameter.IterateState(ScenarioBase.ActiveScenario.currentIteration);
                parameter.IterateState(GetInstanceID());
            }
        }

        public virtual void Reset()
        {
            foreach (var parameter in parameters)
                foreach (var sampler in parameter.samplers)
                    sampler.baseSeed = SamplerUtility.GenerateRandomSeed();
        }
    }
}
