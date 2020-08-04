using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.Entities;
using Unity.Profiling;
using Unity.Simulation;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.GroundTruth
{
    // ##### 3D bounding box
    //
    // A json file that stored collections of 3D bounding boxes.
    // Each bounding box record maps a tuple of (instance, label) to translation, size and rotation that draws a 3D bounding box,
    // as well as velocity and acceleration (optional) of the 3D bounding box.
    // All location data is given with respect to the **sensor coordinate system**.
    //
    //
    // bounding_box_3d {
    //      label_id:     <int> -- Integer identifier of the label
    //      label_name:   <str> -- String identifier of the label
    //      instance_id:  <str> -- UUID of the instance.
    //      translation:  <float, float, float> -- 3d bounding box's center location in meters as center_x, center_y, center_z with respect to global coordinate system.
    //      size:         <float, float, float> -- 3d bounding box size in meters as width, length, height.
    //      rotation:     <float, float, float, float> -- 3d bounding box orientation as quaternion: w, x, y, z.
    //      velocity:     <float, float, float>  -- 3d bounding box velocity in meters per second as v_x, v_y, v_z.
    //      acceleration: <float, float, float> [optional] -- 3d bounding box acceleration in meters per second^2 as a_x, a_y, a_z.
    //  }

    public sealed class BoundingBox3DLabeler : CameraLabeler, IGroundTruthUpdater
    {
        public enum OutputMode
        {
            Verbose,
            Kitti
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        abstract class BoxData
        {
            public int label_id;
            public string label_name;
            public uint instance_id;
        }

        class KittiData : BoxData
        {
            public float[] translation;
            public float[] size;
            public float yaw;
        }

        class VerboseData : BoxData
        {
            public float[] translation;
            public float[] size;
            public float[] rotation;
            public float[] velocity; // TODO
            public float[] acceleration; // TODO
        }

        static ProfilerMarker s_BoundingBoxCallback = new ProfilerMarker("OnBoundingBoxes3DReceived");

        public string annotationId = "0bfbe00d-00fa-4555-88d1-471b58449f5c";

        Dictionary<int, AsyncAnnotation> m_AsyncAnnotations;
        AnnotationDefinition m_AnnotationDefinition;
        BoxData[] m_BoundingBoxValues;

        public OutputMode mode = OutputMode.Kitti;

        public IdLabelConfig idLabelConfig;

        protected override bool supportsVisualization => false;

        public BoundingBox3DLabeler() {}

        public BoundingBox3DLabeler(IdLabelConfig labelConfig)
        {
            this.idLabelConfig = labelConfig;
        }

        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("BoundingBox2DLabeler's idLabelConfig field must be assigned");

            var updater = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystem<GroundTruthUpdateSystem>();
            updater?.Activate(this);

            m_AsyncAnnotations = new Dictionary<int, AsyncAnnotation>();

            m_AnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition("bounding box 3D", idLabelConfig.GetAnnotationSpecification(),
                "Bounding box for each labeled object visible to the sensor", id: new Guid(annotationId));
        }

        protected override void Cleanup()
        {
            var updater = World.DefaultGameObjectInjectionWorld?.GetExistingSystem<GroundTruthUpdateSystem>();
            updater?.Deactivate(this);
        }

        int m_CurrentIndex;
        int m_CurrentFrame;

        public void OnBeginUpdate(int count)
        {
            if (m_BoundingBoxValues == null || count != m_BoundingBoxValues.Length)
                m_BoundingBoxValues = new BoxData[count];

            m_CurrentIndex = 0;
            m_CurrentFrame = Time.frameCount;
        }

        BoxData Convert(IdLabelEntry label, uint instanceId, Renderer renderer, OutputMode outputMode)
        {
            return outputMode == OutputMode.Kitti ? ConvertToKitti(label, instanceId, renderer) : ConvertToVerboseData(label, instanceId, renderer);
        }

        BoxData ConvertToVerboseData(IdLabelEntry label, uint instanceId, Renderer renderer)
        {
            var camTrans = perceptionCamera.transform;

            var bounds = renderer.bounds;

            var localCenter = camTrans.InverseTransformPoint(bounds.center);
            var localRotation = Quaternion.Inverse(renderer.transform.rotation) * camTrans.rotation;

            return new VerboseData
            {
                label_id = label.id,
                label_name = label.label,
                instance_id = instanceId,
                translation = new float[] { localCenter.x, localCenter.y, localCenter.z },
                size = new float[] { bounds.extents.x, bounds.extents.y, bounds.extents.z },
                rotation = new float[] { localRotation.w, localRotation.x, localRotation.y, localRotation.z },
                velocity = null,
                acceleration = null
            };
        }

        BoxData ConvertToKitti(IdLabelEntry label, uint instanceId, Renderer renderer)
        {
            var camTrans = perceptionCamera.transform;

            var bounds = renderer.bounds;

            var localCenter = camTrans.InverseTransformPoint(bounds.center);
            var localRotation = Quaternion.Inverse(renderer.transform.rotation) * camTrans.rotation;

            return new KittiData
            {
                label_id = label.id,
                label_name = label.label,
                instance_id = instanceId,
                translation = new float[] { localCenter.x, localCenter.y, localCenter.z },
                size = new float[] { bounds.extents.x, bounds.extents.y, bounds.extents.z },
                yaw = localRotation.eulerAngles.y,
            };
        }

        public void OnUpdateEntity(Labeling labeling, GroundTruthInfo groundTruthInfo)
        {
            using (s_BoundingBoxCallback.Auto())
            {
                var renderer = labeling.gameObject.GetComponent<Renderer>();

                if (renderer == null) return;

                if (idLabelConfig.TryGetLabelEntryFromInstanceId(groundTruthInfo.instanceId, out var labelEntry))
                {
                    m_BoundingBoxValues[m_CurrentIndex++] = Convert(labelEntry, groundTruthInfo.instanceId, renderer, mode);
                }
            }
        }

        public void OnEndUpdate()
        {
            perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition).ReportValues(m_BoundingBoxValues);
        }
    }
}
