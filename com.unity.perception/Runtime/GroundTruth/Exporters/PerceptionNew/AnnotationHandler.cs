using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Simulation;

namespace UnityEngine.Perception.GroundTruth.Exporters.PerceptionNew
{
    public static class AnnotationHandler
    {
        [Serializable]
        struct BoundingBox2dRecord
        {
            public uint instanceId;
            public int frame;
            public int labelId;
            public string labelName;
            public float x;
            public float y;
            public float width;
            public float height;
            public string annotationId;
            public string annotationDefinition;

            public static BoundingBox2dRecord FromBoundingBoxValue(Guid annotationId, Guid annotationDefinition, BoundingBox2DLabeler.BoundingBoxValue bbox)
            {
                return new BoundingBox2dRecord
                {
                    instanceId = bbox.instance_id,
                    frame = bbox.frame,
                    labelId = bbox.label_id,
                    labelName = bbox.label_name,
                    x = bbox.x,
                    y = bbox.y,
                    width = bbox.width,
                    height = bbox.height,
                    annotationId = annotationId.ToString(),
                    annotationDefinition = annotationDefinition.ToString()
                };
            }

            public string ToJson()
            {
                return JsonUtility.ToJson(this, true);
            }
        }

        public static async Task WriteOutJson(string path, string filename, string json)
        {
            if (true)
            {
                json = JToken.Parse(json).ToString(Formatting.Indented);
            }

            var writePath = Path.Combine(path, filename);
            var file = File.CreateText(writePath);

            await file.WriteAsync(json);
            file.Close();

            Manager.Instance.ConsumerFileProduced(writePath);
        }

        static async Task HandleBoundingBoxAnnotation(string path, Annotation annotation, AnnotationDefinition def, BoundingBox2DLabeler.BoundingBoxValue bbox)
        {


            var id = annotation.Id;
            var defId = def.Id;
            var converted = BoundingBox2dRecord.FromBoundingBoxValue(id, defId, bbox);
            var filename = $"frame_{converted.frame}_id_{converted.instanceId}_bounding_box_2d.json";
            var writePath = Path.Combine(path, filename);
            var file = File.CreateText(writePath);
            await file.WriteAsync(converted.ToJson());
            file.Close();
            Manager.Instance.ConsumerFileProduced(writePath);
        }


        public static async Task HandleAnnotation(string path, Annotation annotation, AnnotationDefinition def, object annotatedData)
        {
            switch (annotatedData)
            {
                case BoundingBox2DLabeler.BoundingBoxValue bbox:
                    await HandleBoundingBoxAnnotation(path, annotation, def, bbox);
                    break;
            }


        }
    }
}
