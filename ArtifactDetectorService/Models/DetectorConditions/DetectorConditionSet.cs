using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ItsApe.ArtifactDetector.DetectorConditions
{
    /// <summary>
    /// Representation of the logical connector for a DetectorConditionSet.
    /// 
    /// Use the Display.Name as "friendly name" for ToString() of this condition set.
    /// </summary>
    internal enum ConditionOperator
    {
        [Display(Name = "&")]
        AND,

        [Display(Name = "|")]
        OR
    }

    /// <summary>
    /// A collection of multiple detector conditions acting as a single condition.
    /// </summary>
    /// <typeparam name="ObjectType">Type of the checked object.</typeparam>
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
        public bool ObjectMatchesConditions(ref ObjectType objectToCheck)
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
            for (var i = 0; i < Conditions.Count; i++)
            {
                // Connect the condition by the logical operator.
                if (Operator == ConditionOperator.AND)
                {
                    returnValue &= Conditions[i].ObjectMatchesConditions(ref objectToCheck);
                }
                else
                {
                    returnValue |= Conditions[i].ObjectMatchesConditions(ref objectToCheck);
                }
            }

            return returnValue;
        }

        /// <summary>
        /// Converts the condition set to a string.
        /// </summary>
        /// <returns>A string, that can be parsed back into this condition using the DetectorConditionParser.</returns>
        public override string ToString()
        {
            if (Conditions.Count < 1)
            {
                return "";
            }

            string output = "(";

            for (var i = 0; i < Conditions.Count; i++)
            {
                output += Conditions[i].ToString();

                // Add the Operator by its "friendly name".
                if (i < Conditions.Count - 1)
                {
                    output += Operator.GetType().GetMember(Operator.ToString()).First().Name;
                }
            }

            return output + ")";
        }
    }
}
