using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;

namespace Randomization.ParameterBehaviours
{
    public abstract class ParameterAsset : ScriptableObject
    {
        internal IEnumerable<Parameter> parameters
        {
            get
            {
                var fields = GetType().GetFields();
                foreach (var field in fields)
                {
                    if (!field.IsPublic || !field.FieldType.IsSubclassOf(typeof(Parameter)))
                        continue;
                    var parameter = (Parameter)field.GetValue(this);
                    if (parameter == null)
                    {
                        parameter = (Parameter)Activator.CreateInstance(field.FieldType);
                        field.SetValue(this, parameter);
                    }
                    yield return parameter;
                }
            }
        }

        /// <summary>
        /// Reset to default values in the Editor
        /// </summary>
        public virtual void Reset()
        {
            foreach (var parameter in parameters)
            foreach (var sampler in parameter.samplers)
                sampler.baseSeed = SamplerUtility.GenerateRandomSeed();
        }
    }
}
