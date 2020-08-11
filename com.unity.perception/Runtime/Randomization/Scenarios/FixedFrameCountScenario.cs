using System;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// An example scenario where each iteration runs for a fixed number of frames
    /// </summary>
    [AddComponentMenu("Randomization/Scenarios/Fixed Frame Count Scenario")]
    public class FixedFrameCountScenario : USimScenario
    {
        public int framesPerIteration;

        public override bool isIterationComplete => currentIterationFrame >= framesPerIteration;

        public FixedFrameCountScenario()
        {
            constants = new USimConstants
            {
                instanceCount = 1,
                instanceIndex = 0,
                totalIterations = 1000
            };
        }

        public override void OnInitialize()
        {
            currentIteration = constants.instanceIndex;
        }
    }
}
