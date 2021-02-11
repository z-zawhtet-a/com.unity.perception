#if HDRP_PRESENT

using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Custom Pass which renders labeled images where each object with a Labeling component is drawn with the value
    /// specified by the given LabelingConfiguration.
    /// </summary>
    public class SemanticSegmentationPass : GroundTruthPass
    {
        public SemanticSegmentationLabelConfig semanticSegmentationLabelConfig;

        SemanticSegmentationCrossPipelinePass m_SemanticSegmentationCrossPipelinePass;

        public SemanticSegmentationPass(SemanticSegmentationLabelConfig semanticSegmentationLabelConfig)
        {
            this.semanticSegmentationLabelConfig = semanticSegmentationLabelConfig;
            EnsureInit();
        }

        void EnsureInit()
        {
            if (m_SemanticSegmentationCrossPipelinePass == null)
            {
                m_SemanticSegmentationCrossPipelinePass = new SemanticSegmentationCrossPipelinePass(semanticSegmentationLabelConfig);
                this.Init(m_SemanticSegmentationCrossPipelinePass);
            }
        }

        public SemanticSegmentationPass()
        {
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            EnsureInit();
            m_SemanticSegmentationCrossPipelinePass.Setup();
        }
    }
}
#endif
