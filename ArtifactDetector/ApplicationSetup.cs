using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ItsApe.ArtifactDetector.DebugUtilities;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace ItsApe.ArtifactDetector
{
    /// <summary>
    /// Singleton class holding global information about the application.
    /// </summary>
    internal class ApplicationSetup
    {
        private static ApplicationSetup _instance;

        /// <summary>
        /// This class handles the parsing of the configuration files, plus the setup of the working environment.
        /// </summary>
        private ApplicationSetup()
        {
            Logger = GetLogger("Setup");

            // Parse the command line.
            Logger.LogDebug("Getting the configuration.");

            ShouldCache = ApplicationConfiguration.Cache;
            ShouldEvaluate = ApplicationConfiguration.Evaluate;

            WorkingDirectory = GetExecutingDirectory();

            // Determine if we use a stopwatch in this run.
            if (ApplicationConfiguration.Evaluate)
            {
                // Get stopwatch for evaluation.
                Stopwatch = DetectorStopwatch.GetInstance();
            }

# if DEBUG
            // Setup display of images if being in debug mode.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif
        }

        /// <summary>
        /// Factory for loggers.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; private set; }

        /// <summary>
        /// Flag for determining whether the program should cache its data for next runs.
        /// </summary>
        public bool ShouldCache { get; } = false;

        /// <summary>
        /// Flag for determining whether this run should be evaluated.
        /// </summary>
        public bool ShouldEvaluate { get; } = false;

        /// <summary>
        /// Stopwatch for evaluation.
        /// </summary>
        public DetectorStopwatch Stopwatch { get; set; } = null;

        /// <summary>
        /// The working directory.
        /// </summary>
        public DirectoryInfo WorkingDirectory { get; }

        /// <summary>
        /// Logger instance for this setup.
        /// </summary>
        private ILogger Logger { get; set; }

        public static ApplicationSetup GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ApplicationSetup();
            }

            return _instance;
        }

        /// <summary>
        /// Get the directory this app is executed in.
        /// </summary>
        /// <returns>Directory information.</returns>
        public DirectoryInfo GetExecutingDirectory()
        {
            if (WorkingDirectory != null)
            {
                return WorkingDirectory;
            }
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).Directory;
        }

        /// <summary>
        /// Get a new logger for the supplied category name.
        /// </summary>
        /// <param name="categoryName">Name for the logging.</param>
        /// <returns>A new ILogger instance for the category name.</returns>
        public ILogger GetLogger(string categoryName)
        {
            if (LoggerFactory == null) { SetupLogging(); }
            return LoggerFactory.CreateLogger(categoryName);
        }

        /// <summary>
        /// Setup the loggerFactory.
        /// </summary>
        /// <returns></returns>
        private void SetupLogging()
        {
            LoggerFactory = new LoggerFactory();
            LoggerFactory.AddProvider(new NLogLoggerProvider());
        }
    }
}
