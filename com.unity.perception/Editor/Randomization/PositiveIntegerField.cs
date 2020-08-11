using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class PositiveIntegerField : IntegerField
    {
        public int maxValue = int.MaxValue;

        public PositiveIntegerField()
        {
            RegisterValueClamping();
        }

        public PositiveIntegerField(int maxValue)
        {
            this.maxValue = maxValue;
            RegisterValueClamping();
        }

        public PositiveIntegerField(SerializedProperty property, int maxValue=int.MaxValue) : this(maxValue)
        {
            this.BindProperty(property);
        }

        void RegisterValueClamping()
        {
            this.RegisterValueChangedCallback(evt =>
            {
                value = math.clamp(evt.newValue, 0, maxValue);
                evt.StopImmediatePropagation();
            });
        }

        public new class UxmlFactory : UxmlFactory<PositiveIntegerField, UxmlTraits> { }

        public new class UxmlTraits : IntegerField.UxmlTraits
        {
            UxmlIntAttributeDescription m_Int = new UxmlIntAttributeDescription { name = "max-value", defaultValue = int.MaxValue };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (!(ve is PositiveIntegerField positiveIntegerField))
                    return;
                positiveIntegerField.maxValue = m_Int.GetValueFromBag(bag, cc);
            }
        }
    }
}
