using ArbitraryArtifactDetector.Service;
using ArbitraryArtifactDetector.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.ServiceProcess;

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

            /*ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new DetectorService(Setup)
            };

            Logger.LogInformation("Starting service DetectorService.");
            ServiceBase.Run(ServicesToRun);
            Logger.LogInformation("Service started.");
            */

            return 0;
        }
    }
}