using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Simulation;
using UnityEngine.Perception.GroundTruth.Exporters.PerceptionFormat;

namespace UnityEngine.Perception.GroundTruth.Exporters.Solo
{
    public class SoloExporter : IDatasetExporter
    {
        public bool prettyPrint = true;

        string m_DirectoryName = string.Empty;

        int m_UnknownFrameCount = 0;

        public string GetRgbCaptureFilename(params (string, object)[] additionalSensorValues)
        {
            var frameArray = additionalSensorValues.Where(p => p.Item1 == "frame").Select(p => p.Item2);
            var frame = frameArray.Any() ? (int)frameArray.First() : m_UnknownFrameCount++;

            return Path.Combine(m_DirectoryName, $"rgb_{frame}.png");
        }

        public void OnSimulationBegin(string directoryName)
        {
            Debug.Log($"SS - New Perception - OnSimBegin");
            m_Metadata = new Metadata
            {
                version = "0.1.1",
                unity_version = "2019.4.26f1",
                perception_version = "0.8.0-preview.4",
                total_frames = 0,
                total_sequences = 0,
                sequences = null,
                sensors = new[]
                {
                    new CameraData
                    {
                        id = "camera",
                        modality = "camera",
                        resolution = new[] { 0, 0 }
                    }
                }
            };

            m_DirectoryName = directoryName + Path.DirectorySeparatorChar + Guid.NewGuid() + Path.DirectorySeparatorChar;
            if (!Directory.Exists(m_DirectoryName))
                Directory.CreateDirectory(m_DirectoryName);
        }

        [Serializable]
        struct Metadata
        {
            public string version;
            public string unity_version;
            public string perception_version;
            public int total_frames;
            public int total_sequences;
            public SequenceData[] sequences;
            public CameraData[] sensors;
        }

        [Serializable]
        struct SequenceData
        {
            public string sequence_id;
            public int steps;
        }

        [Serializable]
        struct CameraData
        {
            public string id;
            public string modality;
            public int[] resolution;
        }

        Metadata m_Metadata;

        void CopyDefFile(string contains, string newName)
        {
            var d = m_DirectoryName;
            var up = Directory.GetParent(m_DirectoryName)?.ToString() ?? string.Empty;
            up = Directory.GetParent(up)?.ToString() ?? string.Empty;

            var files = Directory.EnumerateFiles(up);
            foreach (var file in files)
            {
                if (Path.GetFileName(file).Contains(contains))
                {
                    File.Copy(file, Path.Combine(m_DirectoryName, newName));
                    return;
                }
            }
        }

