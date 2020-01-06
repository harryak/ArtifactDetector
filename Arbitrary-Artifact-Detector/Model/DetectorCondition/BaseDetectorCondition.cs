using ArbitraryArtifactDetector.Model;
using System;
using System.Reflection;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    /// <summary>
    /// This is a callback wrapper: Check whether the given checking function yields true if given the aspect (property name) of a DetectorResponse object.
    /// </summary>
    class BaseDetectorCondition<T> : IDetectorCondition
    {
        /// <summary>
        /// 
        /// </summary>
        protected string AspectToCheck { get; set; } = null;
        protected Func<T, Type, bool> CheckingFunction { get; set; } = null;
        protected Type AspectType { get; set; } = null;

        public BaseDetectorCondition(string aspectToCheck, Func<T, Type, bool> checkingFunction)
        {
            // Check the aspect for validity and get the type.
            PropertyInfo property = typeof(DetectorResponse).GetProperty(aspectToCheck);
            if (property == null)
            {
                throw new ArgumentException("Property not valid for class DetectorResponse.");
            }
            AspectType = property.PropertyType;
            AspectToCheck = aspectToCheck;
            CheckingFunction = checkingFunction;
        }

        public bool ResponseMatchesConditions(DetectorResponse response)
        {
            PropertyInfo property = response.GetType().GetProperty(AspectToCheck);

            if (property == null)
            {
                // Property not found.
                return false;
            }

            T t = (T) property.GetValue(response);

            return AspectToCheck != null && CheckingFunction != null
                && CheckingFunction(t, AspectType);
        }
    }
}
