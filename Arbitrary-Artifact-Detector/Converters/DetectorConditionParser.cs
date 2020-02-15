using ItsApe.ArtifactDetector.DetectorConditions;
using ItsApe.ArtifactDetector.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ItsApe.ArtifactDetector.Converters
{
    internal class DetectorConditionParser<ObjectType>
    {
        private static readonly Dictionary<string, Func<string, string, IDetectorCondition<ObjectType>>> TranslateSimpleExpressionMap = new Dictionary<string, Func<string, string, IDetectorCondition<ObjectType>>>()
        {
            {"<", (string leftPart, string rightPart) => new LessThanDetectorCondition<ObjectType>(leftPart, rightPart) },
            {"<=", (string leftPart, string rightPart) => new LessThanEqualDetectorCondition<ObjectType>(leftPart, rightPart) },
            {">", (string leftPart, string rightPart) => new GreaterThanDetectorCondition<ObjectType>(leftPart, rightPart) },
            {">=", (string leftPart, string rightPart) => new GreaterThanEqualDetectorCondition<ObjectType>(leftPart, rightPart) },
            {"=", (string leftPart, string rightPart) => new EqualityDetectorCondition<ObjectType>(leftPart, rightPart) }
        };

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
                // Replace everything but the allowed characters for simple conditions.
                conditionString = Regex.Replace(conditionString, "[^a-zA-Z0-9<>=]", "");

                // Split the condition string at its operator
                Regex operatorStrings = new Regex(@"(<=|>=|=|<|>)");
                string[] substrings = operatorStrings.Split(conditionString);

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
            else
            {
                // Initialize return object, the conditions will be added later.
                DetectorConditionSet<ObjectType> conditions = new DetectorConditionSet<ObjectType>();

                // Replace everything but the allowed characters for general conditions.
                conditionString = Regex.Replace(conditionString, "[^a-zA-Z0-9<>=&|()]", "");

                // Get index of first opening bracket.
                int firstOpeningBracketPosition = conditionString.IndexOf('(');

                if (firstOpeningBracketPosition < 0)
                {
                    // If there is no opening bracket: Split by operators and add all.
                    Regex logicalCharacterSplit = new Regex(@"(\||&)");
                    string[] substrings = logicalCharacterSplit.Split(conditionString);

                    if (substrings[0] == "&" || substrings[0] == "|"
                        || substrings[substrings.Length - 1] == "&" || substrings[substrings.Length - 1] == "|")
                    {
                        throw new ArgumentException("Malformed condition string: Too many logical connector characters.");
                    }

                    if (substrings[1] == "&")
                    {
                        conditions.Operator = ConditionOperator.AND;
                    }
                    else if (substrings[1] == "|")
                    {
                        conditions.Operator = ConditionOperator.OR;
                    }
                    else
                    {
                        throw new ArgumentException("Malformed condition string: Invalid or missing logical connector character.");
                    }

                    for (int i = 0; i < substrings.Length; i += 2)
                    {
                        conditions.AddCondition(ParseConditionString(substrings[i]));
                    }

                    return conditions;
                }
                else if (firstOpeningBracketPosition > 1)
                {
                    // If the opening bracket is not the first character: Parse the whole string up until the bracket and add to conditions.

                    // Get logical connector to apply.
                    if (conditionString[firstOpeningBracketPosition - 1] == '&')
                    {
                        conditions.Operator = ConditionOperator.AND;
                    }
                    else if (conditionString[firstOpeningBracketPosition - 1] == '|')
                    {
                        conditions.Operator = ConditionOperator.OR;
                    }
                    else
                    {
                        throw new ArgumentException("Malformed condition string: Invalid or missing logical connector character.");
                    }

                    // Add the parsed contents up until the logical connector before the bracket.
                    conditions.AddCondition(ParseConditionString(conditionString.Substring(0, firstOpeningBracketPosition - 2)));
                }

                // Get index of last closing bracket.
                int lastClosingBracketPosition = conditionString.LastIndexOf(')');

                if (lastClosingBracketPosition < 0)
                {
                    // If there is no closing bracket: Malformed input string.
                    throw new ArgumentException("Malformed condition string: Brackets do not match.");
                }
                else if (lastClosingBracketPosition < conditionString.Length - 2)
                {
                    // If the closing bracket is not the last character: Parse the whole string after the bracket and add to conditions.

                    // Get logical connector to apply.
                    if (conditionString[lastClosingBracketPosition + 1] == '&')
                    {
                        if (conditions.Operator != ConditionOperator.AND)
                        {
                            throw new ArgumentException("Malformed condition string: Mismatch of logical connector character.");
                        }
                    }
                    else if (conditionString[lastClosingBracketPosition + 1] == '|')
                    {
                        if (conditions.Operator != ConditionOperator.OR)
                        {
                            throw new ArgumentException("Malformed condition string: Mismatch of logical connector character.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Malformed condition string: Invalid or missing logical connector character.");
                    }

                    // Add the parsed contents after the logical connector behind the bracket.
                    conditions.AddCondition(ParseConditionString(conditionString.Substring(lastClosingBracketPosition + 2)));
                }

                // Finally: Parse and add everything between the first opening and the last closing bracket by recursive call.
                conditions.AddCondition(ParseConditionString(conditionString.Substring(firstOpeningBracketPosition + 1, lastClosingBracketPosition - 1)));

                return conditions;
            }
        }
    }
}