        public void OnSimulationEnd()
        {
            Debug.Log($"SS - New Perception - OnSimEnd");

            var writePath = Path.Combine(m_DirectoryName, "metadata.json");
            var file = File.CreateText(writePath);

            Debug.Log("SS - New Perception - writing");

            var sequences = new Dictionary<int, int>();

            foreach (var (seq, step) in m_FrameToSequenceMap.Values)
            {
                sequences.TryGetValue(seq, out var max);
                if (step > max) max = step;
                sequences[seq] = max;
            }

            m_Metadata.total_sequences = sequences.Count;
            m_Metadata.sequences = new SequenceData[sequences.Count];
            for (var i = 0; i < sequences.Count; i++)
            {
                m_Metadata.sequences[i] = new SequenceData
                {
                    sequence_id = $"sequence_{i}",
                    steps = sequences[i]
                };
            }

            file.Write(JsonUtility.ToJson(m_Metadata, true));
            file.Close();

            CopyDefFile("annotation", "annotations.json");
            CopyDefFile("metric_def", "metrics.json");
            CopyDefFile("sensors", "sensors.json");


            // Copy image files to the proper path

            var d = m_DirectoryName;
            var upOne = Path.Combine(m_DirectoryName, "..");
            var upOne2 = Directory.GetParent(m_DirectoryName)?.ToString() ?? string.Empty;
            var upTwo = Directory.GetParent(upOne2)?.ToString() ?? string.Empty;
            var upThree = Directory.GetParent(upTwo)?.ToString() ?? string.Empty;

            var dirs = Directory.EnumerateDirectories(upThree);

            foreach (var dir in dirs)
            {
                if (dir.Contains("RGB"))
                {
                    foreach (var f in Directory.EnumerateFiles(dir))
                    {
                        // Get the frame number from the path
                        var filename = Path.GetFileName(f);
                        var underscore = filename.IndexOf("_") + 1;
                        var dot = filename.IndexOf(".");
                        var frame = filename.Substring(underscore, dot - underscore);
                        var (seq, step) = m_FrameToSequenceMap[int.Parse(frame)];
                        var newFilename = $"step.{step}.capture.camera.png";


                        File.Copy(f, Path.Combine(m_DirectoryName, $"sequence.{seq}", newFilename));
                    }
                }
                else if (dir.Contains("Instance"))
                {
                    foreach (var f in Directory.EnumerateFiles(dir))
                    {
                        // Get the frame number from the path
                        var filename = Path.GetFileName(f);
                        var underscore = filename.IndexOf("_") + 1;
                        var dot = filename.IndexOf(".");
                        var frame = filename.Substring(underscore, dot - underscore);
                        var (seq, step) = m_FrameToSequenceMap[int.Parse(frame)];
                        var newFilename = $"step.{step}.annotation.semantic_segmentation.camera.png";


                        File.Copy(f, Path.Combine(m_DirectoryName, $"sequence.{seq}", newFilename));
                    }
                }
                else if (dir.Contains("Semantic"))
                {
                    foreach (var f in Directory.EnumerateFiles(dir))
                    {
                        // Get the frame number from the path
                        var filename = Path.GetFileName(f);
                        var underscore = filename.IndexOf("_") + 1;
                        var dot = filename.IndexOf(".");
                        var frame = filename.Substring(underscore, dot - underscore);
                        var (seq, step) = m_FrameToSequenceMap[int.Parse(frame)];
                        var newFilename = $"step.{step}.annotation.instance_segmentation.camera.png";


                        File.Copy(f, Path.Combine(m_DirectoryName, $"sequence.{seq}", newFilename));
                    }
                }
            }


            // Go into the RGB directory


            Manager.Instance.ConsumerFileProduced(writePath);

            Task.WhenAll(m_PendingTasks);
        }

        public void OnAnnotationRegistered<TSpec>(Guid annotationId, TSpec[] values)
        {
            // Right now, do nothing :-)
        }

        Dictionary<Guid, (string, string)> m_MetricIdMap = new Dictionary<Guid, (string, string)>();

        public void OnMetricRegistered(Guid metricId, string name, string description)
        {
            Debug.Log("On MetricRegistered");
            m_MetricIdMap[metricId] = (name, description);
        }

        bool GetFilenameForMetric(int sequence, int step, object value, out string filename, out string id, out string def, out bool reportValues)
        {
            filename = string.Empty;
            id = string.Empty;
            def = string.Empty;
            reportValues = true;
#if false
            if (value is BoundingBox2DLabeler.BoundingBoxValue bbox)
            {

                m_FrameToSequenceMap[bbox.frame] = (sequence, step);
                var frame = bbox.frame;
                id = "bounding_box";
                def = $"{id}_definition";

//                var fmt = new String('0', 5);
//                var format = "{0,20:" + fmt + "}";
//                filename = $"step.{frame.ToString(format)}.annotation.bounding_box.camera.json";
                filename = $"step.{step}.annotation.{id}.camera.json";
                return true;
            }

            if (rawData is InstanceSegmentationLabeler.InstanceColorValue)
            {
                id = "instance_segmentation";
                def = $"{id}_definition";

//                var fmt = new String('0', 5);
//                var format = "{0,20:" + fmt + "}";
//                filename = $"step.{frame.ToString(format)}.annotation.bounding_box.camera.json";
                filename = $"step.{step}.annotation.{id}.camera.json";
                return true;
            }

            if (rawData is SemanticSegmentationLabeler.SegmentationValue)
            {
                id = "semantic_segmentation";
                def = $"{id}_definition";

//                var fmt = new String('0', 5);
//                var format = "{0,20:" + fmt + "}";
//                filename = $"step.{frame.ToString(format)}.annotation.bounding_box.camera.json";
                filename = $"step.{step}.annotation.{id}.camera.json";
                reportValues = false;
                return true;
            }
#endif
            return false;
        }

