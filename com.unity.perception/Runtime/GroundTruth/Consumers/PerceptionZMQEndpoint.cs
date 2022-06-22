using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.Settings;

namespace UnityEngine.Perception.GroundTruth.Consumers
{
    /// <summary>
    /// Endpoint to write out generated data in the perception format.
    /// </summary>
    [Serializable]
    public class PerceptionZMQEndpoint : IConsumerEndpoint
    {
        Dictionary<string, SensorInfo> m_SensorMap = new Dictionary<string, SensorInfo>();
        internal Dictionary<string, AnnotationDefinition> registeredAnnotations = new Dictionary<string, AnnotationDefinition>();
        Dictionary<string, MetricDefinition> m_RegisteredMetrics = new Dictionary<string, MetricDefinition>();
        List<PerceptionCapture> m_CurrentCaptures = new List<PerceptionCapture>();
        internal Dictionary<string, Guid> idToGuidMap = new Dictionary<string, Guid>();
        Guid m_SequenceGuidStart = Guid.NewGuid();


        internal JsonSerializer Serializer { get; } = new JsonSerializer
        {
            ContractResolver = new PerceptionResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        /// <summary>
        /// ZMQ Requests Queue
        /// </summary>
        public Queue<(string, Action<string>)> payloadsRequests = new Queue<(string, Action<string>)>();

        ///// <summary>
        ///// Callback with serialized JSON string for perception metrics with
        ///// the batch size of metricsPerBatch
        ///// </summary>
        //public Action<string> reportMetricsJsonString;

        ///// <summary>
        ///// Callback with Serialized JSON string for perception captures with
        ///// batch size of capturesPerBatch
        ///// </summary>
        //public Action<string> reportCapturesJsonString;

        /// <summary>
        /// The number of captures to write to a single captures batch.
        /// </summary>
        public int capturesPerBatch = 8;

        /// <summary>
        /// The number of metrics to write to a single metrics batch.
        /// </summary>
        public int metricsPerBatch = 8;

        /// <inheritdoc/>
        public string description => "Produces synthetic data in the perception format over a ZMQ socket.";

        /// <summary>
        /// output version
        /// </summary>
        public static string version => "0.0.1";

        // ReSharper disable NotAccessedField.Local
        [Serializable]
        struct SensorInfo
        {
            public string id;
            public string modality;
            public string description;
        }
        // ReSharper enable NotAccessedField.Local

        /// <inheritdoc/>
        public object Clone()
        {
            var cloned = new PerceptionZMQEndpoint
            {
                capturesPerBatch = capturesPerBatch,
                metricsPerBatch = metricsPerBatch,
            };

            // not copying _CurrentPath on purpose. This needs to be set to null
            // for each cloned version of the endpoint so that a new dataset will
            // be created

            return cloned;
        }

        /// <inheritdoc/>
        public void AnnotationRegistered(AnnotationDefinition annotationDefinition)
        {
            if (registeredAnnotations.ContainsKey(annotationDefinition.id))
            {
                Debug.LogError("Tried to register an annotation twice");
                return;
            }

            registeredAnnotations[annotationDefinition.id] = annotationDefinition;
            idToGuidMap[annotationDefinition.id] = Guid.NewGuid();
        }

        /// <inheritdoc/>
        public void MetricRegistered(MetricDefinition metricDefinition)
        {
            if (m_RegisteredMetrics.ContainsKey(metricDefinition.id))
            {
                Debug.LogError("Tried to register a metric twice");
                return;
            }

            m_RegisteredMetrics[metricDefinition.id] = metricDefinition;
            idToGuidMap[metricDefinition.id] = Guid.NewGuid();
        }

        /// <inheritdoc/>
        public void SensorRegistered(SensorDefinition sensor)
        {
            if (m_SensorMap.ContainsKey(sensor.id))
            {
                Debug.LogError("Tried to register a sensor twice");
                return;
            }

            m_SensorMap[sensor.id] = new SensorInfo
            {
                id = sensor.id,
                modality = sensor.modality,
                description = sensor.description
            };

            idToGuidMap[sensor.id] = Guid.NewGuid();
        }


        /// <inheritdoc/>
        public void SimulationStarted(SimulationMetadata metadata)
        {
            Debug.Log("Simulation started.");
        }

        /// <inheritdoc/>
        public void FrameGenerated(Frame frame)
        {
            var seqId = GenerateSequenceId(frame);

            var captureIdMap = new Dictionary<(int step, string sensorId), string>();

            foreach (var sensor in frame.sensors)
            {
                if (sensor is RgbSensor rgb)
                {
                    var path = "None";
                    var sensorJToken = PerceptionJsonFactory.Convert(this, frame, rgb);

                    var annotations = new JArray();

                    foreach (var annotation in rgb.annotations)
                    {
                        registeredAnnotations.TryGetValue(annotation.annotationId, out var def);
                        var defId = def?.id ?? string.Empty;
                        var json = PerceptionJsonFactory.Convert(this, frame, annotation.id, defId, annotation);
                        if (json != null)
                            annotations.Add(json);
                    }

                    var id = Guid.NewGuid().ToString();
                    captureIdMap[(frame.step, rgb.id)] = id;
                    var capture = new PerceptionCapture
                    {
                        id = id,
                        sequence_id = seqId,
                        step = frame.step,
                        timestamp = frame.timestamp,
                        sensor = sensorJToken,
                        ego = JToken.FromObject(defaultEgo, Serializer),
                        filename = path,
                        rgbImg = Convert.ToBase64String(rgb.buffer),
                        format = "JPEG",
                        annotations = annotations
                    };

                    m_CurrentCaptures.Add(capture);
                }
            }

            foreach (var metric in frame.metrics)
            {
                AddMetricToReport(seqId, frame.step, captureIdMap, metric);
            }

            WriteCaptures();
        }

        string GenerateSequenceId(Frame frame)
        {
            //take the randomly generated sequenceGuidStart and increment by the sequence index to get a new unique id
            var hash = m_SequenceGuidStart.ToByteArray();
            var start = BitConverter.ToUInt32(hash, 0);
            start = start + (uint)frame.sequence;
            var startBytes = BitConverter.GetBytes(start);
            //reverse so that the beginning of the guid always changes
            Array.Reverse(startBytes);
            Array.Copy(startBytes, hash, startBytes.Length);
            var seqId = new Guid(hash).ToString();
            return seqId;
        }

        void WriteMetrics(bool flush = false)
        {
            if (flush || m_MetricsReady.Count > metricsPerBatch)
            {
                m_MetricOutCount++;
                WriteMetricsBatch(m_MetricsReady);
                m_MetricsReady.Clear();
            }
        }

        void WriteCaptures(bool flush = false)
        {
            if (flush || (m_CurrentCaptures.Count >= capturesPerBatch))
            {
                m_CurrentCaptureIndex++;
                WriteCaptureBatch(m_CurrentCaptures);
                m_CurrentCaptures.Clear();
            }
        }

        /// <inheritdoc/>
        public void SimulationCompleted(SimulationMetadata metadata)
        {
            WriteSensorsFile();
            WriteAnnotationsDefinitionsFile();
            WriteMetricsDefinitionsFile();

            WriteCaptures(true);
            WriteMetrics(true);

            Debug.Log("Simulation Ended.");
        }

        int m_CurrentCaptureIndex;

        internal string WriteOutImageFile(int frame, RgbSensor rgb)
        {
            return "None";
        }

        void WriteAnnotationsDefinitionsFile()
        {
            var defs = new JArray();

            foreach (var def in registeredAnnotations.Values)
            {
                defs.Add(PerceptionJsonFactory.Convert(this, def.id, def));
            }

            var top = new JObject
            {
                ["version"] = version,
                ["annotation_definitions"] = defs
            };
            
        }

        void WriteMetricsDefinitionsFile()
        {
            var defs = new JArray();

            foreach (var def in m_RegisteredMetrics.Values)
            {
                defs.Add(PerceptionJsonFactory.Convert(this, def.id, def));
            }

            var top = new JObject
            {
                ["version"] = version,
                ["metric_definitions"] = defs
            };
        }

        void WriteSensorsFile()
        {
            var sub = new JArray();
            foreach (var sensor in m_SensorMap)
            {
                sub.Add(JToken.FromObject(sensor.Value, Serializer));
            }
            var top = new JObject
            {
                ["version"] = version,
                ["sensors"] = sub
            };
        }

        JToken ToJToken(string sequenceId, int step, Dictionary<(int step, string sensorId), string> captureIdMap,
            Metric metric)
        {
            string captureId = null;
            string annotationId = null;
            string defId = null;

            if (!string.IsNullOrEmpty(metric.sensorId))
            {
                var sensorId = m_SensorMap[metric.sensorId].id;
                captureIdMap.TryGetValue((step, sensorId), out captureId);
            }

            if (!string.IsNullOrEmpty(metric.annotationId))
            {
                annotationId = registeredAnnotations[metric.annotationId].id;
            }

            if (m_RegisteredMetrics.TryGetValue(metric.id, out var def))
            {
                defId = def.id;
            }

            return new JObject
            {
                ["capture_id"] = captureId,
                ["annotation_id"] = annotationId,
                ["sequence_id"] = sequenceId,
                ["step"] = step,
                ["metric_definition"] = defId,
                ["values"] = JToken.FromObject(metric.GetValues<object>(), Serializer)
            };
        }

        void WriteMetricsBatch(IEnumerable<JToken> metrics)
        {
            var top = new MetricsJson
            {
                version = version,
                metrics = metrics
            };

            //WriteAndReportJson(JToken.FromObject(top, Serializer), StoreType.metrics);

        }

        int m_MetricOutCount;
        List<JToken> m_MetricsReady = new List<JToken>();
        void AddMetricToReport(string sequenceId, int step, Dictionary<(int step, string sensorId), string> captureIdMap,
            Metric metric)
        {
            m_MetricsReady.Add(ToJToken(sequenceId, step, captureIdMap, metric));
            WriteMetrics();
        }


        void WriteCaptureBatch(IEnumerable<PerceptionCapture> captures)
        {
            var top = new PerceptionJson
            {
                version = version,
                captures = captures
            };

            WriteAndReportJson(JToken.FromObject(top, Serializer), StoreType.captures);

        }

        /// <summary>
        /// Pass json string out to a zmq request.
        /// </summary>
        /// <param name="json">The json information to pass out.</param>
        /// <param name="storeType">The type of json string to pass out.</param>
        /// <returns>Did it work correctly</returns>
        void WriteAndReportJson(JToken payloads, StoreType storeType)
        {
            switch (storeType) {
                case StoreType.captures:
                    if (payloadsRequests.Count > 0)
                    {
                        var stringWriter = new StringWriter(new StringBuilder(2048), CultureInfo.InvariantCulture);
                        using (var jsonTextWriter = new JsonTextWriter(stringWriter))
                        {
                            jsonTextWriter.Formatting = Formatting.None;
                            payloads.WriteTo(jsonTextWriter);
                        }

                        var (request, provideResult) = payloadsRequests.Dequeue();

                        provideResult(stringWriter.ToString());
                    }
                    break;
                case StoreType.metrics:
                    //reportMetricsJsonString(stringWriter.ToString());
                    break;
                default:
                    break;
            }
        }

        // ReSharper disable NotAccessedField.Local
        // ReSharper disable InconsistentNaming
        [Serializable]
        struct PerceptionJson
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public string version;
            public IEnumerable<PerceptionCapture> captures;
        }

