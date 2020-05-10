using System;

namespace ItsApe.ArtifactDetector.DetectorConditions
{
    /// <summary>
    /// Specialized condition to check if an aspect of the ObjectType is less than or equal to a value.
    /// </summary>
    /// <typeparam name="ObjectType">Type of the object to get the aspect from.</typeparam>
    internal class LessThanEqualDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, IComparable>, IDetectorCondition<ObjectType>
    {
        public LessThanEqualDetectorCondition(string aspectToCheck, IComparable lessThanEqual)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert(lessThanEqual, aspectType)) <= 0)
        {
            AspectToCheck = aspectToCheck;
            LessThanEqual = lessThanEqual;
        }

        /// <summary>
        /// Contains the value this condition compares to.
        /// </summary>
        private IComparable LessThanEqual { get; set; }

        /// <summary>
        /// Converts the condition to a string.
        /// </summary>
        /// <returns>A string, that can be parsed back into this condition using the DetectorConditionParser.</returns>
        public override string ToString()
        {
            return AspectToCheck + "<=" + LessThanEqual.ToString();
        }
    }
}
