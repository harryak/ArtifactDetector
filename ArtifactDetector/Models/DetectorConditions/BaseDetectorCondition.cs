using System;
using System.Reflection;

namespace ItsApe.ArtifactDetector.DetectorConditions
{
    /// <summary>
    /// This is a callback wrapper: Check whether the given checking function yields true if given the aspect (property name) of a DetectorResponse object.
    /// </summary>
    internal class BaseDetectorCondition<ObjectType, GeneralAspectType> : IDetectorCondition<ObjectType>
    {
        /// <summary>
        /// Constructor of this condition.
        /// Although one could instantiate this directly for a custom condition it is much more likely to use a derived class.
        /// </summary>
        /// <param name="aspectToCheck">Which aspect (property) of the ObjectType to check with this condition.</param>
        /// <param name="checkingFunction">Function to compare the aspect of type GeneralAspectType given its actual Type.</param>
        public BaseDetectorCondition(string aspectToCheck, Func<GeneralAspectType, Type, bool> checkingFunction)
        {
            // Check the aspect for validity and get the type.
            PropertyInfo property = typeof(ObjectType).GetProperty(aspectToCheck);
            if (property == null)
            {
                throw new ArgumentException("Property not valid for given class.");
            }

            AspectToCheck = aspectToCheck;
            CheckingFunction = checkingFunction;
            ActualAspectType = property.PropertyType;
        }

        /// <summary>
        /// The actual, specific type of the aspect.
        /// </summary>
        protected Type ActualAspectType { get; set; } = null;

        /// <summary>
        /// Which aspect (property) of the ObjectType to check with this condition.
        /// </summary>
        protected string AspectToCheck { get; set; } = null;

        /// <summary>
        /// Function to compare the aspect of type GeneralAspectType given its actual Type.
        /// </summary>
        protected Func<GeneralAspectType, Type, bool> CheckingFunction { get; set; } = null;

        /// <summary>
        /// General function to call the checkingFunction callback with the right parameters.
        /// </summary>
        /// <param name="objectToCheck">The object of type ObjectType to get the aspect from and check it.</param>
        /// <returns>True if the checking function yields true given the object's aspect.</returns>
        public bool ObjectMatchesConditions(ObjectType objectToCheck)
        {
            PropertyInfo property = objectToCheck.GetType().GetProperty(AspectToCheck);

            if (property == null)
            {
                // Property not found.
                return false;
            }

            GeneralAspectType t = (GeneralAspectType) property.GetValue(objectToCheck);

            return AspectToCheck != null && CheckingFunction != null
                && CheckingFunction(t, ActualAspectType);
        }
    }
}