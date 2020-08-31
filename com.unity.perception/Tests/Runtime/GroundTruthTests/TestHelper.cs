using System;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace GroundTruthTests
{
    static class TestHelper
    {
        public static GameObject CreateLabeledPlane(float scale = 10, string label = "label")
        {
            GameObject planeObject;
            planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            planeObject.transform.SetPositionAndRotation(new Vector3(0, 0, 10), Quaternion.Euler(90, 0, 0));
            planeObject.transform.localScale = new Vector3(scale, -1, scale);
            var labeling = planeObject.AddComponent<Labeling>();
            labeling.labels.Add(label);
            return planeObject;
        }

        public static GameObject CreateLabeledCube(float scale = 10, string label = "label", float x = 0, float y = 0, float z = 0, float roll = 0, float pitch = 0, float yaw = 0)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(pitch, yaw, roll));
            cube.transform.localScale = new Vector3(scale, scale, scale);
            var labeling = cube.AddComponent<Labeling>();
            labeling.labels.Add(label);
            return cube;
        }

#if UNITY_EDITOR
        public static void LoadAndStartRenderDocCapture(out UnityEditor.EditorWindow gameView)
        {
            UnityEditorInternal.RenderDoc.Load();
            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.GameView");
            gameView = UnityEditor.EditorWindow.GetWindow(type);
            UnityEditorInternal.RenderDoc.BeginCaptureRenderDoc(gameView);
        }

#endif
        public static string NormalizeJson(string json)
        {
            return json.Replace("\r\n", "\n");
        }
    }
}
