using System;
using System.ServiceProcess;
using System.Windows.Forms;
using Emgu.CV.UI;
using ItsApe.ArtifactDetector.Detectors;
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
            /*ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new DetectorService()
                {
                    ServiceName = "ITS.APE Detector Service"
                }
            };

            // Start service.
            ServiceBase.Run(ServicesToRun);*/

            var detector = new CompoundDetector();
            detector.AddDetector(new OpenWindowDetector());
            detector.AddDetector(new TrayIconDetector());
            var info = new ArtifactRuntimeInformation();
            info.PossibleIconSubstrings.Add("Wi-fu 50");

            var response = detector.FindArtifact(ref info);

            if (response.ArtifactPresent == DetectorResponse.ArtifactPresence.Certain)
            {
                Application.Run(new ImageViewer(VisualCapturer.CaptureRegion(info.WindowsInformation[0].BoundingArea)));
            }
        }
    }
}
