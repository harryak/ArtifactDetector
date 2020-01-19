using System.Collections.Generic;
using System.Linq;

namespace ArbitraryArtifactDetector.DetectorCondition.Model
{
    /// <summary>
    /// Representation of the logical connector for a DetectorConditionSet.
    /// </summary>
    internal enum ConditionOperator
    {
        AND,
        OR
    }

    internal class DetectorConditionSet<ObjectType> : IDetectorCondition<ObjectType>
    {
        /// <summary>
        /// Constructor for an empty set.
        /// </summary>
        /// <param name="conditionOperator">Optional. Connecting logical operator.</param>
        public DetectorConditionSet(ConditionOperator conditionOperator = ConditionOperator.AND)
        {
            Operator = conditionOperator;
        }

        /// <summary>
        /// Constructor for a non-empty set.
        /// </summary>
        /// <param name="conditions">Array of conditions to be added.</param>
        /// <param name="conditionOperator">Optional. Connecting logical operator.</param>
        public DetectorConditionSet(IDetectorCondition<ObjectType>[] conditions, ConditionOperator conditionOperator = ConditionOperator.AND)
        {
            Conditions = conditions.ToList();
            Operator = conditionOperator;
        }

        /// <summary>
        /// Constructor for a non-empty set.
        /// </summary>
        /// <param name="conditions">List of conditions to be added.</param>
        /// <param name="conditionOperator">Optional. Connecting logical operator.</param>
        public DetectorConditionSet(List<IDetectorCondition<ObjectType>> conditions, ConditionOperator conditionOperator = ConditionOperator.AND)
        {
            Conditions = conditions;
            Operator = conditionOperator;
        }

        /// <summary>
        /// The connecting operator for this set of conditions.
        /// </summary>
        public ConditionOperator Operator { get; set; } = ConditionOperator.AND;

        /// <summary>
        /// All conditions in this set. Can be condition sets themselves.
        /// </summary>
        private List<IDetectorCondition<ObjectType>> Conditions { get; set; } = new List<IDetectorCondition<ObjectType>>();

        /// <summary>
        /// Add another condition to the set of conditions.
        /// </summary>
        /// <param name="condition">The condition to add.</param>
        public void AddCondition(IDetectorCondition<ObjectType> condition)
        {
            Conditions.Add(condition);
        }

        /// <summary>
        /// Test whether the set is non-empty.
        /// </summary>
        /// <returns>True if it is not empty, false otherwise.</returns>
        public bool NotEmpty()
        {
            return Conditions.Count > 0;
        }

        /// <summary>
        /// Implements interface: Recursively evaluate all conditions in this set and connect them with the logical Operator.
        /// </summary>
        /// <param name="response">The detector response to evaluate.</param>
        /// <returns>True if the conditions in this set apply to the response or this is an empty set.</returns>
        public bool ObjectMatchesConditions(ObjectType objectToCheck)
        {
            // Empty set of conditions means "matches".
            if (Conditions.Count < 1)
            {
                return true;
            }

            // Get variable for the return value.
            bool returnValue;

            // Set return variable to inital value depending on the logical operator.
            if (Operator == ConditionOperator.AND)
            {
                returnValue = true;
            }
            else
            {
                returnValue = false;
            }

            // Go through all the conditions in this set to evaluate them.
            foreach (IDetectorCondition<ObjectType> condition in Conditions)
            {
                // Connect the condition by the logical operator.
                if (Operator == ConditionOperator.AND)
                {
                    returnValue &= condition.ObjectMatchesConditions(objectToCheck);
                }
                else
                {
                    returnValue |= condition.ObjectMatchesConditions(objectToCheck);
                }
            }

            return returnValue;
        }
    }
}