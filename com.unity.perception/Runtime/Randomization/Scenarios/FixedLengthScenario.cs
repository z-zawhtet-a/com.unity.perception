using System;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// A scenario that runs for a fixed number of frames during each iteration
    /// </summary>
    [AddComponentMenu("Perception/Randomization/Scenarios/Fixed Length Scenario")]
    public class FixedLengthScenario : USimScenario
    {
        public int framesPerIteration = 1;
        public int startingIteration;

        /// <summary>
        /// Returns whether the current scenario iteration has completed
        /// </summary>
        public override bool isIterationComplete => currentIterationFrame >= framesPerIteration;

        /// <summary>
        /// Returns whether the scenario has completed
        /// </summary>
        public override bool isScenarioComplete => currentIteration >= constants.totalIterations;

        /// <summary>
        /// Called before the scenario begins iterating
        /// </summary>
        public override void OnInitialize()
        {
#if UNITY_EDITOR
            currentIteration = startingIteration;
#else
            base.OnInitialize();
#endif
        }
    }
}
