using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestTools.Helpers
{
    public static class FileManager
    {

        /// <summary>
        /// Gets the Logging path.
        /// </summary>
        public static void DeleteLastLine(string filepath)
        {
            List<string> lines = File.ReadAllLines(filepath).ToList();
            File.WriteAllLines(filepath, lines.GetRange(0, lines.Count - 1));
        }

        /// <summary>
        /// Gets the Logging path.
        /// </summary>
        /// <value>The path to use for logging</value>
        public static string LoggingPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_LoggingPath))
                {
                    _LoggingPath = Path.Combine(DemonBuddyPath, "TrinityLogs");
                    CreateDirectory(_LoggingPath);
                }
                return _LoggingPath;
            }
        }
        private static string _LoggingPath;

        /// <summary>
        /// Gets the DemonBuddy path.
        /// </summary>
        /// <value>The demon buddy path.</value>
        public static string DemonBuddyPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_DemonBuddyPath))
                    _DemonBuddyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                return _DemonBuddyPath;
            }
        }
        private static string _DemonBuddyPath;

        /// <summary>
        /// Creates the directory structure.
        /// </summary>
        /// <param name="path">The path.</param>
        private static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    CreateDirectory(Path.GetDirectoryName(path));
                }
                Directory.CreateDirectory(path);
            }
        }

    }
}
