using System;

namespace ArbitraryArtifactDetector.DetectorConditions
{
    /// <summary>
    /// Specialized condition to check if an aspect of the ObjectType is less than or equal to a value.
    /// </summary>
    /// <typeparam name="ObjectType">Type of the object to get the aspect from.</typeparam>
    internal class LessThanEqualDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, IComparable>, IDetectorCondition<ObjectType>
    {
        public LessThanEqualDetectorCondition(string aspectToCheck, IComparable lessThan)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert.ChangeType(lessThan, aspectType)) <= 0)
        {
            AspectToCheck = aspectToCheck;
        }
    }
}