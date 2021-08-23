using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityEngine.Perception.GroundTruth.Exporters.Coco
{
    public static class CocoTypes
    {
        public class CocoData
        {
            public Info info;
            public Image[] images;
            public ObjectDetectionAnnotation[] annotations;
            public ObjectDetectionCategory[] categories;
            public License[] licenses;
        }

        public class Info
        {
            public int year;
            public string version;
            public string description;
            public string contributor;
            public string url;
            public string date_created;
        }

        [Serializable]
        public class License
        {
            public int id;
            public string name;
            public string url;
        }

        [Serializable]
        public class Licenses
        {
            public License[] licenses;
        }

        public class Image
        {
            public int id;
            public int width;
            public int height;
            public string file_name;
            public int license;
            public string flickr_url;
            public string coco_url;
            public string date_captured;
        }

        [Serializable]
        public class ObjectDetectionAnnotation
        {
            [JsonProperty(Order = -2)]
            public int id;
            [JsonProperty(Order = -2)]
            public int image_id;
            [JsonProperty(Order = -2)]
            public int category_id;
            [JsonProperty(Order = -2)]
            public float[] segmentation;
            [JsonProperty(Order = -2)]
            public float area;
            [JsonProperty(Order = -2)]
            public float[] bbox;
            [JsonProperty(Order = -2)]
            public int iscrowd;

            public static ObjectDetectionAnnotation FromBoundingBoxValue(BoundingBox2DLabeler.BoundingBoxValue bbox)
            {
                return new ObjectDetectionAnnotation
                {
                    id = (int)bbox.instance_id,
                    image_id = bbox.frame,
                    category_id = bbox.label_id,
                    segmentation = new float[]{},
                    area = bbox.width * bbox.height,
                    bbox = new []{bbox.x, bbox.y, bbox.width, bbox.height},
                    iscrowd = 0
                };
            }
        }

        [Serializable]
        public class ObjectDetectionCategory
        {
            [JsonProperty(Order = -2)]
            public int id;
            [JsonProperty(Order = -2)]
            public string name = string.Empty;
            [JsonProperty(Order = -2)]
            public string supercategory = string.Empty;
        }

        [Serializable]
        public class ObjectDetectionCategories
        {
            public List<ObjectDetectionCategory> categories;
        }

        public class KeypointAnnotation : ObjectDetectionAnnotation
        {
            public int num_keypoints;
            public float[] keypoints;


            public void CopyObjectDetectionData(ObjectDetectionAnnotation objDetection)
            {
                if (objDetection.id == this.id)
                {
                    image_id = objDetection.image_id;
                    area = objDetection.area;
                    bbox = objDetection.bbox;
                    iscrowd = objDetection.iscrowd;
                }
            }

            public static KeypointAnnotation FromKeypointValue(KeypointLabeler.KeypointEntry keypoint)
            {
                var outKeypoint = new KeypointAnnotation()
                {
                    id = (int)keypoint.instance_id,
                    image_id = keypoint.frame,
                    category_id = keypoint.label_id,
                    segmentation = new float[]{},
                    area = 0,
                    bbox = new []{0f},
                    iscrowd = 0,
                    num_keypoints = keypoint.keypoints.Length,
                    keypoints = new float[keypoint.keypoints.Length * 3]
                };

                var i = 0;
                foreach (var k in keypoint.keypoints)
                {
                    outKeypoint.keypoints[i++] = k.x;
                    outKeypoint.keypoints[i++] = k.y;
                    outKeypoint.keypoints[i++] = k.state;
                }

                return outKeypoint;
            }
        }

        [Serializable]
        public class KeypointCategory : ObjectDetectionCategory
        {
            public string[] keypoints;
            public int[][] skeleton;
        }



        [Serializable]
        public class KeypointCategories
        {
            public KeypointCategory[] categories;
        }
    }


}
