using System;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    class EqualityDetectorCondition : BaseDetectorCondition<IComparable>, IDetectorCondition
    {
        public EqualityDetectorCondition(string aspectToCheck, IComparable equalTo)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert.ChangeType(equalTo, aspectType)) == 0)
        {
            AspectToCheck = aspectToCheck;
        }
    }
}
