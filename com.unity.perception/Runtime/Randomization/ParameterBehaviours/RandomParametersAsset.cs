using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;

namespace Randomization.ParameterBehaviours
{
    public abstract class RandomParametersAsset : ScriptableObject
    {
        public abstract IEnumerable<Parameter> parameters { get; }

        /// <summary>
        /// Reset to default values in the Editor
        /// </summary>
        void Reset()
        {
            foreach (var parameter in parameters)
            foreach (var sampler in parameter.samplers)
                sampler.baseSeed = SamplerUtility.GenerateRandomSeed();
        }
    }
}
