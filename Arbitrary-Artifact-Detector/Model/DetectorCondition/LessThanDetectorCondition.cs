using System;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    class LessThanDetectorCondition : BaseDetectorCondition<IComparable>, IDetectorCondition
    {
        public LessThanDetectorCondition(string aspectToCheck, IComparable lessThan)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert.ChangeType(lessThan, aspectType)) < 0)
        {
            AspectToCheck = aspectToCheck;
        }
    }
}
