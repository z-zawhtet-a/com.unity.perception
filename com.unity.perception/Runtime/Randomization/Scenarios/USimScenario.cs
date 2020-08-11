using System;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    public abstract class USimScenario : Scenario<USimConstants>
    {
        public override bool isScenarioComplete => currentIteration >= constants.totalIterations;

        public override void OnInitialize()
        {
            currentIteration = constants.instanceIndex;
        }

        public override void IncrementIteration()
        {
            currentIteration += constants.instanceCount;
        }

        public override void OnFrameStart()
        {
            Debug.Log($"{currentIteration}: {currentIterationFrame}");
        }

        public override void Deserialize()
        {
            if (!string.IsNullOrEmpty(Unity.Simulation.Configuration.Instance.SimulationConfig.app_param_uri))
            {
                Debug.Log("Reading app-params");
                constants = Unity.Simulation.Configuration.Instance.GetAppParams<USimConstants>();
            }
            else
                base.Deserialize();
        }
    }

    [Serializable]
    public struct USimConstants
    {
        public int totalIterations;
        public int instanceCount;
        public int instanceIndex;
    }
}
