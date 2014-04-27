using System.Diagnostics;

namespace QuestTools
{
    public static class Logger
    {
        private static readonly log4net.ILog Logging = Zeta.Common.Logger.GetLoggerInstanceForType();
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

            Logging.InfoFormat("[{0}] " + string.Format(message, args), type.Name);
        }
        /// <summary>
        /// Log Normal
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            Log(message, string.Empty);
        }

        /// <summary>
        /// Log Verbose
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Verbose(string message, params object[] args)
        {
            if (QuestToolsSettings.Instance.DebugEnabled)
            {
                StackFrame frame = new StackFrame(1);
                var method = frame.GetMethod();
                var type = method.DeclaringType;

                Logging.InfoFormat("[{0}] " + string.Format(message, args), type.Name);
            }
        }
        /// <summary>
        /// Log Verbose
        /// </summary>
        /// <param name="message"></param>
        public static void Verbose(string message)
        {
            Verbose(message, string.Empty);
        }        
        
        /// <summary>
        /// Log Debug
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Debug(string message, params object[] args)
        {
            if (QuestToolsSettings.Instance.DebugEnabled)
            {
                StackFrame frame = new StackFrame(1);
                var method = frame.GetMethod();
                var type = method.DeclaringType;

                Logging.DebugFormat("[{0}] " + string.Format(message, args), type.Name);
            }
        }
        /// <summary>
        /// Log Debug
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            Debug(message, string.Empty);
        }

    }
}
