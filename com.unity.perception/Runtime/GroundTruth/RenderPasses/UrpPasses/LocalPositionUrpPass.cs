#if URP_PRESENT
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Perception.GroundTruth
{
    class LocalPositionUrpPass : ScriptableRenderPass
    {
        public LocalPositionCrossPipelinePass localPositionCrossPipelinePass;

        public LocalPositionUrpPass(Camera camera, RenderTexture targetTexture, IdLabelConfig labelConfig)
        {
            localPositionCrossPipelinePass = new LocalPositionCrossPipelinePass(camera, labelConfig);
            ConfigureTarget(targetTexture, targetTexture.depthBuffer);
            localPositionCrossPipelinePass.Setup();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var commandBuffer = CommandBufferPool.Get(nameof(SemanticSegmentationUrpPass));
            localPositionCrossPipelinePass.Execute(context, commandBuffer, renderingData.cameraData.camera, renderingData.cullResults);
            CommandBufferPool.Release(commandBuffer);
        }

        public void Cleanup()
        {
            localPositionCrossPipelinePass.Cleanup();
        }
    }
}
#endif
