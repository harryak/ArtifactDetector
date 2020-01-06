using System;
using System.Linq;

namespace ArbitraryArtifactDetector.Helper
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
    }
}
