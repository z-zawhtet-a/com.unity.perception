using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Custom Pass which renders labeled images where each object labeled with a Labeling component is drawn with the
    /// value specified by the given LabelingConfiguration.
    /// </summary>
    class LocalPositionCrossPipelinePass : GroundTruthCrossPipelinePass
    {
        const string k_ShaderName = "Perception/LocalPosition";
        static readonly int k_Center = Shader.PropertyToID("_Center");
        static readonly int k_Size = Shader.PropertyToID("_Size");

        static int s_LastFrameExecuted = -1;

        IdLabelConfig m_LabelConfig;

        // NOTICE: Serialize the shader so that the shader asset is included in player builds when the SemanticSegmentationPass is used.
        // Currently commented out and shaders moved to Resources folder due to serialization crashes when it is enabled.
        // See https://fogbugz.unity3d.com/f/cases/1187378/
        // [SerializeField]
        Shader m_LocalPositionShader;
        Material m_OverrideMaterial;

        public LocalPositionCrossPipelinePass(
            Camera targetCamera, IdLabelConfig labelConfig) : base(targetCamera)
        {
            m_LabelConfig = labelConfig;
        }

        public override void Setup()
        {
            base.Setup();
            m_LocalPositionShader = Shader.Find(k_ShaderName);

            var shaderVariantCollection = new ShaderVariantCollection();

            if (shaderVariantCollection != null)
            {
                shaderVariantCollection.Add(
                    new ShaderVariantCollection.ShaderVariant(m_LocalPositionShader, PassType.ScriptableRenderPipeline));
            }

            m_OverrideMaterial = new Material(m_LocalPositionShader);

            shaderVariantCollection.WarmUp();
        }

        protected override void ExecutePass(
            ScriptableRenderContext renderContext, CommandBuffer cmd, Camera camera, CullingResults cullingResult)
        {
            if (s_LastFrameExecuted == Time.frameCount)
                return;

            s_LastFrameExecuted = Time.frameCount;
            var renderList = CreateRendererListDesc(camera, cullingResult, "FirstPass", 0, m_OverrideMaterial, -1);
            cmd.ClearRenderTarget(true, true, Color.black);
            DrawRendererList(renderContext, cmd, RendererList.Create(renderList));
        }

        public override void SetupMaterialProperties(
            MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId)
        {
            var bounds = renderer.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
            mpb.SetVector(k_Center, bounds.center);
            mpb.SetVector(k_Size, bounds.size);
        }

        public override void ClearMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId)
        {
            // mpb.SetVector(k_Center, Color.black);
        }
    }
}
