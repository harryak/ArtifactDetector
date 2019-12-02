/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArbitraryArtifactDetector.EnvironmentalDetector.Models;
using ArbitraryArtifactDetector.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.EnvironmentalDetector
{
    class InstalledProgramsDetector : BaseEnvironmentalDetector, IEnvironmentalDetector
    {
        const string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        public InstalledProgramsDetector(ILogger logger, VADStopwatch stopwatch = null) : base(logger, stopwatch) { }

        public IDictionary<int, InstalledProgram> GetInstalledPrograms()
        {
            StartStopwatch();

            IDictionary<int, InstalledProgram> programs = new Dictionary<int, InstalledProgram>();

            GetInstalledProgramsFromRegistry(RegistryView.Registry32, ref programs);
            GetInstalledProgramsFromRegistry(RegistryView.Registry64, ref programs);

            StopStopwatch("Got installed programs in {0} ms.");

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

        public override bool FindArtifact(Setup setup)
        {
            throw new NotImplementedException();
        }
    }
}
