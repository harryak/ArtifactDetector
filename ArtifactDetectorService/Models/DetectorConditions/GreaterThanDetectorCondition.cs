using System;

namespace ItsApe.ArtifactDetector.DetectorConditions
{
    /// <summary>
    /// Specialized condition to check if an aspect of the ObjectType is greater than a value.
    /// </summary>
    /// <typeparam name="ObjectType">Type of the object to get the aspect from.</typeparam>
    internal class GreaterThanDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, IComparable>, IDetectorCondition<ObjectType>
    {
        public GreaterThanDetectorCondition(string aspectToCheck, IComparable greaterThan)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert(greaterThan, aspectType)) > 0)
        {
            AspectToCheck = aspectToCheck;
            GreaterThan = greaterThan;
        }

        /// <summary>
        /// Contains the value this condition compares to.
        /// </summary>
        private IComparable GreaterThan { get; set; }

        /// <summary>
        /// Converts the condition to a string.
        /// </summary>
        /// <returns>A string, that can be parsed back into this condition using the DetectorConditionParser.</returns>
        public override string ToString()
        {
            return AspectToCheck + ">" + GreaterThan.ToString();
        }
    }
}
