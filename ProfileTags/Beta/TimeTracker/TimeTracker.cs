using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestTools.Helpers;
using Zeta.Bot;

namespace QuestTools.ProfileTags.Beta
{
    /// <summary>
    /// Keeps track of multiple timers and persists their stats accross bot sessions
    /// </summary>
    public static class TimeTracker
    {
        public static List<Timing> Timings = new List<Timing>();
        private static string _filePath = Path.Combine(FileManager.LoggingPath, String.Format("TimeTracker.csv"));
        internal static DateTime LastLoad { get; set; }
        internal static DateTime LastSave { get; set; }
        private static bool Initialized { get; set; }

        static TimeTracker()
        {
            if (Initialized)
                return;
            
            WireUp();
            Load();
            Initialized = true;            
        }

        /// <summary>
        /// Start listening to demonbuddy events.
        /// </summary>
        internal static void WireUp()
        {
            BotMain.OnStart += PersistentTiming_OnStart;
            BotMain.OnStop += PersistentTiming_OnStop;
            GameEvents.OnGameChanged += PersistentTiming_OnGameChanged;
        }

        /// <summary>
        /// Stop listening to demonbuddy events.
        /// </summary>
        internal static void UnWire()
        {
            BotMain.OnStart -= PersistentTiming_OnStart;
            BotMain.OnStop -= PersistentTiming_OnStop;
            GameEvents.OnGameChanged -= PersistentTiming_OnGameChanged;
        }

        /// <summary>
        /// When the game is stopped using the START button on DemonBuddy
        /// </summary>
        private static void PersistentTiming_OnStart(IBot bot)
        {
            Load();
        }

        /// <summary>
        /// When the game is started via the STOP button on DemonBuddy
        /// </summary>
        private static void PersistentTiming_OnStop(IBot bot)
        {
            Save();
            Reset();
        }

        /// <summary>
        /// Handle when the game is 'reset' - (leaving game and starting a new one)
        /// </summary>
        private static void PersistentTiming_OnGameChanged(object sender, EventArgs e)
        {
            Logger.Log("Game Changed");

            // Mark any partially finished timers from last game as failed
            Timings.ForEach(t =>
            {
                if (t.IsDirty)
                {
                    t.FailedCount++;
                }
            });

            Save();
            
            Timings.Clear();
        }

        /// <summary>
        /// Clear the timings collection
        /// </summary>
        public static void Reset()
        {
            Timings = new List<Timing>();
        }

        /// <summary>
        /// Start a timer
        /// </summary>
        public static void Start(Timing timing)
        {
            var existingTimer = Timings.Find(t => t.Name == timing.Name);
            if (existingTimer == null)
            {
                timing.Start();
                timing.IsDirty = true;
                Timings.Add(timing);
                Logger.Debug("Starting Timer (New) '{0}' Group:{1} {2} ({3})", timing.Name, timing.Group, timing.QuestName, timing.QuestId);
            }
            else
            {
                existingTimer.Start();
                existingTimer.IsDirty = true;
                Logger.Debug("Starting Timer (Known) '{0}' Group:{1} {2} ({3})", timing.Name, timing.Group, timing.QuestName, timing.QuestId);

            }
        }

        /// <summary>
        /// Stop a timer
        /// </summary>
        public static bool StopTimer(string timerName)
        {
            var found = false;
            Logger.Debug("Stopping Timer: {0}", timerName);
            Timings.ForEach(t =>
            {
                if (t.Name == timerName && t.IsRunning)
                {
                    found = true;
                    t.Update();
                    t.PrintSimple();
                    t.Stop();
                }
            });
            return found;
        }

        /// <summary>
        /// Stop all timers that are part of a group
        /// </summary>
        public static bool StopGroup(string groupName)
        {
            var found = false;
            Logger.Debug("Stopping Group: {0}", groupName);
            Timings.ForEach(t =>
            {
                if (t.Group == groupName && t.IsRunning)
                {
                    found = true;
                    t.Update();
                    t.PrintSimple();
                    t.Stop();
                }
            });
            return found;
        }

        /// <summary>
        /// Stop all timers
        /// </summary>
        public static bool StopAll()
        {
            var found = false;
            Logger.Debug("Stopping All");
            Timings.ForEach(t =>
            {
                if (t.IsRunning)
                {
                    found = true;
                    t.Update();
                    t.PrintSimple();
                    t.Stop();
                }
            });
            return found;
        }

        /// <summary>
        /// Load timing data from file
        /// </summary>
        private static void Load()
        {
            Logger.Debug(">> Loading Timings");
            var output = new List<Timing>();
            try
            {
                if (File.Exists(_filePath))
                {
                    foreach (var line in File.ReadAllLines(_filePath).Skip(1))
                    {
                        var tokens = line.Split(',');
                        var t = new Timing
                        {
                            Name = tokens[0],
                            QuestId = tokens[1].ChangeType<int>(),
                            QuestName = tokens[2],
                            QuestIsBounty = tokens[3].ChangeType<Boolean>(),
                            MinTimeSeconds = tokens[5].ChangeType<int>(),
                            MaxTimeSeconds = tokens[6].ChangeType<int>(),
                            TimesTimed = tokens[7].ChangeType<int>(),
                            TotalTimeSeconds = tokens[8].ChangeType<int>(),
                            Group = tokens[9],
                            FailedCount = tokens[10].ChangeType<int>(),
                        };
                        t.Print("Loaded: ");
                        output.Add(t);
                    }
                    LastLoad = DateTime.UtcNow;
                }
                Timings = output;
            }
            catch (Exception ex)
            {
                Logger.Log("Load Exception: {0}", ex);
            }

        }

        /// <summary>
        /// Save timing data to a file
        /// </summary>
        public static bool Save()
        {
            Logger.Debug(">> Saving Timings");
            var saved = false;

            try
            {
                if (File.Exists(_filePath))
                {
                    //Logger.Log("File Exists, Deleting");
                    File.Delete(_filePath);
                }

                using (var w = new StreamWriter(_filePath, true))
                {
                    var line = string.Empty;
                    var format = "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}\r\n";

                    var headerline = String.Format(format,
                        "Name",
                        "QuestId",
                        "QuestName",
                        "QuestIsBounty",
                        "TimeAverageSeconds",
                        "MinTimeSeconds",
                        "MaxTimeSeconds",
                        "TimesTimed",
                        "TotalTimeSeconds",
                        "Group",
                        "FailedCount"
                    );

                    w.Write(headerline);

                    Timings.ForEach(t =>
                    {

                        line = String.Format(format,
                            t.Name,
                            t.QuestId,
                            t.QuestName,
                            t.QuestIsBounty,
                            t.TimeAverageSeconds,
                            t.MinTimeSeconds,
                            t.MaxTimeSeconds,
                            t.TimesTimed,
                            t.TotalTimeSeconds,
                            t.Group,
                            t.FailedCount
                        );
                        t.Print("Saving: ");
                        t.IsDirty = false;
                        w.Write(line);
                    });
                }
                saved = true;
                LastSave = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Logger.Log("Exception Saving Timer File: {0}", ex);
            }
            return saved;
        }
    }

}
