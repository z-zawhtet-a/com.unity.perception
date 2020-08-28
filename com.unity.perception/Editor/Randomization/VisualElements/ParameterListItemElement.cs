using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.ParameterBehaviours;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    class ParameterListItemElement : VisualElement
    {
        VisualElement m_Properties;
        VisualElement m_TargetContainer;
        ToolbarMenu m_TargetPropertyMenu;
        SerializedProperty m_SerializedProperty;
        SerializedProperty m_Collapsed;

        const string k_CollapsedParameterClass = "collapsed-parameter";

        public bool collapsed
        {
            get => m_Collapsed.boolValue;
            set
            {
                m_Collapsed.boolValue = value;
                m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                if (value)
                    AddToClassList(k_CollapsedParameterClass);
                else
                    RemoveFromClassList(k_CollapsedParameterClass);
            }
        }

        public string displayName => m_SerializedProperty.displayName;

        public bool filtered
        {
            set => style.display = value
                ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        public ParameterListItemElement(SerializedProperty property)
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ParameterListItemElement.uxml");
            template.CloneTree(this);

            m_SerializedProperty = property;
            var parameter = (Parameter)StaticData.GetManagedReferenceValue(property);
            m_Collapsed = property.FindPropertyRelative("collapsed");

            var parameterTypeLabel = this.Query<Label>("parameter-type-label").First();
            parameterTypeLabel.text = Parameter.GetDisplayName(parameter.GetType());

            var parameterNameField = this.Q<TextElement>("name");
            parameterNameField.text = property.displayName;

            var collapseToggle = this.Q<VisualElement>("collapse");
            collapseToggle.RegisterCallback<MouseUpEvent>(evt => collapsed = !collapsed);

            var parameterProperties = this.Q<VisualElement>("properties");
            parameterProperties.Add(new ParameterElement(property));
        }
    }
}
