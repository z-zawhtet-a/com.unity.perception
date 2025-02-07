﻿using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Annotation definition for an instance segmentation
    /// </summary>
    public class InstanceSegmentationDefinition : AnnotationDefinition
    {
        public const string labelDescription = "Produces an instance segmentation image for each frame. The image will render the pixels of each labeled object in a distinct color.";

        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.InstanceSegmentationAnnotation";

        /// <inheritdoc/>
        public override string description => labelDescription;

        /// <summary>
        /// Creates an instance segmentation definition.
        /// </summary>
        /// <param name="id">The registered ID for this definition</param>
        /// <param name="spec">The label config</param>
        public InstanceSegmentationDefinition(string id, IdLabelConfig.LabelEntrySpec[] spec)
            : base(id)
        {
            this.spec = spec;
        }

        /// <summary>
        /// Label config for the simulation
        /// </summary>
        public IdLabelConfig.LabelEntrySpec[] spec { get; }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            foreach (var e in spec)
            {
                var nested = builder.AddNestedMessageToVector("spec");
                e.ToMessage(nested);
            }
        }
    }
}
