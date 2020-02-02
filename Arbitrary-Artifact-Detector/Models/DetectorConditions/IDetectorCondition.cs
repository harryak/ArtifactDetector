namespace ArbitraryArtifactDetector.DetectorConditions
{
    /// <summary>
    /// General interface of any detector condition.
    /// </summary>
    /// <typeparam name="ObjectType">Type of the object to check.</typeparam>
    interface IDetectorCondition<ObjectType>
    {
        /// <summary>
        /// True if the object matches this condition.
        /// </summary>
        /// <param name="objectToCheck">The object's instance to check.</param>
        /// <returns>True if the object matches this condition.</returns>
        bool ObjectMatchesConditions(ObjectType objectToCheck);
    }
}
