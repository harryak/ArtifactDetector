using System;

namespace ItsApe.ArtifactDetector.DetectorConditions
{
    /// <summary>
    /// Specialized condition to check if an aspect of the ObjectType is less than a value.
    /// </summary>
    /// <typeparam name="ObjectType">Type of the object to get the aspect from.</typeparam>
    internal class LessThanDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, IComparable>, IDetectorCondition<ObjectType>
    {
        public LessThanDetectorCondition(string aspectToCheck, IComparable lessThan)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert(lessThan, aspectType)) < 0)
        {
            AspectToCheck = aspectToCheck;
            LessThan = lessThan;
        }

        /// <summary>
        /// Contains the value this condition compares to.
        /// </summary>
        private IComparable LessThan { get; set; }

        /// <summary>
        /// Converts the condition to a string.
        /// </summary>
        /// <returns>A string, that can be parsed back into this condition using the DetectorConditionParser.</returns>
        public override string ToString()
        {
            return AspectToCheck + "<" + LessThan.ToString();
        }
    }
}
