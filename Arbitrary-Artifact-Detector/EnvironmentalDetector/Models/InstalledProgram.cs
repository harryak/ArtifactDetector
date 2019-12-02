/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using Microsoft.Win32;

namespace ArbitraryArtifactDetector.EnvironmentalDetector.Models
{
    class InstalledProgram
    {
        public string Name { get; }
        public string ReleaseType { get; }
        public int SystemComponent { get; }
        public string ParentName { get; }

        public InstalledProgram(string name, string releaseType, int systemComponent, string parentName)
        {
            Name = name;
            ReleaseType = releaseType;
            SystemComponent = systemComponent;
            ParentName = parentName;
        }

        public InstalledProgram(RegistryKey registryEntry)
        {
            Name = (string) registryEntry.GetValue("DisplayName");
            ReleaseType = (string) registryEntry.GetValue("ReleaseType");
            SystemComponent = (int) registryEntry.GetValue("SystemComponent", 0);
            ParentName = (string) registryEntry.GetValue("ParentDisplayName");
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
