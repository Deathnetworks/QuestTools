using System.Collections.Generic;
using System.Diagnostics;

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

            string msg = string.Format("[{0}] " + string.Format(message, args), type.Name);

            if (_lastLogMessage == msg)
                return;

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

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Info(msg);
        }


        public static void LogError(string message)
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

        public static void LogError(string message, params object[] args)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format("[{0}] " + string.Format(message, args), type.Name);

            if (_lastLogMessage == msg)
                return;

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
            if (!QuestToolsSettings.Instance.DebugEnabled)
                return;
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format("[{0}] " + string.Format(message, args), type.Name);

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Info(msg);
        }
        /// <summary>
        /// Log Verbose
        /// </summary>
        /// <param name="message"></param>
        public static void Verbose(string message)
        {

            if (!QuestToolsSettings.Instance.DebugEnabled)
                return;
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            string msg = string.Format("[{0}] " + message, type.Name);

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Info(msg);
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

            if (_lastLogMessage == msg)
                return;

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

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Debug(msg);
        }

    }
}