        bool GetFilenameForAnnotation(int sequence, int step, object rawData, out string filename, out string id, out string def, out bool reportValues)
        {
            filename = string.Empty;
            id = string.Empty;
            def = string.Empty;
            reportValues = true;

            if (rawData is BoundingBox2DLabeler.BoundingBoxValue bbox)
            {
                m_FrameToSequenceMap[bbox.frame] = (sequence, step);
                var frame = bbox.frame;
                id = "bounding_box";
                def = $"{id}_definition";

//                var fmt = new String('0', 5);
//                var format = "{0,20:" + fmt + "}";
//                filename = $"step.{frame.ToString(format)}.annotation.bounding_box.camera.json";
                filename = $"step.{step}.annotation.{id}.camera.json";
                return true;
            }
#if false
            if (rawData is InstanceSegmentationLabeler.InstanceColorValue)
            {
                id = "instance_segmentation";
                def = $"{id}_definition";

//                var fmt = new String('0', 5);
//                var format = "{0,20:" + fmt + "}";
//                filename = $"step.{frame.ToString(format)}.annotation.bounding_box.camera.json";
                filename = $"step.{step}.annotation.{id}.camera.json";
                return true;
            }
#endif
            if (rawData is SemanticSegmentationLabeler.SegmentationValue)
            {
                id = "semantic_segmentation";
                def = $"{id}_definition";

//                var fmt = new String('0', 5);
//                var format = "{0,20:" + fmt + "}";
//                filename = $"step.{frame.ToString(format)}.annotation.bounding_box.camera.json";
                filename = $"step.{step}.annotation.{id}.camera.json";
                reportValues = false;
                return true;
            }

            return false;
        }

        // TODO - handle the 1000's of file writes we will be doing in a more intelligent fashion. Perhaps create a bg thread
        // that reads json records off of a queue and writes them out

        List<Task> m_PendingTasks = new List<Task>();

        int m_CurrentSequence = 0;
        Dictionary<Guid, int> m_SequenceGuidMap = new Dictionary<Guid, int>();

        Dictionary<int, (int, int)> m_FrameToSequenceMap = new Dictionary<int, (int, int)>();

        public Task ProcessPendingMetrics(List<SimulationState.PendingMetric> pendingMetrics, SimulationState simState)
        {
            foreach (var metric in pendingMetrics)
            {
                if (!m_SequenceGuidMap.TryGetValue(metric.SequenceId, out var seq))
                {
                    seq = m_CurrentSequence++;
                    m_SequenceGuidMap[metric.SequenceId] = seq;

                    var seqDir = Path.Combine(m_DirectoryName, $"sequence.{m_SequenceGuidMap[metric.SequenceId]}");

                    // Create a directory
                    if (!Directory.Exists(seqDir))
                        Directory.CreateDirectory(seqDir);
                }

                var path = Path.Combine(m_DirectoryName, $"sequence.{m_SequenceGuidMap[metric.SequenceId]}");

                if (!m_MetricIdMap.ContainsKey(metric.MetricDefinition.Id))
                    continue;

                var label = m_MetricIdMap[metric.MetricDefinition.Id].Item1.Replace(" ", "_");

                var filename = $"step.{metric.Step}.metric.{label}.camera.json";

                var sensor = "camera";

                var json = new StringBuilder();
                json.Append($"{{\"capture_id\": \"{sensor}\",");
                json.Append($"\"annotation_id\": \"\",");
                json.Append($"\"metric_definition\": \"{label}\",");
                json.Append("\"values\":");
                json.Append(metric.Values);
                json.Append("}");

                m_PendingTasks.Add(AnnotationHandler.WriteOutJson(path, filename, json.ToString()));
#if false


                    if (GetFilenameForAnnotation(seq, metric.Step, value, out var filename, out var id, out var def, out var reportValues))
                    {
                        var sensor = "camera";

                        var json = new StringBuilder();
                        json.Append($"{{\"Id\": \"{id}\",");
                        json.Append($"\"definition\": \"{def}\",");
                        json.Append($"\"sequence\": {seq},");
                        json.Append($"\"step\": {metric.Step},");
                        json.Append($"\"sensor\": \"{sensor}\"");

                        if (reportValues)
                        {
                            json.Append(",\"values\":");
                            json.Append(annotationData.ValuesJson);
                        }

                        json.Append("}");

                        m_PendingTasks.Add(AnnotationHandler.WriteOutJson(path, filename, json.ToString()));
                    }
#endif
            }
            return Task.CompletedTask;
        }

