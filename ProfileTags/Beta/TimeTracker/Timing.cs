using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestTools.ProfileTags.Beta
{
    /// <summary>
    /// Timing Object, tracks a period of time
    /// </summary>
    public class Timing
    {
        public string Name = string.Empty;
        public int QuestId = 0;
        public string QuestName = string.Empty;
        public bool QuestIsBounty = false;
        public bool IsRunning = false;
        public bool IsStarted = false;
        public string Group = string.Empty;
        public TimeSpan Elapsed = TimeSpan.MinValue;
        public DateTime StartTime = DateTime.MinValue;
        public DateTime StopTime = DateTime.MinValue;
        public int TimesTimed = 0;
        public int TotalTimeSeconds = 0;
        public int MaxTimeSeconds = 0;
        public int MinTimeSeconds = 0;
        public int FailedCount = 0;
        public bool AllowResetStartTime = false;
        public bool IsDirty = false;

        public float TimeAverageSeconds
        {
            get
            {
                if (TimesTimed > 0)
                {
                    if (MinTimeSeconds == MaxTimeSeconds)
                    {
                        return MaxTimeSeconds;
                    }
                    return (float)TotalTimeSeconds / (float)TimesTimed;
                }
                return 0;
            }
        }


        public void Print()
        {
            Print(string.Empty);
        }

        /// <summary>
        /// Convert a number of seconds into a friendly time format for display
        /// </summary>
        public string FormatTime(int seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            if (seconds == 0) return "0";
            var format = t.Hours > 0 ? "{0:0}h " : string.Empty;
            format += t.Minutes > 0 ? "{1:0}m " : string.Empty;
            format += t.Seconds > 0 ? "{2:0}s" : string.Empty;
            return string.Format(format, t.Hours, t.Minutes, t.Seconds);
        }

        /// <summary>
        /// Start the timer
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            if (StartTime == DateTime.MinValue || AllowResetStartTime)
            {
                StartTime = DateTime.UtcNow;
            };
        }

        /// <summary>
        /// Update statistics for the timer
        /// </summary>
        public void Update()
        {
            Elapsed = DateTime.UtcNow.Subtract(this.StartTime);
            TimesTimed = TimesTimed + 1;
            TotalTimeSeconds += (int)Elapsed.TotalSeconds;
            MaxTimeSeconds = (int)Elapsed.TotalSeconds > MaxTimeSeconds ? (int)Elapsed.TotalSeconds : MaxTimeSeconds;
            MinTimeSeconds = MinTimeSeconds == 0 || (int)Elapsed.TotalSeconds < MinTimeSeconds ? (int)Elapsed.TotalSeconds : MinTimeSeconds;
        }

        /// <summary>
        /// Stop the timer
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            StartTime = DateTime.MinValue;
            StopTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Write the current state of this timer instance to the console
        /// </summary>
        public void Print(string message)
        {

            //var format = message + "Timer '{0}' Group:{11} {3} ({1}) took {10} seconds to complete (Max={5}, Min={6}, Avg={7} from {9} timings)";

            var format = (IsDirty)
                ? message + ">> Dirty Timer '{0}' Group:{11} {3} ({1}) {10}(Max={5}, Min={6}, Avg={7} from {9} timings)"
                : message + "Pass-Through '{0}' Group:{11} {3} ({1}) {10}(Max={5}, Min={6}, Avg={7} from {9} timings)";

            Logger.Debug(format,
                Name,
                QuestId,
                QuestIsBounty,
                QuestName,
                IsRunning,
                FormatTime(MaxTimeSeconds),
                FormatTime(MinTimeSeconds),
                FormatTime((int)TimeAverageSeconds),
                TotalTimeSeconds,
                TimesTimed,
                (Elapsed.TotalSeconds > 0) ? "took " + Elapsed.TotalSeconds + " seconds to complete " : string.Empty,
                Group,
                FailedCount

            );
        }

        public void PrintSimple(string message = "")
        {
            Logger.Warn(message + " {0} ({1}) took {2})", Name, Group, FormatTime(MinTimeSeconds));
        }

    }
}
