using ArbitraryArtifactDetector.Model;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    interface IDetectorCondition
    {
        bool ResponseMatchesConditions(DetectorResponse response);
    }
}
