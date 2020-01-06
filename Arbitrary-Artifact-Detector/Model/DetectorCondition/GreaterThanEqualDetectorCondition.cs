using System;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    class GreaterThanEqualDetectorCondition : BaseDetectorCondition<IComparable>, IDetectorCondition
    {
        public GreaterThanEqualDetectorCondition(string aspectToCheck, IComparable greaterThan)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert.ChangeType(greaterThan, aspectType)) >= 0)
        {
            AspectToCheck = aspectToCheck;
        }
    }
}
