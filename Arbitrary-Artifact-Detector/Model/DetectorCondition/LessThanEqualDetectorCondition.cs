using System;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    class LessThanEqualDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, IComparable>, IDetectorCondition<ObjectType>
    {
        public LessThanEqualDetectorCondition(string aspectToCheck, IComparable lessThan)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert.ChangeType(lessThan, aspectType)) <= 0)
        {
            AspectToCheck = aspectToCheck;
        }
    }
}
