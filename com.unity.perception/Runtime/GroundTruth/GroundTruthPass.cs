#if HDRP_PRESENT

using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Perception.GroundTruth
{
    public abstract class GroundTruthPass : CustomPass
    {
        private GroundTruthCrossPipelinePass m_GroundTruthCrossPipelinePass;
        Dictionary<Camera, RenderTexture> targets = new Dictionary<Camera, RenderTexture>();

        protected GroundTruthPass()
        {
        }

        internal void Init(GroundTruthCrossPipelinePass groundTruthCrossPipelinePass)
        {
            m_GroundTruthCrossPipelinePass = groundTruthCrossPipelinePass;
            m_GroundTruthCrossPipelinePass.Setup();
        }
        public void AddTarget(Camera camera, RenderTexture renderTexture)
        {
            if (targets == null)
                targets = new Dictionary<Camera, RenderTexture>();

            targets[camera] = renderTexture;
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            this.targetColorBuffer = TargetBuffer.Custom;
            this.targetDepthBuffer = TargetBuffer.Custom;
        }

        protected sealed override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
            RenderTexture targetTexture = null;
            if (targets != null && !targets.TryGetValue(hdCamera.camera, out targetTexture))
                return;

            CoreUtils.SetRenderTarget(cmd, targetTexture, ClearFlag.All);
            m_GroundTruthCrossPipelinePass.Execute(renderContext, cmd, hdCamera.camera, cullingResult);
        }
    }
}
#endif
