using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;

namespace UnityEngine.Perception.Randomization.Configuration
{
    /// <summary>
    /// Creates parameter interfaces for randomizing simulations
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Perception/Randomization/ParameterConfiguration")]
    public class ParameterConfiguration : MonoBehaviour
    {
        internal static HashSet<ParameterConfiguration> configurations = new HashSet<ParameterConfiguration>();
        [SerializeReference] internal List<ConfiguredParameter> parameters = new List<ConfiguredParameter>();

        /// <summary>
        /// Find a parameter in this configuration by name
        /// </summary>
        /// <param name="parameterName">The name of the parameter to lookup</param>
        /// <param name="parameterType">The type of parameter to lookup</param>
        /// <returns>The parameter if found, null otherwise</returns>
        /// <exception cref="ParameterConfigurationException"></exception>
        public Parameter GetParameter(string parameterName, Type parameterType)
        {
            foreach (var configParameter in parameters)
            {
                if (configParameter.name == parameterName && configParameter.parameter.GetType() ==  parameterType)
                    return configParameter.parameter;
            }
            return null;
        }

        /// <summary>
        /// Find a parameter in this configuration by name and type
        /// </summary>
        /// <param name="parameterName"></param>
        /// <typeparam name="T">The type of parameter to look for</typeparam>
        /// <returns>The parameter if found, null otherwise</returns>
        public T GetParameter<T>(string parameterName) where T : Parameter
        {
            foreach (var parameter in parameters)
            {
                if (parameter.name == parameterName && parameter is T typedParameter)
                    return typedParameter;
            }
            return null;
        }

        internal ConfiguredParameter AddParameter<T>(string parameterName) where T : Parameter, new()
        {
            var parameter = new T();
            var configParameter = new ConfiguredParameter { name = parameterName, parameter = parameter };
            parameters.Add(configParameter);
            return configParameter;
        }

        internal ConfiguredParameter AddParameter(string parameterName, Type parameterType)
        {
            if (!parameterType.IsSubclassOf(typeof(Parameter)))
                throw new ParameterConfigurationException($"Cannot add non-parameter types ({parameterType})");
            var parameter = (Parameter)Activator.CreateInstance(parameterType);
            var configParameter = new ConfiguredParameter { name = parameterName, parameter = parameter };
            parameters.Add(configParameter);
            return configParameter;
        }

        internal void ApplyParameters(ParameterApplicationFrequency frequency)
        {
            foreach (var configParameter in parameters)
                if (configParameter.target.applicationFrequency == frequency)
                    configParameter.ApplyToTarget();
        }

        internal void ResetParameterStates(int scenarioIteration)
        {
            foreach (var configParameter in parameters)
                configParameter.parameter.ResetState(scenarioIteration);
        }

        internal void ValidateParameters()
        {
            var parameterNames = new HashSet<string>();
            foreach (var configParameter in parameters)
            {
                if (parameterNames.Contains(configParameter.name))
                    throw new ParameterConfigurationException(
                        $"Two or more parameters cannot share the same name (\"{configParameter.name}\")");
                parameterNames.Add(configParameter.name);
                configParameter.parameter.Validate();
            }
        }

        void OnEnable()
        {
            configurations.Add(this);
        }

        void OnDisable()
        {
            configurations.Remove(this);
        }
    }
}
