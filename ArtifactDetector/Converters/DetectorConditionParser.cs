using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ItsApe.ArtifactDetector.DetectorConditions;
using ItsApe.ArtifactDetector.Helpers;

namespace ItsApe.ArtifactDetector.Converters
{
    /// <summary>
    /// Class for parsing conditional strings for ItsApe.ArtifactDetector.Detectors.IDetector instances.
    /// Generally speaking it can parse equations and inequations of a specific form to ItsApe.ArtifactDetector.DetectorConditions.IDetectorCondition objects.
    /// </summary>
    /// <typeparam name="ObjectType">Passed through to the detector condition objects. One of ArtifactRuntimeInformation or DetectorResponse.</typeparam>
    internal class DetectorConditionParser<ObjectType>
    {
        /// <summary>
        /// Map used to get the right condition object from the given operator.
        /// </summary>
        private static readonly Dictionary<string, Func<string, string, IDetectorCondition<ObjectType>>> TranslateSimpleExpressionMap =
            new Dictionary<string, Func<string, string, IDetectorCondition<ObjectType>>>()
        {
            {"<", (string leftPart, string rightPart) => new LessThanDetectorCondition<ObjectType>(leftPart, rightPart) },
            {"<=", (string leftPart, string rightPart) => new LessThanEqualDetectorCondition<ObjectType>(leftPart, rightPart) },
            {">", (string leftPart, string rightPart) => new GreaterThanDetectorCondition<ObjectType>(leftPart, rightPart) },
            {">=", (string leftPart, string rightPart) => new GreaterThanEqualDetectorCondition<ObjectType>(leftPart, rightPart) },
            {"=", (string leftPart, string rightPart) => new EqualityDetectorCondition<ObjectType>(leftPart, rightPart) }
        };

        /// <summary>
        /// Parse the given string into a detector condition object.
        ///
        /// The following rules apply:
        /// conditionString   = " " conditionString | conditionString " " | "none" | simpleCondition | "(" conditionString ")" | compoundCondition.
        /// simpleCondition   = literal | literal operator value.
        /// compoundCondition = conditionString logicalConnector conditionString.
        /// logicalConnector  = "&" | "|"
        /// literal           = "a" | ... | "z" | "A" | ... | "Z" | literal value | literal literal.
        /// operator          = "<" | ">" | "<=" | ">=" | "=".
        /// value             = "0" | ... | "9" | value value.
        ///
        /// Notes: The literals should refer to properties of the given ObjectType's class, otherwise testing against
        /// the resulting objects will lead to an exception.
        /// </summary>
        /// <param name="conditionString">A conditional string following the above rules.</param>
        /// <returns>An instance of IDetectorCondition corresponding to the given condition string.</returns>
        public static IDetectorCondition<ObjectType> ParseConditionString(string conditionString)
        {
            // Special word "none"
            if (conditionString == "" || conditionString == "none")
            {
                // Empty condition set.
                return new DetectorConditionSet<ObjectType>();
            }

            // First check simplest case: No conjunction or disjunction.
            if (!conditionString.Contains('&') && !conditionString.Contains('|'))
            {
                return ParseSimpleConditionString(conditionString);
            }
            else
            {
                return ParseComplexConditionString(conditionString);
            }
        }

        /// <summary>
        /// Call this with an operator and some set of conditions to check whether the new operator meets the old ones.
        /// If the new operator does not comply with the old ones, this will throw an ArgumentException.
        /// </summary>
        /// <param name="newOperator">Operator to check.</param>
        /// <param name="previousConditions">Conditions connected with an operator.</param>
        private static void CheckOperatorCompliance(char newOperator, ref DetectorConditionSet<ObjectType> previousConditions)
        {
            switch (newOperator)
            {
                case '&':
                    if (previousConditions.Operator != ConditionOperator.AND)
                        throw new ArgumentException("Malformed condition string: Mismatch of logical connector character.");
                    break;

                case '|':
                    if (previousConditions.Operator != ConditionOperator.OR)
                        throw new ArgumentException("Malformed condition string: Mismatch of logical connector character.");
                    break;

                default:
                    throw new ArgumentException("Malformed condition string: Invalid or missing logical connector character.");
            }
        }

        /// <summary>
        /// Check if a given character is a valid logical operator.
        /// </summary>
        /// <param name="testCharacter">Character to test.</param>
        /// <returns></returns>
        private static bool IsLogicalOperator(char testCharacter)
        {
            return "&|".Any(logicalOperator => logicalOperator == testCharacter);
        }

