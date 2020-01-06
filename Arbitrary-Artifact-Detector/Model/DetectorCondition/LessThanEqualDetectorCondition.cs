using System;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    class LessThanEqualDetectorCondition : BaseDetectorCondition<IComparable>, IDetectorCondition
    {
        public LessThanEqualDetectorCondition(string aspectToCheck, IComparable lessThan)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert.ChangeType(lessThan, aspectType)) <= 0)
        {
            AspectToCheck = aspectToCheck;
        }
    }
}
