using System;
using System.Linq;

namespace ItsApe.ArtifactDetector.Helpers
{
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
        /// Checks the source string for substring value
        /// </summary>
        /// <param name="source">Haystack string.</param>
        /// <param name="value">Possible substring, needle.</param>
        /// <param name="comparisonType">E.g. InvariantCultureIgnoreCase.</param>
        /// <returns>True if the needle was found in the haystack.</returns>
        public static bool Contains(this string source, string value, StringComparison comparisonType)
        {
            return source?.IndexOf(value, comparisonType) >= 0;
        }
    }
}
