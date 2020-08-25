using System;

namespace UnityEngine.Perception.Randomization.ParameterBehaviours
{
    [Serializable]
    class ParameterListException : Exception
    {
        public ParameterListException(string message) : base(message) { }
        public ParameterListException(string message, Exception innerException) : base(message, innerException) { }
    }
}
