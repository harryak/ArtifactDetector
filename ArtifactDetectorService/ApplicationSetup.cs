using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
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

            WorkingDirectory = GetExecutingDirectory();
            ApplicationGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(
                typeof(GuidAttribute), false).GetValue(0)).Value.ToString();
# if DEBUG
            // Setup display of images if being in debug mode.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif
            Logger.LogDebug("Setup complete");
        }

        ~ApplicationSetup()
        {
            NLog.LogManager.Shutdown();
        }

        /// <summary>
        /// Factory for loggers.
        /// </summary>
        public ILoggerFactory LoggerFactoryInstance { get; private set; }

        /// <summary>
        /// The working directory.
        /// </summary>
        public DirectoryInfo WorkingDirectory { get; }

        /// <summary>
        /// Application UID to identify global associated resources like mutexes.
        /// </summary>
        public string ApplicationGuid { get; }

        /// <summary>
        /// Logger instance for this setup.
        /// </summary>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Retrieve singleton instance of setup.
        /// </summary>
        /// <returns>The instance of ApplicationSetup.</returns>
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
            if (LoggerFactoryInstance == null) { SetupLogging(); }
            return LoggerFactoryInstance.CreateLogger(categoryName);
        }

        /// <summary>
        /// Setup the loggerFactory.
        /// </summary>
        /// <returns></returns>
        private void SetupLogging()
        {
            var eventLogSettings = new EventLogSettings()
            {
                SourceName = "ITS.APE ArtifactDetector Service"
            };
            // Set event log filtering to "information".
            eventLogSettings.Filter = (_, logLevel) => logLevel >= LogLevel.Debug;

            // Add factory with NLog and Windows EventLog.
            LoggerFactoryInstance = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(new NLogLoggerProvider())
                    .AddEventLog(eventLogSettings);
            });
        }
    }
}