        public Task ProcessPendingCaptures(List<SimulationState.PendingCapture> pendingCaptures, SimulationState simState)
        {
            foreach (var cap in pendingCaptures)
            {
                if (!m_SequenceGuidMap.TryGetValue(cap.SequenceId, out var seq))
                {
                    seq = m_CurrentSequence++;
                    m_SequenceGuidMap[cap.SequenceId] = seq;

                    var seqDir = Path.Combine(m_DirectoryName, $"sequence.{m_SequenceGuidMap[cap.SequenceId]}");

                    // Create a directory
                    if (!Directory.Exists(seqDir))
                        Directory.CreateDirectory(seqDir);
                }

                var path = Path.Combine(m_DirectoryName, $"sequence.{m_SequenceGuidMap[cap.SequenceId]}");

                foreach (var (annotation, annotationData) in cap.Annotations)
                {
                    // Create a file for the annotation

#if false
                    if (annotationData.RawValues.Any())
                    {
                        var first = annotationData.RawValues.First();

                        if (GetFilenameForAnnotation(first, frame, out var filename))
                        {
                            var json = new StringBuilder("{");
                            json.Append(annotationData.ValuesJson);
                            json.Append("}");
                            m_PendingTasks.Add(AnnotationHandler.WriteOutJson(m_DirectoryName, filename, json.ToString()));
#if false
                            // Need to revisit this and handle this in a performant way
                            var jObject = PerceptionExporter.JObjectFromAnnotation((annotation, annotationData));
                            PerceptionExporter.WriteJObjectToFile(jObject, m_DirectoryName, filename);
#endif
                        }
                    }
#endif
                    foreach (var rawValue in annotationData.RawValues)
                    {

                        if (GetFilenameForAnnotation(seq, cap.Step, rawValue, out var filename, out var id, out var def, out var reportValues))
                        {
#if true
                            var sensor = "camera";

                            var json = new StringBuilder();
                            json.Append($"{{\"Id\": \"{id}\",");
                            json.Append($"\"definition\": \"{def}\",");
                            json.Append($"\"sequence\": {seq},");
                            json.Append($"\"step\": {cap.Step},");
                            json.Append($"\"sensor\": \"{sensor}\"");

                            if (reportValues)
                            {
                                json.Append(",\"values\":");
                                json.Append(annotationData.ValuesJson);
                            }

                            json.Append("}");

                            m_PendingTasks.Add(AnnotationHandler.WriteOutJson(path, filename, json.ToString()));
#else
                            // Need to revisit this and handle this in a performant way
                            var jObject = PerceptionExporter.JObjectFromAnnotation((annotation, annotationData));
                            PerceptionExporter.WriteJObjectToFile(jObject, m_DirectoryName, filename);
#endif
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }

        [Serializable]
        struct SensorData
        {
            public string id;
            public int sequence;
            public int step;
            public Vector3 translation;
            public Vector3 rotation;
            public Vector3 velocity;
            public Vector3 acceleration;
        }

        public Task OnCaptureReported(Guid sequence, int step, int width, int height, string filename, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 acceleration)
        {
            var sensor = new SensorData
            {
                id = "camera",
                sequence = 0,
                step = 0,
                translation = position,
                rotation = rotation.eulerAngles,
                velocity = velocity,
                acceleration = acceleration
            };

            if (!m_SequenceGuidMap.TryGetValue(sequence, out var seq))
            {
                seq = m_CurrentSequence++;
                m_SequenceGuidMap[sequence] = seq;
            }

            var path = Path.Combine(m_DirectoryName, $"sequence.{seq}");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var f = $"step.{step}.camera.json";


            var json = JsonUtility.ToJson(sensor, true);

            m_PendingTasks.Add(AnnotationHandler.WriteOutJson(path, f, json));

            m_Metadata.total_frames++;
            m_Metadata.sensors[0].resolution = new[] { width, height };
            return null;
        }
    }
}
