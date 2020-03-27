using System.ComponentModel;

namespace ItsApe.ArtifactDetector
{
    /// <summary>
    /// Installs the detector service.
    /// </summary>
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
