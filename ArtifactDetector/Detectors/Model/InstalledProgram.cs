using Microsoft.Win32;

namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Data class to store information about an installed program.
    /// </summary>
    internal class InstalledProgram
    {
        /// <summary>
        /// Instantiate via classic values.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="releaseType"></param>
        /// <param name="systemComponent"></param>
        /// <param name="parentName"></param>
        public InstalledProgram(string name, string releaseType, int systemComponent, string parentName)
        {
            Name = name;
            ReleaseType = releaseType;
            SystemComponent = systemComponent;
            ParentName = parentName;
        }

        /// <summary>
        /// Instantiate via registry key.
        /// </summary>
        /// <param name="registryEntry"></param>
        public InstalledProgram(RegistryKey registryEntry)
        {
            Name = (string)registryEntry.GetValue("DisplayName");
            ReleaseType = (string)registryEntry.GetValue("ReleaseType");
            SystemComponent = (int)registryEntry.GetValue("SystemComponent", 0);
            ParentName = (string)registryEntry.GetValue("ParentDisplayName");
        }

        /// <summary>
        /// Name of the program.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Parent's program name (if set)
        /// </summary>
        public string ParentName { get; }

        /// <summary>
        /// Release type of program.
        /// </summary>
        public string ReleaseType { get; }

        /// <summary>
        /// Flag whether program is a system component.
        /// </summary>
        public int SystemComponent { get; }

        /// <summary>
        /// Debug method.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
