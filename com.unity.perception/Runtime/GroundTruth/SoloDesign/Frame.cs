using System.Collections.Generic;

namespace UnityEngine.Perception.GroundTruth.SoloDesign
{
    /// <summary>
    /// Data generated from a perception simulation will be pushed to an active
    /// consumer.
    /// </summary>
    public interface IPerceptionConsumer
    {
        /// <summary>
        /// Called when the simulation begins. Provides simulation wide metadata to
        /// the consumer.
        /// </summary>
        /// <param name="metadata">Metadata describing the active simulation</param>
        void OnSimulationStarted(SimulationMetadata metadata);
        /// <summary>
        /// Called at the end of each frame. Contains all of the generated data for the
        /// frame. This method is called after the frame has entirely finished processing.
        /// </summary>
        /// <param name="frame">The frame data.</param>
        void OnFrameGenerated(Frame frame);
        /// <summary>
        /// Called at the end of the simulation. Contains metadata describing the entire
        /// simulation process.
        /// </summary>
        /// <param name="metadata">Metadata describing the entire simulation process</param>
        void OnSimulationCompleted(CompletionMetadata metadata);
    }

    /// <summary>
    /// Metadata describing the simulation.
    /// </summary>
    public class SimulationMetadata
    {
        /// <summary>
        /// The version of the Unity editor executing the simulation.
        /// </summary>
        public string unityVersion;
        /// <summary>
        /// The version of the perception package used to generate the data.
        /// </summary>
        public string perceptionVersion;
        /// <summary>
        /// The render pipeline used to create the data. Currently either URP or HDRP.
        /// </summary>
        public string renderPipeline;
        /// <summary>
        /// Additional key/value pair metadata that can be associated with
        /// the simulation.
        /// </summary>
        public Dictionary<string, object> metadata;

        // We could probably list all of the randomizers here...
    }

    /// <summary>
    /// Metadata describing the final metrics of the simulation.
    /// </summary>
    public class CompletionMetadata : SimulationMetadata
    {
        public struct Sequence
        {
            /// <summary>
            /// The ID of the sequence
            /// </summary>
            public int id;
            /// <summary>
            /// The number of steps in the sequence.
            /// </summary>
            public int numberOfSteps;
        }

        /// <summary>
        /// Total frames processed in the simulation. These frames are distributed
        /// over sequence and steps.
        /// </summary>
        public int totalFrames;
        /// <summary>
        /// A list of all of the sequences and the number of steps in the sequence for
        /// a simulation.
        /// </summary>
        public List<Sequence> sequences;
    }

    /// <summary>
    /// The top level structure that holds all of the artifacts of a simulation
    /// frame. This is only reported after all of the captures, annotations, and
    /// metrics are ready to report for a single frame.
    /// </summary>
    public class Frame
    {
        /// <summary>
        /// The perception frame number of this record
        /// </summary>
        public int frame;
        /// <summary>
        /// The sequence that this record is a part of
        /// </summary>
        public int sequence;
        /// <summary>
        /// The step in the sequence that this record is a part of
        /// </summary>
        public int step;
        /// <summary>
        /// A list of all of the sensor captures recorded for the frame.
        /// </summary>
        public List<Sensor> sensors;
        /// <summary>
        /// A list of all of the annotations recorded recorded for the frame.
        /// </summary>
        public List<Annotation> annotations;
        /// <summary>
        /// A list of all of the metrics recorded recorded for the frame.
        /// </summary>
        public List<Metric> metrics;
    }

    /// <summary>
    /// Abstract sensor class that holds all of the common information for a sensor.
    /// </summary>
    public abstract class Sensor
    {
        /// <summary>
        /// The unique, human readable ID for the sensor.
        /// </summary>
        public string Id;
        /// <summary>
        /// The type of the sensor.
        /// </summary>
        public string sensorType;
        /// <summary>
        /// The position (xyz) of the sensor in the world.
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The rotation in euler angles.
        /// </summary>
        public Vector3 rotation;
        /// <summary>
        /// The current velocity (xyz) of the sensor.
        /// </summary>
        public Vector3 velocity;
        /// <summary>
        /// The current acceleration (xyz) of the sensor.
        /// </summary>
        public Vector3 acceleration;
        /// <summary>
        /// Additional key/value pair metadata that can be associated with
        /// the sensor.
        /// </summary>
        public Dictionary<string, object> metadata;
    }

