using System;
using Randomization.ParameterBehaviours;
using UnityEditor;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    [CustomEditor(typeof(ParameterAsset), true)]
    public class ParametersAssetEditor : UnityEditor.Editor
    {
        VisualElement m_Root;
        VisualElement m_ParameterContainer;
        SerializedProperty m_Parameters;

        string m_FilterString = string.Empty;
        string FilterString
        {
            set
            {
                m_FilterString = value;
                var lowerFilter = m_FilterString.ToLower();
                foreach (var child in m_ParameterContainer.Children())
                {
                    var param = (ParameterListItemElement)child;
                    param.filtered = param.displayName.ToLower().Contains(lowerFilter);
                }
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ParameterAssetEditor.uxml").CloneTree();

            m_ParameterContainer = m_Root.Q<VisualElement>("parameters-container");

            var filter = m_Root.Q<TextField>("filter-parameters");
            filter.RegisterValueChangedCallback((e) => { FilterString = e.newValue; });

            var collapseAllButton = m_Root.Q<Button>("collapse-all");
            collapseAllButton.clicked += () => CollapseParameters(true);

            var expandAllButton = m_Root.Q<Button>("expand-all");
            expandAllButton.clicked += () => CollapseParameters(false);

            RefreshParameterElements();

            return m_Root;
        }

        void RefreshParameterElements()
        {
            m_ParameterContainer.Clear();
            var properties = serializedObject.GetIterator();
            if (properties.NextVisible(true))
            {
                do
                {
                    var propertyValue = StaticData.GetManagedReferenceValue(properties);
                    if (propertyValue != null && propertyValue.GetType().IsSubclassOf(typeof(Parameter)))
                        m_ParameterContainer.Add(new ParameterListItemElement(properties.Copy()));
                } while (properties.NextVisible(false));
            }
        }

        void CollapseParameters(bool collapsed)
        {
            foreach (var child in m_ParameterContainer.Children())
                ((ParameterListItemElement)child).collapsed = collapsed;
        }
    }
}
