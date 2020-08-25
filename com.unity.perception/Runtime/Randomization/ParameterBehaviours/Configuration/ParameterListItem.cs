using System;
using UnityEngine.Perception.Randomization.Parameters;

namespace UnityEngine.Perception.Randomization.ParameterBehaviours
{
    [Serializable]
    class ParameterListItem
    {
        public string name = "Parameter";
        [SerializeReference] public Parameter parameter;
        [HideInInspector, SerializeField] public ParameterTarget target = new ParameterTarget();

        public bool hasTarget => target.component != null;

        public void ApplyToTarget()
        {
            if (!hasTarget)
                return;
            target.ApplyValueToTarget(parameter.GenericSample());
        }

        public void Validate()
        {
            target.Validate();
            parameter.Validate();
        }
    }
}
