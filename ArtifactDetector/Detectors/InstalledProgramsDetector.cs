using System;
using System.Collections.Generic;
using System.Linq;
using ItsApe.ArtifactDetector.Models;
using Microsoft.Win32;

namespace ItsApe.ArtifactDetector.Detectors
{
    internal class InstalledProgramsDetector : BaseDetector, IDetector
    {
        private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            // Stopwatch for evaluation.
            StartStopwatch();

            // Check whether we have enough data to detect the artifact.
            if (runtimeInformation.PossibleProgramNames.Count < 1)
            {
                throw new ArgumentException("No program names given to look for.");
            }

            var possibleProgramNames = runtimeInformation.PossibleProgramNames;

            if (IsProgramInstalledInRegistry(RegistryView.Registry32, ref possibleProgramNames)
                || IsProgramInstalledInRegistry(RegistryView.Registry64, ref possibleProgramNames))
            {
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        /// <summary>
        /// Given a registry view this function checks if any entry in the given list of possible program names is substring of an installed program.
        /// </summary>
        /// <param name="registryView">The registry view, e.g. RegistryView.Registry32 or RegistryView.Registry64.</param>
        /// <param name="possibleProgramNames">List of substrings the program to find might contain.</param>
        /// <returns>True if any string of the possible program names is substring of a visible, installed program.</returns>
        private bool IsProgramInstalledInRegistry(RegistryView registryView, ref IList<string> possibleProgramNames)
        {
            using (var key = Microsoft.Win32.RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView).OpenSubKey(RegistryKey))
            {
                string currentProgramName;
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    using (var subkey = key.OpenSubKey(subkey_name))
                    {
                        if (IsProgramVisible(subkey))
                        {
                            currentProgramName = (string) subkey.GetValue("DisplayName");
                            if (possibleProgramNames.FirstOrDefault(s => currentProgramName.Contains(s)) != default(string))
                            {
                                return true;
                            }
                        }
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
    }
}
