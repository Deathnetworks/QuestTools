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
        private static bool _loadFailed;

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
            Pulsator.OnPulse += PulsatorOnOnPulse;
            GameEvents.OnGameChanged += PersistentTiming_OnGameChanged;
        }

        private static DateTime _lastPulse = DateTime.MinValue;
        private static void PulsatorOnOnPulse(object sender, EventArgs eventArgs)
        {
            if (!QuestToolsSettings.Instance.DebugEnabled || DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < 30000 || !Timings.Any(t => t.IsRunning))
                return;

            _lastPulse = DateTime.UtcNow;
            Logger.Debug("----- Timings Total={0} Running={1} ChangedSinceLoad={2} -----", Timings.Count, Timings.Count(t => t.IsRunning), Timings.Count(t => t.IsDirty));
            Timings.ForEach(t => t.DebugPrint());
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
            // Mark any unfinished timers from last game as failed
            Timings.ForEach(t =>
            {
                if (t.IsRunning)
                {
                    if (QuestToolsSettings.Instance.DebugEnabled)
                        Logger.Debug("Found a timer still running '{0}', stopping it and marking as failed", t.Name);

                    t.FailedCount++;
                    t.Stop();
                }
            });

            Save();
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
                
                if (QuestToolsSettings.Instance.DebugEnabled)
                    Logger.Debug("Starting Timer (New) Name={0} Group={1}", timing.Name, timing.Group);
            }
            else
            {
                existingTimer.Start();
                existingTimer.IsDirty = true;

                if (QuestToolsSettings.Instance.DebugEnabled)
                    Logger.Debug("Starting Timer (Known) '{0}' Group:{1}", timing.Name, timing.Group);

            }
        }

        /// <summary>
        /// Stop a timer
        /// </summary>
        public static bool StopTimer(string timerName, bool objectiveFound)
        {
            var found = false;

            if (QuestToolsSettings.Instance.DebugEnabled)
                Logger.Debug("Stopping Timer Name={0}", timerName);

            Timings.ForEach(t =>
            {
                if (t.Name != timerName || !t.IsRunning) 
                    return;

                found = true;
                t.ObjectiveComplete = objectiveFound;
                t.DebugPrint("Pre-Update");
                t.Update();
                t.DebugPrint("Post-Update");
                t.PrintSimple();
                t.Stop();
            });
            return found;
        }

        /// <summary>
        /// Stop all timers that are part of a group
        /// </summary>
        public static bool StopGroup(string groupName, bool objectiveFound)
        {
            var found = false;
            
            if (QuestToolsSettings.Instance.DebugEnabled)
                Logger.Debug("Stopping Timers Group={0}", groupName);
            
            Timings.ForEach(t =>
            {
                if (t.Group != groupName || !t.IsRunning) 
                    return;

                found = true;
                t.ObjectiveComplete = objectiveFound;
                t.DebugPrint("Pre-Update");
                t.Update();
                t.DebugPrint("Post-Update");
                t.PrintSimple();
                t.Stop();
            });
            return found;
        }

        /// <summary>
        /// Stop all timers
        /// </summary>
        public static bool StopAll(bool objectiveFound)
        {
            var found = false;

            if (QuestToolsSettings.Instance.DebugEnabled)
                Logger.Debug("Stopping All");

            Timings.ForEach(t =>
            {
                if (!t.IsRunning) return;

                found = true;
                t.ObjectiveComplete = objectiveFound;
                t.DebugPrint("Pre-Update");
                t.Update();
                t.DebugPrint("Post-Update");
                t.PrintSimple();
                t.Stop();
            });
            return found;
        }

        private const string FileFieldFormat = "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}\r\n";

        private static readonly string FileHeaderLabels = String.Format(FileFieldFormat,
            "Name",
            "Group",
            "TimeAverageSeconds",
            "MinTimeSeconds",
            "MaxTimeSeconds",
            "TimesTimed",
            "TotalTimeSeconds",
            "FailedCount",
            "ObjectiveCount",
            "ObjectivePercent"
        );



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
                    var lines = File.ReadAllLines(_filePath);

                     // If file format doesnt match, ignore and abort, data will be lost on save.
                    if (lines.First() != FileHeaderLabels)
                    {
                        Logger.Warn("TimeTracker.csv data format doesn't match, all old timing data will be lost on save");
                        Reset();
                        return;
                    }

                    foreach (var line in lines.Skip(1))
                    {
                        var tokens = line.Split(',');
                        var t = new Timing
                        {
                            Name = tokens[0],
                            Group = tokens[1],
                            MinTimeSeconds = tokens[2].ChangeType<int>(),
                            MaxTimeSeconds = tokens[3].ChangeType<int>(),
                            TimesTimed = tokens[4].ChangeType<int>(),
                            TotalTimeSeconds = tokens[5].ChangeType<int>(),
                            FailedCount = tokens[6].ChangeType<int>(),
                            ObjectiveCount = tokens[7].ChangeType<int>(),
                            ObjectivePercent = tokens[8].ChangeType<double>(),
                        };
                        t.DebugPrint("Loaded: ");
                        output.Add(t);
                    }
                    LastLoad = DateTime.UtcNow;
                    _loadFailed = false;
                }
                Timings = output;
            }
            catch (Exception ex)
            {
                Logger.Log("Load Exception, data will not be saved this game: {0}", ex);
                _loadFailed = true;
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
                if (_loadFailed)
                    return false;

                if (File.Exists(_filePath))
                {
                    Logger.Debug("Timings File Exists at {0}, Overwriting!", _filePath);
                    File.Delete(_filePath);
                }

                using (var w = new StreamWriter(_filePath, true))
                {
                    w.Write(FileHeaderLabels);
                    Timings.ForEach(t =>
                    {
                        var line = String.Format(FileFieldFormat,
                            t.Name,
                            t.Group,
                            t.TimeAverageSeconds,
                            t.MinTimeSeconds,
                            t.MaxTimeSeconds,
                            t.TimesTimed,
                            t.TotalTimeSeconds,
                            t.FailedCount,  
                            t.ObjectiveCount,
                            t.ObjectivePercent
                        );
                        t.DebugPrint("Saving: ");
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
