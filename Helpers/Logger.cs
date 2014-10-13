using System.Collections.Generic;
using System.Diagnostics;
using log4net.Core;
using Zeta.Bot.Settings;
using Zeta.Common;

namespace QuestTools
{
    public static class Logger
    {
        private static readonly log4net.ILog Logging = Zeta.Common.Logger.GetLoggerInstanceForType();

        private static string _lastLogMessage = "";

        /// <summary>
        /// Log Normal
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Log(string message, params object[] args)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = "[QuestTools][" + type.Name + "] " + string.Format(message, args);

            //if (_lastLogMessage == msg)
            //    return;

            _lastLogMessage = msg;
            Logging.Info(msg);
        }
        /// <summary>
        /// Log Normal
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format("[{0}] " + message, type.Name);

            //if (_lastLogMessage == msg)
            //    return;

            _lastLogMessage = msg;
            Logging.Info(msg);
        }

        /// <summary>
        /// Log without the Plugin Identifier
        /// </summary>
        public static void RawLog(string message)
        {
            var frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format(message, type.Name);

            _lastLogMessage = msg;
            Logging.Info(msg);
        }

        /// <summary>
        /// Log without the Plugin Identifier
        /// </summary>
        public static void RawLog(string message, params object[] args)
        {
            var frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format(message, args);

            _lastLogMessage = msg;
            Logging.Info(msg);
        }

        /// <summary>
        /// Log Warning
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Warn(string message, params object[] args)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = "[QuestTools][" + type.Name + "] " + string.Format(message, args);

            //if (_lastLogMessage == msg)
            //    return;

            _lastLogMessage = msg;
            Logging.Warn(msg);
        }
        /// <summary>
        /// Log Warning
        /// </summary>
        /// <param name="message"></param>
        public static void Warn(string message)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format("[{0}] " + message, type.Name);

            //if (_lastLogMessage == msg)
            //    return;

            _lastLogMessage = msg;
            Logging.Warn(msg);
        }

        public static void Error(string message)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format("[{0}] " + message, type.Name);

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Error(msg);
        }

        public static void Error(string message, params object[] args)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format("[{0}] " + string.Format(message, args), type.Name);

            //if (_lastLogMessage == msg)
            //    return;

            _lastLogMessage = msg;
            Logging.Error(msg);
        }

        /// <summary>
        /// Log Verbose
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Verbose(string message, params object[] args)
        {
            if (!QuestToolsSettings.Instance.DebugEnabled || GlobalSettings.Instance.ActualLogLevel != Level.Verbose)
                return;

            var frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format("[{0}] " + string.Format(message, args), type.Name);

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Debug(msg);
        }

        /// <summary>
        /// Log Verbose
        /// </summary>
        /// <param name="message"></param>
        public static void Verbose(string message)
        {
            if (!QuestToolsSettings.Instance.DebugEnabled || GlobalSettings.Instance.ActualLogLevel != Level.Verbose)
                return;

            var frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format("[{0}] " + message, type.Name);

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Debug(msg);
        }

        /// <summary>
        /// Log Debug
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Debug(string message, params object[] args)
        {
            if (!QuestToolsSettings.Instance.DebugEnabled)
                return;
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format("[{0}] " + string.Format(message, args), type.Name);

            //if (_lastLogMessage == msg)
            //    return;

            _lastLogMessage = msg;
            Logging.Debug(msg);
        }
        /// <summary>
        /// Log Debug
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            if (!QuestToolsSettings.Instance.DebugEnabled)
                return;
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format("[{0}] " + message, type.Name);

            //if (_lastLogMessage == msg)
            //    return;

            _lastLogMessage = msg;
            Logging.Debug(msg);
        }

    }
}
