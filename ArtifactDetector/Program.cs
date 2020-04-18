using System;
using System.ServiceProcess;
using System.Windows.Forms;
using Emgu.CV.UI;
using ItsApe.ArtifactDetector.Detectors;
using ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Services;
using ItsApe.ArtifactDetector.Utilities;

namespace ItsApe.ArtifactDetector
{
    /// <summary>
    /// Starting point of this program.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Starting point of the execution, just instantiates and runs the
        /// detector service.
        /// </summary>
        /// <param name="args">Command line arguments, not parsed atm.</param>
        [STAThread]
        private static void Main(string[] args)
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new DetectorService()
                {
                    ServiceName = "ITS.APE Detector Service"
                }
            };

            // Start service.
            ServiceBase.Run(ServicesToRun);
        }
    }
}
