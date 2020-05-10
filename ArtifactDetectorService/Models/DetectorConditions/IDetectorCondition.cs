namespace ItsApe.ArtifactDetector.DetectorConditions
{
    /// <summary>
    /// General interface of any detector condition.
    /// </summary>
    /// <typeparam name="ObjectType">Type of the object to check.</typeparam>
    internal interface IDetectorCondition<ObjectType>
    {
        /// <summary>
        /// True if the object matches this condition.
        /// </summary>
        /// <param name="objectToCheck">The object's instance to check.</param>
        /// <returns>True if the object matches this condition.</returns>
        bool ObjectMatchesConditions(ref ObjectType objectToCheck);

        /// <summary>
        /// Converts the condition to a string.
        /// </summary>
        /// <returns>A string, that can be parsed back into this condition using the DetectorConditionParser.</returns>
        string ToString();
    }
}
