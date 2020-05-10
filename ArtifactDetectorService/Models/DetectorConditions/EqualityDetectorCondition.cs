using System;

namespace ItsApe.ArtifactDetector.DetectorConditions
{
    /// <summary>
    /// Specialized condition to check if an aspect of the ObjectType equals a value.
    /// </summary>
    /// <typeparam name="ObjectType">Type of the object to get the aspect from.</typeparam>
    internal class EqualityDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, IComparable>, IDetectorCondition<ObjectType>
    {
        public EqualityDetectorCondition(string aspectToCheck, IComparable equalTo)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert(equalTo, aspectType)) == 0)
        {
            AspectToCheck = aspectToCheck;
            EqualTo = equalTo;
        }

        /// <summary>
        /// Contains the value this condition compares to.
        /// </summary>
        private IComparable EqualTo { get; set; }

        /// <summary>
        /// Converts the condition to a string.
        /// </summary>
        /// <returns>A string, that can be parsed back into this condition using the DetectorConditionParser.</returns>
        public override string ToString()
        {
            return AspectToCheck + "=" + EqualTo.ToString();
        }
    }
}
