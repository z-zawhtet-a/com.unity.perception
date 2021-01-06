﻿using System;

namespace UnityEngine.Experimental.Perception.Randomization.Parameters
{
    class ParameterValidationException : Exception
    {
        public ParameterValidationException(string msg) : base(msg) {}
        public ParameterValidationException(string msg, Exception innerException) : base(msg, innerException) {}
    }
}
