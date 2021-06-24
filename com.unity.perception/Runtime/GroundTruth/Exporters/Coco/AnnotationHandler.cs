using System;

namespace UnityEngine.Perception.GroundTruth.Exporters.Coco
{
    public static class AnnotationHandler
    {
        public static object HandleAnnotation(AsyncAnnotation asyncAnnotation, object annotation)
        {
            object converted = annotation switch
            {
                BoundingBox2DLabeler.BoundingBoxValue bbox => CocoTypes.ObjectDetectionAnnotation.FromBoundingBoxValue(bbox),
                KeypointLabeler.KeypointEntry keypoint => CocoTypes.KeypointAnnotation.FromKeypointValue(keypoint),
                _ => null
            };

            return converted;
        }

        public static CocoTypes.KeypointCategory ToKeypointCategory(KeypointLabeler.KeypointJson keypointJson)
        {
            var keypoints = new string[keypointJson.key_points.Length];
            var skeleton = new int[keypointJson.skeleton.Length][];

            foreach (var kp in keypointJson.key_points)
            {
                keypoints[kp.index] = kp.label;
            }

            var i = 0;
            foreach (var bone in keypointJson.skeleton)
            {
                var joints = new int[]
                {
                    bone.joint1,
                    bone.joint2
                };
                skeleton[i++] = joints;
            }

            return new CocoTypes.KeypointCategory
            {
                keypoints = keypoints,
                skeleton = skeleton
            };
        }


        public static object HandleCameraCapture(int id, int width, int height, string filename)
        {
            var image = new CocoTypes.Image()
            {
                id = id,
                width = width,
                height = height,
                file_name = filename,
                license = 0,
                flickr_url = "",
                coco_url = "",
                date_captured =  DateTime.Today.ToString("D")
            };

            return image;
        }
    }
}
