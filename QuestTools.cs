using System;
using System.Collections.Generic;
using System.Diagnostics;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.Game;
using Zeta.Game.Internals;

namespace QuestTools
{

    public partial class QuestTools : IPlugin
    {
        public static Version PluginVersion = new Version(1, 5, 54);

        public Version Version
        {
            get
            {
                return PluginVersion;
            }
        }
        private static readonly log4net.ILog Log = Zeta.Common.Logger.GetLoggerInstanceForType();
        private static int worldId = 0;
        private static int levelAreaId = 0;
        private static int questId = 0;
        private static int questStepId = 0;
        // static int sceneId = 0;
        private static Zeta.Game.Act currentAct = Zeta.Game.Act.Invalid;
        private static int gameCounter = 0;
        private static string myName = "[QuestTools] ";
        private static Stopwatch timer = new Stopwatch();

        private static bool reloadProfileOnDeath = true;
        public static bool ReloadProfileOnDeath { get { return reloadProfileOnDeath; } set { reloadProfileOnDeath = value; } }

        private static bool enableDebugLogging = false;
        public static bool EnableDebugLogging { get { return enableDebugLogging; } set { enableDebugLogging = value; } }

        private static bool forceReloadProfile = false;
        private static bool somethingChanged = false;

        public static DateTime lastProfileReload = DateTime.MinValue;

        private ProfileBehavior currentBehavior = null;
        private DateTime lastBehaviorChange = DateTime.UtcNow;
        private DateTime LastBotStart = DateTime.MinValue;

        private static TimeSpan behaviorTimeout = new TimeSpan(0, 5, 0, 0);

        private static Dictionary<string, TimeSpan> behaviorTimeouts =
            new Dictionary<string, TimeSpan>
            {
                { "ExploreAreaTag", new TimeSpan(0, 15, 0, 0) },
                { "default", new TimeSpan(0,  3, 0, 0) },
            };

        private Stopwatch skipEventTimer = new Stopwatch();
        private int skipEventDuration = -1;
        private DateTime lastEvent = DateTime.UtcNow;

        /// <summary>
        /// Starts the Random event timer
        /// </summary>
        /// <param name="min">Random time minimum</param>
        /// <param name="max">Random time maximum</param>
        /// <returns>If the timer was started</returns>
        private bool SetStartEventTimer(int min = 900, int max = 2200)
        {
            if (skipEventTimer.IsRunning)
                return false;

            skipEventDuration = new Random().Next(min, max);
            skipEventTimer.Start();
            return true;
        }

        /// <summary>
        /// Resets the Event Timer
        /// </summary>
        /// <returns>If timer was succesfully reset</returns>
        private bool StopEventTimer()
        {
            if (skipEventTimer.IsRunning)
            {
                skipEventTimer.Reset();
                skipEventDuration = -1;
                return true;
            }
            return false;
        }

        internal static HashSet<Vector3> PositionCache = new HashSet<Vector3>();
        internal static Vector3 PositionCacheLastPosition = Vector3.Zero;
        internal static DateTime PositionCacheLastRecorded = DateTime.MinValue;
        internal static bool ForceGeneratePath = false;

        internal static DateTime LastPluginPulse = DateTime.MinValue;

        internal static ulong LastGameId = 0;

        public static double GetMillisecondsSincePulse()
        {
            return DateTime.UtcNow.Subtract(LastPluginPulse).TotalMilliseconds;
        }

