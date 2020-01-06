using System;
using System.Reflection;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    /// <summary>
    /// This is a callback wrapper: Check whether the given checking function yields true if given the aspect (property name) of a DetectorResponse object.
    /// </summary>
    class BaseDetectorCondition<ObjectType, GeneralAspectType> : IDetectorCondition<ObjectType>
    {
        /// <summary>
        /// 
        /// </summary>
        protected string AspectToCheck { get; set; } = null;
        protected Func<GeneralAspectType, Type, bool> CheckingFunction { get; set; } = null;
        protected Type ActualAspectType { get; set; } = null;

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
