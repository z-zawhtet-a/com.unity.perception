using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;

namespace Randomization.ParameterBehaviours
{
    public abstract class ParameterAsset : ScriptableObject
    {
        public abstract IEnumerable<Parameter> parameters { get; }

        /// <summary>
        /// Reset to default values in the Editor
        /// </summary>
        void Reset()
        {
            var fields = GetType().GetFields();
            foreach (var field in fields)
            {
                if (!field.IsPublic || !field.FieldType.IsSubclassOf(typeof(Parameter)))
                    continue;
                var parameter = (Parameter)field.GetValue(this);
                if (parameter == null)
                    field.SetValue(this, Activator.CreateInstance(field.FieldType));
            }

            foreach (var parameter in parameters)
            foreach (var sampler in parameter.samplers)
                sampler.baseSeed = SamplerUtility.GenerateRandomSeed();
        }
    }
}
