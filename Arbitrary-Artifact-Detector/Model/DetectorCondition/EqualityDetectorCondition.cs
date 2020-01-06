using System;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    class EqualityDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, IComparable>, IDetectorCondition<ObjectType>
    {
        public EqualityDetectorCondition(string aspectToCheck, IComparable equalTo)
            : base(aspectToCheck, (IComparable aspect, Type aspectType) => aspect.CompareTo(Convert.ChangeType(equalTo, aspectType)) == 0)
        {
            AspectToCheck = aspectToCheck;
        }
    }
}
