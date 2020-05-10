using System;

namespace ItsApe.ArtifactDetector.DetectorConditions
{
    /// <summary>
    /// Specialized condition to check if an aspect of the ObjectType greater than or equal to a value.
    /// </summary>
    /// <typeparam name="ObjectType">Type of the object to get the aspect from.</typeparam>
    internal class GreaterThanEqualDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, IComparable>, IDetectorCondition<ObjectType>
    {
        public GreaterThanEqualDetectorCondition(string aspectToCheck, IComparable greaterThanEqual)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert(greaterThanEqual, aspectType)) >= 0)
        {
            AspectToCheck = aspectToCheck;
            GreaterThanEqual = greaterThanEqual;
        }

        /// <summary>
        /// Contains the value this condition compares to.
        /// </summary>
        private IComparable GreaterThanEqual { get; set; }

        /// <summary>
        /// Converts the condition to a string.
        /// </summary>
        /// <returns>A string, that can be parsed back into this condition using the DetectorConditionParser.</returns>
        public override string ToString()
        {
            return AspectToCheck + ">=" + GreaterThanEqual.ToString();
        }
    }
}
