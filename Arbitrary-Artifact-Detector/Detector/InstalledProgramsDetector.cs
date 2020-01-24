using ArbitraryArtifactDetector.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Detector
{
    class InstalledProgramsDetector : BaseDetector, IDetector
    {
        const string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        public InstalledProgramsDetector(Setup setup) : base(setup) { }

        public IDictionary<int, InstalledProgram> GetInstalledPrograms()
        {
            StartStopwatch();

            IDictionary<int, InstalledProgram> programs = new Dictionary<int, InstalledProgram>();

            GetInstalledProgramsFromRegistry(RegistryView.Registry32, ref programs);
            GetInstalledProgramsFromRegistry(RegistryView.Registry64, ref programs);

            StopStopwatch("Got installed programs in {0}ms.");

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

        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            throw new NotImplementedException();
        }
    }
}
