#if HDRP_PRESENT

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A CustomPass for creating object instance segmentation images. GameObjects containing Labeling components
    /// are assigned unique IDs, which are rendered into the target texture.
    /// </summary>
    public class InstanceSegmentationPass : GroundTruthPass
    {
        InstanceSegmentationCrossPipelinePass m_InstanceSegmentationCrossPipelinePass;

        [UsedImplicitly]
        public InstanceSegmentationPass()
        {}

        public void EnsureInit()
        {
            if (m_InstanceSegmentationCrossPipelinePass == null)
            {
                m_InstanceSegmentationCrossPipelinePass = new InstanceSegmentationCrossPipelinePass();
                this.Init(m_InstanceSegmentationCrossPipelinePass);
            }
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            base.Setup(renderContext, cmd);
            Debug.Assert(m_InstanceSegmentationCrossPipelinePass != null, "InstanceSegmentationPass.EnsureInit() should be called before the first camera render to get proper object labels in the first frame");
            EnsureInit();
        }
    }
}
#endif