        public void OnPulse()
        {
            try
            {
                if (QuestToolsSettings.Instance.DebugEnabled)
                {
                    CheckForChanges();
                }

                if (!timer.IsRunning)
                {
                    timer.Start();
                    return;
                }

                if (timer.ElapsedMilliseconds < 100)
                    return;

                if (GetMillisecondsSincePulse() > 500)
                {
                    ForceGeneratePath = true;
                }

                LastPluginPulse = DateTime.UtcNow;

                if (ZetaDia.Me == null || !ZetaDia.Me.IsValid || !ZetaDia.Service.IsValid || !ZetaDia.IsInGame || ZetaDia.IsLoadingWorld || ZetaDia.Me.IsDead || !ZetaDia.WorldInfo.IsValid || !ZetaDia.ActInfo.IsValid)
                {
                    return;
                }

                timer.Reset();

                ProfileBehaviorTimeout();

                if (ZetaDia.Me.IsDead && ReloadProfileOnDeath && QuestToolsSettings.Instance.ReloadProfileOnDeath)
                {
                    forceReloadProfile = true;
                }

                if (DateTime.UtcNow.Subtract(PositionCacheLastRecorded).TotalMilliseconds > 100 && PositionCacheLastPosition.Distance2D(ZetaDia.Me.Position) > 5f)
                {
                    PositionCache.Add(ZetaDia.Me.Position);
                }

                if (Zeta.Bot.CombatTargeting.Instance.FirstNpc != null || Zeta.Bot.CombatTargeting.Instance.FirstObject != null)
                {
                    ForceGeneratePath = true;
                }

                ulong thisGameId = ZetaDia.Service.CurrentGameId.FactoryId;
                bool gameIdMatch = thisGameId == LastGameId;
                if (!gameIdMatch)
                {
                    LastGameId = thisGameId;
                }

                if (gameCounter > 60 && DateTime.UtcNow.Subtract(LastBotStart).TotalSeconds > 30 && Zeta.Bot.GameStats.Instance.GamesPerHour > 60 && !gameIdMatch)
                {
                    Zeta.Bot.BotMain.Stop(false, string.Format("[QuestTools] Forcing bot stop - high rate of games/hour detected: {0} Games/hour", Zeta.Bot.GameStats.Instance.GamesPerHour));
                }

                if (forceReloadProfile)
                {
                    Zeta.Bot.ProfileManager.Load(Zeta.Bot.ProfileManager.CurrentProfile.Path);
                    Logger.Debug("Reloading profile {0} - {1}", Zeta.Bot.ProfileManager.CurrentProfile.Name, Zeta.Bot.ProfileManager.CurrentProfile.Path);
                    forceReloadProfile = false;
                }

                if (ZetaDia.IsPlayingCutscene)
                {
                    if (!skipEventTimer.IsRunning)
                    {
                        SetStartEventTimer(250, 750);
                        lastEvent = DateTime.UtcNow;
                        Logger.Debug("Waiting {0:0}ms to skip Cutscene", skipEventDuration);
                    }
                    else if (skipEventTimer.ElapsedMilliseconds > skipEventDuration)
                    {
                        Logger.Debug("Skipping Cutscene");
                        ZetaDia.Me.SkipCutscene();
                        StopEventTimer();
                    }

                }
                if (ZetaDia.Me.IsInConversation)
                {
                    if (skipEventTimer.IsRunning)
                    {
                        SetStartEventTimer(500, 1100);
                        lastEvent = DateTime.UtcNow;
                        Logger.Debug("Waiting {0:0}ms before Advancing conversation");
                    }
                    else if (skipEventTimer.ElapsedMilliseconds > skipEventDuration)
                    {
                        Logger.Debug("Advancing Conversation");
                        ZetaDia.Me.AdvanceConversation();
                        StopEventTimer();
                    }
                }


            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        /// <summary>
        /// Checks to make sure the current profile behavior hasn't exceeded the allocated timeout
        /// </summary>
        private void ProfileBehaviorTimeout()
        {
            if (currentBehavior == null)
            {
                currentBehavior = ProfileManager.CurrentProfileBehavior;
                lastBehaviorChange = DateTime.UtcNow;
            }
            else if (DateTime.UtcNow.Subtract(lastBehaviorChange) > behaviorTimeout && currentBehavior != ProfileManager.CurrentProfileBehavior)
            {
                Logger.Log("Behavior Timeout: {0} exceeded for Profile: {1} Behavior: {2}",
                    behaviorTimeout,
                    ProfileManager.CurrentProfile.Name,
                    currentBehavior
                    );

                currentBehavior = null;
                lastBehaviorChange = DateTime.UtcNow;
                ProfileManager.Load(Zeta.Bot.ProfileManager.CurrentProfile.Path);
            }
            else
            {
                currentBehavior = Zeta.Bot.ProfileManager.CurrentProfileBehavior;
                lastBehaviorChange = DateTime.UtcNow;
            }
        }

        private bool TimeoutExceededForCurrentBehavior()
        {
            if (ProfileManager.CurrentProfileBehavior != currentBehavior)
                return false;

            Type T = ProfileManager.CurrentProfileBehavior.GetType();

            switch (T.ToString())
            {
                case "ExploreAreaTag":
                    return DateTime.UtcNow.Subtract(lastBehaviorChange) > behaviorTimeouts[T.ToString()];
                default:
                    return DateTime.UtcNow.Subtract(lastBehaviorChange) > behaviorTimeouts["default"];
            }
        }

        private static int cachedLevelAreaId = -1;
        private static DateTime lastUpdatedLevelAreaId = DateTime.MinValue;
        public static int LevelAreaId
        {
            get
            {
                if (cachedLevelAreaId == -1 || DateTime.UtcNow.Subtract(lastUpdatedLevelAreaId).TotalSeconds > 2)
                {
                    cachedLevelAreaId = ZetaDia.CurrentLevelAreaId;
                    lastUpdatedLevelAreaId = DateTime.UtcNow;
                    return cachedLevelAreaId;
                }
                return cachedLevelAreaId;
            }
        }

        private DateTime lastChangeCheckTime = DateTime.MinValue;
        private void CheckForChanges()
        {
            if (DateTime.UtcNow.Subtract(lastChangeCheckTime).TotalMilliseconds < 1000)
            {
                return;
            }
            lastChangeCheckTime = DateTime.UtcNow;

            if (!ZetaDia.IsInGame)
                return;

            if (!ZetaDia.Me.IsValid)
                return;

            if (ZetaDia.IsLoadingWorld)
                return;

            Act newAct = ZetaDia.CurrentAct;
            if (ZetaDia.ActInfo.IsValid && newAct != currentAct)
            {
                Logger.Verbose("Act changed from {1} to {2} ({3}) SnoId={4}", myName, currentAct.ToString(), newAct, (int)newAct, ZetaDia.CurrentActSNOId);
                currentAct = newAct;
                somethingChanged = true;
                PositionCache.Clear();
            }

            int newWorldId = ZetaDia.CurrentWorldId;
            if (ZetaDia.WorldInfo.IsValid && ZetaDia.CurrentWorldId != worldId)
            {
                string worldName = ZetaDia.WorldInfo.Name;
                Logger.Verbose("worldId changed from {1} to {2} - {3}", myName, worldId, newWorldId, worldName);
                worldId = newWorldId;
                somethingChanged = true;
                PositionCache.Clear();
            }

            if (ZetaDia.WorldInfo.IsValid && LevelAreaId != levelAreaId)
            {
                string levelAreaName = ZetaDia.SNO.LookupSNOName(SNOGroup.LevelArea, LevelAreaId);
                Logger.Verbose("levelAreaId changed from {1} to {2} - {3}", myName, levelAreaId, LevelAreaId, levelAreaName);
                levelAreaId = LevelAreaId;
                somethingChanged = true;
            }

            if (ZetaDia.CurrentQuest != null && ZetaDia.CurrentQuest.IsValid)
            {
                int newSno = ZetaDia.CurrentQuest.QuestSNO;
                if (newSno != questId)
                {
                    Logger.Verbose("questId changed from {1} to {2} - {3}", myName, questId, newSno, ZetaDia.CurrentQuest.Name);
                    questId = newSno;
                    somethingChanged = true;
                }

                int newStep = ZetaDia.CurrentQuest.StepId;
                if (newStep != questStepId)
                {
                    Logger.Verbose("questStepId changed from {1} to {2}", myName, questStepId, newStep);
                    questStepId = newStep;
                    somethingChanged = true;
                }
            }
            else if (ZetaDia.CurrentQuest == null)
            {
                Logger.Verbose("questId changed from {0} to NULL", questId);
                questId = -1;
                somethingChanged = true;
            }

            if (somethingChanged && ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld && ZetaDia.Me.Position != Vector3.Zero)
            {
                Logger.Verbose("Change(s) occured at Position {1} ", myName, GetProfileCoordinates(ZetaDia.Me.Position));
                somethingChanged = false;
            }
        }
        public static string GetProfileCoordinates(Vector3 position)
        {
            return string.Format("x=\"{0:0}\" y=\"{1:0}\" z=\"{2:0}\"", position.X, position.Y, position.Z);
        }
        public void OnEnabled()
        {
            Logger.Log("Plugin v{0} Enabled", Version);

            Zeta.Bot.GameEvents.OnPlayerDied += new EventHandler<EventArgs>(GameEvents_OnPlayerDied);

            BotMain.OnStart += BotMain_OnStart;

            TabUI.InstallTab();

        }
        void BotMain_OnStart(IBot bot)
        {
            LastBotStart = DateTime.UtcNow;
            QuestTools.PositionCache.Clear();
            ReloadProfile._lastReloadLoopQuestStep = "";
            ReloadProfile._questStepReloadLoops = 0;
        }


        public void OnDisabled()
        {
            currentAct = Act.Invalid;
            levelAreaId = 0;
            questId = 0;
            questStepId = 0;
            worldId = 0;
            somethingChanged = true;
            Zeta.Bot.GameEvents.OnPlayerDied -= GameEvents_OnPlayerDied;
            BotMain.OnStart -= BotMain_OnStart;

            TabUI.RemoveTab();
        }

        public static bool IsPlayerValid()
        {
            if (!ZetaDia.IsInGame)
                return false;
            if (ZetaDia.IsLoadingWorld)
                return false;
            if (ZetaDia.Me == null)
                return false;
            if (!ZetaDia.Me.IsValid)
                return false;
            if (ZetaDia.Me.HitpointsCurrent <= 0)
                return false;

            return true;
        }



        internal static string GetProfilePosition(Vector3 pos)
        {
            return string.Format("x=\"{0:0}\" y=\"{1:0}\" z=\"{2:0}\" ", pos.X, pos.Y, pos.Z);
        }
        internal static string GetSimplePosition(Vector3 pos)
        {
            return string.Format("{0:0}, {1:0}, {2:0}", pos.X, pos.Y, pos.Z);
        }
        internal static string SpacedConcat(params object[] args)
        {
            string output = "";

            foreach (object o in args)
            {
                output += o.ToString() + ", ";
            }

            return output;
        }

        void GameEvents_OnPlayerDied(object sender, EventArgs e)
        {
            if (ReloadProfileOnDeath && QuestToolsSettings.Instance.ReloadProfileOnDeath)
                forceReloadProfile = true;

            Logger.Log("Player died! Position={0} QuestId={1} StepId={2} WorldId={3}",
                ZetaDia.Me.Position, ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, ZetaDia.CurrentWorldId);
        }


        public void OnShutdown()
        {

        }

        public string Author
        {
            get { return "rrrix"; }
        }

        public string Description
        {
            get { return "Advanced Demonbuddy Profile Support"; }
        }

        public System.Windows.Window DisplayWindow
        {
            get { return Config.GetDisplayWindow(); }
        }

        public string Name
        {
            get { return "QuestTools"; }
        }

        public void OnInitialize()
        {
            LastJoinedGame = DateTime.MinValue;
            Zeta.Bot.GameEvents.OnGameJoined += GameEvents_OnGameJoined;
        }

        internal static DateTime LastJoinedGame { get; private set; }
        void GameEvents_OnGameJoined(object sender, EventArgs e)
        {
            LastJoinedGame = DateTime.UtcNow;
            Logger.Debug("LastJoinedGame is {0}", LastJoinedGame);
            gameCounter++;
        }

        public bool Equals(IPlugin other) { return (other.Name == Name) && (other.Version == Version); }
    }
}
