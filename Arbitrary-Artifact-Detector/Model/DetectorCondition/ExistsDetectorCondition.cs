using System;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    class ExistsDetectorCondition : BaseDetectorCondition<object>, IDetectorCondition
    {
        public ExistsDetectorCondition(string aspectToCheck) : base(aspectToCheck, (object aspect, Type t) => aspect != null)
        {
        }
    }
}
