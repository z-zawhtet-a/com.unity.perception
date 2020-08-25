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
        Type m_ParameterSampleType;
        VisualElement m_Properties;
        VisualElement m_TargetContainer;
        ToolbarMenu m_TargetPropertyMenu;
        SerializedProperty m_SerializedProperty;
        SerializedProperty m_Collapsed;
        SerializedProperty m_Target;
        SerializedProperty m_TargetComponent;
        SerializedProperty m_TargetProperty;

        const string k_CollapsedParameterClass = "collapsed-parameter";

        public ParameterListEditor configEditor { get; }

        public int ParameterIndex => parent.IndexOf(this);

        GameObject gameObject => ((ParameterList)m_SerializedProperty.serializedObject.targetObject).gameObject;
        GameObject targetGameObject => m_TargetComponent.objectReferenceValue != null
                ? ((Component)m_TargetComponent.objectReferenceValue).gameObject : null;

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

        public bool filtered
        {
            set => style.display = value
                ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        public ParameterListItemElement(SerializedProperty property, ParameterListEditor config)
        {
            configEditor = config;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ParameterListItemElement.uxml");
            template.CloneTree(this);

            m_SerializedProperty = property;
            var parameterProperty = m_SerializedProperty.FindPropertyRelative("parameter");
            var parameter = (Parameter)StaticData.GetManagedReferenceValue(parameterProperty);
            m_ParameterSampleType = parameter.sampleType;
            m_Collapsed = parameterProperty.FindPropertyRelative("collapsed");
            m_Target = m_SerializedProperty.FindPropertyRelative("target");
            m_TargetComponent = m_Target.FindPropertyRelative("component");
            m_TargetProperty = m_Target.FindPropertyRelative("propertyName");

            this.AddManipulator(new ParameterDragManipulator());

            var removeButton = this.Q<Button>("remove-parameter");
            removeButton.RegisterCallback<MouseUpEvent>(evt => configEditor.RemoveParameter(this));

            var parameterTypeLabel = this.Query<Label>("parameter-type-label").First();
            parameterTypeLabel.text = Parameter.GetDisplayName(parameter.GetType());

            var parameterNameField = this.Q<TextField>("name");
            parameterNameField.isDelayed = true;
            parameterNameField.BindProperty(m_SerializedProperty.FindPropertyRelative("name"));

            var targetObj = targetGameObject;
            if (targetObj == null)
                m_TargetProperty.stringValue = string.Empty;
            else if (targetObj != gameObject)
            {
                var component = (Component)m_TargetComponent.objectReferenceValue;
                m_TargetComponent.objectReferenceValue = gameObject.GetComponent(component.GetType());
                m_SerializedProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            m_TargetPropertyMenu = this.Q<ToolbarMenu>("property-select-menu");
            FillPropertySelectMenu();

            var frequencyField = this.Q<EnumField>("application-frequency");
            frequencyField.Init(ParameterApplicationFrequency.EveryIteration);
            var applicationFrequency = m_Target.FindPropertyRelative("applicationFrequency");
            frequencyField.BindProperty(applicationFrequency);

            var collapseToggle = this.Q<VisualElement>("collapse");
            collapseToggle.RegisterCallback<MouseUpEvent>(evt => collapsed = !collapsed);

            var parameterProperties = this.Q<VisualElement>("properties");
            parameterProperties.Add(new ParameterElement(m_SerializedProperty.FindPropertyRelative("parameter")));
        }

        void SetTarget(ParameterTarget newTarget)
        {
            m_Target.FindPropertyRelative("component").objectReferenceValue = newTarget.component;
            m_Target.FindPropertyRelative("propertyName").stringValue = newTarget.propertyName;
            m_Target.FindPropertyRelative("fieldOrProperty").intValue = (int)newTarget.fieldOrProperty;
            m_SerializedProperty.serializedObject.ApplyModifiedProperties();
            m_TargetPropertyMenu.text = TargetPropertyDisplayText(newTarget.component, newTarget.propertyName);
        }

        static string TargetPropertyDisplayText(Component component, string propertyName)
        {
            return $"{component.GetType().Name}.{propertyName}";
        }

        void FillPropertySelectMenu()
        {
            m_TargetPropertyMenu.menu.MenuItems().Clear();
            var options = GatherPropertyOptions(m_ParameterSampleType);
            if (options.Count == 0)
            {
                m_TargetPropertyMenu.text = "No compatible properties";
                m_TargetPropertyMenu.SetEnabled(false);
            }
            else
            {
                m_TargetPropertyMenu.SetEnabled(true);
                foreach (var option in options)
                {
                    m_TargetPropertyMenu.menu.AppendAction(
                        TargetPropertyDisplayText(option.component, option.propertyName),
                        a => { SetTarget(option); });
                }
                m_TargetPropertyMenu.text = m_TargetProperty.stringValue == string.Empty
                    ? "Select a property"
                    : TargetPropertyDisplayText((Component)m_TargetComponent.objectReferenceValue, m_TargetProperty.stringValue);
            }
        }

        List<ParameterTarget> GatherPropertyOptions(Type propertyType)
        {
            var obj = ((ParameterList)m_SerializedProperty.serializedObject.targetObject).gameObject;
            var options = new List<ParameterTarget>();
            foreach (var component in obj.GetComponents<Component>())
            {
                if (component == null)
                    continue;
                var componentType = component.GetType();
                var fieldInfos = componentType.GetFields();
                foreach (var fieldInfo in fieldInfos)
                {
                    if (fieldInfo.FieldType == propertyType && fieldInfo.IsPublic && !fieldInfo.IsInitOnly)
                        options.Add(new ParameterTarget()
                        {
                            component = component,
                            propertyName = fieldInfo.Name,
                            fieldOrProperty = FieldOrProperty.Field
                        });
                }

                var propertyInfos = componentType.GetProperties();
                foreach (var propertyInfo in propertyInfos)
                {
                    if (propertyInfo.PropertyType == propertyType && propertyInfo.GetSetMethod() != null)
                        options.Add(new ParameterTarget()
                        {
                            component = component,
                            propertyName = propertyInfo.Name,
                            fieldOrProperty = FieldOrProperty.Property
                        });
                }
            }
            return options;
        }
    }
}
