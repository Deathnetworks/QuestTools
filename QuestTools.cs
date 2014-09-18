using System;
using System.Diagnostics;
using System.Linq;
using QuestTools.Helpers;
using QuestTools.ProfileTags;
using QuestTools.ProfileTags.Complex;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Game;
using Zeta.Game.Internals;

namespace QuestTools
{
    public class QuestTools
    {
        public static Version PluginVersion = new Version(2, 1, 32);

        private static int _skipEventDuration = -1;
        private static readonly Stopwatch SkipEventTimer = new Stopwatch();

        public static bool EnableDebugLogging { get { return QuestToolsSettings.Instance.DebugEnabled; } }

        /// <summary>
        /// Starts the Random event timer
        /// </summary>
        /// <param name="min">Random time minimum</param>
        /// <param name="max">Random time maximum</param>
        /// <returns>If the timer was started</returns>
        private static void SetStartEventTimer(int min = 900, int max = 2200)
        {
            if (SkipEventTimer.IsRunning)
                return;

            _skipEventDuration = new Random().Next(min, max);
            SkipEventTimer.Start();
        }

        /// <summary>
        /// Resets the Event Timer
        /// </summary>
        /// <returns>If timer was succesfully reset</returns>
        private static void StopEventTimer()
        {
            if (!SkipEventTimer.IsRunning)
                return;
            SkipEventTimer.Reset();
            _skipEventDuration = -1;
        }

        internal static ulong LastGameId = 0;

        internal static void Pulse()
        {
            try
            {
                if (!Player.IsPlayerValid())
                    return;

                ChangeMonitor.CheckForChanges();

                PositionCache.RecordPosition();

                // Mark Dungeon Explorer nodes as Visited if combat pulls us into it
                if (ProfileManager.CurrentProfileBehavior != null)
                {
                    Type profileBehaviorType = ProfileManager.CurrentProfileBehavior.GetType();
                    if (profileBehaviorType == typeof(ExploreDungeonTag))
                    {
                        ExploreDungeonTag exploreDungeonTag = (ExploreDungeonTag)ProfileManager.CurrentProfileBehavior;
                        exploreDungeonTag.MarkNearbyNodesVisited();
                    }
                }
                LoadOnceTag.RecordLoadOnceProfile();

                RiftTrial.PulseRiftTrial();
                CheckGamesPerHourStop();
                SkipCutScene();
                AdvanceConversation();
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        public static class RiftTrial
        {
            public static bool InProgress;
            public static int CurrentWave;

            private static bool _countedWave;
            private static bool _lastCheckBelowThreshold;
            private static bool _finished;
            private static bool _isAborting;

            public static void PulseRiftTrial()
            {
                int maxWave = QuestToolsSettings.Instance.TrialRiftMaxLevel;

                if (!ZetaDia.IsInGame || !ZetaDia.Actors.Me.IsValid || !ZetaDia.ActInfo.IsValid)
                    return;

                var quest = ZetaDia.ActInfo.ActiveQuests.FirstOrDefault(q => q.QuestSNO == 405695);

                if (quest == null || ZetaDia.IsInTown || ZetaDia.WorldInfo.SNOId != 405684 || !QuestToolsSettings.Instance.EnableTrialRiftMaxLevel)
                {
                    InProgress = false;
                    CurrentWave = 0;
                    _countedWave = false;
                    _lastCheckBelowThreshold = false;
                    _finished = false;

                    if (_isAborting)
                    {
                        Logger.Log("Re-Enabling Combat");
                        TrinityApi.SetProperty("CombatBase", "IsCombatAllowed", true);
                        _isAborting = false;
                    }

                    return;
                }

                if (quest.State == QuestState.InProgress && quest.QuestStep != 9)
                {
                    if (!InProgress)
                        InProgress = true;

                    if (!_countedWave && quest.QuestMeter >= 0.95f)
                    {
                        _countedWave = true;
                        CurrentWave = CurrentWave + 1;
                        Logger.Log("Starting Wave: {0}", CurrentWave);
                    }

                    if (_lastCheckBelowThreshold && quest.QuestMeter >= 0.95)
                    {
                        _countedWave = false;
                    }

                    _lastCheckBelowThreshold = quest.QuestMeter < 0.95f && quest.QuestMeter >= 0;
                }

                if (CurrentWave == maxWave && !_isAborting)
                {
                    Logger.Log("Reached Wave {0} Disabling Combat", maxWave);
                    TrinityApi.SetProperty("CombatBase", "IsCombatAllowed", false);

                    BrainBehavior.ForceTownrun("Abort Trial", true);

                    _isAborting = true;
                }


            }

        }

        private static void CheckGamesPerHourStop()
        {
            ulong currentGameId = ZetaDia.Service.CurrentGameId.FactoryId;
            bool gameIdMatch = currentGameId == LastGameId;
            if (!gameIdMatch)
            {
                LastGameId = currentGameId;
            }

            if (BotEvents.GameCount > 90 && DateTime.UtcNow.Subtract(BotEvents.LastBotStart).TotalSeconds > 60 && GameStats.Instance.GamesPerHour > 90 && !gameIdMatch)
            {
                BotMain.Stop(false, string.Format("[QuestTools] Forcing bot stop - high rate of games/hour detected: {0} Games/hour", GameStats.Instance.GamesPerHour));
            }

        }

        private static void SkipCutScene()
        {
            if (!ZetaDia.IsPlayingCutscene || !QuestToolsSettings.Instance.SkipCutScenes)
                return;

            if (!SkipEventTimer.IsRunning)
            {
                SetStartEventTimer(250, 750);
                Logger.Debug("Waiting {0:0}ms to skip Cutscene", _skipEventDuration);
            }
            else if (SkipEventTimer.ElapsedMilliseconds > _skipEventDuration)
            {
                Logger.Debug("Skipping Cutscene");
                ZetaDia.Me.SkipCutscene();
                StopEventTimer();
            }
        }

        private static void AdvanceConversation()
        {
            if (!ZetaDia.Me.IsInConversation)
                return;

            if (SkipEventTimer.IsRunning)
            {
                SetStartEventTimer(500, 1100);
                Logger.Debug("Waiting {0:0}ms before Advancing conversation");
            }
            else if (SkipEventTimer.ElapsedMilliseconds > _skipEventDuration)
            {
                Logger.Debug("Advancing Conversation");
                ZetaDia.Me.AdvanceConversation();
                StopEventTimer();
            }
        }


    }
}
