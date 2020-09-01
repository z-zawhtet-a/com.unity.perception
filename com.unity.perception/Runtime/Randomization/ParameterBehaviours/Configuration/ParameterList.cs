using System;
using System.Collections.Generic;
using Randomization.ParameterBehaviours;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;

namespace UnityEngine.Perception.Randomization.ParameterBehaviours
{
    /// <summary>
    /// Defines a list of parameters for randomizing simulations
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Perception/Randomization/Parameter List")]
    public class ParameterList : ParameterBehaviour
    {
        [SerializeReference] internal List<ParameterListItem> configuredParameters = new List<ParameterListItem>();

        // /// <summary>
        // /// The parameters contained within this ParameterList
        // /// </summary>
        // public override IEnumerable<Parameter> parameters
        // {
        //     get
        //     {
        //         foreach (var configParameter in configuredParameters)
        //             yield return configParameter.parameter;
        //     }
        // }

        internal ParameterListItem AddParameter<T>(string parameterName) where T : Parameter, new()
        {
            var parameter = new T();
            var configParameter = new ParameterListItem { name = parameterName, parameter = parameter };
            configuredParameters.Add(configParameter);
            return configParameter;
        }

        internal ParameterListItem AddParameter(string parameterName, Type parameterType)
        {
            if (!parameterType.IsSubclassOf(typeof(Parameter)))
                throw new ParameterListException($"Cannot add non-parameter types ({parameterType})");
            var parameter = (Parameter)Activator.CreateInstance(parameterType);
            var configParameter = new ParameterListItem { name = parameterName, parameter = parameter };
            configuredParameters.Add(configParameter);
            return configParameter;
        }

        /// <summary>
        /// Find a parameter in this configuration by name
        /// </summary>
        /// <param name="parameterName">The name of the parameter to lookup</param>
        /// <param name="parameterType">The type of parameter to lookup</param>
        /// <returns>The parameter if found, null otherwise</returns>
        /// <exception cref="ParameterListException"></exception>
        public Parameter GetParameter(string parameterName, Type parameterType)
        {
            foreach (var configParameter in configuredParameters)
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
            foreach (var configuredParameter in configuredParameters)
            {
                if (configuredParameter.name == parameterName && configuredParameter.parameter is T typedParameter)
                    return typedParameter;
            }
            return null;
        }

        /// <summary>
        /// Applies parameter samples to their targets at the start of each scenario iteration
        /// </summary>
        public override void OnIterationStart()
        {
            foreach (var configParameter in configuredParameters)
                if (configParameter.target.applicationFrequency == ParameterApplicationFrequency.EveryIteration)
                    configParameter.ApplyToTarget();
        }

        /// <summary>
        /// Applies parameter samples to their targets at the start of every frame
        /// </summary>
        public override void OnFrameStart()
        {
            foreach (var configParameter in configuredParameters)
                if (configParameter.target.applicationFrequency == ParameterApplicationFrequency.EveryFrame)
                    configParameter.ApplyToTarget();
        }

        // /// <summary>
        // /// Validates the settings of each parameter within this ParameterList
        // /// </summary>
        // /// <exception cref="ParameterListException"></exception>
        // public override void Validate()
        // {
        //     var parameterNames = new HashSet<string>();
        //     foreach (var configParameter in configuredParameters)
        //     {
        //         if (parameterNames.Contains(configParameter.name))
        //             throw new ParameterListException(
        //                 $"Two or more parameters cannot share the same name (\"{configParameter.name}\")");
        //         parameterNames.Add(configParameter.name);
        //         configParameter.Validate();
        //     }
        // }
    }
}
