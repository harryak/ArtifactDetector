using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            ArtifactConfiguration config = JsonConvert.DeserializeObject<ArtifactConfiguration>(@"
    {
        detectors: {0: 'OpenWindowDetector;none;none', 1: 'VisualFeatureDetector;none;none'},
        artifact_name: 'test',
        'detection_interval': 5000,
        runtime_information: {
            artifact_name: 'yeeee',
            process_names: 'test1|test2',
            reference_images: 'C:\\Users\\Felix\\source\\repos\\recipes\\01_Jibberish-Mittel\\screenshot'
        }
    }
");
            /*ServiceBase[] ServicesToRun;
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
*/
            return 0;
        }
    }
}