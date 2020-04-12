using System;
using System.Collections.Generic;
using System.IO;
using ItsApe.ArtifactDetector.Helpers;
using ItsApe.ArtifactDetector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ItsApe.ArtifactDetector.Detectors
{
    internal class InstalledProgramsDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Dictionary of hive and key path for installations lookup.
        /// </summary>
        private readonly IList<Tuple<RegistryHive, string>> installationsKeyPaths = new List<Tuple<RegistryHive, string>>()
        {
            new Tuple<RegistryHive, string>(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            new Tuple<RegistryHive, string>(RegistryHive.CurrentUser,  @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            new Tuple<RegistryHive, string>(RegistryHive.LocalMachine, @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall")
        };

        /// <summary>
        /// Internal counter.
        /// </summary>
        private int foundMatches = 0;

        /// <summary>
        /// The program names to check in the registry.
        /// </summary>
        private IList<string> PossibleProgramSubstrings { get; set; }

        /// <summary>
        /// Storage for matching programs' executables to store in the runtime information.
        /// </summary>
        private IList<string> ProgramExecutables { get; } = new List<string>();

        /// <summary>
        /// Detect installed programs by the info given.
        /// </summary>
        /// <param name="runtimeInformation">Data object with possible program names set.</param>
        /// <param name="previousResponse"></param>
        /// <returns>A DetectorResponse based on the success of detection.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            Logger.LogInformation("Detecting installed programs now.");

            // Stopwatch for evaluation.
            StartStopwatch();

            // Check whether we have enough data to detect the artifact.
            if (runtimeInformation.PossibleProgramSubstrings.Count < 1)
            {
                StopStopwatch("Got all installed programs in {0}ms.");
                Logger.LogWarning("No possible program names given for detector. Could not find matching installed programs.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            PossibleProgramSubstrings = runtimeInformation.PossibleProgramSubstrings;

            AnalyzeRegistryView(RegistryView.Default);

            if (foundMatches > 0)
            {
                runtimeInformation.ProgramExecutables = ProgramExecutables;

                StopStopwatch("Got all installed programs in {0}ms.");
                Logger.LogInformation("Found {0} matching installed programs.", foundMatches);
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            StopStopwatch("Got all installed programs in {0}ms.");
            Logger.LogInformation("Found no matching installed programs.");
            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        /// <summary>
        /// Add the FileInfo of all *.exe files in the given directory path.
        /// </summary>
        /// <param name="installDirectoryPath">Path in which to look for *.exe files.</param>
        private void AddProgramExecutables(string installDirectoryPath)
        {
            var installDirectory = new DirectoryInfo(installDirectoryPath);

            foreach (var file in installDirectory.EnumerateFiles("*.exe"))
            {
                if (!ProgramExecutables.Contains(file.FullName))
                {
                    ProgramExecutables.Add(file.FullName);
                }
            }
        }

        /// <summary>
        /// Loop through subkeys of provided key and find out if a program is installed.
        /// </summary>
        /// <param name="key">Key to loop through.</param>
        private void AnalyzeRegistrySubkey(RegistryKey key)
        {
            foreach (string subkeyName in key.GetSubKeyNames())
            {
                using (var subkey = key.OpenSubKey(subkeyName))
                {
                    if (SubkeyMatchesConstraints(subkey))
                    {
                        foundMatches++;
                        AddProgramExecutables((string)subkey.GetValue("InstallLocation"));
                    }
                }
            }
        }

        /// <summary>
        /// Given a registry view this function checks if any entry in the given list of possible program names is substring of an installed program.
        /// It checks the local machine as well as the current user.
        /// </summary>
        /// <param name="registryView">The registry view, e.g. RegistryView.Registry32 or RegistryView.Registry64.</param>
        private void AnalyzeRegistryView(RegistryView registryView)
        {
            foreach (var installationsKeyPathsEntry in installationsKeyPaths)
            {
                using (var subkey = RegistryKey.OpenBaseKey(installationsKeyPathsEntry.Item1, registryView).OpenSubKey(installationsKeyPathsEntry.Item2))
                {
                    AnalyzeRegistrySubkey(subkey);
                }
            }
        }

        /// <summary>
        /// Check whether a subkey is an installed application and its name matches.
        /// </summary>
        /// <param name="subkey">Registry key to check</param>
        /// <returns>True if the display name matches.</returns>
        private bool SubkeyMatchesConstraints(RegistryKey subkey)
        {
            string displayName = (string)subkey.GetValue("DisplayName", "");
            int systemComponent = (int)subkey.GetValue("SystemComponent", 0);

            return displayName != "" && systemComponent == 0
                    && displayName.ContainsAny(PossibleProgramSubstrings);
        }
    }
}
