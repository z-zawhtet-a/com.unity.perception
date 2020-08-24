using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.ParameterBehaviours.Configuration;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    class ConfiguredParameterElement : VisualElement
    {
        bool m_Filtered;
        Type m_ParameterSampleType;

        VisualElement m_Properties;
        VisualElement m_TargetContainer;
        ToolbarMenu m_TargetPropertyMenu;
        SerializedProperty m_SerializedProperty;
        SerializedProperty m_Collapsed;
        SerializedProperty m_Target;
        SerializedProperty m_TargetGameObject;
        SerializedProperty m_TargetComponent;
        SerializedProperty m_TargetProperty;

        const string k_CollapsedParameterClass = "collapsed-parameter";

        public ParameterConfigurationEditor configEditor { get; private set; }

        public int ParameterIndex => parent.IndexOf(this);

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
            get => m_Filtered;
            set
            {
                m_Filtered = value;
                style.display = value
                    ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                    : new StyleEnum<DisplayStyle>(DisplayStyle.None);
            }
        }

        public ConfiguredParameterElement(SerializedProperty property, ParameterConfigurationEditor config)
        {
            configEditor = config;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ConfiguredParameterElement.uxml");
            template.CloneTree(this);

            m_SerializedProperty = property;
            var parameterProperty = m_SerializedProperty.FindPropertyRelative("parameter");
            var parameter = (Parameter)StaticData.GetManagedReferenceValue(parameterProperty);
            m_ParameterSampleType = parameter.sampleType;
            m_Collapsed = parameterProperty.FindPropertyRelative("collapsed");
            m_Target = m_SerializedProperty.FindPropertyRelative("target");
            m_TargetGameObject = m_Target.FindPropertyRelative("gameObject");
            m_TargetComponent = m_Target.FindPropertyRelative("component");
            m_TargetProperty = m_Target.FindPropertyRelative("property");

            this.AddManipulator(new ParameterDragManipulator());

            var removeButton = this.Q<Button>("remove-parameter");
            removeButton.RegisterCallback<MouseUpEvent>(evt => configEditor.RemoveParameter(this));

            var parameterTypeLabel = this.Query<Label>("parameter-type-label").First();
            parameterTypeLabel.text = Parameter.GetDisplayName(parameter.GetType());

            var parameterNameField = this.Q<TextField>("name");
            parameterNameField.isDelayed = true;
            parameterNameField.BindProperty(m_SerializedProperty.FindPropertyRelative("name"));

            m_TargetContainer = this.Q<VisualElement>("target-container");
            m_TargetPropertyMenu = this.Q<ToolbarMenu>("property-select-menu");

            ToggleTargetContainer();

            var frequencyField = this.Q<EnumField>("application-frequency");
            frequencyField.Init(ParameterApplicationFrequency.OnIterationStart);
            var applicationFrequency = m_Target.FindPropertyRelative("applicationFrequency");
            frequencyField.BindProperty(applicationFrequency);

            var targetField = this.Q<ObjectField>("target");
            targetField.objectType = typeof(GameObject);
            targetField.value = m_TargetGameObject.objectReferenceValue;
            targetField.RegisterCallback<ChangeEvent<Object>>(evt =>
            {
                ClearTarget();
                var appFreqEnumIndex = applicationFrequency.intValue;
                m_TargetGameObject.objectReferenceValue = (GameObject)evt.newValue;
                applicationFrequency.intValue = appFreqEnumIndex;
                m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                ToggleTargetContainer();
                FillPropertySelectMenu();
            });
            FillPropertySelectMenu();

            var collapseToggle = this.Q<VisualElement>("collapse");
            collapseToggle.RegisterCallback<MouseUpEvent>(evt => collapsed = !collapsed);

            var parameterProperties = this.Q<VisualElement>("properties");
            parameterProperties.Add(new ParameterElement(m_SerializedProperty.FindPropertyRelative("parameter")));
        }

        void ToggleTargetContainer()
        {
            m_TargetContainer.style.display = m_TargetGameObject.objectReferenceValue == null
                ? new StyleEnum<DisplayStyle>(DisplayStyle.None)
                : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }

        void ClearTarget()
        {
            m_Target.FindPropertyRelative("component").objectReferenceValue = null;
            m_Target.FindPropertyRelative("propertyName").stringValue = string.Empty;
            m_SerializedProperty.serializedObject.ApplyModifiedProperties();
        }

        void SetTarget(ParameterTarget newTarget)
        {
            m_TargetGameObject.objectReferenceValue = newTarget.gameObject;
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
            if (m_TargetGameObject.objectReferenceValue == null)
                return;

            m_TargetPropertyMenu.menu.MenuItems().Clear();
            var options = GatherPropertyOptions((GameObject)m_TargetGameObject.objectReferenceValue, m_ParameterSampleType);
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

        static List<ParameterTarget> GatherPropertyOptions(GameObject obj, Type propertyType)
        {
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
                            gameObject = obj,
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
                            gameObject = obj,
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
