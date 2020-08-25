using System;
using Randomization.ParameterBehaviours;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    [CustomEditor(typeof(ParameterBehaviour), true)]
    class ParameterBehaviourEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var iterator = serializedObject.GetIterator();

            // Start iterating over properties
            iterator.NextVisible(true);

            // Skip m_Script property
            iterator.NextVisible(false);

            // Create property fields
            do { root.Add(new PropertyField(iterator.Copy())); }
            while (iterator.NextVisible(false));

            return root;
        }
    }
}
