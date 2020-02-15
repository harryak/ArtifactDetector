using System;

namespace ItsApe.ArtifactDetector.DetectorConditions
{
    /// <summary>
    /// Specialized condition to check if an aspect of the ObjectType exists (aka. is set).
    /// </summary>
    /// <typeparam name="ObjectType">Type of the object to get the aspect from.</typeparam>
    internal class ExistsDetectorCondition<ObjectType> : BaseDetectorCondition<ObjectType, object>, IDetectorCondition<ObjectType>
    {
        public ExistsDetectorCondition(string aspectToCheck) : base(aspectToCheck, (object aspect, Type t) => aspect != null)
        {
        }
    }
}