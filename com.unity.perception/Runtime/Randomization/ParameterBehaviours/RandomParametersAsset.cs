using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;

namespace Randomization.ParameterBehaviours
{
    public abstract class RandomParametersAsset : ScriptableObject
    {
        public abstract IEnumerable<Parameter> parameters { get; }
    }
}