    /// <summary>
    /// The concrete class for an RGB sensor.
    /// </summary>
    public class RgbSensor : Sensor
    {
        // The format of the image type
        public string imageFormat;
        // The dimensions (width, height) of the image
        public Vector2 dimension;
        // The raw bytes of the image file
        public byte[] buffer;
    }

    /// <summary>
    /// Abstract class that holds the common data found in all
    /// annotations. Concrete instances of this class will add
    /// data for their specific annotation type.
    /// </summary>
    public abstract class Annotation
    {
        /// <summary>
        /// The unique, human readable ID for the annotation.
        /// </summary>
        public string Id;
        /// <summary>
        /// The sensor that this annotation is associated with.
        /// </summary>
        public string sensorId;
        /// <summary>
        /// The description of the annotation.
        /// </summary>
        public string description;
        /// <summary>
        /// The type of the annotation, this will map directly to one of the
        /// annotation subclasses that are concrete implementations of this abstract
        /// class.
        /// </summary>
        public string annotationType;
        /// <summary>
        /// Additional key/value pair metadata that can be associated with
        /// any record.
        /// </summary>
        public Dictionary<string, object> metadata;
    }

    /// <summary>
    /// Bounding boxes for all of the labeled objects in a capture
    /// </summary>
    public class BoundingBoxAnnotation : Annotation
    {
        public struct Entry
        {
            // The instance ID of the object
            public int instanceId;
            // The type of the object
            public string label;
            /// <summary>
            /// (xy) pixel location of the object's bounding box
            /// </summary>
            public Vector2 origin;
            /// <summary>
            /// (width/height) dimensions of the bounding box
            /// </summary>
            public Vector2 dimension;
        }

        /// <summary>
        /// The bounding boxes recorded by the annotator
        /// </summary>
        public List<Entry> boxes;
    }

    /// <summary>
    /// The instance segmentation image recorded for a capture. This
    /// includes the data that associates a pixel color to an object.
    /// </summary>
    public class InstanceSegmentation : Annotation
    {
        public struct Entry
        {
            /// <summary>
            /// The instance ID associated with a pixel color
            /// </summary>
            public int instanceId;
            /// <summary>
            /// The color (rgba) value
            /// </summary>
            public Color32 rgba;
        }

        /// <summary>
        /// This instance to pixel map
        /// </summary>
        public List<Entry> instances;
        // The format of the image type
        public string imageFormat;
        // The dimensions (width, height) of the image
        public Vector2 dimension;
        // The raw bytes of the image file
        public byte[] buffer;
    }

    /// <summary>
    /// Abstract class that holds the common data found in all
    /// metrics. Concrete instances of this class will add
    /// data for their specific metric type.
    /// </summary>
    public abstract class Metric
    {
        /// <summary>
        /// The sensor ID that this metric is associated with
        /// </summary>
        public string sensorId;
        /// <summary>
        /// The annotation ID that this metric is associated with. If the value is none ("")
        /// then the metric is capture wide, and not associated with a specific annotation.
        /// </summary>
        public string annotationId;
        /// <summary>
        /// A human readable description of what this metric is for.
        /// </summary>
        public string description;
        /// <summary>
        /// Additional key/value pair metadata that can be associated with
        /// any metric.
        /// </summary>
        public Dictionary<string, object> metadata;
    }

    /// <summary>
    /// The object count metric records how many of a particular object are
    /// present in a capture.
    /// </summary>
    public class ObjectCountMetric : Metric
    {
        public struct Entry
        {
            /// <summary>
            /// The label of the category
            /// </summary>
            public string labelName;
            /// <summary>
            /// The number of instances for a particular category.
            /// </summary>
            public int count;
        }

        /// <summary>
        ///  The object counts
        /// </summary>
        public List<Entry> objectCounts;
    }
}
