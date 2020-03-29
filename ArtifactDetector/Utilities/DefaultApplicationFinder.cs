using System;
using System.IO;
using Microsoft.Win32;

namespace ItsApe.ArtifactDetector.Utilities
{
    /// <summary>
    /// Helper to find default application executables.
    /// </summary>
    internal class DefaultApplicationFinder
    {
        /// <summary>
        /// Static string containing the file suffix for executables.
        /// </summary>
        private const string ExeSuffix = ".exe";

        /// <summary>
        /// Registry key containing browser user choice.
        /// </summary>
        private const string KeyBrowserUserChoice = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";

        /// <summary>
        /// Registry key containing mailto user choice.
        /// </summary>
        private const string KeyMailtoUserChoice = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\mailto\UserChoice";

        /// <summary>
        /// Returns the information on the executable file of the default browser for the current user.
        /// </summary>
        /// <returns>File information object for executable.</returns>
        public static FileInfo GetDefaultBrowserExecutable()
        {
            return GetDefaultApplicationExecutable(GetDefaultBrowserName());
        }

        /// <summary>
        /// Returns the information on the executable file of the default mail reader for the current user.
        /// </summary>
        /// <returns>File information object for executable.</returns>
        public static FileInfo GetDefaultMailReaderExecutable()
        {
            return GetDefaultApplicationExecutable(GetDefaultMailReaderName());
        }

        /// <summary>
        /// Gets the executable of any application name previously retrieved from the registry.
        /// </summary>
        /// <param name="applicationName">Application name previously retrieved from the registry</param>
        /// <returns>File information object for the application's executable.</returns>
        private static FileInfo GetDefaultApplicationExecutable(string applicationName)
        {
            string path = applicationName + @"\shell\open\command";
            FileInfo applicationPath = null;
            using (var pathKey = Registry.ClassesRoot.OpenSubKey(path))
            {
                if (pathKey == null)
                {
                    return null;
                }

                // Trim parameters.
                try
                {
                    path = pathKey.GetValue(null).ToString().ToLower().Replace("\"", "");
                    if (!path.EndsWith(ExeSuffix))
                    {
                        path = path.Substring(0, path.LastIndexOf(ExeSuffix, StringComparison.Ordinal) + ExeSuffix.Length);
                        applicationPath = new FileInfo(path);
                    }
                }
                catch
                {
                    // Assume the registry value is set incorrectly.
                }
            }

            return applicationPath;
        }

        /// <summary>
        /// Get the application name stored for the user choice registry key given.
        /// </summary>
        /// <param name="userChoice">Registry key path to a user choice.</param>
        /// <returns>Name of the application in the registry.</returns>
        private static string GetDefaultApplicationName(string userChoice)
        {
            string applicationName = "";
            using (var userChoiceKey = Registry.CurrentUser.OpenSubKey(userChoice))
            {
                if (userChoiceKey == null)
                {
                    return "";
                }

                object progIdValue = userChoiceKey.GetValue("Progid");
                if (progIdValue == null)
                {
                    return "";
                }
                applicationName = progIdValue.ToString();
            }

            return applicationName;
        }

        /// <summary>
        /// Get the application name stored for the user choice of the browser.
        /// </summary>
        /// <returns>Name of the application in the registry.</returns>
        private static string GetDefaultBrowserName()
        {
            return GetDefaultApplicationName(KeyBrowserUserChoice);
        }

        /// <summary>
        /// Get the application name stored for the user choice of the mailto client.
        /// </summary>
        /// <returns>Name of the application in the registry.</returns>
        private static string GetDefaultMailReaderName()
        {
            return GetDefaultApplicationName(KeyMailtoUserChoice);
        }
    }
}