        [Serializable]
        struct MetricsJson
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public string version;
            public IEnumerable<JToken> metrics;
        }

        [Serializable]
        struct PerceptionCapture
        {
            public string id;
            public string sequence_id;
            public int step;
            public float timestamp;
            public JToken sensor;
            public JToken ego;
            public string filename;
            public string format;
            public string rgbImg;
            public JArray annotations;
        }

        [Serializable]
        struct Ego
        {
            public string ego_id;
            public Vector3 translation;
            public Quaternion rotation;
            public Vector3? velocity;
            public Vector3? acceleration;
        }

        Ego defaultEgo => new Ego
        {
            ego_id = "ego",
            translation = Vector3.zero,
            rotation = Quaternion.identity,
            velocity = null,
            acceleration = null
        };

        static float[][] ToFloatArray(float3x3 inF3)
        {
            return new[]
            {
                new [] { inF3[0][0], inF3[0][1], inF3[0][2] },
                new [] { inF3[1][0], inF3[1][1], inF3[1][2] },
                new [] { inF3[2][0], inF3[2][1], inF3[2][2] }
            };
        }


        // ReSharper enable NotAccessedField.Local
        // ReSharper enable InconsistentNaming
    }

    enum StoreType
    {
        captures,
        metrics
    }

    class PerceptionResolver : DefaultContractResolver
    {
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);
            if (objectType == typeof(Vector3) ||
                objectType == typeof(Vector2) ||
                objectType == typeof(Color) ||
                objectType == typeof(Quaternion))
            {
                contract.Converter = PerceptionConverter.Instance;
            }

            return contract;
        }
    }

    class PerceptionConverter : JsonConverter
    {
        public static PerceptionConverter Instance = new PerceptionConverter();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (value)
            {
                case int _:
                case uint _:
                case float _:
                case double _:
                case string _:
                    writer.WriteValue(value);
                    break;
                case Vector3 v3:
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(v3.x);
                        writer.WriteValue(v3.y);
                        writer.WriteValue(v3.z);
                        writer.WriteEndArray();
                        break;
                    }
                case Vector2 v2:
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(v2.x);
                        writer.WriteValue(v2.y);
                        writer.WriteEndArray();
                        break;
                    }
                case Color32 rgba:
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("r");
                        writer.WriteValue(rgba.r);
                        writer.WritePropertyName("g");
                        writer.WriteValue(rgba.g);
                        writer.WritePropertyName("b");
                        writer.WriteValue(rgba.b);
                        writer.WritePropertyName("a");
                        writer.WriteValue(rgba.a);
                        writer.WriteEndObject();
                        break;
                    }
                case Color rgba:
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("r");
                        writer.WriteValue(rgba.r);
                        writer.WritePropertyName("g");
                        writer.WriteValue(rgba.g);
                        writer.WritePropertyName("b");
                        writer.WriteValue(rgba.b);
                        writer.WritePropertyName("a");
                        writer.WriteValue(rgba.a);
                        writer.WriteEndObject();
                        break;
                    }
                case Quaternion quaternion:
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(quaternion.x);
                        writer.WriteValue(quaternion.y);
                        writer.WriteValue(quaternion.z);
                        writer.WriteValue(quaternion.w);
                        writer.WriteEndArray();
                        break;
                    }
                case float3x3 f3x3:
                    writer.WriteStartArray();
                    writer.WriteStartArray();
                    writer.WriteValue(f3x3.c0[0]);
                    writer.WriteValue(f3x3.c0[1]);
                    writer.WriteValue(f3x3.c0[2]);
                    writer.WriteEndArray();
                    writer.WriteStartArray();
                    writer.WriteValue(f3x3.c1[0]);
                    writer.WriteValue(f3x3.c1[1]);
                    writer.WriteValue(f3x3.c1[2]);
                    writer.WriteEndArray();
                    writer.WriteStartArray();
                    writer.WriteValue(f3x3.c2[0]);
                    writer.WriteValue(f3x3.c2[1]);
                    writer.WriteValue(f3x3.c2[2]);
                    writer.WriteEndArray();
                    writer.WriteEndArray();
                    break;
            }
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return null;
        }

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(int)) return true;
            if (objectType == typeof(uint)) return true;
            if (objectType == typeof(double)) return true;
            if (objectType == typeof(float)) return true;
            if (objectType == typeof(string)) return true;
            if (objectType == typeof(Vector3)) return true;
            if (objectType == typeof(Vector2)) return true;
            if (objectType == typeof(Quaternion)) return true;
            if (objectType == typeof(float3x3)) return true;
            if (objectType == typeof(Color)) return true;
            return objectType == typeof(Color32);
        }
    }
}
