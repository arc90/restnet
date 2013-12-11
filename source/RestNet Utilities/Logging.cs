using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using log4net.Core;
using System.Reflection;

namespace RestNet
{
    public static class Logging
    {
        private static readonly ILogger logger = GetLogger();

        private static ILogger GetLogger()
        {
            if (log4net.LogManager.GetAllRepositories().Length == 0 || log4net.LogManager.GetAllRepositories()[0].Configured == false)
                log4net.Config.XmlConfigurator.Configure();
            return LoggerManager.GetLogger(Assembly.GetCallingAssembly(), "RestNet");
        }

        private static void Log(string message, Level level, Exception ex)
        {
            logger.Log(typeof(Logging), level, message, ex);
        }

        private static void LogFormat(string format, Level level, Exception ex, params object[] args)
        {
            if (logger.IsEnabledFor(level))
            {
                Log(string.Format(format, args), level, ex);
            }
        }

        public static void Debug(string message)
        {
            Log(message, Level.Debug, null);
        }

        public static void Debug(string message, Exception ex)
        {
            Log(message, Level.Debug, ex);
        }

        public static void DebugFormat(string format, params object[] args)
        {
            LogFormat(format, Level.Debug, null, args);
        }

        public static void Info(string message)
        {
            Log(message, Level.Info, null);
        }

        public static void Info(string message, Exception ex)
        {
            Log(message, Level.Info, ex);
        }

        public static void InfoFormat(string format, params object[] args)
        {
            LogFormat(format, Level.Info, null, args);
        }

        public static void Warn(string message)
        {
            Log(message, Level.Warn, null);
        }

        public static void Warn(string message, Exception ex)
        {
            Log(message, Level.Warn, ex);
        }

        public static void WarnFormat(string format, params object[] args)
        {
            LogFormat(format, Level.Warn, null, args);
        }
        
        public static void Error(string message)
        {
            Log(message, Level.Error, null);
        }

        public static void Error(string message, Exception ex)
        {
            Log(message, Level.Error, ex);
        }

        public static void ErrorFormat(string format, params object[] args)
        {
            LogFormat(format, Level.Error, null, args);
        }
        
        public static void Fatal(string message)
        {
            Log(message, Level.Fatal, null);
        }

        public static void Fatal(string message, Exception ex)
        {
            Log(message, Level.Fatal, ex);
        }

        public static void FatalFormat(string format, params object[] args)
        {
            LogFormat(format, Level.Fatal, null, args);
        }

    }
}
