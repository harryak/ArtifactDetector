
using ArbitraryArtifactDetector.EnvironmentalDetector.Models;
using ArbitraryArtifactDetector.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.EnvironmentalDetector
{
    class InstalledProgramsDetector : IEnvironmentalDetector
    {
        const string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        private VADStopwatch Stopwatch { get; set; }
        private ILogger Logger { get; set; }

        public InstalledProgramsDetector(ILogger logger, VADStopwatch stopwatch = null)
        {
            Logger = logger;
            Stopwatch = stopwatch;
        }

        public IDictionary<int, InstalledProgram> GetInstalledPrograms()
        {
            if (Stopwatch != null)
            {
                Stopwatch.Restart();
            }

            IDictionary<int, InstalledProgram> programs = new Dictionary<int, InstalledProgram>();
            GetInstalledProgramsFromRegistry(RegistryView.Registry32, ref programs);
            GetInstalledProgramsFromRegistry(RegistryView.Registry64, ref programs);

            if (Stopwatch != null)
            {
                Stopwatch.Stop("get_installed_programs");
                Logger.LogDebug("Got installed programs in {0} ms.", Stopwatch.ElapsedMilliseconds);
            }

            return programs;
        }

        private int GetInstalledProgramsFromRegistry(RegistryView registryView, ref IDictionary<int, InstalledProgram> programs)
        {
            int count = 0;

            using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView).OpenSubKey(registry_key))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                    {
                        if (IsProgramVisible(subkey))
                        {
                            programs.Add(programs.Count, new InstalledProgram(subkey));
                        }
                    }
                }
            }

            return count;
        }

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
                && (systemComponent == null || (int) systemComponent == 0);
        }
    }
}
