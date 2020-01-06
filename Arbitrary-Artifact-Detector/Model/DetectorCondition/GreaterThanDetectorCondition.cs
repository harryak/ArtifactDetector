using System;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    class GreaterThanDetectorCondition : BaseDetectorCondition<IComparable>, IDetectorCondition
    {
        public GreaterThanDetectorCondition(string aspectToCheck, IComparable greaterThan)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert.ChangeType(greaterThan, aspectType)) > 0)
        {
            AspectToCheck = aspectToCheck;
        }
    }
}
