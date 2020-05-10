using System;

namespace ItsApe.ArtifactDetector.DetectorConditions
{
    /// <summary>
    /// Specialized condition to check if an aspect of the ObjectType exists (aka. is set).
    /// </summary>
    /// <typeparam name="ObjectType">Type of the object to get the aspect from.</typeparam>
    internal class ExistsDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, object>, IDetectorCondition<ObjectType>
    {
        public ExistsDetectorCondition(string aspectToCheck) : base(aspectToCheck, (object aspect, Type t) => aspect != null)
        {
        }

        /// <summary>
        /// Converts the condition to a string.
        /// </summary>
        /// <returns>A string, that can be parsed back into this condition using the DetectorConditionParser.</returns>
        public override string ToString()
        {
            return AspectToCheck;
        }
    }
}
