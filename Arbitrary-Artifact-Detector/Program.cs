using ArbitraryArtifactDetector.Service;
using Microsoft.Extensions.Logging;
using System;
using System.ServiceProcess;
using System.Windows.Forms;

namespace ArbitraryArtifactDetector
{
    internal class Program
    {
        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private static ILogger Logger { get; set; }

        [STAThread]
        private static int Main(string[] args)
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

#if DEBUG
            // Prepare debug window output.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif

            return 0;
        }
    }
}