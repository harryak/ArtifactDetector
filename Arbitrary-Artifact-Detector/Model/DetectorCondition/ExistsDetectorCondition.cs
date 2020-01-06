using System;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    class ExistsDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, object>, IDetectorCondition<ObjectType>
    {
        public ExistsDetectorCondition(string aspectToCheck) : base(aspectToCheck, (object aspect, Type t) => aspect != null)
        {
        }
    }
}
