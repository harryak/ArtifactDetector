using System.Collections;
using System.ComponentModel;
using System.ServiceProcess;

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

        protected override void OnCommitted(IDictionary savedState)
        {
            base.OnCommitted(savedState);

            new ServiceController(serviceInstaller1.ServiceName).Start();
        }
    }
}
