using System;
using System.Collections.Generic;
using System.IO;
using ItsApe.ArtifactDetector.Helpers;
using ItsApe.ArtifactDetector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ItsApe.ArtifactDetector.Detectors
{
    internal class InstalledProgramDetector : BaseDetector, IDetector
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
        /// Detect installed programs by the info given.
        /// </summary>
        /// <param name="runtimeInformation">Data object with possible program names set.</param>
        /// <returns>A DetectorResponse based on the success of detection.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation)
        {
            if (!IsScreenActive(ref runtimeInformation))
            {
                Logger.LogInformation("Not detecting, screen is locked.");
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
            }
            Logger.LogInformation("Detecting installed programs now.");

            // Check whether we have enough data to detect the artifact.
            if (runtimeInformation.PossibleProgramSubstrings.Count < 1)
            {
                Logger.LogWarning("No possible program names given for detector. Could not find matching installed programs.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            InitializeDetection(ref runtimeInformation);
            AnalyzeRegistryView(RegistryView.Default, ref runtimeInformation);

            if (runtimeInformation.CountInstalledPrograms > 0)
            {
                Logger.LogInformation("Found {0} matching installed programs.", runtimeInformation.CountInstalledPrograms);
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }
            
            Logger.LogInformation("Found no matching installed programs.");
            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        /// <summary>
        /// Add the FileInfo of all *.exe files in the given directory path.
        /// </summary>
        /// <param name="installDirectoryPath">Path in which to look for *.exe files.</param>
        private void AddProgramExecutables(string installDirectoryPath, ref ArtifactRuntimeInformation runtimeInformation)
        {
            var installDirectory = new DirectoryInfo(installDirectoryPath);

            foreach (var file in installDirectory.EnumerateFiles("*.exe"))
            {
                if (!runtimeInformation.ProgramExecutables.Contains(file.FullName))
                {
                    runtimeInformation.ProgramExecutables.Add(file.FullName);
                }
            }
        }

        /// <summary>
        /// Loop through subkeys of provided key and find out if a program is installed.
        /// </summary>
        /// <param name="key">Key to loop through.</param>
        private void AnalyzeRegistrySubkey(RegistryKey key, ref ArtifactRuntimeInformation runtimeInformation)
        {
            foreach (string subkeyName in key.GetSubKeyNames())
            {
                using (var subkey = key.OpenSubKey(subkeyName))
                {
                    if (SubkeyMatchesConstraints(subkey, ref runtimeInformation))
                    {
                        runtimeInformation.CountInstalledPrograms++;
                        AddProgramExecutables((string)subkey.GetValue("InstallLocation"), ref runtimeInformation);
                    }
                }
            }
        }

        /// <summary>
        /// Given a registry view this function checks if any entry in the given list of possible program names is substring of an installed program.
        /// It checks the local machine as well as the current user.
        /// </summary>
        /// <param name="registryView">The registry view, e.g. RegistryView.Registry32 or RegistryView.Registry64.</param>
        private void AnalyzeRegistryView(RegistryView registryView, ref ArtifactRuntimeInformation runtimeInformation)
        {
            foreach (var installationsKeyPathsEntry in installationsKeyPaths)
            {
                using (var subkey = RegistryKey.OpenBaseKey(installationsKeyPathsEntry.Item1, registryView).OpenSubKey(installationsKeyPathsEntry.Item2))
                {
                    AnalyzeRegistrySubkey(subkey, ref runtimeInformation);
                }
            }
        }

        /// <summary>
        /// Initialize (or reset) the detection for FindArtifact.
        /// </summary>
        /// <param name="runtimeInformation">Reference to object to initialize from.</param>
        public void InitializeDetection(ref ArtifactRuntimeInformation runtimeInformation)
        {
            runtimeInformation.CountInstalledPrograms = 0;
        }

        /// <summary>
        /// Check whether a subkey is an installed application and its name matches.
        /// </summary>
        /// <param name="subkey">Registry key to check</param>
        /// <returns>True if the display name matches.</returns>
        private bool SubkeyMatchesConstraints(RegistryKey subkey, ref ArtifactRuntimeInformation runtimeInformation)
        {
            string displayName = (string)subkey.GetValue("DisplayName", "");
            int systemComponent = (int)subkey.GetValue("SystemComponent", 0);

            return displayName != "" && systemComponent == 0
                    && displayName.ContainsAny(runtimeInformation.PossibleProgramSubstrings);
        }
    }
}
