using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.ParameterBehaviours;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    [CustomEditor(typeof(ParameterList))]
    class ParameterListEditor : UnityEditor.Editor
    {
        VisualElement m_Root;
        VisualElement m_ParameterContainer;
        SerializedProperty m_Parameters;

        public ParameterList config;

        string m_FilterString = string.Empty;
        public string FilterString
        {
            get => m_FilterString;
            private set
            {
                m_FilterString = value;
                var lowerFilter = m_FilterString.ToLower();
                foreach (var child in m_ParameterContainer.Children())
                {
                    var paramIndex = m_ParameterContainer.IndexOf(child);
                    var param = config.configuredParameters[paramIndex];
                    ((ParameterListItemElement)child).filtered = param.name.ToLower().Contains(lowerFilter);
                }
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            config = (ParameterList)target;
            m_Parameters = serializedObject.FindProperty("configuredParameters");
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ParameterListEditor.uxml").CloneTree();

            m_ParameterContainer = m_Root.Q<VisualElement>("parameters-container");

            var filter = m_Root.Q<TextField>("filter-parameters");
            filter.RegisterValueChangedCallback((e) => { FilterString = e.newValue; });

            var collapseAllButton = m_Root.Q<Button>("collapse-all");
            collapseAllButton.clicked += () => CollapseParameters(true);

            var expandAllButton = m_Root.Q<Button>("expand-all");
            expandAllButton.clicked += () => CollapseParameters(false);

            var parameterTypeMenu = m_Root.Q<ToolbarMenu>("parameter-type-menu");
            foreach (var parameterType in StaticData.parameterTypes)
            {
                parameterTypeMenu.menu.AppendAction(
                    Parameter.GetDisplayName(parameterType),
                    a => { AddParameter(parameterType); },
                    a => DropdownMenuAction.Status.Normal);
            }

            RefreshParameterElements();

            return m_Root;
        }

        void RefreshParameterElements()
        {
            m_ParameterContainer.Clear();
            for (var i = 0; i < m_Parameters.arraySize; i++)
                m_ParameterContainer.Add(new ParameterListItemElement(m_Parameters.GetArrayElementAtIndex(i), this));
        }

        void AddParameter(Type parameterType)
        {
            var configuredParameter = config.AddParameter($"Parameter{m_Parameters.arraySize}", parameterType);
            configuredParameter.parameter.RandomizeSamplers();
            serializedObject.Update();
            RefreshParameterElements();
        }

        public void RemoveParameter(VisualElement template)
        {
            var paramIndex = m_ParameterContainer.IndexOf(template);
            m_ParameterContainer.RemoveAt(paramIndex);
            config.configuredParameters.RemoveAt(paramIndex);
            serializedObject.Update();
            RefreshParameterElements();
        }

        public void ReorderParameter(int currentIndex, int nextIndex)
        {
            if (currentIndex == nextIndex)
                return;

            if (nextIndex > currentIndex)
                nextIndex--;

            var parameterElement = m_ParameterContainer[currentIndex];
            var parameter = config.configuredParameters[currentIndex];

            parameterElement.RemoveFromHierarchy();
            config.configuredParameters.RemoveAt(currentIndex);

            m_ParameterContainer.Insert(nextIndex, parameterElement);
            config.configuredParameters.Insert(nextIndex, parameter);

            serializedObject.Update();

            RefreshParameterElements();
        }

        void CollapseParameters(bool collapsed)
        {
            foreach (var child in m_ParameterContainer.Children())
                ((ParameterListItemElement)child).collapsed = collapsed;
        }
    }
}
