using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Simulation;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Labeler which generates a local position image each frame.
    /// </summary>
    [Serializable]
    public sealed class LocalPositionLabeler : CameraLabeler, IOverlayPanelProvider
    {
        ///<inheritdoc/>
        public override string description
        {
            get => "Generates a local position image for each captured frame.";
            protected set {}
        }

        const string k_LocalPositionDirectory = "LocalPosition";
        const string k_LocalPositionFilePrefix = "localPosition_";
        internal string LocalPositionDirectory;

        /// <summary>
        /// The id to associate with semantic segmentation annotations in the dataset.
        /// </summary>
        [Tooltip("The id to associate with local position annotations in the dataset.")]
        public string annotationId = "12f94d8d-5425-4deb-9b21-5e53ad957d66";

        /// <summary>
        /// The IdLabelConfig which maps labels to pixel values.
        /// </summary>
        public IdLabelConfig labelConfig;

        /// <summary>
        /// Event information for <see cref="LocalPositionLabeler.imageReadback"/>
        /// </summary>
        public struct ImageReadbackEventArgs
        {
            /// <summary>
            /// The <see cref="Time.frameCount"/> on which the image was rendered. This may be multiple frames in the past.
            /// </summary>
            public int frameCount;
            /// <summary>
            /// Color pixel data.
            /// </summary>
            public NativeArray<Color32> data;
            /// <summary>
            /// The source image texture.
            /// </summary>
            public RenderTexture sourceTexture;
        }

        /// <summary>
        /// Event which is called each frame a local position image is read back from the GPU.
        /// </summary>
        public event Action<ImageReadbackEventArgs> imageReadback;

        /// <summary>
        /// The RenderTexture on which local position images are drawn. Will be resized on startup to match
        /// the camera resolution.
        /// </summary>
        public RenderTexture targetTexture => m_TargetTextureOverride;

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public Texture overlayImage=> targetTexture;

        /// <inheritdoc cref="IOverlayPanelProvider"/>
        public string label => "LocalPosition";

        [Tooltip("(Optional) The RenderTexture on which local position images will be drawn. Will be reformatted on startup.")]
        [SerializeField]
        RenderTexture m_TargetTextureOverride;

        AnnotationDefinition m_LocalPositionAnnotationDefinition;
        RenderTextureReader<Color32> m_LocalPositionTextureReader;

#if HDRP_PRESENT
        LocalPositionPass m_LocalPositionPass;
        LensDistortionPass m_LensDistortionPass;
    #elif URP_PRESENT
        LocalPositionUrpPass m_LocalPositionPass;
        LensDistortionUrpPass m_LensDistortionPass;
    #endif

        Dictionary<int, AsyncAnnotation> m_AsyncAnnotations;

        /// <summary>
        /// Creates a new LocalPositionLabeler. Be sure to assign <see cref="labelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
        /// </summary>
        public LocalPositionLabeler() { }

        /// <summary>
        /// Creates a new LocalPositionLabeler with the given <see cref="IdLabelConfig"/>.
        /// </summary>
        /// <param name="labelConfig">The label config associating labels with colors.</param>
        /// <param name="targetTextureOverride">Override the target texture of the labeler. Will be reformatted on startup.</param>
        public LocalPositionLabeler(IdLabelConfig labelConfig, RenderTexture targetTextureOverride = null)
        {
            this.labelConfig = labelConfig;
            m_TargetTextureOverride = targetTextureOverride;
        }

        struct LocalPositionSpec
        {
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once NotAccessedField.Local
            public int label_id;
        }

        struct AsyncLocalPositionWrite
        {
            public NativeArray<Color32> data;
            public int width;
            public int height;
            public string path;
        }

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <inheritdoc/>
        protected override void Setup()
        {
            var myCamera = perceptionCamera.GetComponent<Camera>();
            var camWidth = myCamera.pixelWidth;
            var camHeight = myCamera.pixelHeight;

            if (labelConfig == null)
            {
                throw new InvalidOperationException(
                    "LocalPositionLabeler's LabelConfig must be assigned");
            }

            m_AsyncAnnotations = new Dictionary<int, AsyncAnnotation>();

            if (targetTexture != null)
            {
                if (targetTexture.sRGB)
                {
                    Debug.LogError("targetTexture supplied to LocalPositionLabeler must be in Linear mode. Disabling labeler.");
                    enabled = false;
                }
                var renderTextureDescriptor = new RenderTextureDescriptor(camWidth, camHeight, GraphicsFormat.R8G8B8A8_UNorm, 8);
                targetTexture.descriptor = renderTextureDescriptor;
            }
            else
                m_TargetTextureOverride = new RenderTexture(camWidth, camHeight, 8, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            targetTexture.Create();
            targetTexture.name = "Labeling";
            LocalPositionDirectory = k_LocalPositionDirectory + Guid.NewGuid();

#if HDRP_PRESENT
            var gameObject = perceptionCamera.gameObject;
            var customPassVolume = gameObject.GetComponent<CustomPassVolume>() ?? gameObject.AddComponent<CustomPassVolume>();
            customPassVolume.injectionPoint = CustomPassInjectionPoint.BeforeRendering;
            customPassVolume.isGlobal = true;
            m_LocalPositionPass = new LocalPositionPass(myCamera, targetTexture, labelConfig)
            {
                name = "Labeling Pass"
            };
            customPassVolume.customPasses.Add(m_LocalPositionPass);

            m_LensDistortionPass = new LensDistortionPass(myCamera, targetTexture)
            {
                name = "Lens Distortion Pass"
            };
            customPassVolume.customPasses.Add(m_LensDistortionPass);
#elif URP_PRESENT
            // Local Position Pass
            m_LocalPositionPass = new LocalPositionUrpPass(myCamera, targetTexture, labelConfig);
            perceptionCamera.AddScriptableRenderPass(m_LocalPositionPass);

            // Lens Distortion Pass
            m_LensDistortionPass = new LensDistortionUrpPass(myCamera, targetTexture);
            perceptionCamera.AddScriptableRenderPass(m_LensDistortionPass);
#endif

            var specs = labelConfig.labelEntries.Select(l => new LocalPositionSpec { label_id = l.id });

            m_LocalPositionAnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition(
                "local position",
                specs.ToArray(),
                "pixel-wise local position label",
                "PNG",
                Guid.Parse(annotationId));

            m_LocalPositionTextureReader = new RenderTextureReader<Color32>(targetTexture);
            visualizationEnabled = supportsVisualization;
        }

        void OnLocalPositionImageRead(int frameCount, NativeArray<Color32> data)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var annotation))
                return;

            var datasetRelativePath = $"{LocalPositionDirectory}/{k_LocalPositionFilePrefix}{frameCount}.png";
            var localPath = $"{Manager.Instance.GetDirectoryFor(LocalPositionDirectory)}/{k_LocalPositionFilePrefix}{frameCount}.png";

            annotation.ReportFile(datasetRelativePath);

            var asyncRequest = Manager.Instance.CreateRequest<AsyncRequest<AsyncLocalPositionWrite>>();

            imageReadback?.Invoke(new ImageReadbackEventArgs
            {
                data = data,
                frameCount = frameCount,
                sourceTexture = targetTexture
            });
            asyncRequest.data = new AsyncLocalPositionWrite
            {
                data = new NativeArray<Color32>(data, Allocator.Persistent),
                width = targetTexture.width,
                height = targetTexture.height,
                path = localPath
            };
            asyncRequest.Enqueue(r =>
            {
                var pngBytes = ImageConversion.EncodeArrayToPNG(
                    r.data.data.ToArray(), GraphicsFormat.R8G8B8A8_UNorm, (uint)r.data.width, (uint)r.data.height);
                File.WriteAllBytes(r.data.path, pngBytes);
                Manager.Instance.ConsumerFileProduced(r.data.path);
                r.data.data.Dispose();
                return AsyncRequest.Result.Completed;
            });
            asyncRequest.Execute();
        }

        /// <inheritdoc/>
        protected override void OnEndRendering(ScriptableRenderContext scriptableRenderContext)
        {
            m_AsyncAnnotations[Time.frameCount] = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_LocalPositionAnnotationDefinition);
            m_LocalPositionTextureReader.Capture(scriptableRenderContext,
                (frameCount, data, renderTexture) => OnLocalPositionImageRead(frameCount, data));
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
            m_LocalPositionTextureReader?.WaitForAllImages();
            m_LocalPositionTextureReader?.Dispose();
            m_LocalPositionTextureReader = null;

            if (m_TargetTextureOverride != null)
                m_TargetTextureOverride.Release();

            m_TargetTextureOverride = null;
        }
    }
}
