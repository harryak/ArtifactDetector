using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ItsApe.ArtifactDetector.Utilities
{
    internal class LoginUserUtility
    {
        /// <summary>
        /// Find out the currently logged in user.
        /// </summary>
        /// <returns>Username of logged in user.</returns>
        public static string GetLoggedInUser()
        {
            string userInformation = GetLoggedInUserInformation();

            // Parse username that is marked active.
            var userRegex = new Regex(@"^[>|\s](\S+)\s+.*\s+A[ck]tiv[e]?", RegexOptions.Multiline);
            var userMatch = userRegex.Matches(userInformation);

            if (userMatch.Count > 0)
            {
                var data = userMatch[userMatch.Count - 1].Groups;
                return data[1].ToString();
            }

            return "";
        }

        /// <summary>
        /// Find out the currently logged in user's session ID.
        /// </summary>
        /// <returns>Session ID of logged in user.</returns>
        public static int GetUserSessionID()
        {
            string userInformation = GetLoggedInUserInformation();

            // Parse sesson id that is marked active
            var userRegex = new Regex(@"(\d+)\s+A[ck]tiv[e]*", RegexOptions.Multiline);
            var userMatch = userRegex.Matches(userInformation);
            if (userMatch.Count > 0)
            {
                var data = userMatch[userMatch.Count - 1].Groups;
                return int.Parse(data[1].Value);
            }

            return -1;
        }

        /// <summary>
        /// Get information from the OS about the currently logged in user.
        /// </summary>
        /// <returns>Information string for the logged in user.</returns>
        private static string GetLoggedInUserInformation()
        {
            // Prepare process to find out the user.
            string nsExecParams = "user";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo("query", nsExecParams)
                {
                    UseShellExecute = false,
                    ErrorDialog = false,
                    RedirectStandardOutput = true
                }
            };
            string queryResult = "";
            bool processStarted = process.Start();
            using (var nsAnswer = process.StandardOutput)
            {
                queryResult = nsAnswer.ReadToEnd();
                process.WaitForExit();
            }

            return queryResult;
        }
    }
}
