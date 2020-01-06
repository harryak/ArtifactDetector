namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    interface IDetectorCondition<T>
    {
        bool ObjectMatchesConditions(T objectToCheck);
    }
}
