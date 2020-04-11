using System;
using System.Collections.Generic;
using ItsApe.ArtifactDetector.Helpers;
using ItsApe.ArtifactDetector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ItsApe.ArtifactDetector.Detectors
{
    internal class InstalledProgramsDetector : BaseDetector, IDetector
    {
        private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        private IList<string> PossibleProgramNames { get; set; }

        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            // Stopwatch for evaluation.
            StartStopwatch();

            // Check whether we have enough data to detect the artifact.
            if (runtimeInformation.PossibleProgramNames.Count < 1)
            {
                StopStopwatch("Got all installed programs in {0}ms.");
                Logger.LogWarning("No possible program names given for detector. Could not find matching installed programs.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            PossibleProgramNames = runtimeInformation.PossibleProgramNames;

            if (IsProgramInstalledInRegistry(RegistryView.Registry32)
                || IsProgramInstalledInRegistry(RegistryView.Registry64))
            {
                StopStopwatch("Got all installed programs in {0}ms.");
                Logger.LogInformation("Found no matching open windows.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            StopStopwatch("Got all installed programs in {0}ms.");
            Logger.LogInformation("Found a matching installed programs.");
            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        /// <summary>
        /// Given a registry view this function checks if any entry in the given list of possible program names is substring of an installed program.
        /// </summary>
        /// <param name="registryView">The registry view, e.g. RegistryView.Registry32 or RegistryView.Registry64.</param>
        /// <param name="possibleProgramNames">List of substrings the program to find might contain.</param>
        /// <returns>True if any string of the possible program names is substring of a visible, installed program.</returns>
        private bool IsProgramInstalledInRegistry(RegistryView registryView)
        {
            using (var key = Microsoft.Win32.RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView).OpenSubKey(RegistryKey))
            {
                return IsProgramInstalledInSubkeys(key);
            }
        }

        /// <summary>
        /// Loop through subkeys of provided key and find out if a program is installed.
        /// </summary>
        /// <param name="key">Key to loop through.</param>
        /// <returns>True when the program is installed.</returns>
        private bool IsProgramInstalledInSubkeys(RegistryKey key)
        {
            foreach (string subkeyName in key.GetSubKeyNames())
            {
                using (var subkey = key.OpenSubKey(subkeyName))
                {
                    if (SubkeyMatchesPossibleTitles(subkey))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the given registry key has the necessary values filled in for a visible program.
        /// </summary>
        /// <param name="subkey">The registry key containing information about the program.</param>
        /// <returns>True if the program is visible.</returns>
        private bool IsProgramVisible(RegistryKey subkey)
        {
            var name = (string)subkey.GetValue("DisplayName");
            var releaseType = (string)subkey.GetValue("ReleaseType");
            var systemComponent = subkey.GetValue("SystemComponent");
            var parentName = (string)subkey.GetValue("ParentDisplayName");

            return
                !string.IsNullOrEmpty(name)
                && string.IsNullOrEmpty(releaseType)
                && string.IsNullOrEmpty(parentName)
                && (systemComponent == null || (int)systemComponent == 0);
        }

        /// <summary>
        /// Check whether a sukey matches the possible titles.
        /// </summary>
        /// <param name="subkey">Registry key to check</param>
        /// <returns>True if the display name matches.</returns>
        private bool SubkeyMatchesPossibleTitles(RegistryKey subkey)
        {
            if (IsProgramVisible(subkey))
            {
                string currentProgramName = (string)subkey.GetValue("DisplayName");
                if (currentProgramName.ContainsAny(PossibleProgramNames, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
