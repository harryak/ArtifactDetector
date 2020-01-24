using ArbitraryArtifactDetector.Service;
using ArbitraryArtifactDetector.Viewer;
using Emgu.CV;
using Microsoft.Extensions.Logging;
using System;
using System.ServiceProcess;
using System.Windows.Forms;

namespace ArbitraryArtifactDetector
{
    internal class Program
    {
        /// <summary>
        /// Setup for this run, holding arguments and other necessary objects.
        /// </summary>
        internal static Setup Setup { get; set; }

        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private static ILogger Logger { get; set; }

        [STAThread]
        private static int Main(string[] args)
        {
            // 1. Initalize program setup with command line arguments.
            try
            {
                Setup = new Setup();
            }
            catch (SetupError)
            {
                return -1;
            }

            Logger = Setup.GetLogger("Main");

            Logger.LogInformation("Creating service now.");

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new DetectorService(Setup)
                {
                    ServiceName = "ITS.APE Detector Service"
                }
            };

            Logger.LogInformation("Starting service DetectorService.");
            ServiceBase.Run(ServicesToRun);
            Logger.LogInformation("Service started.");

#if DEBUG
            // Prepare debug window output.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            /*Mat screenshot = null;
            // Show the results in a window.
            if (screenshot != null)
                Application.Run(new ImageViewer(screenshot));*/
#endif

            return 0;
        }
    }
}