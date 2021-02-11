using System;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#if HDRP_PRESENT
    using UnityEngine.Rendering.HighDefinition;
#elif URP_PRESENT
    using UnityEngine.Rendering.Universal;
#endif

#if HDRP_PRESENT || URP_PRESENT

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Custom Pass which will apply a lens distortion (per the respective volume override in URP or HDRP, or
    /// through a custom override directly through the pass) to an incoming mask / texture.  The purpose of this
    /// is to allow the same lens distortion being applied to the RGB image ine the perception camera to be applied
    /// to the respective ground truths generated.
    /// </summary>
    internal class LensDistortionCrossPipelinePass : GroundTruthCrossPipelinePass
    {
        const string k_ShaderName = "Perception/LensDistortion";

        //Serialize the shader so that the shader asset is included in player builds when the SemanticSegmentationPass is used.
        //Currently commented out and shaders moved to Resources folder due to serialization crashes when it is enabled.
        //See https://fogbugz.unity3d.com/f/cases/1187378/
        //[SerializeField]

        // Lens Distortion Shader
        Shader m_LensDistortionShader;
        Material m_LensDistortionMaterial;

        bool m_fInitialized = false;

        public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
        public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");

        internal float? lensDistortionOverride = null;        // Largely for testing, but could be useful otherwise

        RenderTexture m_TargetTexture;
        RenderTexture m_distortedTexture;

        public LensDistortionCrossPipelinePass(RenderTexture targetTexture)
        {
            m_TargetTexture = targetTexture;
        }


        public override void Setup()
        {
            base.Setup();
            m_LensDistortionShader = Shader.Find(k_ShaderName);

            var shaderVariantCollection = new ShaderVariantCollection();

            if (shaderVariantCollection != null)
                shaderVariantCollection.Add(new ShaderVariantCollection.ShaderVariant(m_LensDistortionShader, PassType.ScriptableRenderPipeline));

            m_LensDistortionMaterial = new Material(m_LensDistortionShader);

            if(shaderVariantCollection != null)
                shaderVariantCollection.WarmUp();

            // Set up a new texture
            if (m_distortedTexture == null || m_distortedTexture.width != Screen.width || m_distortedTexture.height != Screen.height) {

                if (m_distortedTexture != null)
                    m_distortedTexture.Release();

                m_distortedTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                m_distortedTexture.enableRandomWrite = true;
                m_distortedTexture.filterMode = FilterMode.Point;
                m_distortedTexture.Create();
            }
            m_fInitialized = true;
        }

        protected override void ExecutePass(ScriptableRenderContext renderContext, CommandBuffer cmd, Camera camera, CullingResults cullingResult)
        {
            if (m_fInitialized == false)
                return;

            // Grab the lens distortion
            LensDistortion lensDistortion;
#if HDRP_PRESENT
            // Grab the Lens Distortion from Perception Camera stack
            var hdCamera = HDCamera.GetOrCreate(camera);
            var stack = hdCamera.volumeStack;
            lensDistortion = stack.GetComponent<LensDistortion>();
#elif URP_PRESENT
            var stack = VolumeManager.instance.stack;
            lensDistortion = stack.GetComponent<LensDistortion>();
#endif

            if (SetLensDistortionShaderParameters(lensDistortion, camera) == false)
                return;

            // Blitmayhem
            cmd.Blit(m_TargetTexture, m_distortedTexture, m_LensDistortionMaterial);
            cmd.Blit(m_distortedTexture, m_TargetTexture);

            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public bool SetLensDistortionShaderParameters(LensDistortion lensDistortion, Camera camera)
        {
            if (m_fInitialized == false)
                return false;

            // This code is lifted from the SetupLensDistortion() function in
            // https://github.com/Unity-Technologies/Graphics/blob/257b08bba6c11de0f894e42e811124247a522d3c/com.unity.render-pipelines.universal/Runtime/Passes/PostProcessPass.cs
            // This is in UnityEngine.Rendering.Universal.Internal.PostProcessPass::SetupLensDistortion so it's
            // unclear how to re-use this code

            float intensity = 0.5f;
            float scale = 1.0f;
            var center = new Vector2(0.0f, 0.0f);
            var mult = new Vector2(1.0f, 1.0f);

        #if HDRP_PRESENT
            if(lensDistortion == null)
                return false;
        #elif URP_PRESENT
            var UACD = camera.GetUniversalAdditionalCameraData();

            if(UACD.renderPostProcessing == false && lensDistortionOverride.HasValue == false)
                return false;

            if (lensDistortion.active == false)
                return false;

        #else
            return false;
        #endif

            if (lensDistortionOverride.HasValue)
            {
                intensity = lensDistortionOverride.Value;
            }
            else if (lensDistortion != null)
            {
                // This is a bit finicky for URP - since Lens Distortion comes off the VolumeManager stack as active
                // even if post processing is not enabled.  An intensity of 0.0f is untenable, so the below checks
                // ensures post processing hasn't been enabled but Lens Distortion actually overriden
                if (lensDistortion.intensity.value != 0.0f)
                {
                    intensity = lensDistortion.intensity.value;
                    center = lensDistortion.center.value * 2f - Vector2.one;
                    mult.x = Mathf.Max(lensDistortion.xMultiplier.value, 1e-4f);
                    mult.y = Mathf.Max(lensDistortion.yMultiplier.value, 1e-4f);
                    scale = 1.0f / lensDistortion.scale.value;
                }
                else
                {
                    return false;
                }
            }

            float amount = 1.6f * Mathf.Max(Mathf.Abs(intensity * 100.0f), 1.0f);
            float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            float sigma = 2.0f * Mathf.Tan(theta * 0.5f);

            var p1 = new Vector4(
                center.x,
                center.y,
                mult.x,
                mult.y
            );

            var p2 = new Vector4(
                intensity >= 0f ? theta : 1f / theta,
                sigma,
                scale,
                intensity * 100.0f
            );

            // Set Shader Constants
            m_LensDistortionMaterial.SetVector(_Distortion_Params1, p1);
            m_LensDistortionMaterial.SetVector(_Distortion_Params2, p2);

            return true;
        }

        public override void SetupMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId)
        {
        }
    }
}

#endif // ! HDRP_PRESENT || URP_PRESENT
