using System;
using System.Collections.Generic;
using System.Linq;

namespace ItsApe.OpenWindowDetector.Helpers
{
    /// <summary>
    /// Class to extend the String class by helper methods.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Capitalize the first letter of the given string.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>Input string with first letter in upper case.</returns>
        public static string CapitalizeFirstLetter(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": return input;
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }

        /// <summary>
        /// Checks the source string for substring value.
        /// </summary>
        /// <param name="source">Haystack string.</param>
        /// <param name="value">Possible substring, needle.</param>
        /// <param name="comparisonType">E.g. InvariantCultureIgnoreCase.</param>
        /// <returns>True if the needle was found in the haystack.</returns>
        public static bool Contains(this string source, string value, StringComparison comparisonType)
        {
            return source?.IndexOf(value, comparisonType) >= 0;
        }

        /// <summary>
        /// Checks the source for any of the substrings in values.
        /// </summary>
        /// <param name="source">Haystack string.</param>
        /// <param name="values">Possible substrings, needles.</param>
        /// <param name="comparisonType">E.g. InvariantCultureIgnoreCase.</param>
        /// <returns>Tue if any needle was found in the haystack.</returns>
        public static bool ContainsAny(this string source, IList<string> values)
        {
            return values.FirstOrDefault(
                            s => Contains(source, s, StringComparison.InvariantCultureIgnoreCase)
                        ) != default(string);
        }

        /// <summary>
        /// Checks the source for any of the substrings in values.
        /// </summary>
        /// <param name="source">Haystack string.</param>
        /// <param name="values">Possible substrings, needles.</param>
        /// <param name="comparisonType">E.g. InvariantCultureIgnoreCase.</param>
        /// <returns>Tue if any needle was found in the haystack.</returns>
        public static bool ContainsAny(this string source, IList<string> values, StringComparison comparisonType)
        {
            return values.FirstOrDefault(
                            s => Contains(source, s, comparisonType)
                        ) != default(string);
        }
    }
}