        /// <summary>
        /// Parse a complex condition string following the rules defined at ParseConditionString.
        /// </summary>
        /// <param name="complexConditionString">The condition string following the rules.</param>
        /// <returns>An instance of IDetectorCondition corresponding to the given condition string.</returns>
        private static IDetectorCondition<ObjectType> ParseComplexConditionString(string complexConditionString)
        {
            // Replace everything but the allowed characters for general conditions.
            complexConditionString = Regex.Replace(complexConditionString, "[^a-zA-Z0-9<>=&|()]", "");

            // Get index of first opening bracket.
            int firstOpeningBracketPosition = complexConditionString.IndexOf('(');

            if (firstOpeningBracketPosition < 0)
            {
                // If there is no opening bracket: Split by operators and add all.
                return ParseCompoundConditionString(complexConditionString);
            }

            // Initialize return object, the conditions will be added later.
            var conditions = new DetectorConditionSet<ObjectType>();
            if (firstOpeningBracketPosition > 1)
            {
                // If the opening bracket is not the first character: Parse the whole string up until the bracket and add to conditions.

                // Get logical connector to apply.
                conditions.Operator = ParseLogicalOperator(complexConditionString[firstOpeningBracketPosition - 1]);

                // Add the parsed contents up until the logical connector before the bracket.
                conditions.AddCondition(ParseConditionString(complexConditionString.Substring(0, firstOpeningBracketPosition - 2)));
            }

            // Get index of last closing bracket.
            int lastClosingBracketPosition = complexConditionString.LastIndexOf(')');

            if (lastClosingBracketPosition < 0)
            {
                // If there is no closing bracket: Malformed input string.
                throw new ArgumentException("Malformed condition string: Brackets do not match.");
            }
            else if (lastClosingBracketPosition < complexConditionString.Length - 2)
            {
                // If the closing bracket is not the last character: Parse the whole string after the bracket and add to conditions.

                // This function throws if the new operator does not match the old ones.
                CheckOperatorCompliance(complexConditionString[lastClosingBracketPosition + 1], ref conditions);

                // Add the parsed contents after the logical connector behind the bracket.
                conditions.AddCondition(ParseConditionString(complexConditionString.Substring(lastClosingBracketPosition + 2)));
            }

            // Finally: Parse and add everything between the first opening and the last closing bracket by recursive call.
            conditions.AddCondition(ParseConditionString(complexConditionString.Substring(firstOpeningBracketPosition + 1, lastClosingBracketPosition - 1)));

            return conditions;
        }

        /// <summary>
        /// Parse a compound condition string following the rules defined at ParseConditionString.
        /// </summary>
        /// <param name="compoundConditionString">The condition string following the rules.</param>
        /// <returns>An instance of IDetectorCondition corresponding to the given condition string.</returns>
        private static IDetectorCondition<ObjectType> ParseCompoundConditionString(string compoundConditionString)
        {
            // Initialize return object, the conditions will be added later.
            var conditions = new DetectorConditionSet<ObjectType>();

            var logicalCharacterSplit = new Regex(@"(\||&)");
            string[] substrings = logicalCharacterSplit.Split(compoundConditionString);

            if (IsLogicalOperator(substrings[0].First()) || IsLogicalOperator(substrings[substrings.Length - 1].First()))
            {
                throw new ArgumentException("Malformed condition string: Too many logical connector characters.");
            }

            conditions.Operator = ParseLogicalOperator(substrings[1].First());

            // Add every other substring as new condition part, as the ones in between are the logical operators.
            for (int i = 0; i < substrings.Length; i += 2)
            {
                conditions.AddCondition(ParseConditionString(substrings[i]));
            }

            return conditions;
        }

        /// <summary>
        /// Parse a logical connector string following the rules defined at ParseConditionString.
        /// </summary>
        /// <param name="logicalConnectorString">The logical connector following the rules for compoundCondition.</param>
        /// <returns></returns>
        private static ConditionOperator ParseLogicalOperator(char logicalConnectorString)
        {
            switch (logicalConnectorString)
            {
                case '&':
                    return ConditionOperator.AND;

                case '|':
                    return ConditionOperator.OR;

                default:
                    throw new ArgumentException("Malformed condition string: Invalid or missing logical connector character.");
            }
        }

        /// <summary>
        /// Parse a simple condition string following the rules defined at ParseConditionString.
        /// </summary>
        /// <param name="simpleConditionString">The condition string following the rules for simpleCondition.</param>
        /// <returns>An instance of IDetectorCondition corresponding to the given condition string.</returns>
        private static IDetectorCondition<ObjectType> ParseSimpleConditionString(string simpleConditionString)
        {
            // Replace everything but the allowed characters for simple conditions.
            simpleConditionString = Regex.Replace(simpleConditionString, "[^a-zA-Z0-9<>=]", "");

            // Split the condition string at its operator
            var operatorStrings = new Regex(@"(<=|>=|=|<|>)");
            string[] substrings = operatorStrings.Split(simpleConditionString);

            if (substrings.Length == 1)
            {
                return new ExistsDetectorCondition<ObjectType>(substrings[0]);
            }
            else if (substrings.Length == 3)
            {
                return TranslateSimpleExpressionMap[substrings[1]](substrings[0].CapitalizeFirstLetter(), substrings[2]);
            }

            throw new ArgumentOutOfRangeException("Can not parse the condition string.");
        }
    }
}
